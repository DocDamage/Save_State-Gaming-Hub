using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Benchmarks;

/// <summary>
/// Benchmarks for game search operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
[RankColumn]
public class GameSearchBenchmarks
{
    private List<Game> _games = null!;
    private string _searchTerm = "zelda";

    [GlobalSetup]
    public void Setup()
    {
        // Create test data
        _games = Enumerable.Range(1, 10000)
            .Select(i => Game.Create(
                $"Game {i} - {(i % 100 == 0 ? "Zelda" : "Other")} Title",
                platformId: Guid.NewGuid(),
                description: $"Description for game {i}"))
            .ToList();
    }

    [Benchmark(Baseline = true)]
    public List<Game> SearchByTitle_Linq()
    {
        return _games
            .Where(g => g.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [Benchmark]
    public List<Game> SearchByTitle_Parallel()
    {
        return _games
            .AsParallel()
            .Where(g => g.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [Benchmark]
    public List<Game> SearchByTitle_Span()
    {
        var results = new List<Game>();
        var searchSpan = _searchTerm.AsSpan();
        
        foreach (var game in _games)
        {
            if (game.Title.AsSpan().IndexOf(searchSpan, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add(game);
            }
        }
        
        return results;
    }

    [Benchmark]
    public Dictionary<string, List<Game>> GroupByPlatform()
    {
        return _games
            .GroupBy(g => g.PlatformId?.ToString() ?? "unknown")
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    [Benchmark]
    public List<Game> OrderByTitle()
    {
        return _games
            .OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [Benchmark]
    public List<Game> FilterAndSort()
    {
        return _games
            .Where(g => g.Title.StartsWith("Game", StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.CreatedAt)
            .Take(100)
            .ToList();
    }
}
