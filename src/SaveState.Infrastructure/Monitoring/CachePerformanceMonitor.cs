using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Monitoring;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Monitoring;

/// <summary>
/// Monitors cache performance and provides detailed cache metrics.
/// Tracks hit ratios, eviction rates, and cache efficiency across different cache implementations.
/// </summary>
public class CachePerformanceMonitor : ICachePerformanceMonitor
{
    private readonly IApplicationMetrics _metrics;
    private readonly ILogger<CachePerformanceMonitor> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Timer _cacheTimer;
    private bool _disposed;

    // Cache statistics tracking
    private readonly ConcurrentDictionary<string, CacheStats> _cacheStatistics = new();

    public CachePerformanceMonitor(
        IApplicationMetrics metrics,
        ILogger<CachePerformanceMonitor> logger,
        ITimeProvider timeProvider)
    {
        _metrics = metrics;
        _logger = logger;
        _timeProvider = timeProvider;

        // Monitor cache performance every 5 minutes
        _cacheTimer = new Timer(AnalyzeCachePerformance, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

        _logger.LogInformation("Cache performance monitor initialized");
    }

    /// <summary>
    /// Records a cache hit for the specified cache.
    /// </summary>
    public void RecordCacheHit(string cacheName)
    {
        _metrics.RecordCacheHit(cacheName);
        UpdateCacheStats(cacheName, hit: true);
    }

    /// <summary>
    /// Records a cache miss for the specified cache.
    /// </summary>
    public void RecordCacheMiss(string cacheName)
    {
        _metrics.RecordCacheMiss(cacheName);
        UpdateCacheStats(cacheName, hit: false);
    }

    /// <summary>
    /// Records a cache eviction for the specified cache.
    /// </summary>
    public void RecordCacheEviction(string cacheName)
    {
        _metrics.RecordCacheEviction(cacheName);
        UpdateCacheStats(cacheName, eviction: true);
    }

    private void UpdateCacheStats(string cacheName, bool hit = false, bool eviction = false)
    {
        var stats = _cacheStatistics.GetOrAdd(cacheName, _ => new CacheStats());

        lock (stats)
        {
            if (hit)
            {
                stats.Hits++;
            }
            else if (!eviction)
            {
                stats.Misses++;
            }

            if (eviction)
            {
                stats.Evictions++;
            }

            stats.LastAccessTime = _timeProvider.UtcNow;
        }

        stats.LastAccessTime = _timeProvider.UtcNow;
    }

    private void AnalyzeCachePerformance(object? state)
    {
        try
        {
            if (_disposed)
                return;

            var now = _timeProvider.UtcNow;

            foreach (var (cacheName, stats) in _cacheStatistics)
            {
                var totalRequests = stats.Hits + stats.Misses;
                if (totalRequests == 0)
                    continue;

                var hitRatio = (double)stats.Hits / totalRequests;
                var evictionRate = stats.Evictions > 0 ? (double)stats.Evictions / totalRequests : 0;

                // Log performance insights
                if (hitRatio < 0.5 && totalRequests > 100)
                {
                    _logger.LogWarning(
                        "Low cache hit ratio detected for {CacheName}: {HitRatio:P2} ({Hits}/{Total} requests)",
                        cacheName, hitRatio, stats.Hits, totalRequests);
                }

                if (evictionRate > 0.1 && totalRequests > 50)
                {
                    _logger.LogWarning(
                        "High cache eviction rate detected for {CacheName}: {EvictionRate:P2} ({Evictions} evictions)",
                        cacheName, evictionRate, stats.Evictions);
                }

                // Reset rolling statistics periodically
                if (now - stats.LastAccessTime > TimeSpan.FromHours(1))
                {
                    stats.Reset();
                }
            }

            _logger.LogDebug("Cache performance analysis completed for {CacheCount} caches", _cacheStatistics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze cache performance");
        }
    }

    /// <summary>
    /// Gets cache statistics for a specific cache.
    /// </summary>
    public CacheStats GetCacheStats(string cacheName)
    {
        return _cacheStatistics.GetValueOrDefault(cacheName, new CacheStats());
    }

    /// <summary>
    /// Gets statistics for all caches.
    /// </summary>
    public IReadOnlyDictionary<string, CacheStats> GetAllCacheStats()
    {
        return new Dictionary<string, CacheStats>(_cacheStatistics);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cacheTimer?.Dispose();

        _logger.LogInformation("Cache performance monitor disposed");
    }
}

/// <summary>
/// Statistics for a specific cache instance.
/// </summary>
public class CacheStats
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long Evictions { get; set; }
    public DateTime LastAccessTime { get; set; } = SystemTimeProvider.Instance.UtcNow;

    public double HitRatio => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;
    public long TotalRequests => Hits + Misses;

    public void Reset()
    {
        Hits = 0;
        Misses = 0;
        Evictions = 0;
    }
}

/// <summary>
/// Extension methods for cache monitoring.
/// </summary>
public static class CacheMonitoringExtensions
{
    public static T WithCacheMonitoring<T>(
        this CachePerformanceMonitor monitor,
        string cacheName,
        Func<T> operation)
    {
        try
        {
            var result = operation();
            monitor.RecordCacheHit(cacheName);
            return result;
        }
        catch
        {
            monitor.RecordCacheMiss(cacheName);
            throw;
        }
    }

    public static async Task<T> WithCacheMonitoringAsync<T>(
        this CachePerformanceMonitor monitor,
        string cacheName,
        Func<Task<T>> operation)
    {
        try
        {
            var result = await operation().ConfigureAwait(false);
            monitor.RecordCacheHit(cacheName);
            return result;
        }
        catch
        {
            monitor.RecordCacheMiss(cacheName);
            throw;
        }
    }
}
