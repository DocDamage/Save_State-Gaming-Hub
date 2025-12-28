using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Ai;

/// <summary>
/// Manages caching for AI responses with TTL support and automatic cleanup.
/// Provides thread-safe operations for cache storage and retrieval.
/// </summary>
public class CacheManager : IAiCacheCoordinator, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<CacheManager>();
    private readonly ConcurrentDictionary<string, (string Value, DateTime Expiry)> _cache = new();
    private readonly Timer _cleanupTimer;
    private bool _disposed = false;

    public CacheManager()
    {
        // Start background cleanup every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Enables caching for keys matching the specified pattern.
    /// </summary>
    public void EnableCache(string keyPattern, TimeSpan ttl)
    {
        _logger.Information("Cache enabled for pattern: {Pattern} with TTL: {Ttl}", keyPattern, ttl);
        // Pattern-based caching is handled at the usage level
    }

    /// <summary>
    /// Stores a value in the cache with the specified TTL.
    /// </summary>
    public void Store(string key, string value, TimeSpan ttl)
    {
        var expiry = DateTime.UtcNow.Add(ttl);
        _cache[key] = (value, expiry);
        _logger.Debug("Cached value for key: {Key} (expires: {Expiry})", key, expiry);
    }

    /// <summary>
    /// Retrieves a value from the cache if it exists and hasn't expired.
    /// </summary>
    public string? Get(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.Expiry)
            {
                _logger.Debug("Cache hit for key: {Key}", key);
                return entry.Value;
            }
            else
            {
                // Remove expired entry
                _cache.TryRemove(key, out _);
                _logger.Debug("Removed expired cache entry: {Key}", key);
            }
        }

        _logger.Debug("Cache miss for key: {Key}", key);
        return null;
    }

    /// <summary>
    /// Invalidates cache entries matching the specified pattern.
    /// </summary>
    public void InvalidateCache(string keyPattern)
    {
        var regex = new Regex(keyPattern, RegexOptions.IgnoreCase);
        var keysToRemove = _cache.Keys.Where(key => regex.IsMatch(key)).ToList();

        foreach (var key in keysToRemove)
        {
            if (_cache.TryRemove(key, out _))
            {
                _logger.Debug("Invalidated cache entry: {Key}", key);
            }
        }

        _logger.Information("Invalidated {Count} cache entries matching pattern: {Pattern}",
            keysToRemove.Count, keyPattern);
    }

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    public void ClearCache()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.Information("Cleared all cache entries ({Count} items)", count);
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public (int TotalEntries, int ExpiredEntries, DateTime? NextExpiry) GetStats()
    {
        var now = DateTime.UtcNow;
        var expiredCount = _cache.Count(kvp => now >= kvp.Value.Expiry);
        var nextExpiry = _cache.Values
            .Where(entry => now < entry.Expiry)
            .Select(entry => entry.Expiry)
            .OrderBy(expiry => expiry)
            .FirstOrDefault();

        return (_cache.Count, expiredCount, nextExpiry == default ? null : nextExpiry);
    }

    /// <summary>
    /// Performs manual cleanup of expired entries.
    /// </summary>
    public void CleanupExpiredEntries()
    {
        CleanupExpiredEntries(null);
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => now >= kvp.Value.Expiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Any())
        {
            _logger.Debug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Generates a cache key from the input string.
    /// </summary>
    public static string GenerateCacheKey(string input)
    {
        // Simple hash-based key generation
        // In production, you might want more sophisticated key generation
        return $"cache_{input.GetHashCode():X}";
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cleanupTimer?.Dispose();
        _disposed = true;
    }
}
