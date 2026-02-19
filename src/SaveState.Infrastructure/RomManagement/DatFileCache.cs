using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Infrastructure.RomManagement;

/// <summary>
/// Caches parsed DAT file entries for improved performance.
/// </summary>
public class DatFileCache
{
    private readonly MemoryCache _cache;
    private readonly ILogger<DatFileCache> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);
    private int _cachedFileCount = 0;

    public DatFileCache(ILogger<DatFileCache> logger)
    {
        _logger = logger;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 100 * 1024 * 1024, // 100 MB cache limit
            CompactionPercentage = 0.25
        });
    }

    /// <summary>
    /// Gets DAT file entries from cache or loads them if not cached.
    /// </summary>
    public async Task<List<DatFileEntry>> GetOrLoadAsync(
        string datFilePath,
        Func<string, Task<List<DatFileEntry>>> loader,
        CancellationToken ct = default)
    {
        var cacheKey = $"datfile_{GetFileHash(datFilePath)}";

        if (_cache.TryGetValue(cacheKey, out List<DatFileEntry>? entries) && entries != null)
        {
            _logger.LogDebug("DAT file cache hit: {DatFile}", datFilePath);
            return entries;
        }

        _logger.LogDebug("DAT file cache miss: {DatFile}", datFilePath);

        // Load entries
        entries = await loader(datFilePath);

        // Cache with file dependency
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSize(EstimateSize(entries))
            .SetAbsoluteExpiration(_cacheDuration);

        // Add file watcher if file exists
        if (File.Exists(datFilePath))
        {
            var fileInfo = new FileInfo(datFilePath);
            cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _logger.LogDebug("DAT file evicted from cache: {Reason}", reason);
            });
        }

        _cache.Set(cacheKey, entries, cacheOptions);
        Interlocked.Increment(ref _cachedFileCount);

        return entries;
    }

    /// <summary>
    /// Invalidates cached entries for a DAT file.
    /// </summary>
    public void Invalidate(string datFilePath)
    {
        var cacheKey = $"datfile_{GetFileHash(datFilePath)}";
        _cache.Remove(cacheKey);
        _logger.LogDebug("DAT file cache invalidated: {DatFile}", datFilePath);
    }

    /// <summary>
    /// Clears all cached DAT files.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _cachedFileCount, 0);
        _logger.LogInformation("DAT file cache cleared");
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public DatFileCacheStats GetStats()
    {
        // Use tracked count since MemoryCache doesn't expose direct count
        return new DatFileCacheStats
        {
            CachedFileCount = _cachedFileCount
        };
    }

    private static string GetFileHash(string filePath)
    {
        // Simple hash based on full path and last write time
        var fileInfo = new FileInfo(filePath);
        var hash = fileInfo.Exists
            ? $"{filePath.GetHashCode()}_{fileInfo.LastWriteTimeUtc.Ticks}"
            : filePath.GetHashCode().ToString();
        return hash;
    }

    private static long EstimateSize(List<DatFileEntry> entries)
    {
        // Rough estimation: ~200 bytes per entry
        return entries.Count * 200;
    }
}

/// <summary>
/// Cache statistics for DAT files.
/// </summary>
public class DatFileCacheStats
{
    public int CachedFileCount { get; set; }
}
