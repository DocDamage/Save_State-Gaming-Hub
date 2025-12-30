using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Ai.Context;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Core.Monitoring;

namespace SaveState.Infrastructure.Ai;

public class AiOrchestrator : IAiOrchestrator
{
    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly ICacheService _cache;
    private readonly ILogger<AiOrchestrator> _logger;
    private readonly AiOptions _options;
    private readonly IApplicationMetrics _metrics;
    private readonly ICachePerformanceMonitor _cacheMonitor;
    private readonly IConversationContextService _contextService;
    private long _cacheRequests;
    private long _cacheHits;

    public AiOrchestrator(
        IEnumerable<ILlmProvider> providers,
        ICacheService cache,
        IOptions<AiOptions> options,
        ILogger<AiOrchestrator> logger,
        IApplicationMetrics metrics,
        ICachePerformanceMonitor cacheMonitor,
        IConversationContextService contextService)
    {
        _providers = providers;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
        _cacheMonitor = cacheMonitor;
        _contextService = contextService;
    }

    public async Task<AiResponse> ProcessRequestAsync(AiRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startTime = DateTime.UtcNow;
        var cacheKey = GenerateCacheKey(request);

        if (request.AllowCache)
        {
            Interlocked.Increment(ref _cacheRequests);

            if (_cache.TryGetValue(cacheKey, out AiResponse? cached))
            {
                Interlocked.Increment(ref _cacheHits);
                var hitRate = (double)_cacheHits / _cacheRequests * 100;
                _logger.LogDebug("Cache hit for AI request (hit rate: {HitRate:F1}%)", hitRate);

                // Record cache hit metrics
                _cacheMonitor.RecordCacheHit("AiOrchestrator");
                _metrics.RecordAiRequest("Cache", "Hit", DateTime.UtcNow - startTime, true);

                return cached!;
            }
            else
            {
                // Record cache miss
                _cacheMonitor.RecordCacheMiss("AiOrchestrator");
            }
        }

        var provider = SelectProvider(request.PreferredProvider);
        if (provider is null)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordAiRequest("None", "NoProvider", duration, false);
            return new AiResponse("", "", new TokenUsage(0, 0, 0), "", "", false, "No AI providers available");
        }

        try
        {
            AiResponse response;

            if (request.Type == AiRequestType.Chat)
            {
                var chatResult = await provider.ChatAsync(
                    new ChatRequest(request.Messages!, request.Model ?? _options.DefaultModel, request.MaxTokens ?? 1000), ct).ConfigureAwait(false);

                if (chatResult.IsFailure)
                {
                    var duration = DateTime.UtcNow - startTime;
                    _metrics.RecordAiRequest(provider.ProviderName, "Chat", duration, false);
                    _metrics.RecordApiError(provider.ProviderName, chatResult.Error ?? "Unknown");
                    return new AiResponse("", "", new TokenUsage(0, 0, 0), "", provider.ProviderName, false, chatResult.Error);
                }

                response = new AiResponse(chatResult.Value!.Content, chatResult.Value.FinishReason, chatResult.Value.Usage, chatResult.Value.Model, provider.ProviderName);

                // Record token usage
                _metrics.RecordAiTokenUsage(provider.ProviderName, chatResult.Value.Usage.PromptTokens, chatResult.Value.Usage.CompletionTokens);
            }
            else
            {
                var completionResult = await provider.CompleteAsync(
                    new CompletionRequest(request.Prompt!, request.Model ?? _options.DefaultModel, request.MaxTokens ?? 1000, request.Temperature ?? 0.7f), ct).ConfigureAwait(false);

                if (completionResult.IsFailure)
                {
                    var duration = DateTime.UtcNow - startTime;
                    _metrics.RecordAiRequest(provider.ProviderName, "Completion", duration, false);
                    _metrics.RecordApiError(provider.ProviderName, completionResult.Error ?? "Unknown");
                    return new AiResponse("", "", new TokenUsage(0, 0, 0), "", provider.ProviderName, false, completionResult.Error);
                }

                response = new AiResponse(completionResult.Value!.Text, completionResult.Value.FinishReason, completionResult.Value.Usage, completionResult.Value.Model, provider.ProviderName);

                // Record token usage
                _metrics.RecordAiTokenUsage(provider.ProviderName, completionResult.Value.Usage.PromptTokens, completionResult.Value.Usage.CompletionTokens);
            }

            // Record successful AI request
            var totalDuration = DateTime.UtcNow - startTime;
            _metrics.RecordAiRequest(provider.ProviderName, request.Type.ToString(), totalDuration, true);

            if (request.AllowCache)
            {
                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(_options.CacheTtlMinutes));
            }

            return response;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "AI request failed with {Provider}", provider.ProviderName);

            // Record error metrics
            _metrics.RecordException("AiOrchestrator", ex.GetType().Name, ex.Message);
            _metrics.RecordAiRequest(provider.ProviderName, request.Type.ToString(), duration, false);

            if (_options.EnableFallback)
            {
                return await TryFallbackAsync(request, provider, ct).ConfigureAwait(false);
            }

            return new AiResponse("", "", new TokenUsage(0, 0, 0), "", provider.ProviderName, false, ex.Message);
        }
    }

    public IReadOnlyList<string> GetAvailableProviders()
        => _providers.Where(p => p.IsAvailable).Select(p => p.ProviderName).ToList();

    public Task<bool> IsProviderHealthyAsync(string providerName, CancellationToken ct)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName);
        return Task.FromResult(provider?.IsAvailable ?? false);
    }

    public (long Requests, long Hits, double HitRate) GetCacheStatistics()
    {
        var requests = Interlocked.Read(ref _cacheRequests);
        var hits = Interlocked.Read(ref _cacheHits);
        var hitRate = requests > 0 ? (double)hits / requests * 100 : 0;
        return (requests, hits, hitRate);
    }

    private ILlmProvider? SelectProvider(string? preferredProvider)
    {
        if (!string.IsNullOrEmpty(preferredProvider))
        {
            var preferred = _providers.FirstOrDefault(p =>
                p.ProviderName.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase) && p.IsAvailable);
            if (preferred is not null) return preferred;
        }

        return _providers.FirstOrDefault(p => p.IsAvailable);
    }

    private async Task<AiResponse> TryFallbackAsync(AiRequest request, ILlmProvider failedProvider, CancellationToken ct)
    {
        var fallback = _providers.FirstOrDefault(p => p != failedProvider && p.IsAvailable);
        if (fallback is null)
            return new AiResponse("", "", new TokenUsage(0, 0, 0), "", "", false, "All providers failed");

        _logger.LogInformation("Trying fallback provider {Provider}", fallback.ProviderName);
        return await ProcessRequestAsync(request with { PreferredProvider = fallback.ProviderName }, ct).ConfigureAwait(false);
    }

    public async Task<AiResponse> ProcessRequestWithContextAsync(
        string sessionId,
        AiRequest request,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(request);

        // Get conversation history
        var historyResult = await _contextService.GetHistoryAsync(sessionId, ct);

        List<ChatMessage> messagesWithHistory;

        if (historyResult.IsSuccess && historyResult.Value!.Count > 0)
        {
            messagesWithHistory = historyResult.Value.ToList();

            // Add current user message if this is a chat request
            if (request.Messages?.Count > 0)
            {
                messagesWithHistory.AddRange(request.Messages);
            }
            else if (!string.IsNullOrEmpty(request.Prompt))
            {
                messagesWithHistory.Add(new ChatMessage("user", request.Prompt));
            }
        }
        else
        {
            messagesWithHistory = request.Messages?.ToList() ?? new List<ChatMessage>();
            if (!string.IsNullOrEmpty(request.Prompt) && messagesWithHistory.Count == 0)
            {
                messagesWithHistory.Add(new ChatMessage("user", request.Prompt));
            }
        }

        // Store user message in context
        if (messagesWithHistory.Count > 0)
        {
            var lastUserMessage = messagesWithHistory.LastOrDefault(m => m.Role == "user");
            if (lastUserMessage != null)
            {
                await _contextService.AddMessageAsync(sessionId, lastUserMessage, ct);
            }
        }

        // Create modified request with full history
        var contextualRequest = request with
        {
            Messages = messagesWithHistory,
            Type = AiRequestType.Chat,
            AllowCache = false  // Don't cache contextual responses
        };

        // Process request
        var response = await ProcessRequestAsync(contextualRequest, ct);

        // Store AI response in context
        if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
        {
            await _contextService.AddMessageAsync(
                sessionId,
                new ChatMessage("assistant", response.Content),
                ct);
        }

        return response;
    }

    public async Task<bool> ClearConversationAsync(string sessionId, CancellationToken ct = default)
    {
        var result = await _contextService.ClearSessionAsync(sessionId, ct);
        return result.IsSuccess;
    }

    private static string GenerateCacheKey(AiRequest request)
        => $"ai:{request.Type}:{request.Model}:{request.Prompt?.GetHashCode() ?? request.Messages?.GetHashCode() ?? 0}";
}
