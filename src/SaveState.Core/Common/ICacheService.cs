using Microsoft.Extensions.Caching.Memory;
using SaveState.Core.Common;

namespace SaveState.Core.Common;

/// <summary>
/// Abstraction for caching operations to improve testability and allow for different cache implementations.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Attempts to get a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cached value if found.</param>
    /// <returns>True if the value was found in the cache, false otherwise.</returns>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration time for the cache entry.</param>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Asynchronous set for cache implementations that are I/O bound or want to expose async API.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously attempts to get a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached value if found, or default.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    void Remove(string key);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The cache key to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    bool Contains(string key);

    /// <summary>
    /// Gets or creates a value in the cache asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory function to create the value if not cached. Takes an ICacheEntry for configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<ICacheEntry, Task<T>> factory, CancellationToken ct = default);

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Sets multiple values in the cache efficiently.
    /// </summary>
    /// <typeparam name="T">The type of the values to cache.</typeparam>
    /// <param name="keyValuePairs">Dictionary of key-value pairs to cache.</param>
    /// <param name="expiration">Optional expiration time for all entries.</param>
    void SetBatch<T>(IDictionary<string, T> keyValuePairs, TimeSpan? expiration = null);

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Gets multiple values from the cache efficiently.
    /// </summary>
    /// <typeparam name="T">The type of the cached values.</typeparam>
    /// <param name="keys">The cache keys to retrieve.</param>
    /// <returns>Dictionary of found key-value pairs.</returns>
    IDictionary<string, T> GetBatch<T>(IEnumerable<string> keys);

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Removes multiple keys from the cache efficiently.
    /// </summary>
    /// <param name="keys">The cache keys to remove.</param>
    void RemoveBatch(IEnumerable<string> keys);

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Clears all cache entries with a specific prefix.
    /// </summary>
    /// <param name="prefix">The key prefix to clear.</param>
    void ClearByPrefix(string prefix);

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Gets cache statistics for monitoring.
    /// </summary>
    /// <returns>Cache performance statistics.</returns>
    CacheStatistics GetStatistics();
}

/// <summary>
/// PERFORMANCE OPTIMIZATION: Cache statistics for monitoring and optimization.
/// </summary>
public class CacheStatistics
{
    public long TotalRequests { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double HitRate => TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0;
    public int CurrentEntryCount { get; set; }
    public long TotalBytesUsed { get; set; }
}
