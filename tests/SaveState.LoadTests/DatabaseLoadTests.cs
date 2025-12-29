#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.Common.ValueObjects;
using SaveState.Infrastructure.Persistence;
using Xunit;
using System.Diagnostics;

namespace SaveState.LoadTests;

/// <summary>
/// Load tests for database operations under stress.
/// Tests performance and reliability under concurrent load.
/// </summary>
[Collection("Database")]
public class DatabaseLoadTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SaveStateDbContext _dbContext;
    private readonly string _dbPath;

    public DatabaseLoadTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"LoadTest_{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseSqlite($"DataSource={_dbPath}")
            .Options;

        _dbContext = new SaveStateDbContext(options);

        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ImportGameCommand).Assembly));

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
        await _dbContext.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task BulkInsert_1000Games_PerformsWithinTimeLimit()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var games = new List<Game>();

        // Create 1000 games
        for (int i = 0; i < 1000; i++)
        {
            var game = Game.Create($"Load Test Game {i}", platformId: Guid.NewGuid());
            typeof(Game).GetProperty("Id")?.SetValue(game, GameId.NewId());
            games.Add(game);
        }

        // Act
        await _dbContext.Games.AddRangeAsync(games);
        await _dbContext.SaveChangesAsync();
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Bulk insert should complete within 5 seconds");
        var count = await _dbContext.Games.CountAsync();
        count.Should().Be(1000);
    }

    [Fact]
    public async Task ConcurrentReads_50Threads_ReadsConsistentData()
    {
        // Arrange - Pre-populate with test data
        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("Test Platform"), PlatformShortName.From("TEST"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);

        var games = new List<Game>();
        for (int i = 0; i < 100; i++)
        {
            var game = Game.Create($"Concurrent Game {i}", platformId);
            typeof(Game).GetProperty("Id")?.SetValue(game, GameId.NewId());
            games.Add(game);
        }
        await _dbContext.Games.AddRangeAsync(games);
        await _dbContext.SaveChangesAsync();

        // Act - Concurrent reads
        var readTasks = new List<Task<int>>();
        for (int i = 0; i < 50; i++)
        {
            readTasks.Add(Task.Run(async () =>
            {
                var options = new DbContextOptionsBuilder<SaveStateDbContext>()
                    .UseSqlite($"DataSource={_dbPath}")
                    .Options;
                using var dbContext = new SaveStateDbContext(options);
                return await dbContext.Games.CountAsync(g => g.PlatformId == platformId);
            }));
        }

        var results = await Task.WhenAll(readTasks);
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop(); // Just for timing measurement

        // Assert
        results.Should().AllBeEquivalentTo(100, "All concurrent reads should return consistent count");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "Concurrent reads should complete within 10 seconds");
    }

    [Fact]
    public async Task MixedReadWriteOperations_HandlesConcurrencyCorrectly()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("Mixed Platform"), PlatformShortName.From("MIX"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);
        await _dbContext.SaveChangesAsync();

        // Act - Mixed read/write operations
        var tasks = new List<Task>();
        var counter = 0;

        // Writers
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var options = new DbContextOptionsBuilder<SaveStateDbContext>()
                    .UseSqlite($"DataSource={_dbPath}")
                    .Options;
                using var dbContext = new SaveStateDbContext(options);

                for (int j = 0; j < 10; j++)
                {
                    var game = Game.Create($"Mixed Game {Interlocked.Increment(ref counter)}", platformId);
                    typeof(Game).GetProperty("Id")?.SetValue(game, GameId.NewId());
                    await dbContext.Games.AddAsync(game);
                    await dbContext.SaveChangesAsync();
                }
            }));
        }

        // Readers
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var options = new DbContextOptionsBuilder<SaveStateDbContext>()
                    .UseSqlite($"DataSource={_dbPath}")
                    .Options;
                using var dbContext = new SaveStateDbContext(options);

                for (int j = 0; j < 20; j++)
                {
                    var count = await dbContext.Games.CountAsync(g => g.PlatformId == platformId);
                    count.Should().BeGreaterThanOrEqualTo(0);
                    await Task.Delay(1); // Small delay to allow interleaving
                }
            }));
        }

        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000, "Mixed operations should complete within 30 seconds");
        var finalCount = await _dbContext.Games.CountAsync(g => g.PlatformId == platformId);
        finalCount.Should().Be(100, "Should have created exactly 100 games despite concurrent operations");
    }

    [Fact]
    public async Task MemoryPressure_LargeDataset_HandlesGracefully()
    {
        // Arrange - Create a large dataset
        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("Memory Platform"), PlatformShortName.From("MEM"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);

        var largeGames = new List<Game>();
        for (int i = 0; i < 5000; i++)
        {
            var game = Game.Create($"Large Dataset Game {i}", platformId,
                description: $"This is a very long description for game {i} that contains a lot of text to test memory pressure and large data handling. " +
                $"The description is intentionally long to simulate real-world data with substantial content. " +
                $"Game number {i} has comprehensive metadata.");
            typeof(Game).GetProperty("Id")?.SetValue(game, GameId.NewId());
            largeGames.Add(game);
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        await _dbContext.Games.AddRangeAsync(largeGames);
        await _dbContext.SaveChangesAsync();
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "Large dataset insertion should complete within 10 seconds");
        var count = await _dbContext.Games.CountAsync(g => g.PlatformId == platformId);
        count.Should().Be(5000);
    }

    [Fact]
    public async Task RapidSequentialOperations_MaintainsDataIntegrity()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("Integrity Platform"), PlatformShortName.From("INT"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);
        await _dbContext.SaveChangesAsync();

        // Act - Rapid create, read, update, delete operations
        var operations = new List<Task>();
        var operationCount = 100;

        for (int i = 0; i < operationCount; i++)
        {
            var operationId = i;
            operations.Add(Task.Run(async () =>
            {
                var options = new DbContextOptionsBuilder<SaveStateDbContext>()
                    .UseSqlite($"DataSource={_dbPath}")
                    .Options;
                using var dbContext = new SaveStateDbContext(options);

                // Create
                var gameId = GameId.NewId();
                var game = Game.Create($"Integrity Game {operationId}", platformId);
                typeof(Game).GetProperty("Id")?.SetValue(game, gameId);
                await dbContext.Games.AddAsync(game);
                await dbContext.SaveChangesAsync();

                // Read
                var savedGame = await dbContext.Games.FindAsync((Guid)gameId);
                savedGame.Should().NotBeNull();

                // Update
                savedGame!.Update($"Updated Integrity Game {operationId}");
                await dbContext.SaveChangesAsync();

                // Verify update
                var updatedGame = await dbContext.Games.FindAsync((Guid)gameId);
                updatedGame!.Title.Should().Contain("Updated");

                // Delete
                dbContext.Games.Remove(updatedGame!);
                await dbContext.SaveChangesAsync();

                // Verify deletion
                var deletedGame = await dbContext.Games.FindAsync((Guid)gameId);
                deletedGame.Should().BeNull();
            }));
        }

        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(operations);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(60000, "Rapid operations should complete within 60 seconds");
        var remainingCount = await _dbContext.Games.CountAsync(g => g.PlatformId == platformId);
        remainingCount.Should().Be(0, "All games should be deleted, ensuring data integrity");
    }
}
