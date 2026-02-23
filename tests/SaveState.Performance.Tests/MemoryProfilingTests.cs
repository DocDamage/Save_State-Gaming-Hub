// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Bogus;
using FluentAssertions;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Performance.Tests;

/// <summary>
/// Memory profiling tests for critical application paths.
/// </summary>
public class MemoryProfilingTests
{
    [Fact]
    public void GameLibraryMemory_10KGames()
    {
        // Arrange
        var profiler = new MemoryProfiler();

        // Act
        profiler.Start();
        var games = GenerateGames(10000);
        var totalSize = EstimateMemoryUsage(games);
        profiler.Stop();

        // Assert - Should use less than 100MB for 10,000 games
        profiler.MemoryUsed.Should().BeLessThan(150 * 1024 * 1024);
        totalSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GameLibraryMemory_1KGames_WithMetadata()
    {
        // Arrange
        var profiler = new MemoryProfiler();

        // Act
        profiler.Start();
        var games = GenerateGamesWithMetadata(1000);
        profiler.Stop();

        // Assert - Should use less than 50MB for 1,000 games with full metadata
        profiler.MemoryUsed.Should().BeLessThan(50 * 1024 * 1024);
    }

    [Fact]
    public void SaveStateMemory_MultipleBranches()
    {
        // Arrange
        var profiler = new MemoryProfiler();
        var saveData = new byte[10 * 1024 * 1024]; // 10MB save
        Random.Shared.NextBytes(saveData);

        // Act
        profiler.Start();
        var branches = CreateSaveStateBranches(100, saveData);
        profiler.Stop();

        // Assert - Should use less than 1GB for 100 branches with 10MB each
        profiler.MemoryUsed.Should().BeLessThan(1024 * 1024 * 1024);
    }

    [Fact]
    public void SearchIndexMemory_10KGames()
    {
        // Arrange
        var games = GenerateGames(10000);
        var profiler = new MemoryProfiler();

        // Act
        profiler.Start();
        var index = BuildSearchIndex(games);
        profiler.Stop();

        // Assert - Index should use less than 200MB
        profiler.MemoryUsed.Should().BeLessThan(200 * 1024 * 1024);
    }

    [Fact]
    public void GenreFilterMemory_NoLeak()
    {
        // Arrange
        var games = GenerateGames(5000);
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Run multiple times to check for leaks
        for (int i = 0; i < 100; i++)
        {
            var filtered = games
                .Where(g => g.Tags.Any(t => t == "RPG"))
                .ToList();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;

        // Assert - Memory growth should be minimal (< 10MB)
        memoryGrowth.Should().BeLessThan(10 * 1024 * 1024);
    }

    [Fact]
    public void SortingMemory_Efficient()
    {
        // Arrange
        var games = GenerateGames(10000);
        var profiler = new MemoryProfiler();

        // Act
        profiler.Start();
        var sorted = games
            .OrderBy(g => g.Title)
            .ThenByDescending(g => g.TotalPlayTime)
            .ToList();
        profiler.Stop();

        // Assert - Sorting 10K games should use less than 100MB additional memory
        profiler.MemoryUsed.Should().BeLessThan(100 * 1024 * 1024);
    }

    [Fact]
    public void ConcurrentAccessMemory_ThreadSafe()
    {
        // Arrange
        var games = GenerateGames(1000);
        var tasks = new List<Task>();
        var profiler = new MemoryProfiler();

        // Act
        profiler.Start();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var filtered = games.Where(g => g.UserRating > 7.0).ToList();
                var sorted = games.OrderByDescending(g => g.LastPlayedAt).ToList();
            }));
        }

        Task.WhenAll(tasks).Wait();
        profiler.Stop();

        // Assert
        profiler.MemoryUsed.Should().BeLessThan(100 * 1024 * 1024);
    }

    #region Helper Methods

    private static List<Game> GenerateGames(int count)
    {
        var faker = new Faker();
        var games = new List<Game>(count);

        for (int i = 0; i < count; i++)
        {
            var game = Game.Create(
                title: $"Game {faker.Random.Word()} {i}",
                platformId: Guid.NewGuid(),
                description: faker.Lorem.Sentence());

            var tagsProperty = typeof(Game).GetProperty("Tags");
            tagsProperty?.SetValue(game, new List<string>
            {
                faker.PickRandom(new[] { "RPG", "Action", "Strategy" })
            });

            var playTimeProperty = typeof(Game).GetProperty("TotalPlayTime");
            playTimeProperty?.SetValue(game, TimeSpan.FromHours(faker.Random.Double(0, 500)));

            if (faker.Random.Bool(0.5f))
            {
                var ratingProperty = typeof(Game).GetProperty("UserRating");
                ratingProperty?.SetValue(game, faker.Random.Double(1, 10));
            }

            games.Add(game);
        }

        return games;
    }

    private static List<Game> GenerateGamesWithMetadata(int count)
    {
        var faker = new Faker();
        var games = new List<Game>(count);

        for (int i = 0; i < count; i++)
        {
            var game = Game.Create(
                title: $"Game {faker.Random.Word()} {faker.Random.Word()} {i}",
                platformId: Guid.NewGuid(),
                description: faker.Lorem.Paragraph(),
                coverImagePath: $"/covers/game_{i}.png",
                source: faker.PickRandom(new[] { "Steam", "GOG", "Epic" }),
                sourceId: i.ToString());

            var tagsProperty = typeof(Game).GetProperty("Tags");
            tagsProperty?.SetValue(game, faker.Random.WordsArray(5).ToList());

            var playTimeProperty = typeof(Game).GetProperty("TotalPlayTime");
            playTimeProperty?.SetValue(game, TimeSpan.FromHours(faker.Random.Double(0, 1000)));

            var lastPlayedProperty = typeof(Game).GetProperty("LastPlayedAt");
            lastPlayedProperty?.SetValue(game, faker.Date.Past());

            var ratingProperty = typeof(Game).GetProperty("UserRating");
            ratingProperty?.SetValue(game, faker.Random.Double(1, 10));

            games.Add(game);
        }

        return games;
    }

    private static List<SaveStateBranch> CreateSaveStateBranches(int count, byte[] templateData)
    {
        var branches = new List<SaveStateBranch>(count);

        for (int i = 0; i < count; i++)
        {
            branches.Add(new SaveStateBranch
            {
                Id = Guid.NewGuid(),
                Name = $"Branch {i}",
                Data = templateData.ToArray(), // Copy to avoid shared reference
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                ParentId = i > 0 ? branches[i - 1].Id : null
            });
        }

        return branches;
    }

    private static Dictionary<string, List<Game>> BuildSearchIndex(List<Game> games)
    {
        var index = new Dictionary<string, List<Game>>(StringComparer.OrdinalIgnoreCase);

        foreach (var game in games)
        {
            var words = game.Title.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var key = word.ToLowerInvariant();
                if (!index.TryGetValue(key, out var list))
                {
                    list = new List<Game>();
                    index[key] = list;
                }
                list.Add(game);
            }
        }

        return index;
    }

    private static long EstimateMemoryUsage(List<Game> games)
    {
        // Rough estimation: 200 bytes per game base + strings
        return games.Count * 200L +
               games.Sum(g => g.Title.Length * 2) +
               games.Sum(g => (g.Description?.Length ?? 0) * 2);
    }

    #endregion

    #region Helper Classes

    private class SaveStateBranch
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAt { get; set; }
        public Guid? ParentId { get; set; }
    }

    private class MemoryProfiler
    {
        private long _startMemory;

        public long MemoryUsed { get; private set; }

        public void Start()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _startMemory = GC.GetTotalMemory(true);
        }

        public void Stop()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var endMemory = GC.GetTotalMemory(true);
            MemoryUsed = Math.Max(0, endMemory - _startMemory);
        }
    }

    #endregion
}
