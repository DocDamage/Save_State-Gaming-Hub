using System;

namespace SaveState.Core.Monitoring;

/// <summary>
/// Interface for monitoring cache performance metrics.
/// </summary>
public interface ICachePerformanceMonitor : IDisposable
{
    /// <summary>
    /// Records a cache hit for the specified cache.
    /// </summary>
    /// <param name="cacheName">The name of the cache that had a hit.</param>
    void RecordCacheHit(string cacheName);

    /// <summary>
    /// Records a cache miss for the specified cache.
    /// </summary>
    /// <param name="cacheName">The name of the cache that had a miss.</param>
    void RecordCacheMiss(string cacheName);
}
