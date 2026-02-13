using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Infrastructure.Persistence;
using Xunit;

namespace SaveState.IntegrationTests;

[Collection("DatabaseTests")]
public class GameLibraryIntegrationTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SaveStateDbContext _dbContext;

    public GameLibraryIntegrationTests()
    {
        var services = new ServiceCollection();

        // Configure in-memory database for testing
        services.AddDbContext<SaveStateDbContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
        });

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<SaveStateDbContext>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CreateAndRetrieveGame_EndToEnd()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        await _dbContext.Platforms.AddAsync(platform);
        await _dbContext.SaveChangesAsync();

        var game = Game.Create("Integration Test Game", (Guid)platform.Id!, "A test game for integration testing");

        // Act - Save game
        await _dbContext.Games.AddAsync(game);
        await _dbContext.SaveChangesAsync();

        // Assert - Retrieve game
        var retrievedGame = await _dbContext.Games
            .Include(g => g.Platform)
            .FirstOrDefaultAsync(g => g.Id == (Guid)game.Id);

        retrievedGame.Should().NotBeNull();
        retrievedGame!.Title.Should().Be("Integration Test Game");
        retrievedGame.Platform.Should().NotBeNull();
        retrievedGame.Platform.Name.Value.Should().Be("PC");
        retrievedGame.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SoftDelete_WorksCorrectly()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        await _dbContext.Platforms.AddAsync(platform);
        await _dbContext.SaveChangesAsync();

        var game = Game.Create("Soft Delete Test", (Guid)platform.Id!);
        await _dbContext.Games.AddAsync(game);
        await _dbContext.SaveChangesAsync();

        // Act - Mark as deleted
        game.MarkAsDeleted();
        await _dbContext.SaveChangesAsync();

        // Assert - Game should not appear in normal queries
        var activeGames = await _dbContext.Games.ToListAsync();
        activeGames.Should().NotContain(g => g.Id == (Guid)game.Id);

        // But should exist when including deleted
        var allGames = await _dbContext.Games.IgnoreQueryFilters().ToListAsync();
        allGames.Should().Contain(g => g.Id == (Guid)game.Id && g.IsDeleted);
    }
}
