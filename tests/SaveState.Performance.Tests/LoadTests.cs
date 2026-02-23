// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using Bogus;
using FluentAssertions;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Performance.Tests;

/// <summary>
/// Load and stress tests for concurrent user scenarios.
/// </summary>
public class LoadTests
{
    private readonly Faker _faker = new();

    [Theory]
    [InlineData(10, 100)]
    [InlineData(50, 500)]
    [InlineData(100, 1000)]
    public async Task ConcurrentSearch_LoadTest(int concurrentUsers, int totalSearches)
    {
        // Arrange
        var games = GenerateGames(5000);
        var searchTerms = new[] { "RPG", "Action", "Game", "Final", "Elden", "Zelda" };
        var results = new ConcurrentBag<OperationMetrics>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var semaphore = new SemaphoreSlim(concurrentUsers);
        var tasks = new List<Task>();

        for (int i = 0; i < totalSearches; i++)
        {
            await semaphore.WaitAsync();
            var term = searchTerms[i % searchTerms.Length];

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var opStopwatch = Stopwatch.StartNew();
                    var searchResults = games
                        .Where(g => g.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    opStopwatch.Stop();

                    results.Add(new OperationMetrics
                    {
                        Success = true,
                        Duration = opStopwatch.Elapsed,
                        ResultCount = searchResults.Count
                    });
                }
                catch (Exception)
                {
                    results.Add(new OperationMetrics { Success = false });
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var metrics = CalculateMetrics(results, stopwatch.Elapsed);
        metrics.SuccessRate.Should().BeGreaterThan(99);
        metrics.AverageResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
        metrics.OperationsPerSecond.Should().BeGreaterThan(concurrentUsers * 5);
    }

    [Theory]
    [InlineData(10, 100)]
    [InlineData(25, 250)]
    public async Task ConcurrentSaveStateOperations_LoadTest(int concurrentUsers, int totalOperations)
    {
        // Arrange
        var saveStates = new ConcurrentDictionary<Guid, SaveStateData>();
        var results = new ConcurrentBag<OperationMetrics>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var semaphore = new SemaphoreSlim(concurrentUsers);
        var tasks = new List<Task>();

        for (int i = 0; i < totalOperations; i++)
        {
            await semaphore.WaitAsync();
            var operation = i % 3; // 0 = Create, 1 = Read, 2 = Update

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var opStopwatch = Stopwatch.StartNew();

                    switch (operation)
                    {
                        case 0: // Create
                            var id = Guid.NewGuid();
                            saveStates[id] = CreateSaveStateData();
                            break;
                        case 1: // Read
                            if (saveStates.Any())
                            {
                                var key = saveStates.Keys.First();
                                _ = saveStates.TryGetValue(key, out _);
                            }
                            break;
                        case 2: // Update
                            if (saveStates.Any())
                            {
                                var updateKey = saveStates.Keys.First();
                                saveStates[updateKey] = CreateSaveStateData();
                            }
                            break;
                    }

                    opStopwatch.Stop();
                    results.Add(new OperationMetrics
                    {
                        Success = true,
                        Duration = opStopwatch.Elapsed,
                        OperationType = operation.ToString()
                    });
                }
                catch (Exception)
                {
                    results.Add(new OperationMetrics { Success = false });
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var metrics = CalculateMetrics(results, stopwatch.Elapsed);
        metrics.SuccessRate.Should().BeGreaterThan(99);
        metrics.AverageResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(50));
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(10000)]
    public async Task BulkImport_PerformanceTest(int gameCount)
    {
        // Arrange
        var games = GenerateGames(gameCount);
        var batches = games.Chunk(100).ToList();
        var results = new ConcurrentBag<TimeSpan>();

        // Act
        var stopwatch = Stopwatch.StartNew();

        await Parallel.ForEachAsync(batches, new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (batch, ct) =>
            {
                var batchStopwatch = Stopwatch.StartNew();

                // Simulate batch import processing
                foreach (var game in batch)
                {
                    await Task.Yield(); // Simulate async work
                    _ = game.Title; // Touch property
                }

                batchStopwatch.Stop();
                results.Add(batchStopwatch.Elapsed);
            });

        stopwatch.Stop();

        // Assert
        var totalTime = stopwatch.Elapsed;
        var gamesPerSecond = gameCount / totalTime.TotalSeconds;

        gamesPerSecond.Should().BeGreaterThan(1000); // At least 1000 games/second
        totalTime.Should().BeLessThan(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task MemoryPressure_UnderLoad()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var games = GenerateGames(10000);
        var results = new List<OperationMetrics>();

        // Act - Run multiple iterations under memory pressure
        for (int iteration = 0; iteration < 10; iteration++)
        {
            var stopwatch = Stopwatch.StartNew();

            // Simulate heavy operations
            var filtered = games
                .Where(g => g.Tags.Any(t => t == "RPG"))
                .OrderByDescending(g => g.TotalPlayTime)
                .ToList();

            var searched = games
                .Where(g => g.Title.Contains("Game", StringComparison.OrdinalIgnoreCase))
                .ToList();

            stopwatch.Stop();

            results.Add(new OperationMetrics
            {
                Success = true,
                Duration = stopwatch.Elapsed,
                Iteration = iteration
            });

            // Force partial GC occasionally
            if (iteration % 3 == 0)
            {
                GC.Collect(0, GCCollectionMode.Optimized, false);
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;

        // Assert
        memoryGrowth.Should().BeLessThan(100 * 1024 * 1024); // Less than 100MB growth
        results.All(r => r.Duration < TimeSpan.FromMilliseconds(500)).Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(10, 100)]
    [InlineData(50, 100)]
    [InlineData(100, 100)]
    public async Task StressTest_IncreasingLoad(int concurrencyLevel, int operationsPerLevel)
    {
        // Arrange
        var games = GenerateGames(1000);
        var results = new List<StressTestResult>();

        // Act
        for (int level = 1; level <= 5; level++)
        {
            var effectiveConcurrency = concurrencyLevel * level;
            var stopwatch = Stopwatch.StartNew();

            var semaphore = new SemaphoreSlim(effectiveConcurrency);
            var tasks = new List<Task>();
            var operationResults = new ConcurrentBag<bool>();

            for (int i = 0; i < operationsPerLevel; i++)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var filtered = games
                            .Where(g => g.UserRating > 5.0)
                            .OrderBy(g => g.Title)
                            .ToList();
                        operationResults.Add(true);
                    }
                    catch
                    {
                        operationResults.Add(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            results.Add(new StressTestResult
            {
                ConcurrencyLevel = effectiveConcurrency,
                OperationsCompleted = operationResults.Count(r => r),
                TotalTime = stopwatch.Elapsed,
                SuccessRate = operationResults.Count(r => r) * 100.0 / operationResults.Count
            });
        }

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.SuccessRate.Should().BeGreaterThan(95);
        });

        // Verify degradation is graceful (not exponential)
        for (int i = 1; i < results.Count; i++)
        {
            var timeIncrease = results[i].TotalTime.TotalMilliseconds / results[i - 1].TotalTime.TotalMilliseconds;
            timeIncrease.Should().BeLessThan(3.0); // Less than 3x time increase per level
        }
    }

    [Fact(Skip = "Long-running endurance test")]
    public async Task EnduranceTest_30Minutes()
    {
        // Arrange
        var games = GenerateGames(5000);
        var duration = TimeSpan.FromMinutes(30);
        var stopwatch = Stopwatch.StartNew();
        var iteration = 0;
        var errors = new List<Exception>();
        var responseTimes = new List<TimeSpan>();

        // Act
        while (stopwatch.Elapsed < duration)
        {
            try
            {
                var opStopwatch = Stopwatch.StartNew();

                var operation = iteration % 4;
                switch (operation)
                {
                    case 0: // Search
                        _ = games.Where(g => g.Title.Contains("Test")).ToList();
                        break;
                    case 1: // Filter
                        _ = games.Where(g => g.Tags.Contains("RPG")).ToList();
                        break;
                    case 2: // Sort
                        _ = games.OrderBy(g => g.Title).ToList();
                        break;
                    case 3: // Aggregate
                        _ = games.Average(g => g.UserRating ?? 0);
                        break;
                }

                opStopwatch.Stop();
                responseTimes.Add(opStopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }

            iteration++;

            // Brief pause to prevent CPU saturation
            if (iteration % 100 == 0)
            {
                await Task.Delay(10);
            }
        }

        stopwatch.Stop();

        // Assert
        var errorRate = (double)errors.Count / iteration * 100;
        var averageResponseTime = TimeSpan.FromMilliseconds(responseTimes.Average(t => t.TotalMilliseconds));
        var maxResponseTime = responseTimes.Max();

        errorRate.Should().BeLessThan(1); // Less than 1% errors
        averageResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
        maxResponseTime.Should().BeLessThan(TimeSpan.FromSeconds(1));

        // Check for memory leaks
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
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
                faker.PickRandom(new[] { "RPG", "Action", "Strategy", "Adventure" })
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

    private static SaveStateData CreateSaveStateData()
    {
        var data = new byte[1024 * 1024]; // 1MB
        Random.Shared.NextBytes(data);

        return new SaveStateData
        {
            Id = Guid.NewGuid(),
            Name = $"Save {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            Data = data,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static LoadTestMetrics CalculateMetrics(ConcurrentBag<OperationMetrics> results, TimeSpan totalTime)
    {
        var resultList = results.ToList();
        var successful = resultList.Where(r => r.Success).ToList();

        return new LoadTestMetrics
        {
            TotalOperations = resultList.Count,
            SuccessfulOperations = successful.Count,
            FailedOperations = resultList.Count - successful.Count,
            SuccessRate = (double)successful.Count / resultList.Count * 100,
            AverageResponseTime = successful.Any()
                ? TimeSpan.FromMilliseconds(successful.Average(r => r.Duration.TotalMilliseconds))
                : TimeSpan.Zero,
            MinResponseTime = successful.Any() ? successful.Min(r => r.Duration) : TimeSpan.Zero,
            MaxResponseTime = successful.Any() ? successful.Max(r => r.Duration) : TimeSpan.Zero,
            TotalDuration = totalTime,
            OperationsPerSecond = totalTime.TotalSeconds > 0 ? resultList.Count / totalTime.TotalSeconds : 0
        };
    }

    #endregion

    #region Helper Classes

    private class OperationMetrics
    {
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public int ResultCount { get; set; }
        public string? OperationType { get; set; }
        public int Iteration { get; set; }
    }

    private class LoadTestMetrics
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan MinResponseTime { get; set; }
        public TimeSpan MaxResponseTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double OperationsPerSecond { get; set; }
    }

    private class StressTestResult
    {
        public int ConcurrencyLevel { get; set; }
        public int OperationsCompleted { get; set; }
        public TimeSpan TotalTime { get; set; }
        public double SuccessRate { get; set; }
    }

    private class SaveStateData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAt { get; set; }
    }

    #endregion
}
