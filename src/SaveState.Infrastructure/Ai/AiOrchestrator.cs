using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Ai.Memory;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Ai.Context;
using SaveState.Infrastructure.Ai.Knowledge;
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
    private readonly SemanticKnowledgeClient _knowledgeClient;
    private readonly IShortTermMemory _memory;
    private readonly IWebSearchService _searchService;
    private readonly IKnowledgeBaseService _kbService;
    private long _cacheRequests;
    private long _cacheHits;

    public AiOrchestrator(
        IEnumerable<ILlmProvider> providers,
        ICacheService cache,
        IOptions<AiOptions> options,
        ILogger<AiOrchestrator> logger,
        IApplicationMetrics metrics,
        ICachePerformanceMonitor cacheMonitor,
        IConversationContextService contextService,
        SemanticKnowledgeClient knowledgeClient,
        IShortTermMemory memory,
        IWebSearchService searchService,
        IKnowledgeBaseService kbService)
    {
        _providers = providers;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
        _cacheMonitor = cacheMonitor;
        _contextService = contextService;
        _knowledgeClient = knowledgeClient;
        _memory = memory;
        _searchService = searchService;
        _kbService = kbService;
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

    public async Task<Result<string>> GenerateTextAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            var request = new AiRequest(
                Type: AiRequestType.Completion,
                Prompt: prompt,
                MaxTokens: 500,
                Temperature: 0.7f);

            var response = await ProcessRequestAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessful)
            {
                return Result.Success<string>(response.Content);
            }
            else
            {
                return Result.Failure<string>(response.Error ?? "AI generation failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate text for prompt: {Prompt}", prompt);
            return Result.Failure<string>($"Text generation failed: {ex.Message}");
        }
    }

    public async Task<Result<AiResponse>> ExecutePromptAsync(
        string sessionId,
        string prompt,
        CancellationToken ct = default)
    {
        try
        {
            var request = new AiRequest(
                Type: AiRequestType.Chat,
                Prompt: prompt,
                MaxTokens: 1000,
                Temperature: 0.7f);

            var response = await ProcessRequestWithContextAsync(sessionId, request, ct).ConfigureAwait(false);

            if (response.IsSuccessful)
            {
                return Result.Success<AiResponse>(response);
            }
            else
            {
                return Result.Failure<AiResponse>(response.Error ?? "AI execution failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute prompt for session {SessionId}: {Prompt}", sessionId, prompt);
            return Result.Failure<AiResponse>($"Prompt execution failed: {ex.Message}");
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

        var query = request.Prompt ?? request.Messages?.LastOrDefault()?.Content ?? "";

        // 1. Retrieve Contexts
        var relevantContext = await _knowledgeClient.GetRelevantContextAsync(query, ct);
        var webContext = await HandleWebSearchFallbackAsync(query, relevantContext, ct);

        var shortTermMemories = await _memory.SearchAsync(query, 3, ct);
        var stmContext = string.Join("\n", shortTermMemories.Select(m => m.Content));

        // 2. Build Message History
        var historyResult = await _contextService.GetHistoryAsync(sessionId, ct);
        var messagesWithHistory = BuildMessageHistory(request, historyResult.Value);

        // 3. Inject Context
        InjectContextMessages(messagesWithHistory, relevantContext, webContext, stmContext);

        // 4. Update Conversation History
        if (messagesWithHistory.Count > 0)
        {
            var lastUserMessage = messagesWithHistory.LastOrDefault(m => m.Role == "user");
            if (lastUserMessage != null)
            {
                await _contextService.AddMessageAsync(sessionId, lastUserMessage, ct);
            }
        }

        // 5. Process Request
        var contextualRequest = request with
        {
            Messages = messagesWithHistory,
            Type = AiRequestType.Chat,
            AllowCache = false
        };

        var response = await ProcessRequestAsync(contextualRequest, ct);

        // 6. Record Outcome
        if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
        {
            await _contextService.AddMessageAsync(sessionId, new ChatMessage("assistant", response.Content), ct);
            await _memory.StoreAsync(new MemoryEntry(
                Guid.NewGuid().ToString(),
                $"User asked: {query}\nAI responded: {response.Content}",
                DateTime.UtcNow,
                new[] { "conversation", sessionId }), ct);
        }

        return response;
    }

    private async Task<string?> HandleWebSearchFallbackAsync(string query, string relevantContext, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(relevantContext) && !query.Contains("search", StringComparison.OrdinalIgnoreCase) && !query.Contains("internet", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        _logger.LogInformation("Insufficient local knowledge or search requested for '{Query}'. Triggering web search.", query);
        var webContext = await _searchService.SearchAsync(query, ct);

        if (!string.IsNullOrEmpty(webContext))
        {
            var sanitizedQuery = Regex.Replace(query, @"[^a-zA-Z0-9_\-]", "_");
            if (sanitizedQuery.Length > 50) sanitizedQuery = sanitizedQuery.Substring(0, 50);

            var fileName = $"Search_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{sanitizedQuery}.md";
            var mdContent = $"# Web Search Result: {query}\n" +
                           $"**Date**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n" +
                           $"**Query**: {query}\n\n" +
                           $"## Summary\n{webContext}\n\n" +
                           $"---\n*This information was automatically retrieved from the internet and saved to the knowledge base.*";

            _ = _kbService.SaveToKnowledgeBaseAsync("internet-search", fileName, mdContent, CancellationToken.None);
        }

        return webContext;
    }

    private List<ChatMessage> BuildMessageHistory(AiRequest request, IReadOnlyList<ChatMessage>? history)
    {
        var messages = history?.ToList() ?? new List<ChatMessage>();

        if (request.Messages?.Count > 0)
        {
            messages.AddRange(request.Messages);
        }
        else if (!string.IsNullOrEmpty(request.Prompt))
        {
            // Avoid duplicates if the history already has it
            if (messages.Count == 0 || messages.Last().Content != request.Prompt)
            {
                messages.Add(new ChatMessage("user", request.Prompt));
            }
        }

        return messages;
    }

    private void InjectContextMessages(List<ChatMessage> messages, string? localContext, string? webContext, string? stmContext)
    {
        if (string.IsNullOrEmpty(localContext) && string.IsNullOrEmpty(webContext) && string.IsNullOrEmpty(stmContext))
        {
            return;
        }

        var contextMsg = "CONTEXT INFORMATION (RAG/BMAD/WEB):\n";
        if (!string.IsNullOrEmpty(localContext)) contextMsg += $"[Local Knowledge Base]: {localContext}\n";
        if (!string.IsNullOrEmpty(webContext)) contextMsg += $"[Recent Web Search]: {webContext}\n";
        if (!string.IsNullOrEmpty(stmContext)) contextMsg += $"[Short-term Memory]: {stmContext}\n";

        messages.Insert(0, new ChatMessage("system", contextMsg));
    }

    public async Task<bool> ClearConversationAsync(string sessionId, CancellationToken ct = default)
    {
        var result = await _contextService.ClearSessionAsync(sessionId, ct);
        return result.IsSuccess;
    }

    private static string GenerateCacheKey(AiRequest request)
        => $"ai:{request.Type}:{request.Model}:{request.Prompt?.GetHashCode() ?? request.Messages?.GetHashCode() ?? 0}";
}

