using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Collections.Concurrent;

namespace SaveState.Benchmarks;

/// <summary>
/// Benchmarks for cloud sync operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
[RankColumn]
public class CloudSyncBenchmarks
{
    private List<string> _fileList = null!;
    private ConcurrentDictionary<string, byte[]> _fileCache = null!;

    [Params(100, 1000, 5000)]
    public int FileCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _fileList = Enumerable.Range(1, FileCount)
            .Select(i => $"/savestates/game_{i}/save_{Guid.NewGuid()}.dat")
            .ToList();

        _fileCache = new ConcurrentDictionary<string, byte[]>();
        foreach (var file in _fileList)
        {
            var data = new byte[1024 * 1024]; // 1MB each
            Random.Shared.NextBytes(data);
            _fileCache[file] = data;
        }
    }

    [Benchmark(Baseline = true)]
    public List<string> FilterFiles_Sequential()
    {
        return _fileList
            .Where(f => f.Contains("game_1"))
            .ToList();
    }

    [Benchmark]
    public List<string> FilterFiles_Parallel()
    {
        return _fileList
            .AsParallel()
            .Where(f => f.Contains("game_1"))
            .ToList();
    }

    [Benchmark]
    public long CalculateTotalSize_Sequential()
    {
        long total = 0;
        foreach (var kvp in _fileCache)
        {
            total += kvp.Value.Length;
        }
        return total;
    }

    [Benchmark]
    public long CalculateTotalSize_Parallel()
    {
        return _fileCache
            .AsParallel()
            .Sum(kvp => (long)kvp.Value.Length);
    }

    [Benchmark]
    public Dictionary<string, long> GroupByFolder()
    {
        return _fileList
            .GroupBy(f => Path.GetDirectoryName(f) ?? "unknown")
            .ToDictionary(
                g => g.Key,
                g => g.LongCount());
    }

    [Benchmark]
    public List<string> SortFiles_ByName()
    {
        return _fileList
            .OrderBy(f => f)
            .ToList();
    }

    [Benchmark]
    public List<string> SortFiles_ByFolderThenName()
    {
        return _fileList
            .OrderBy(f => Path.GetDirectoryName(f))
            .ThenBy(f => Path.GetFileName(f))
            .ToList();
    }
}
