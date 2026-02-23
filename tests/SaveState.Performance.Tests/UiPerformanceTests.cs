// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using Bogus;
using FluentAssertions;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Performance.Tests;

/// <summary>
/// UI performance tests for rendering and interaction benchmarks.
/// </summary>
public class UiPerformanceTests
{
    [Fact]
    public void ListRendering_10KItems()
    {
        // Arrange
        var games = GenerateGames(10000);
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate list virtualization
        var virtualizedItems = VirtualizeList(games, viewportSize: 20, scrollOffset: 0);
        stopwatch.Stop();

        // Assert - Should render virtualized view in under 50ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(50));
        virtualizedItems.Count.Should().Be(20);
    }

    [Fact]
    public void ListRendering_ScrollPerformance()
    {
        // Arrange
        var games = GenerateGames(10000);
        var renderTimes = new List<TimeSpan>();

        // Act - Simulate scrolling through list
        for (int scrollOffset = 0; scrollOffset < 1000; scrollOffset += 50)
        {
            var stopwatch = Stopwatch.StartNew();
            var virtualizedItems = VirtualizeList(games, viewportSize: 20, scrollOffset: scrollOffset);
            stopwatch.Stop();
            renderTimes.Add(stopwatch.Elapsed);
        }

        // Assert - 95th percentile should be under 10ms
        var percentile95 = renderTimes.OrderBy(t => t).Skip((int)(renderTimes.Count * 0.95)).First();
        percentile95.Should().BeLessThan(TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void SearchDebouncePerformance()
    {
        // Arrange
        var games = GenerateGames(5000);
        var searchTerms = new[] { "E", "El", "Eld", "Elde", "Elden" };
        var results = new List<List<Game>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate debounced search typing
        foreach (var term in searchTerms)
        {
            var searchStopwatch = Stopwatch.StartNew();
            var result = games
                .Where(g => g.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(20)
                .ToList();
            searchStopwatch.Stop();

            // Simulate debounce delay
            Thread.Sleep(50);
            results.Add(result);
        }

        stopwatch.Stop();

        // Assert - Total time should be under 500ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void FilterChainingPerformance()
    {
        // Arrange
        var games = GenerateGames(10000);
        var stopwatch = Stopwatch.StartNew();

        // Act - Apply multiple filters
        var result = games
            .Where(g => g.Status == SaveState.Core.GameLibrary.Enums.GameStatus.Installed)
            .Where(g => g.Tags.Any(t => t == "RPG"))
            .Where(g => g.UserRating >= 7.0)
            .OrderByDescending(g => g.LastPlayedAt)
            .Take(50)
            .ToList();

        stopwatch.Stop();

        // Assert - Complex filter chain should complete under 100ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void GridLayoutCalculation_1000Items()
    {
        // Arrange
        var items = Enumerable.Range(0, 1000).ToList();
        var stopwatch = Stopwatch.StartNew();

        // Act - Calculate grid layout
        var layout = CalculateGridLayout(items, columns: 4, itemWidth: 200, itemHeight: 300);
        stopwatch.Stop();

        // Assert - Layout calculation should be under 10ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(10));
        layout.Count.Should().Be(1000);
    }

    [Fact]
    public void ThumbnailLoadingSimulation()
    {
        // Arrange
        var gameCount = 100;
        var concurrentLoads = 10;
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate concurrent thumbnail loading
        var semaphore = new SemaphoreSlim(concurrentLoads);
        var tasks = new List<Task>();

        for (int i = 0; i < gameCount; i++)
        {
            tasks.Add(LoadThumbnailAsync(semaphore, i));
        }

        Task.WhenAll(tasks).Wait();
        stopwatch.Stop();

        // Assert - Should complete within reasonable time
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AnimationFrameBudget()
    {
        // Arrange
        const double targetFps = 60.0;
        const double frameBudgetMs = 1000.0 / targetFps; // ~16.67ms
        var frameTimings = new ConcurrentBag<double>();

        // Act - Simulate UI update cycles
        Parallel.For(0, 100, i =>
        {
            var stopwatch = Stopwatch.StartNew();

            // Simulate work
            var games = GenerateGames(100);
            var sorted = games.OrderBy(g => g.Title).ToList();

            stopwatch.Stop();
            frameTimings.Add(stopwatch.Elapsed.TotalMilliseconds);
        });

        // Assert - 95% of frames should meet budget
        var sortedTimings = frameTimings.OrderBy(t => t).ToList();
        var percentile95 = sortedTimings[(int)(sortedTimings.Count * 0.95)];
        percentile95.Should().BeLessThan(frameBudgetMs * 2); // Allow some tolerance
    }

    [Fact]
    public void GameCardDataBinding()
    {
        // Arrange
        var games = GenerateGames(100);
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate data binding preparation
        var viewModels = games.Select(g => new GameCardViewModel
        {
            Id = Guid.NewGuid(),
            Title = g.Title,
            CoverImagePath = g.CoverImagePath,
            PlayTime = g.TotalPlayTime,
            LastPlayed = g.LastPlayedAt,
            Rating = g.UserRating,
            Status = g.Status.ToString()
        }).ToList();

        stopwatch.Stop();

        // Assert - ViewModel creation should be fast
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(10));
        viewModels.Count.Should().Be(100);
    }

    [Fact]
    public void ContextMenuGeneration()
    {
        // Arrange
        var game = GenerateGames(1).First();
        var stopwatch = Stopwatch.StartNew();

        // Act - Generate context menu items
        var menuItems = new List<ContextMenuItem>
        {
            new() { Label = "Play", Icon = "play", Action = "play" },
            new() { Label = "Edit", Icon = "edit", Action = "edit" },
            new() { Label = "Delete", Icon = "delete", Action = "delete" },
            new() { Label = "Add to Collection", Icon = "folder", Action = "add_to_collection" },
            new() { Label = "Create Save State", Icon = "save", Action = "create_save" },
            new() { Label = "Properties", Icon = "settings", Action = "properties" }
        };

        // Simulate conditional menu items
        if (game.Status == SaveState.Core.GameLibrary.Enums.GameStatus.Installed)
        {
            menuItems.Insert(1, new ContextMenuItem { Label = "Launch Options", Icon = "rocket", Action = "launch_options" });
        }

        stopwatch.Stop();

        // Assert - Menu generation should be under 5ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(5));
    }

    #region Helper Methods

    private static List<Game> VirtualizeList(List<Game> allItems, int viewportSize, int scrollOffset)
    {
        return allItems
            .Skip(scrollOffset)
            .Take(viewportSize)
            .ToList();
    }

    private static List<GridItemLayout> CalculateGridLayout(List<int> items, int columns, double itemWidth, double itemHeight)
    {
        return items.Select((item, index) =>
        {
            var row = index / columns;
            var col = index % columns;
            return new GridItemLayout
            {
                ItemId = item,
                X = col * itemWidth,
                Y = row * itemHeight,
                Width = itemWidth,
                Height = itemHeight
            };
        }).ToList();
    }

    private static async Task LoadThumbnailAsync(SemaphoreSlim semaphore, int index)
    {
        await semaphore.WaitAsync();
        try
        {
            // Simulate async thumbnail loading
            await Task.Delay(Random.Shared.Next(10, 50));
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static List<Game> GenerateGames(int count)
    {
        var faker = new Faker();
        var games = new List<Game>(count);

        for (int i = 0; i < count; i++)
        {
            var game = Game.Create(
                title: $"Game {faker.Random.Word()} {i}",
                platformId: Guid.NewGuid(),
                description: faker.Lorem.Sentence(),
                coverImagePath: $"/covers/game_{i}.jpg");

            var tagsProperty = typeof(Game).GetProperty("Tags");
            tagsProperty?.SetValue(game, new List<string> { "RPG", "Action" });

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

    #endregion

    #region Helper Classes

    private class GameCardViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CoverImagePath { get; set; }
        public TimeSpan PlayTime { get; set; }
        public DateTime? LastPlayed { get; set; }
        public double? Rating { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private class GridItemLayout
    {
        public int ItemId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    private class ContextMenuItem
    {
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    #endregion
}
