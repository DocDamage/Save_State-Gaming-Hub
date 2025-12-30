using Microsoft.Extensions.Caching.Memory;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Common;

/// <summary>
/// Memory cache implementation of ICacheService using Microsoft.Extensions.Caching.Memory.IMemoryCache.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        return _cache.TryGetValue(key, out value);
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }

        _cache.Set(key, value, options);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    public bool Contains(string key)
    {
        return _cache.TryGetValue(key, out _);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<ICacheEntry, Task<T>> factory, CancellationToken ct = default)
    {
#pragma warning disable CS8603 // Possible null reference return - Expected for nullable types in cache operations
        return await _cache.GetOrCreateAsync(key, factory).ConfigureAwait(false);
#pragma warning restore CS8603
    }
}
