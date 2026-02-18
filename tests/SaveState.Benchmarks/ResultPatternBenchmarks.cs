using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using SaveState.Core.Common;

namespace SaveState.Benchmarks;

/// <summary>
/// Benchmarks for Result pattern performance.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
[RankColumn]
public class ResultPatternBenchmarks
{
    private const int Iterations = 1000;

    [Benchmark(Baseline = true)]
    public int Result_Success()
    {
        var count = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = Result<int>.Success(i);
            if (result.IsSuccess)
            {
                count += result.Value;
            }
        }
        return count;
    }

    [Benchmark]
    public int Result_Failure()
    {
        var count = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = Result<int>.Failure("Error", ErrorType.Internal);
            if (result.IsFailure)
            {
                count++;
            }
        }
        return count;
    }

    [Benchmark]
    public int Exception_ThrowCatch()
    {
        var count = 0;
        for (int i = 0; i < Iterations; i++)
        {
            try
            {
                throw new InvalidOperationException("Error");
            }
            catch
            {
                count++;
            }
        }
        return count;
    }

    [Benchmark]
    public int Nullable_ReturnNull()
    {
        var count = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = i % 2 == 0 ? i : (int?)null;
            if (result.HasValue)
            {
                count += result.Value;
            }
        }
        return count;
    }

    [Benchmark]
    public async Task<int> Result_Async_Success()
    {
        var tasks = Enumerable.Range(0, 100)
            .Select(async i =>
            {
                await Task.Yield();
                return Result<int>.Success(i);
            });

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r.IsSuccess).Sum(r => r.Value);
    }

    [Benchmark]
    public async Task<int> Result_Async_Failure()
    {
        var tasks = Enumerable.Range(0, 100)
            .Select(async i =>
            {
                await Task.Yield();
                return i % 2 == 0 
                    ? Result<int>.Success(i) 
                    : Result<int>.Failure("Error", ErrorType.Internal);
            });

        var results = await Task.WhenAll(tasks);
        return results.Count(r => r.IsSuccess);
    }
}

/// <summary>
/// Benchmarks for string operations used throughout the app.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
public class StringOperationBenchmarks
{
    private List<string> _gameTitles = null!;
    private string _searchTerm = "the";

    [GlobalSetup]
    public void Setup()
    {
        _gameTitles = new List<string>
        {
            "The Legend of Zelda: Breath of the Wild",
            "The Witcher 3: Wild Hunt",
            "The Elder Scrolls V: Skyrim",
            "The Last of Us",
            "The Dark Souls",
            "The Grand Theft Auto V",
            "The Halo Infinite",
            "The God of War",
            "The Uncharted 4",
            "The Horizon Zero Dawn"
        };
    }

    [Benchmark(Baseline = true)]
    public List<string> Contains_OrdinalIgnoreCase()
    {
        return _gameTitles
            .Where(t => t.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [Benchmark]
    public List<string> Contains_ToLower()
    {
        var lowerTerm = _searchTerm.ToLower();
        return _gameTitles
            .Where(t => t.ToLower().Contains(lowerTerm))
            .ToList();
    }

    [Benchmark]
    public List<string> Span_Contains()
    {
        var results = new List<string>();
        var searchSpan = _searchTerm.AsSpan();
        
        foreach (var title in _gameTitles)
        {
            if (title.AsSpan().IndexOf(searchSpan, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add(title);
            }
        }
        
        return results;
    }

    [Benchmark]
    public List<string> Regex_Match()
    {
        var regex = new System.Text.RegularExpressions.Regex(
            _searchTerm, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return _gameTitles
            .Where(t => regex.IsMatch(t))
            .ToList();
    }
}
