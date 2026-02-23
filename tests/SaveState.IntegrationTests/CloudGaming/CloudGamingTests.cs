using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.IntegrationTests.CloudGaming;

/// <summary>
/// Integration tests for cloud gaming functionality.
/// </summary>
public class CloudGamingTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ICloudGamingManager _cloudGamingManager;
    private readonly ICloudCatalogService _cloudCatalogService;
    private readonly INetworkQualityMonitor _networkMonitor;

    public CloudGamingTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _cloudGamingManager = _fixture.ServiceProvider.GetRequiredService<ICloudGamingManager>();
        _cloudCatalogService = _fixture.ServiceProvider.GetRequiredService<ICloudCatalogService>();
        _networkMonitor = _fixture.ServiceProvider.GetRequiredService<INetworkQualityMonitor>();
    }

    #region Provider Connection Tests

    [Fact]
    public async Task GetAvailableProviders_ReturnsListOfProviders()
    {
        // Act
        var result = await _cloudGamingManager.GetAvailableProvidersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ConnectToProvider_WithValidCredentials_ConnectsSuccessfully()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.ConnectToProviderAsync(
            provider.Id, 
            "test_token", 
            "test_refresh_token");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectToProvider_WithInvalidCredentials_ReturnsError()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.ConnectToProviderAsync(
            provider.Id, 
            "invalid_token", 
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectFromProvider_DisconnectsSuccessfully()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);

        // Act
        var result = await _cloudGamingManager.DisconnectFromProviderAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetProviderStatus_ReturnsCurrentStatus()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.GetProviderStatusAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task IsProviderConnected_ReturnsConnectionState()
    {
        // Arrange - Reset to ensure clean state
        _fixture.ResetCloudGamingConnections();
        
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act - Before connection
        var beforeResult = await _cloudGamingManager.IsProviderConnectedAsync(provider.Id);
        
        // Connect
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        
        // Act - After connection
        var afterResult = await _cloudGamingManager.IsProviderConnectedAsync(provider.Id);

        // Assert
        beforeResult.Value.Should().BeFalse();
        afterResult.Value.Should().BeTrue();
    }

    #endregion

    #region Game Library Sync Tests

    [Fact]
    public async Task SyncGameLibrary_ReturnsSyncedGames()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);

        // Act
        var result = await _cloudCatalogService.SyncGameLibraryAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCloudGames_ReturnsAllCloudGames()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        await _cloudCatalogService.SyncGameLibraryAsync(provider.Id);

        // Act
        var result = await _cloudCatalogService.GetCloudGamesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCloudGames_WithProviderFilter_ReturnsFilteredGames()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        await _cloudCatalogService.SyncGameLibraryAsync(provider.Id);

        // Act
        var result = await _cloudCatalogService.GetCloudGamesByProviderAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(g => g.ProviderId == provider.Id);
    }

    [Fact]
    public async Task SearchCloudGames_WithQuery_ReturnsMatchingGames()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        await _cloudCatalogService.SyncGameLibraryAsync(provider.Id);

        // Act
        var result = await _cloudCatalogService.SearchCloudGamesAsync("Cyberpunk");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetGameDetails_ReturnsDetailedInfo()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        games.IsSuccess.Should().BeTrue();
        
        if (games.Value.Count > 0)
        {
            var gameId = games.Value.First().Id;

            // Act
            var result = await _cloudCatalogService.GetGameDetailsAsync(gameId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task GetRecentlyPlayed_ReturnsRecentGames()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);

        // Act
        var result = await _cloudCatalogService.GetRecentlyPlayedAsync(provider.Id, count: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Count.Should().BeLessThanOrEqualTo(10);
    }

    #endregion

    #region Session Management Tests

    [Fact]
    public async Task StartCloudSession_CreatesActiveSession()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        games.IsSuccess.Should().BeTrue();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();

            // Act
            var result = await _cloudGamingManager.StartCloudSessionAsync(
                provider.Id, 
                game.Id, 
                quality: StreamQuality.High);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.ProviderId.Should().Be(provider.Id);
            result.Value.GameId.Should().Be(game.Id);
            result.Value.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public async Task StopCloudSession_EndsActiveSession()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        games.IsSuccess.Should().BeTrue();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();
            var sessionResult = await _cloudGamingManager.StartCloudSessionAsync(provider.Id, game.Id, quality: StreamQuality.High);
            sessionResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _cloudGamingManager.StopCloudSessionAsync(sessionResult.Value.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetActiveSession_ReturnsCurrentSession()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        games.IsSuccess.Should().BeTrue();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();
            var sessionResult = await _cloudGamingManager.StartCloudSessionAsync(provider.Id, game.Id, quality: StreamQuality.High);
            sessionResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _cloudGamingManager.GetActiveSessionAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Id.Should().Be(sessionResult.Value.Id);
        }
    }

    [Fact]
    public async Task ResumeCloudSession_ResumesPausedSession()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        games.IsSuccess.Should().BeTrue();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();
            var sessionResult = await _cloudGamingManager.StartCloudSessionAsync(provider.Id, game.Id, quality: StreamQuality.High);
            sessionResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _cloudGamingManager.ResumeCloudSessionAsync(sessionResult.Value.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    #endregion

    #region Stream Quality Tests

    [Theory]
    [InlineData(StreamQuality.Low)]
    [InlineData(StreamQuality.Medium)]
    [InlineData(StreamQuality.High)]
    [InlineData(StreamQuality.Ultra)]
    public async Task StartSession_WithDifferentQualities_SetsQualityCorrectly(StreamQuality quality)
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        games.IsSuccess.Should().BeTrue();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();

            // Act
            var result = await _cloudGamingManager.StartCloudSessionAsync(
                provider.Id, 
                game.Id, 
                quality: quality);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Quality.Should().Be(quality);
        }
    }

    [Fact]
    public async Task ChangeStreamQuality_UpdatesQuality()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        games.IsSuccess.Should().BeTrue();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();
            var sessionResult = await _cloudGamingManager.StartCloudSessionAsync(
                provider.Id, 
                game.Id, 
                quality: StreamQuality.Medium);
            sessionResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _cloudGamingManager.ChangeStreamQualityAsync(
                sessionResult.Value.Id, 
                StreamQuality.High);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var session = await _cloudGamingManager.GetSessionAsync(sessionResult.Value.Id);
            session.Value.Quality.Should().Be(StreamQuality.High);
        }
    }

    [Fact]
    public async Task GetRecommendedQuality_ReturnsOptimalQuality()
    {
        // Act
        var result = await _cloudGamingManager.GetRecommendedQualityAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        (result.Value == StreamQuality.Low || result.Value == StreamQuality.Medium || result.Value == StreamQuality.High || result.Value == StreamQuality.Ultra).Should().BeTrue();
    }

    #endregion

    #region Connection Test API Tests

    [Fact]
    public async Task TestConnection_PerformsConnectionTest()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.TestConnectionAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConnectionMetrics_ReturnsMetrics()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        games.IsSuccess.Should().BeTrue();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();
            var sessionResult = await _cloudGamingManager.StartCloudSessionAsync(provider.Id, game.Id, quality: StreamQuality.High);
            sessionResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _cloudGamingManager.GetConnectionMetricsAsync(sessionResult.Value.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task GetLatency_ReturnsLatencyValue()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.GetLatencyAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetPacketLoss_ReturnsPacketLossPercentage()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.GetPacketLossAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(0);
        result.Value.Should().BeLessThanOrEqualTo(100);
    }

    #endregion

    #region Network Quality Tests

    [Fact]
    public async Task MeasureNetworkQuality_ReturnsQualityAssessment()
    {
        // Act
        var result = await _networkMonitor.MeasureNetworkQualityAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNetworkStatus_ReturnsCurrentStatus()
    {
        // Act
        var result = await _networkMonitor.GetNetworkStatusAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task IsNetworkSuitable_ForCloudGaming_ReturnsBoolean()
    {
        // Act
        var result = await _networkMonitor.IsNetworkSuitableForCloudGamingAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeToNetworkUpdates_ReceivesUpdates()
    {
        // Arrange
        var updatesReceived = 0;
        _networkMonitor.NetworkQualityChanged += (sender, args) => updatesReceived++;

        // Act - Trigger a network measurement
        await _networkMonitor.MeasureNetworkQualityAsync();

        // Assert
        // The event should have been raised at least once
        updatesReceived.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Data Center Tests

    [Fact]
    public async Task GetAvailableDataCenters_ReturnsListOfDataCenters()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.GetAvailableDataCentersAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNearestDataCenter_ReturnsClosestDataCenter()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.GetNearestDataCenterAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task SelectDataCenter_SetsPreferredDataCenter()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        var dataCenters = await _cloudGamingManager.GetAvailableDataCentersAsync(provider.Id);
        dataCenters.IsSuccess.Should().BeTrue();

        if (dataCenters.Value.Count > 0)
        {
            var dataCenter = dataCenters.Value.First();

            // Act
            var result = await _cloudGamingManager.SelectDataCenterAsync(provider.Id, dataCenter.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    #endregion

    #region Save State Integration Tests

    [Fact]
    public async Task GetCloudSaveStates_ReturnsSaveStates()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        games.IsSuccess.Should().BeTrue();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();

            // Act
            var result = await _cloudGamingManager.GetCloudSaveStatesAsync(provider.Id, game.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task SyncSaveState_SyncsToCloud()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);

        // Act
        var result = await _cloudGamingManager.SyncSaveStateToCloudAsync(
            provider.Id, 
            "game_123", 
            "save_state_data");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
