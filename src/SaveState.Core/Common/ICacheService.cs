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
}
