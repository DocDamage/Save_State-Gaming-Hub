using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Configuration;

namespace SaveState.Infrastructure.Ai;

public class AiOrchestrator : IAiOrchestrator
{
    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiOrchestrator> _logger;
    private readonly AiOptions _options;
    private long _cacheRequests;
    private long _cacheHits;

    public AiOrchestrator(
        IEnumerable<ILlmProvider> providers,
        IMemoryCache cache,
        IOptions<AiOptions> options,
        ILogger<AiOrchestrator> logger)
    {
        _providers = providers;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiResponse> ProcessRequestAsync(AiRequest request, CancellationToken ct = default)
    {
        var cacheKey = GenerateCacheKey(request);

        if (request.AllowCache)
        {
            Interlocked.Increment(ref _cacheRequests);

            if (_cache.TryGetValue(cacheKey, out AiResponse? cached))
            {
                Interlocked.Increment(ref _cacheHits);
                var hitRate = (double)_cacheHits / _cacheRequests * 100;
                _logger.LogDebug("Cache hit for AI request (hit rate: {HitRate:F1}%)", hitRate);
                return cached!;
            }
        }

        var provider = SelectProvider(request.PreferredProvider);
        if (provider is null)
            return new AiResponse("", "", new TokenUsage(0, 0, 0), "", "", false, "No AI providers available");

        try
        {
            AiResponse response;

            if (request.Type == AiRequestType.Chat)
            {
                var chatResult = await provider.ChatAsync(
                    new ChatRequest(request.Messages!, request.Model ?? _options.DefaultModel, request.MaxTokens ?? 1000), ct).ConfigureAwait(false);

                if (chatResult.IsFailure)
                {
                    return new AiResponse("", "", new TokenUsage(0, 0, 0), "", provider.ProviderName, false, chatResult.Error);
                }

                response = new AiResponse(chatResult.Value!.Content, chatResult.Value.FinishReason, chatResult.Value.Usage, chatResult.Value.Model, provider.ProviderName);
            }
            else
            {
                var completionResult = await provider.CompleteAsync(
                    new CompletionRequest(request.Prompt!, request.Model ?? _options.DefaultModel, request.MaxTokens ?? 1000, request.Temperature ?? 0.7f), ct).ConfigureAwait(false);

                if (completionResult.IsFailure)
                {
                    return new AiResponse("", "", new TokenUsage(0, 0, 0), "", provider.ProviderName, false, completionResult.Error);
                }

                response = new AiResponse(completionResult.Value!.Text, completionResult.Value.FinishReason, completionResult.Value.Usage, completionResult.Value.Model, provider.ProviderName);
            }

            if (request.AllowCache)
            {
                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(_options.CacheTtlMinutes));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI request failed with {Provider}", provider.ProviderName);

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

    private static string GenerateCacheKey(AiRequest request)
        => $"ai:{request.Type}:{request.Model}:{request.Prompt?.GetHashCode() ?? request.Messages?.GetHashCode() ?? 0}";
}
