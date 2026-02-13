using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using System.Text.Json;

namespace SaveState.Infrastructure.Caching;

/// <summary>
/// Distributed cache service with Redis support for enterprise-grade caching.
/// PHASE 7: REQUIRED - Distributed Caching Layer
/// </summary>
public class DistributedCacheService : IDistributedCache
{
    private readonly IDistributedCache _innerCache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly Dictionary<string, CacheMetrics> _metrics = new();
    private readonly object _metricsLock = new();

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger)
    {
        _innerCache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a value from cache with automatic deserialization.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
#if DEBUG
            _logger.LogDebug("Cache GET: {Key}", key);
#endif

        try
        {
            var bytes = await _innerCache.GetAsync(key, token);
            if (bytes == null)
            {
                RecordMetric(key, false);
                return default;
            }

            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var value = JsonSerializer.Deserialize<T>(json);

            RecordMetric(key, true);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache GET failed for key: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Sets a value in cache with automatic serialization.
    /// </summary>
    public async Task SetAsync<T>(
        string key,
        T value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken token = default)
    {
#if DEBUG
            _logger.LogDebug("Cache SET: {Key}", key);
#endif

        try
        {
            var json = JsonSerializer.Serialize(value);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            options ??= new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };

            await _innerCache.SetAsync(key, bytes, options, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache SET failed for key: {Key}", key);
        }
    }

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
#if DEBUG
            _logger.LogDebug("Cache REMOVE: {Key}", key);
#endif

        try
        {
            await _innerCache.RemoveAsync(key, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache REMOVE failed for key: {Key}", key);
        }
    }

    /// <summary>
    /// Gets or creates a cached value.
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken token = default)
    {
        try
        {
            var cached = await GetAsync<T>(key, token);
#if DEBUG
            if (cached != null)
            {
                _logger.LogDebug("Cache HIT: {Key}", key);
                return cached;
            }

            _logger.LogDebug("Cache MISS: {Key}", key);
#endif


            var value = await factory();
            await SetAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
            }, token);

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrCreateAsync failed for key: {Key}", key);
            return await factory();
        }
    }

    /// <summary>
    /// Invalidates cache by pattern (Redis SCAN pattern).
    /// </summary>
    public async Task InvalidatePatternAsync(string pattern, CancellationToken token = default)
    {
        try
        {
            _logger.LogInformation("Invalidating cache pattern: {Pattern}", pattern);
            // In production, implement Redis SCAN or similar pattern matching
            // For now, log the intent
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pattern invalidation failed for: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// Warms cache with frequently accessed data.
    /// </summary>
    public async Task WarmCacheAsync<T>(
        Dictionary<string, T> preloadData,
        TimeSpan? expiration = null,
        CancellationToken token = default)
    {
        try
        {
            _logger.LogInformation("Warming cache with {Count} entries", preloadData.Count);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(24)
            };

            foreach (var kvp in preloadData)
            {
                await SetAsync(kvp.Key, kvp.Value, options, token);
            }

            _logger.LogInformation("Cache warmed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warming failed");
        }
    }

    /// <summary>
    /// Gets cache statistics and hit/miss ratios.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        lock (_metricsLock)
        {
            var totalHits = _metrics.Values.Sum(m => m.Hits);
            var totalMisses = _metrics.Values.Sum(m => m.Misses);
            var totalRequests = totalHits + totalMisses;
            var hitRatio = totalRequests > 0 ? (double)totalHits / totalRequests : 0;

            return new CacheStatistics(
                TotalKeys: _metrics.Count,
                TotalHits: totalHits,
                TotalMisses: totalMisses,
                HitRatio: hitRatio,
                TopKeys: _metrics
                    .OrderByDescending(m => m.Value.Hits)
                    .Take(10)
                    .Select(m => new KeyStatistic(m.Key, m.Value.Hits, m.Value.Misses))
                    .ToList());
        }
    }

    /// <summary>
    /// Clears all metrics.
    /// </summary>
    public void ClearMetrics()
    {
        lock (_metricsLock)
        {
            _metrics.Clear();
        }
    }

    private void RecordMetric(string key, bool isHit)
    {
        lock (_metricsLock)
        {
            if (!_metrics.TryGetValue(key, out var metric))
            {
                metric = new CacheMetrics(0, 0);
            }

            _metrics[key] = isHit
                ? new CacheMetrics(metric.Hits + 1, metric.Misses)
                : new CacheMetrics(metric.Hits, metric.Misses + 1);
        }
    }

    // IDistributedCache interface implementation
    async Task<byte[]?> IDistributedCache.GetAsync(string key, CancellationToken token)
    {
        return await _innerCache.GetAsync(key, token);
    }

    async Task IDistributedCache.SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token)
    {
        await _innerCache.SetAsync(key, value, options, token);
    }

    async Task IDistributedCache.RemoveAsync(string key, CancellationToken token)
    {
        await _innerCache.RemoveAsync(key, token);
    }

    // Synchronous methods required by IDistributedCache
    byte[]? IDistributedCache.Get(string key)
    {
        return _innerCache.Get(key);
    }

    void IDistributedCache.Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _innerCache.Set(key, value, options);
    }

    void IDistributedCache.Remove(string key)
    {
        _innerCache.Remove(key);
    }

    void IDistributedCache.Refresh(string key)
    {
        _innerCache.Refresh(key);
    }

    async Task IDistributedCache.RefreshAsync(string key, CancellationToken token)
    {
        await _innerCache.RefreshAsync(key, token);
    }
}

/// <summary>
/// Cache metrics for a single key.
/// </summary>
public record CacheMetrics(long Hits, long Misses);

/// <summary>
/// Statistics for a cached key.
/// </summary>
public record KeyStatistic(string Key, long Hits, long Misses);

/// <summary>
/// Overall cache statistics.
/// </summary>
public record CacheStatistics(
    int TotalKeys,
    long TotalHits,
    long TotalMisses,
    double HitRatio,
    List<KeyStatistic> TopKeys);
