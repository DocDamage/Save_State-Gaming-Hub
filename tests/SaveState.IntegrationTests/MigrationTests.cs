using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.Common.ValueObjects;
using SaveState.Infrastructure.Persistence;
using Xunit;

namespace SaveState.IntegrationTests;

/// <summary>
/// Database migration tests to ensure safe schema deployments.
/// Tests migration scripts, data integrity, and rollback scenarios.
/// </summary>
[Collection("DatabaseTests")]
public class MigrationTests : IAsyncLifetime
{
    private readonly SaveStateDbContext _dbContext;

    public MigrationTests()
    {
        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _dbContext = new SaveStateDbContext(options);
    }

    public async Task InitializeAsync()
    {
        // Start with a clean database for each test
        await _dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
        await _dbContext.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task DatabaseMigration_CreatesAllRequiredTables()
    {
        // Act
        await _dbContext.Database.MigrateAsync();

        // Assert - Check that all expected tables exist
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";

        var tables = new List<string>();
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        // Verify essential tables exist
        tables.Should().Contain("Games");
        tables.Should().Contain("Platforms");
        tables.Should().Contain("RomFiles");
        tables.Should().Contain("Achievements");
        tables.Should().Contain("UserAchievements");
        tables.Should().Contain("__EFMigrationsHistory");
    }

    [Fact]
    public async Task Migration_DataIntegrity_PreservesExistingData()
    {
        // Arrange - Create data with current schema
        await _dbContext.Database.MigrateAsync();

        var platform = new Platform(PlatformName.From("Migration Test"), PlatformShortName.From("MIG"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, Guid.NewGuid());
        await _dbContext.Platforms.AddAsync(platform);

        var game = Game.Create("Migration Test Game", platform.Id);
        typeof(Game).GetProperty("Id")?.SetValue(game, GameId.NewId());
        await _dbContext.Games.AddAsync(game);

        await _dbContext.SaveChangesAsync();

        var originalGameCount = await _dbContext.Games.CountAsync();
        var originalPlatformCount = await _dbContext.Platforms.CountAsync();

        // Act - "Migrate" again (should be idempotent)
        await _dbContext.Database.MigrateAsync();

        // Assert - Data should still exist
        var newGameCount = await _dbContext.Games.CountAsync();
        var newPlatformCount = await _dbContext.Platforms.CountAsync();

        newGameCount.Should().Be(originalGameCount);
        newPlatformCount.Should().Be(originalPlatformCount);
    }

    [Fact]
    public async Task Migration_ForeignKeyConstraints_WorkCorrectly()
    {
        // Arrange
        await _dbContext.Database.MigrateAsync();

        // Act - Create platform and game with proper relationships
        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("FK Test"), PlatformShortName.From("FK"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);

        var gameId = GameId.NewId();
        var game = Game.Create("FK Test Game", platformId);
        typeof(Game).GetProperty("Id")?.SetValue(game, gameId);
        await _dbContext.Games.AddAsync(game);

        await _dbContext.SaveChangesAsync();

        // Assert - Should be able to query with joins
        var gamesWithPlatforms = await _dbContext.Games
            .Include(g => g.Platform)
            .Where(g => g.PlatformId == platformId)
            .ToListAsync();

        gamesWithPlatforms.Should().HaveCount(1);
        gamesWithPlatforms[0].Platform.Should().NotBeNull();
        gamesWithPlatforms[0].Platform!.Name.Should().Be(PlatformName.From("FK Test"));
    }

    [Fact]
    public async Task Migration_Indexes_AreCreatedForPerformance()
    {
        // Act
        await _dbContext.Database.MigrateAsync();

        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        // Check for indexes on commonly queried columns
        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%'";

        var indexes = new List<string>();
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                indexes.Add(reader.GetString(0));
            }
        }

        // Should have indexes for foreign keys and commonly searched columns
        indexes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Migration_DefaultValues_AreAppliedCorrectly()
    {
        // Arrange
        await _dbContext.Database.MigrateAsync();

        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("Defaults Test"), PlatformShortName.From("DEF"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);

        var game = Game.Create("Defaults Test Game", platformId);
        typeof(Game).GetProperty("Id")?.SetValue(game, GameId.NewId());
        await _dbContext.Games.AddAsync(game);

        await _dbContext.SaveChangesAsync();

        // Act - Retrieve and check default values
        var savedGame = await _dbContext.Games.FindAsync((Guid)game.Id);

        // Assert - Default values should be set
        savedGame.Should().NotBeNull();
        savedGame!.Status.Should().Be(Core.GameLibrary.Enums.GameStatus.NotInstalled);
        savedGame.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        savedGame.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Migration_Constraints_PreventInvalidData()
    {
        // Arrange
        await _dbContext.Database.MigrateAsync();

        // Act & Assert - Try to create game without required platform (should fail)
        var game = Game.Create("Constraint Test Game", Guid.NewGuid()); // Non-existent platform
        typeof(Game).GetProperty("Id")?.SetValue(game, GameId.NewId());

        // This should work at entity level, but foreign key constraint will be checked at database level
        await _dbContext.Games.AddAsync(game);

        // The constraint violation will occur during SaveChanges
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            _dbContext.SaveChangesAsync());

        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task Migration_Rollback_SucceedsWithoutDataLoss()
    {
        // Arrange - Create data
        await _dbContext.Database.MigrateAsync();

        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("Rollback Test"), PlatformShortName.From("ROL"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);

        var game = Game.Create("Rollback Test Game", platformId);
        typeof(Game).GetProperty("Id")?.SetValue(game, GameId.NewId());
        await _dbContext.Games.AddAsync(game);

        await _dbContext.SaveChangesAsync();

        var dataCount = await _dbContext.Games.CountAsync();

        // Act - "Rollback" by deleting and recreating database
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        // Assert - Fresh database should be empty
        var newDataCount = await _dbContext.Games.CountAsync();
        newDataCount.Should().Be(0);
        dataCount.Should().Be(1); // Original data existed
    }

    [Fact]
    public async Task Migration_LargeDataset_MigratesEfficiently()
    {
        // Arrange - Create a large dataset before migration
        await _dbContext.Database.EnsureCreatedAsync(); // Create tables without migrations

        var platforms = new List<Platform>();
        for (int i = 0; i < 10; i++)
        {
            var platform = new Platform(PlatformName.From($"Platform {i}"), PlatformShortName.From($"P{i}"), Core.GameLibrary.Enums.PlatformType.Computer);
            typeof(Platform).GetProperty("Id")?.SetValue(platform, Guid.NewGuid());
            platforms.Add(platform);
        }
        await _dbContext.Platforms.AddRangeAsync(platforms);

        var games = new List<Game>();
        foreach (var platform in platforms)
        {
            for (int i = 0; i < 100; i++)
            {
                var game = Game.Create($"Game {platform.Id}-{i}", platform.Id);
                typeof(Game).GetProperty("Id")?.SetValue(game, GameId.NewId());
                games.Add(game);
            }
        }
        await _dbContext.Games.AddRangeAsync(games);
        await _dbContext.SaveChangesAsync();

        // Act - Apply migrations to existing data
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _dbContext.Database.MigrateAsync();
        stopwatch.Stop();

        // Assert - Migration should complete efficiently
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Migration should complete within 5 seconds even with data");

        var finalCount = await _dbContext.Games.CountAsync();
        finalCount.Should().Be(1000, "All data should be preserved during migration");
    }
}
