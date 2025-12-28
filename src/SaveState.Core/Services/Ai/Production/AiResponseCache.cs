using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.Production
{
    public interface IAiResponseCache
    {
        ProductionAiResponse? Get(string input, ProductionAiRequestContext? context);
        void Set(string input, ProductionAiRequestContext? context, ProductionAiResponse response);
        void Invalidate(string? pattern = null);
        Task StartCleanupAsync(CancellationToken ct);
    }

    public class AiResponseCache : IAiResponseCache
    {
        private readonly ConcurrentDictionary<string, (ProductionAiResponse Response, DateTime Expiry)> _cache = new();
        private readonly ProductionAiConfig _config;

        public AiResponseCache(ProductionAiConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public ProductionAiResponse? Get(string input, ProductionAiRequestContext? context)
        {
            var cacheKey = GenerateCacheKey(input, context);

            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                if (DateTime.UtcNow < cached.Expiry)
                {
                    // Clone the response to avoid mutation
                    return new ProductionAiResponse
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        Success = cached.Response.Success,
                        Content = cached.Response.Content,
                        Confidence = cached.Response.Confidence,
                        AgentUsed = cached.Response.AgentUsed,
                        IntentDetected = cached.Response.IntentDetected,
                        UsedCache = true,
                        Metadata = new ProductionAiResponseMetadata
                        {
                            TokensUsed = cached.Response.Metadata.TokensUsed,
                            ModelUsed = cached.Response.Metadata.ModelUsed,
                            Additional = cached.Response.Metadata.Additional
                        }
                    };
                }
                else
                {
                    _cache.TryRemove(cacheKey, out _);
                }
            }
            return null;
        }

        public void Set(string input, ProductionAiRequestContext? context, ProductionAiResponse response)
        {
            if (_cache.Count >= _config.MaxCacheSize)
            {
                // Evict oldest/earliest expiry
                var oldest = _cache.OrderBy(c => c.Value.Expiry).FirstOrDefault();
                if (oldest.Key != null)
                {
                    _cache.TryRemove(oldest.Key, out _);
                }
            }

            var cacheKey = GenerateCacheKey(input, context);
            _cache[cacheKey] = (response, DateTime.UtcNow.Add(_config.CacheDuration));
        }

        public void Invalidate(string? pattern = null)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                _cache.Clear();
            }
            else
            {
                var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
                foreach (var key in keysToRemove)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        public async Task StartCleanupAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), ct);

                    var now = DateTime.UtcNow;
                    var expiredKeys = _cache
                        .Where(kvp => kvp.Value.Expiry < now)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in expiredKeys)
                    {
                        _cache.TryRemove(key, out _);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* Ignore errors in background loop */ }
            }
        }

        private string GenerateCacheKey(string input, ProductionAiRequestContext? context)
        {
            var contextHash = context != null
                ? $"_{context.CurrentScene}_{context.InCombat}_{context.InDialogue}"
                : "";
            return $"{input.GetHashCode()}{contextHash}";
        }
    }
}
