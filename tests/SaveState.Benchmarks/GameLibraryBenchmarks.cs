// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Bogus;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Benchmarks;

/// <summary>
/// Benchmarks for GameLibrary operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
[RankColumn]
public class GameLibraryBenchmarks
{
    private List<Game> _testGames = null!;
    private List<Game> _sortedGames = null!;

    [Params(100, 1000, 10000)]
    public int GameCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _testGames = GenerateTestGames(GameCount);
        _sortedGames = _testGames.OrderBy(g => g.Title).ToList();
    }

    [Benchmark]
    public List<Game> FilterByGenre()
    {
        return _testGames
            .Where(g => g.Tags.Any(t => t.Equals("RPG", StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    [Benchmark]
    public List<Game> SearchByTitle()
    {
        return _testGames
            .Where(g => g.Title.Contains("Elden", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [Benchmark]
    public List<Game> SortByName()
    {
        return _testGames
            .OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [Benchmark]
    public List<Game> SortByPlayTime()
    {
        return _testGames
            .OrderByDescending(g => g.TotalPlayTime)
            .ToList();
    }

    [Benchmark]
    public List<Game> SortByLastPlayed()
    {
        return _testGames
            .Where(g => g.LastPlayedAt.HasValue)
            .OrderByDescending(g => g.LastPlayedAt)
            .ToList();
    }

    [Benchmark]
    public List<Game> FilterInstalled()
    {
        return _testGames
            .Where(g => g.Status == GameStatus.Installed || g.Status == GameStatus.Running)
            .ToList();
    }

    [Benchmark]
    public Dictionary<string, List<Game>> GroupByStatus()
    {
        return _testGames
            .GroupBy(g => g.Status.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    [Benchmark]
    public List<Game> ComplexFilterAndSort()
    {
        return _testGames
            .Where(g => g.Status == GameStatus.Installed)
            .Where(g => g.TotalPlayTime > TimeSpan.FromHours(10))
            .OrderByDescending(g => g.UserRating ?? 0)
            .ThenBy(g => g.Title)
            .Take(100)
            .ToList();
    }

    [Benchmark]
    public double CalculateAverageRating()
    {
        return _testGames
            .Where(g => g.UserRating.HasValue)
            .Average(g => g.UserRating!.Value);
    }

    [Benchmark]
    public TimeSpan CalculateTotalPlayTime()
    {
        return TimeSpan.FromTicks(_testGames.Sum(g => g.TotalPlayTime.Ticks));
    }

    [Benchmark]
    public List<Game> SearchParallel()
    {
        return _testGames
            .AsParallel()
            .Where(g => g.Title.Contains("Game", StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.Title)
            .ToList();
    }

    private static List<Game> GenerateTestGames(int count)
    {
        var genres = new[] { "RPG", "Action", "Strategy", "Adventure", "Simulation", "Sports" };
        var platforms = new[] { "Steam", "GOG", "Epic", "Xbox", "PlayStation" };

        var faker = new Faker();
        var games = new List<Game>(count);

        for (int i = 0; i < count; i++)
        {
            var game = Game.Create(
                title: $"{faker.Random.Word()} {faker.Random.Word()} - Game {i}",
                platformId: Guid.NewGuid(),
                description: faker.Lorem.Sentence(),
                coverImagePath: $"/covers/game_{i}.jpg",
                source: faker.PickRandom(platforms),
                sourceId: i.ToString());

            // Use reflection to set properties that don't have public setters
            var tagsProperty = typeof(Game).GetProperty("Tags");
            var tags = new List<string>
            {
                faker.PickRandom(genres),
                faker.Random.Bool() ? "Multiplayer" : "Singleplayer"
            };
            tagsProperty?.SetValue(game, tags);

            var playTimeProperty = typeof(Game).GetProperty("TotalPlayTime");
            playTimeProperty?.SetValue(game, TimeSpan.FromHours(faker.Random.Double(0, 500)));

            if (faker.Random.Bool(0.7f))
            {
                var lastPlayedProperty = typeof(Game).GetProperty("LastPlayedAt");
                lastPlayedProperty?.SetValue(game, faker.Date.Past());
            }

            if (faker.Random.Bool(0.5f))
            {
                var ratingProperty = typeof(Game).GetProperty("UserRating");
                ratingProperty?.SetValue(game, faker.Random.Double(1, 10));
            }

            // Randomly set status
            var status = faker.PickRandom<GameStatus>();
            var statusProperty = typeof(Game).GetProperty("Status");
            statusProperty?.SetValue(game, status);

            games.Add(game);
        }

        return games;
    }
}
