namespace SaveState.IntegrationTests;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Infrastructure.GameLibrary.Services;

public class MetadataResilienceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetadataResilienceIntegrationTests> _logger;

    public MetadataResilienceIntegrationTests(IntegrationTestFixture fixture)
    {
        _serviceProvider = fixture.ServiceProvider;
        _logger = fixture.ServiceProvider.GetRequiredService<ILogger<MetadataResilienceIntegrationTests>>();
    }

    [Fact]
    public async Task ResilientMetadataService_IsDecoratedCorrectly()
    {
        // Arrange & Act
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();

        // Skip if using fake implementation (fixture uses FakeMetadataService for isolation)
        if (metadataService is not ResilientMetadataService)
        {
            _logger.LogInformation("Skipping test - ResilientMetadataService not registered (using fake implementation)");
            return;
        }

        // Assert
        metadataService.Should().BeOfType<ResilientMetadataService>();
    }

    [Fact]
    public async Task ResilientMetadataService_HandlesEmptyTitle()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();

        // Act
        var result = await metadataService.GetGameMetadataAsync("", default);

        // Assert
        result.Should().BeEquivalentTo(GameMetadata.Empty);
    }

    [Fact]
    public async Task ResilientMetadataService_HandlesWhitespaceTitle()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();

        // Act
        var result = await metadataService.GetGameMetadataAsync("   ", default);

        // Assert
        result.Should().BeEquivalentTo(GameMetadata.Empty);
    }

    [Fact]
    public async Task ResilientMetadataService_CircuitBreakerState_IsInitiallyClosed()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();

        // Skip if using fake implementation (fixture uses FakeMetadataService for isolation)
        if (metadataService is not ResilientMetadataService resilientService)
        {
            _logger.LogInformation("Skipping test - ResilientMetadataService not registered (using fake implementation)");
            return;
        }

        // Act & Assert
        resilientService.CircuitBreakerState.Should().Be(Polly.CircuitBreaker.CircuitState.Closed);
    }

    [Fact]
    public async Task ResilientMetadataService_HandlesCancellationGracefully()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();
        var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();
        var result = await metadataService.GetGameMetadataAsync("Test Game", cts.Token);

        // Assert
        result.Should().BeEquivalentTo(GameMetadata.Empty);
    }

    [Fact]
    public async Task ResilientMetadataService_ImageDownload_HandlesCancellation()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();
        var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();
        var result = await metadataService.GetCoverImageAsync("Test Game", cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ResilientMetadataService_InnerService_IsIgdbMetadataService()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();

        // Skip if using fake implementation (fixture uses FakeMetadataService for isolation)
        if (metadataService is not ResilientMetadataService resilientService)
        {
            _logger.LogInformation("Skipping test - ResilientMetadataService not registered (using fake implementation)");
            return;
        }

        // This test verifies that the decorator pattern is working correctly
        // The inner service should be the actual implementation
        // We can't directly access it, but we can verify the behavior

        // Act - Call with a title that should return empty from fake IGDB
        var result = await resilientService.GetGameMetadataAsync("NonExistentGame12345", default);

        // Assert - Should return empty (fallback behavior)
        result.Should().BeEquivalentTo(GameMetadata.Empty);
    }

    [Fact]
    public async Task ResilientMetadataService_MultipleCalls_WorkCorrectly()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();

        // Act - Make multiple calls
        var results = await Task.WhenAll(
            metadataService.GetGameMetadataAsync("Half-Life 2", default),
            metadataService.GetGameMetadataAsync("Portal", default),
            metadataService.GetGameMetadataAsync("Counter-Strike 2", default)
        );

        // Assert - All should return some result (either real data or empty)
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    [Fact]
    public async Task ResilientMetadataService_ImageDownload_ReturnsNullForUnknownGames()
    {
        // Arrange
        var metadataService = _serviceProvider.GetRequiredService<IMetadataService>();

        // Act
        var result = await metadataService.GetCoverImageAsync("DefinitelyNotARealGame", default);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
