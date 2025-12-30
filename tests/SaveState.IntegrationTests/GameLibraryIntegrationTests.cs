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
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<SaveStateDbContext>();
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
    public async Task CreateAndRetrieveGame_EndToEnd()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        await _dbContext.Platforms.AddAsync(platform).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        var game = Game.Create("Integration Test Game", (Guid)platform.Id!, "A test game for integration testing");

        // Act - Save game
        await _dbContext.Games.AddAsync(game).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        // Assert - Retrieve game
        var retrievedGame = await _dbContext.Games
            .Include(g => g.Platform)
            .FirstOrDefaultAsync(g => g.Id == (Guid)game.Id).ConfigureAwait(false);

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
        await _dbContext.Platforms.AddAsync(platform).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        var game = Game.Create("Soft Delete Test", (Guid)platform.Id!);
        await _dbContext.Games.AddAsync(game).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        // Act - Mark as deleted
        game.MarkAsDeleted();
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        // Assert - Game should not appear in normal queries
        var activeGames = await _dbContext.Games.ToListAsync().ConfigureAwait(false);
        activeGames.Should().NotContain(g => g.Id == (Guid)game.Id);

        // But should exist when including deleted
        var allGames = await _dbContext.Games.IgnoreQueryFilters().ToListAsync().ConfigureAwait(false);
        allGames.Should().Contain(g => g.Id == (Guid)game.Id && g.IsDeleted);
    }
}
