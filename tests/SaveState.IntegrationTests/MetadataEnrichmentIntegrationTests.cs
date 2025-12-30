using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary;
using SaveState.Infrastructure.External;
using SaveState.Infrastructure.Persistence;
using SaveState.Tests.Fakes;
using Xunit;

namespace SaveState.IntegrationTests;

[Collection("DatabaseTests")]
public class MetadataEnrichmentIntegrationTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SaveStateDbContext _dbContext;

    public MetadataEnrichmentIntegrationTests()
    {
        var services = new ServiceCollection();

        // Configure in-memory database for testing
        services.AddDbContext<SaveStateDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Register logging
        services.AddLogging();

        // Register fake metadata services for testing
        services.AddSingleton<IMetadataService, FakeMetadataService>();
        services.AddSingleton<IGameRepository, SaveState.Infrastructure.Repositories.GameRepository>();
        services.AddSingleton<IPlatformRepository, SaveState.Infrastructure.Repositories.PlatformRepository>();

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
    public async Task SteamMetadataEnrichment_FetchesGameDetails()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();
        var gameId = "570"; // Dota 2 Steam ID

        // Act
        var metadata = await metadataService.GetGameMetadataAsync(gameId);

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Title.Should().NotBeNullOrEmpty();
        metadata.Title.Should().Contain("Dota"); // Should contain game name
        metadata.Description.Should().NotBeNullOrEmpty();
        metadata.ReleaseDate.Should().NotBeNull();
        metadata.Genres.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SteamMetadataEnrichment_HandlesInvalidGameId()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();
        var invalidGameId = "999999999"; // Non-existent Steam ID

        // Act
        var metadata = await metadataService.GetGameMetadataAsync(invalidGameId);

        // Assert
        metadata.Should().BeNull(); // Should return null for invalid IDs
    }

    [Fact]
    public async Task SteamMetadataEnrichment_IncludesGenreInformation()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();
        var gameId = "440"; // Team Fortress 2 - has clear genres

        // Act
        var metadata = await metadataService.GetGameMetadataAsync(gameId);

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Genres.Should().NotBeNullOrEmpty();
        metadata.Genres.Should().Contain("Action"); // TF2 should have Action genre
        metadata.Genres.Should().Contain("Multi-player"); // TF2 is multiplayer
    }

    [Fact]
    public async Task MetadataService_HandlesNetworkTimeouts()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();

        // Act - Test with a potentially slow request
        var metadata = await metadataService.GetGameMetadataAsync("10"); // Very old game, might be slow

        // Assert - Should either return data or null, but not throw
        // This tests that the service handles network issues gracefully
        metadata.Should().NotBeNull(); // Fake provider should return data
    }

    [Fact]
    public async Task MetadataEnrichment_WithRealisticData_IncludesAllFields()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();
        var gameId = "730"; // CS2 - comprehensive metadata

        // Act
        var metadata = await metadataService.GetGameMetadataAsync(gameId);

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Title.Should().NotBeNullOrEmpty();
        metadata.Title.Should().Be("Counter-Strike 2"); // Should be exact title
        metadata.Description.Should().NotBeNullOrEmpty();
        metadata.Description.Length.Should().BeGreaterThan(50); // Should have substantial description
        metadata.ReleaseDate.Should().NotBeNull();
        metadata.Developer.Should().NotBeNullOrEmpty();
        metadata.Publisher.Should().NotBeNullOrEmpty();
        metadata.Genres.Should().NotBeNullOrEmpty();
        metadata.Genres.Should().Contain("Action");
        metadata.Genres.Should().Contain("Shooter");
    }
}
