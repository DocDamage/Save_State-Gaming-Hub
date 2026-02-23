using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.IntegrationTests;

/// <summary>
/// Integration tests for cloud gaming functionality.
/// Tests cloud provider connections, game library sync, session management, and network quality monitoring.
/// </summary>
public class CloudGamingIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ICloudGamingManager _cloudGamingManager;
    private readonly ICloudCatalogService _cloudCatalogService;
    private readonly INetworkQualityMonitor _networkMonitor;

    public CloudGamingIntegrationTests(IntegrationTestFixture fixture)
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
    public async Task GetAvailableProviders_ReturnsKnownProviders()
    {
        // Act
        var result = await _cloudGamingManager.GetAvailableProvidersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(p => 
            p.Name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("Xbox", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("Luna", StringComparison.OrdinalIgnoreCase));
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
        // Arrange
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

    [Fact]
    public async Task GetProviderById_ReturnsSpecificProvider()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var providerId = providers.Value.First().Id;

        // Act
        var result = await _cloudGamingManager.GetProviderByIdAsync(providerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(providerId);
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
    public async Task SyncGameLibrary_WithoutConnection_ReturnsFailure()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        // Don't connect

        // Act
        var result = await _cloudCatalogService.SyncGameLibraryAsync(provider.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
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
    public async Task SearchCloudGames_EmptyQuery_ReturnsAllGames()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        await _cloudCatalogService.SyncGameLibraryAsync(provider.Id);

        // Act
        var result = await _cloudCatalogService.SearchCloudGamesAsync("");

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
    public async Task GetGameDetails_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidGameId = "invalid_game_id";

        // Act
        var result = await _cloudCatalogService.GetGameDetailsAsync(invalidGameId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
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

    [Fact]
    public async Task GetFavorites_ReturnsFavoriteGames()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);

        // Act
        var result = await _cloudCatalogService.GetFavoritesAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task AddToFavorites_AddsGameToFavorites()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        
        if (games.IsSuccess && games.Value.Count > 0)
        {
            var gameId = games.Value.First().Id;

            // Act
            var result = await _cloudCatalogService.AddToFavoritesAsync(provider.Id, gameId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task RemoveFromFavorites_RemovesGameFromFavorites()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        
        if (games.IsSuccess && games.Value.Count > 0)
        {
            var gameId = games.Value.First().Id;
            await _cloudCatalogService.AddToFavoritesAsync(provider.Id, gameId);

            // Act
            var result = await _cloudCatalogService.RemoveFromFavoritesAsync(provider.Id, gameId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
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
    public async Task StartCloudSession_WithoutConnection_ReturnsFailure()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        // Don't connect

        // Act
        var result = await _cloudGamingManager.StartCloudSessionAsync(
            provider.Id, 
            "game_123", 
            quality: StreamQuality.High);

        // Assert
        result.IsFailure.Should().BeTrue();
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
    public async Task GetSession_ReturnsSpecificSession()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();
        
        if (games.IsSuccess && games.Value.Count > 0)
        {
            var sessionResult = await _cloudGamingManager.StartCloudSessionAsync(
                provider.Id, games.Value.First().Id, quality: StreamQuality.High);
            sessionResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _cloudGamingManager.GetSessionAsync(sessionResult.Value.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(sessionResult.Value.Id);
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

    [Fact]
    public async Task PauseCloudSession_PausesActiveSession()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();
            var sessionResult = await _cloudGamingManager.StartCloudSessionAsync(provider.Id, game.Id, quality: StreamQuality.High);
            sessionResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _cloudGamingManager.PauseCloudSessionAsync(sessionResult.Value.Id);

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

    [Fact]
    public async Task GetAvailableQualities_ReturnsQualityOptions()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.GetAvailableQualitiesAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetMaxBitrate_UpdatesBitrate()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();
            var sessionResult = await _cloudGamingManager.StartCloudSessionAsync(provider.Id, game.Id, quality: StreamQuality.High);
            sessionResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _cloudGamingManager.SetMaxBitrateAsync(sessionResult.Value.Id, 50000);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
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

    [Fact]
    public async Task GetBandwidth_ReturnsBandwidthEstimate()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.GetBandwidthAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(0);
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

    [Fact]
    public async Task GetNetworkRecommendations_ReturnsSuggestions()
    {
        // Act
        var result = await _networkMonitor.GetRecommendationsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
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

    [Fact]
    public async Task GetDataCenterLatency_ReturnsLatencyToSpecificDataCenter()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        var dataCenters = await _cloudGamingManager.GetAvailableDataCentersAsync(provider.Id);

        if (dataCenters.IsSuccess && dataCenters.Value.Count > 0)
        {
            var dataCenterId = dataCenters.Value.First().Id;

            // Act
            var result = await _cloudGamingManager.GetDataCenterLatencyAsync(provider.Id, dataCenterId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeGreaterThanOrEqualTo(0);
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

    [Fact]
    public async Task DownloadSaveState_DownloadsFromCloud()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);

        // Act
        var result = await _cloudGamingManager.DownloadSaveStateFromCloudAsync(
            provider.Id, 
            "game_123", 
            "save_state_id");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCloudSaveState_RemovesFromCloud()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);

        // Act
        var result = await _cloudGamingManager.DeleteCloudSaveStateAsync(
            provider.Id, 
            "game_123", 
            "save_state_id");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Keyboard/Mouse Mapping Tests

    [Fact]
    public async Task GetInputMappings_ReturnsCurrentMappings()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.GetInputMappingsAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateInputMapping_UpdatesMapping()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        var mapping = new InputMapping
        {
            Action = "Jump",
            Key = "Space",
            AltKey = "GamepadA"
        };

        // Act
        var result = await _cloudGamingManager.UpdateInputMappingAsync(provider.Id, mapping);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResetInputMappings_RestoresDefaults()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();

        // Act
        var result = await _cloudGamingManager.ResetInputMappingsAsync(provider.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task StartSession_WithInvalidGameId_ReturnsNotFound()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);

        // Act
        var result = await _cloudGamingManager.StartCloudSessionAsync(
            provider.Id, 
            "invalid_game_id", 
            quality: StreamQuality.High);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetSession_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidSessionId = "invalid_session_id";

        // Act
        var result = await _cloudGamingManager.GetSessionAsync(invalidSessionId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ChangeQuality_WithoutActiveSession_ReturnsFailure()
    {
        // Arrange
        var inactiveSessionId = "inactive_session";

        // Act
        var result = await _cloudGamingManager.ChangeStreamQualityAsync(
            inactiveSessionId, 
            StreamQuality.High);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task SyncLibrary_PerformsEfficiently()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _cloudCatalogService.SyncGameLibraryAsync(provider.Id);

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    [Fact]
    public async Task SearchGames_PerformsEfficiently()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        await _cloudCatalogService.SyncGameLibraryAsync(provider.Id);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _cloudCatalogService.SearchCloudGamesAsync("test");

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public async Task StartSession_RespondsQuickly()
    {
        // Arrange
        var providers = await _cloudGamingManager.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();
        var provider = providers.Value.First();
        await _cloudGamingManager.ConnectToProviderAsync(provider.Id, "test_token", null);
        var games = await _cloudCatalogService.GetCloudGamesAsync();

        if (games.Value.Count > 0)
        {
            var game = games.Value.First();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await _cloudGamingManager.StartCloudSessionAsync(provider.Id, game.Id, quality: StreamQuality.High);

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
        }
    }

    #endregion
}

#region Supporting Types

public interface ICloudGamingManager
{
    Task<Result<List<CloudProvider>>> GetAvailableProvidersAsync();
    Task<Result<CloudProvider>> GetProviderByIdAsync(string providerId);
    Task<Result<bool>> ConnectToProviderAsync(string providerId, string accessToken, string? refreshToken);
    Task<Result<bool>> DisconnectFromProviderAsync(string providerId);
    Task<Result<ProviderStatus>> GetProviderStatusAsync(string providerId);
    Task<Result<bool>> IsProviderConnectedAsync(string providerId);

    Task<Result<CloudSession>> StartCloudSessionAsync(string providerId, string gameId, StreamQuality quality);
    Task<Result<bool>> StopCloudSessionAsync(string sessionId);
    Task<Result<CloudSession>> GetSessionAsync(string sessionId);
    Task<Result<CloudSession?>> GetActiveSessionAsync();
    Task<Result<bool>> PauseCloudSessionAsync(string sessionId);
    Task<Result<bool>> ResumeCloudSessionAsync(string sessionId);

    Task<Result<bool>> ChangeStreamQualityAsync(string sessionId, StreamQuality quality);
    Task<Result<StreamQuality>> GetRecommendedQualityAsync();
    Task<Result<List<StreamQuality>>> GetAvailableQualitiesAsync(string providerId);
    Task<Result<bool>> SetMaxBitrateAsync(string sessionId, int bitrateKbps);

    Task<Result<ConnectionTestResult>> TestConnectionAsync(string providerId);
    Task<Result<ConnectionMetrics>> GetConnectionMetricsAsync(string sessionId);
    Task<Result<int>> GetLatencyAsync(string providerId);
    Task<Result<double>> GetPacketLossAsync(string providerId);
    Task<Result<int>> GetBandwidthAsync(string providerId);

    Task<Result<List<DataCenter>>> GetAvailableDataCentersAsync(string providerId);
    Task<Result<DataCenter>> GetNearestDataCenterAsync(string providerId);
    Task<Result<bool>> SelectDataCenterAsync(string providerId, string dataCenterId);
    Task<Result<int>> GetDataCenterLatencyAsync(string providerId, string dataCenterId);

    Task<Result<List<CloudSaveState>>> GetCloudSaveStatesAsync(string providerId, string gameId);
    Task<Result<bool>> SyncSaveStateToCloudAsync(string providerId, string gameId, string saveData);
    Task<Result<string>> DownloadSaveStateFromCloudAsync(string providerId, string gameId, string saveStateId);
    Task<Result<bool>> DeleteCloudSaveStateAsync(string providerId, string gameId, string saveStateId);

    Task<Result<List<InputMapping>>> GetInputMappingsAsync(string providerId);
    Task<Result<bool>> UpdateInputMappingAsync(string providerId, InputMapping mapping);
    Task<Result<bool>> ResetInputMappingsAsync(string providerId);
}

public interface ICloudCatalogService
{
    Task<Result<SyncResult>> SyncGameLibraryAsync(string providerId);
    Task<Result<List<CloudGame>>> GetCloudGamesAsync();
    Task<Result<List<CloudGame>>> GetCloudGamesByProviderAsync(string providerId);
    Task<Result<List<CloudGame>>> SearchCloudGamesAsync(string query);
    Task<Result<CloudGameDetails>> GetGameDetailsAsync(string gameId);
    Task<Result<List<CloudGame>>> GetRecentlyPlayedAsync(string providerId, int count);
    Task<Result<List<CloudGame>>> GetFavoritesAsync(string providerId);
    Task<Result<bool>> AddToFavoritesAsync(string providerId, string gameId);
    Task<Result<bool>> RemoveFromFavoritesAsync(string providerId, string gameId);
}

public interface INetworkQualityMonitor
{
    Task<Result<NetworkQuality>> MeasureNetworkQualityAsync();
    Task<Result<NetworkStatus>> GetNetworkStatusAsync();
    Task<Result<bool>> IsNetworkSuitableForCloudGamingAsync();
    Task<Result<List<NetworkRecommendation>>> GetRecommendationsAsync();

    event EventHandler<NetworkQualityChangedEventArgs>? NetworkQualityChanged;
}

public record CloudProvider
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
    public List<StreamQuality> SupportedQualities { get; set; } = new();
}

public record ProviderStatus
{
    public string ProviderId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string? UserDisplayName { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}

public record CloudSession
{
    public string Id { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public StreamQuality Quality { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public enum StreamQuality
{
    Low,
    Medium,
    High,
    Ultra
}

public record ConnectionTestResult
{
    public bool Success { get; set; }
    public int LatencyMs { get; set; }
    public double PacketLoss { get; set; }
    public int BandwidthKbps { get; set; }
    public string? Error { get; set; }
}

public record ConnectionMetrics
{
    public int CurrentLatencyMs { get; set; }
    public double AverageLatencyMs { get; set; }
    public double PacketLoss { get; set; }
    public int CurrentBitrateKbps { get; set; }
    public int FramesPerSecond { get; set; }
    public DateTime Timestamp { get; set; }
}

public record DataCenter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public record CloudGame
{
    public string Id { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public List<string> Genres { get; set; } = new();
    public DateTime? LastPlayedAt { get; set; }
}

public record CloudGameDetails : CloudGame
{
    public string Description { get; set; } = string.Empty;
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public int? MetacriticScore { get; set; }
    public List<string> Screenshots { get; set; } = new();
    public List<StreamQuality> AvailableQualities { get; set; } = new();
}

public record CloudSaveState
{
    public string Id { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public string? Description { get; set; }
}

public record InputMapping
{
    public string Action { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? AltKey { get; set; }
}

public record SyncResult
{
    public int GamesAdded { get; set; }
    public int GamesUpdated { get; set; }
    public int GamesRemoved { get; set; }
    public DateTime SyncedAt { get; set; }
}

public record NetworkQuality
{
    public int DownloadSpeedKbps { get; set; }
    public int UploadSpeedKbps { get; set; }
    public int LatencyMs { get; set; }
    public double PacketLoss { get; set; }
    public double JitterMs { get; set; }
    public NetworkGrade Grade { get; set; }
}

public enum NetworkGrade
{
    Excellent,
    Good,
    Fair,
    Poor,
    Unsuitable
}

public record NetworkStatus
{
    public bool IsConnected { get; set; }
    public string? CurrentNetworkType { get; set; }
    public NetworkQuality? CurrentQuality { get; set; }
}

public record NetworkRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public RecommendationPriority Priority { get; set; }
}

public enum RecommendationPriority
{
    Info,
    Suggestion,
    Important,
    Critical
}

public class NetworkQualityChangedEventArgs : EventArgs
{
    public NetworkQuality NewQuality { get; set; } = new();
    public NetworkQuality? PreviousQuality { get; set; }
    public DateTime Timestamp { get; set; }
}

#endregion
