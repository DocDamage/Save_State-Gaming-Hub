using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Common.Constants;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.Configuration;
using SaveState.Core.GameLibrary;
using SaveState.Core.Common.Services;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Implementation of cloud gaming manager with support for multiple providers.
/// </summary>
public class CloudGamingManager : ICloudGamingManager
{
    private readonly IGameRepository _gameRepository;
    private readonly INetworkQualityMonitor _networkQualityMonitor;
    private readonly ICloudCatalogService _cloudCatalogService;
    private readonly ILogger<CloudGamingManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly CloudGamingOptions _options;

    // In-memory storage for active sessions (can be replaced with repository)
    private readonly Dictionary<Guid, CloudSession> _activeSessions = new();

    // User overrides for cloud availability (gameId -> provider -> isAvailable)
    private readonly Dictionary<Guid, Dictionary<CloudGamingProvider, bool>> _userCloudOverrides = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudGamingManager"/> class.
    /// </summary>
    /// <param name="gameRepository">Repository for accessing game data.</param>
    /// <param name="networkQualityMonitor">Service for monitoring network quality.</param>
    /// <param name="cloudCatalogService">Service for cloud gaming catalog lookups.</param>
    /// <param name="options">Cloud gaming configuration options.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public CloudGamingManager(
        IGameRepository gameRepository,
        INetworkQualityMonitor networkQualityMonitor,
        ICloudCatalogService cloudCatalogService,
        IOptions<CloudGamingOptions> options,
        ILogger<CloudGamingManager> logger,
        ITimeProvider timeProvider)
    {
        _gameRepository = gameRepository;
        _networkQualityMonitor = networkQualityMonitor;
        _cloudCatalogService = cloudCatalogService;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;

        // Log configuration status on initialization
        LogConfigurationStatus();
    }

    private void LogConfigurationStatus()
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Cloud Gaming is disabled in configuration");
            return;
        }

        _logger.LogInformation("Cloud Gaming enabled - Default provider: {Provider}", _options.DefaultProvider);

        var enabledProviders = new List<string>();
        if (_options.GeForceNow.Enabled) enabledProviders.Add("GeForce NOW");
        if (_options.XboxCloud.Enabled) enabledProviders.Add("Xbox Cloud");
        if (_options.AmazonLuna.Enabled && !string.IsNullOrEmpty(_options.AmazonLuna.ApiKey)) enabledProviders.Add("Amazon Luna");
        if (_options.PlayStationNow.Enabled) enabledProviders.Add("PlayStation Now");
        if (_options.ShadowPC.Enabled && !string.IsNullOrEmpty(_options.ShadowPC.ApiKey)) enabledProviders.Add("Shadow PC");

        if (enabledProviders.Any())
        {
            _logger.LogInformation("Configured cloud gaming providers: {Providers}", string.Join(", ", enabledProviders));
        }
        else
        {
            _logger.LogWarning("No cloud gaming providers are properly configured");
        }
    }

    /// <summary>
    /// Gets a list of all supported cloud gaming providers.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of available cloud gaming providers.</returns>
    public async Task<Result<IReadOnlyList<CloudGamingProvider>>> GetAvailableProvidersAsync(
        CancellationToken ct = default)
    {
        try
        {
            if (!_options.Enabled)
            {
                _logger.LogWarning("Cloud gaming is disabled - returning empty provider list");
                return Result.Success<IReadOnlyList<CloudGamingProvider>>(Array.Empty<CloudGamingProvider>());
            }

            // Return only configured and enabled providers
            var providers = new List<CloudGamingProvider>();

            if (_options.GeForceNow.Enabled)
            {
                providers.Add(CloudGamingProvider.GeForceNow);
            }

            if (_options.XboxCloud.Enabled && !string.IsNullOrEmpty(_options.XboxCloud.AccountEmail))
            {
                providers.Add(CloudGamingProvider.XboxCloud);
            }

            if (_options.AmazonLuna.Enabled && !string.IsNullOrEmpty(_options.AmazonLuna.ApiKey))
            {
                providers.Add(CloudGamingProvider.AmazonLuna);
            }

            if (_options.PlayStationNow.Enabled && !string.IsNullOrEmpty(_options.PlayStationNow.AccountId))
            {
                providers.Add(CloudGamingProvider.PlayStationNow);
            }

            if (_options.ShadowPC.Enabled && !string.IsNullOrEmpty(_options.ShadowPC.ApiKey))
            {
                // Add Shadow PC as a custom/other provider - not in the enum yet
                // providers.Add(CloudGamingProvider.ShadowPC);
            }

            // If no providers are configured, return all as potential options
            if (!providers.Any())
            {
                _logger.LogInformation("No cloud gaming providers configured - returning all options");
                providers.AddRange(new[]
                {
                    CloudGamingProvider.GeForceNow,
                    CloudGamingProvider.XboxCloud,
                    CloudGamingProvider.AmazonLuna,
                    CloudGamingProvider.PlayStationNow,
                    CloudGamingProvider.Boosteroid
                });
            }

            _logger.LogInformation("Retrieved {Count} available cloud gaming providers", providers.Count);
            return Result.Success<IReadOnlyList<CloudGamingProvider>>(providers.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available cloud gaming providers");
            return Result.Failure<IReadOnlyList<CloudGamingProvider>>(ErrorMessages.OperationFailed);
        }
    }

    /// <summary>
    /// Validates the configuration for a specific cloud gaming provider.
    /// </summary>
    /// <param name="provider">The provider to validate.</param>
    /// <returns>A result indicating whether the provider is properly configured.</returns>
    private Result ValidateProviderConfiguration(CloudGamingProvider provider)
    {
        if (!_options.Enabled)
        {
            return Result.Failure($"{ErrorMessages.CloudGaming}: {ErrorMessages.FeatureNotEnabled}. Enable it in appsettings.json under {ConfigurationKeys.CloudGamingEnabled}");
        }

        switch (provider)
        {
            case CloudGamingProvider.GeForceNow:
                if (!_options.GeForceNow.Enabled)
                {
                    return Result.Failure($"{ErrorMessages.CloudProviderNotEnabled}. Set {ConfigurationKeys.GeForceNowEnabled} to true");
                }
                // GeForce NOW doesn't require API key for basic usage
                return Result.Success();

            case CloudGamingProvider.XboxCloud:
                if (!_options.XboxCloud.Enabled)
                {
                    return Result.Failure($"{ErrorMessages.CloudProviderNotEnabled}. Set {ConfigurationKeys.XboxCloudEnabled} to true");
                }
                if (string.IsNullOrEmpty(_options.XboxCloud.AccountEmail))
                {
                    return Result.Failure($"{ErrorMessages.CloudProviderNotConfigured}. Set {ConfigurationKeys.XboxCloudAccountEmail}");
                }
                if (!_options.XboxCloud.HasGamePassUltimate)
                {
                    return Result.Failure("Xbox Cloud Gaming requires Xbox Game Pass Ultimate subscription");
                }
                return Result.Success();

            case CloudGamingProvider.AmazonLuna:
                if (!_options.AmazonLuna.Enabled)
                {
                    return Result.Failure($"{ErrorMessages.CloudProviderNotEnabled}. Set {ConfigurationKeys.AmazonLunaEnabled} to true");
                }
                if (string.IsNullOrEmpty(_options.AmazonLuna.ApiKey))
                {
                    return Result.Failure($"{ErrorMessages.CloudProviderNotConfigured}. Set {ConfigurationKeys.AmazonLunaApiKey} or use environment variable {EnvironmentVariables.AmazonLunaApiKey}");
                }
                return Result.Success();

            case CloudGamingProvider.PlayStationNow:
                if (!_options.PlayStationNow.Enabled)
                {
                    return Result.Failure($"{ErrorMessages.CloudProviderNotEnabled}. Set {ConfigurationKeys.PlayStationNowEnabled} to true");
                }
                if (string.IsNullOrEmpty(_options.PlayStationNow.AccountId))
                {
                    return Result.Failure($"{ErrorMessages.CloudProviderNotConfigured}. Set {ConfigurationKeys.PlayStationNowAccountId}");
                }
                return Result.Success();

            case CloudGamingProvider.Boosteroid:
                // Boosteroid doesn't have specific configuration yet
                return Result.Success();

            default:
                return Result.Failure($"{ErrorMessages.InvalidValue}: {provider}");
        }
    }

    /// <summary>
    /// Starts a new cloud gaming session for the specified game and provider.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game to stream.</param>
    /// <param name="provider">The cloud gaming provider to use.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the created cloud session information.</returns>
    public async Task<Result<CloudSession>> StartSessionAsync(
        Guid gameId,
        CloudGamingProvider provider,
        CancellationToken ct = default)
    {
        try
        {
            // Validate provider configuration first
            var configValidation = ValidateProviderConfiguration(provider);
            if (!configValidation.IsSuccess)
            {
                _logger.LogWarning("Cannot start session - configuration invalid: {Error}", configValidation.Error);
                return Result.Failure<CloudSession>(configValidation.Error!);
            }

            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<CloudSession>(ErrorMessages.GameNotFound);
            }

            // Check if game is available on the provider
            var availabilityResult = await IsGameAvailableAsync(gameId, provider, ct)
                .ConfigureAwait(false);

            if (!availabilityResult.IsSuccess || !availabilityResult.Value)
            {
                return Result.Failure<CloudSession>(
                    $"Game '{game.Title}' is not available on {provider}");
            }

            // Check network quality
            var networkQuality = await GetNetworkQualityAsync(ct)
                .ConfigureAwait(false);

            if (!networkQuality.IsSuccess)
            {
                return Result.Failure<CloudSession>(
                    $"Failed to assess network quality: {networkQuality.Error}");
            }

            // Check if network quality meets minimum thresholds
            if (!MeetsMinimumQualityRequirements(networkQuality.Value))
            {
                _logger.LogWarning("Network quality below minimum requirements for cloud gaming - Latency: {Latency}ms, Bandwidth: {Bandwidth}Mbps",
                    networkQuality.Value.LatencyMs, networkQuality.Value.BandwidthMbps);
                
                return Result.Failure<CloudSession>(
                    $"Network quality does not meet minimum requirements. Latency: {networkQuality.Value.LatencyMs}ms (max {_options.NetworkMonitoring.MinimumLatencyMs}ms), " +
                    $"Bandwidth: {networkQuality.Value.BandwidthMbps}Mbps (min {_options.NetworkMonitoring.MinimumBandwidthMbps}Mbps)");
            }

            // Warn about poor quality but allow session to start
            if (networkQuality.Value.Level == QualityLevel.Poor)
            {
                _logger.LogWarning("Starting cloud gaming session with poor network quality for game {GameId}", gameId);
            }

            // Create new session
            var session = new CloudSession(
                Id: Guid.NewGuid(),
                GameId: gameId,
                Provider: provider,
                StartedAt: _timeProvider.UtcNow,
                InitialQuality: networkQuality.Value);

            // Store session
            _activeSessions[session.Id] = session;

            _logger.LogInformation("Started cloud gaming session {SessionId} for game {GameId} ({GameTitle}) on {Provider}",
                session.Id, gameId, game.Title, provider);

            return Result.Success<CloudSession>(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start cloud gaming session for game {GameId} on {Provider}",
                gameId, provider);
            return Result.Failure<CloudSession>($"Failed to start session: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if current network quality meets minimum requirements for cloud gaming.
    /// </summary>
    private bool MeetsMinimumQualityRequirements(NetworkQuality quality)
    {
        return quality.LatencyMs <= _options.NetworkMonitoring.MinimumLatencyMs &&
               quality.BandwidthMbps >= _options.NetworkMonitoring.MinimumBandwidthMbps &&
               quality.PacketLossPercent <= _options.NetworkMonitoring.MaximumPacketLossPercent;
    }

    /// <summary>
    /// Ends an active cloud gaming session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session to end.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> EndSessionAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result.Failure($"Cloud gaming session {sessionId} not found"));
            }

            // Remove from active sessions
            _activeSessions.Remove(sessionId);

            var duration = _timeProvider.UtcNow - session.StartedAt;
            _logger.LogInformation("Ended cloud gaming session {SessionId} after {Duration}",
                sessionId, duration);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end cloud gaming session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure($"Failed to end session: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets the current network quality metrics for cloud gaming.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the current network quality information.</returns>
    public async Task<Result<NetworkQuality>> GetNetworkQualityAsync(
        CancellationToken ct = default)
    {
        return await _networkQualityMonitor.GetCurrentQualityAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a list of all currently active cloud gaming sessions.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of active sessions.</returns>
    public Task<Result<IReadOnlyList<CloudSession>>> GetActiveSessionsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var sessions = _activeSessions.Values.ToArray();
            return Task.FromResult(Result.Success<IReadOnlyList<CloudSession>>(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active cloud gaming sessions");
            return Task.FromResult(Result.Failure<IReadOnlyList<CloudSession>>(
                $"Failed to get active sessions: {ex.Message}"));
        }
    }

    /// <summary>
    /// Optimizes network settings for the specified cloud gaming provider.
    /// </summary>
    /// <param name="provider">The cloud gaming provider to optimize for.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the optimization.</returns>
    public async Task<Result> OptimizeNetworkSettingsAsync(
        CloudGamingProvider provider,
        CancellationToken ct = default)
    {
        try
        {
            // Get network recommendations for this provider
            var recommendationsResult = await GetNetworkRecommendationsAsync(provider, ct)
                .ConfigureAwait(false);

            if (!recommendationsResult.IsSuccess)
            {
                return Result.Failure($"Failed to get recommendations: {recommendationsResult.Error}");
            }

            var recommendations = recommendationsResult.Value;

            // Apply optimizations based on recommendations
            // This is a placeholder - actual implementation would modify network settings
            foreach (var recommendation in recommendations)
            {
                _logger.LogInformation("Applying network optimization: {Recommendation}", recommendation);
            }

            _logger.LogInformation("Applied network optimizations for {Provider}", provider);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize network settings for {Provider}", provider);
            return Result.Failure($"Failed to optimize network: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a game is available for streaming on the specified cloud gaming provider.
    /// Uses a multi-tier lookup strategy: user override → Steam ID catalog → title catalog → heuristic fallback.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="provider">The cloud gaming provider to check.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing true if the game is available, false otherwise.</returns>
    public async Task<Result<bool>> IsGameAvailableAsync(
        Guid gameId,
        CloudGamingProvider provider,
        CancellationToken ct = default)
    {
        try
        {
            // Tier 0: Check user override first (highest priority)
            if (_userCloudOverrides.TryGetValue(gameId, out var providerOverrides) &&
                providerOverrides.TryGetValue(provider, out var userOverride))
            {
                _logger.LogDebug("Game {GameId} availability on {Provider}: {Available} (user override)",
                    gameId, provider, userOverride);
                return Result.Success(userOverride);
            }

            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<bool>($"Game with ID {gameId} not found");
            }

            string gameTitle = game.Title;

            // Tier 1: Try Steam App ID lookup for more accurate results
            if (game.Source == "Steam" && int.TryParse(game.SourceId, out var steamAppId) && steamAppId > 0)
            {
                var steamResult = await _cloudCatalogService.IsGameAvailableOnProviderBySteamIdAsync(
                    steamAppId, provider, ct).ConfigureAwait(false);

                if (steamResult.IsSuccess)
                {
                    _logger.LogDebug("Game '{Title}' (Steam:{SteamId}) availability on {Provider}: {Available} (from catalog by Steam ID)",
                        gameTitle, steamAppId, provider, steamResult.Value);
                    return steamResult;
                }
            }

            // Tier 2: Check the cloud catalog by game title
            var catalogResult = await _cloudCatalogService.IsGameAvailableOnProviderAsync(
                gameTitle, provider, ct).ConfigureAwait(false);

            if (catalogResult.IsSuccess)
            {
                _logger.LogDebug("Game '{Title}' availability on {Provider}: {Available} (from catalog by title)",
                    gameTitle, provider, catalogResult.Value);
                return catalogResult;
            }

            // Tier 3: Fallback to heuristic if all catalog lookups failed
            _logger.LogWarning("All catalog lookups failed for '{Title}', using fallback heuristic", gameTitle);
            var isAvailable = IsGameLikelyAvailableOnProvider(gameTitle, provider);

            _logger.LogDebug("Game '{Title}' availability on {Provider}: {Available} (heuristic fallback)",
                gameTitle, provider, isAvailable);

            return Result.Success(isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check game availability for game {GameId} on {Provider}",
                gameId, provider);
            return Result.Failure<bool>($"Failed to check availability: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets a user override for cloud gaming availability.
    /// This allows users to manually mark games as available or unavailable on specific providers.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="provider">The cloud gaming provider.</param>
    /// <param name="isAvailable">Whether the game is available on the provider.</param>
    /// <returns>A result indicating success.</returns>
    public Result SetCloudAvailabilityOverride(
        Guid gameId,
        CloudGamingProvider provider,
        bool isAvailable)
    {
        try
        {
            if (!_userCloudOverrides.TryGetValue(gameId, out var providerOverrides))
            {
                providerOverrides = new Dictionary<CloudGamingProvider, bool>();
                _userCloudOverrides[gameId] = providerOverrides;
            }

            providerOverrides[provider] = isAvailable;

            _logger.LogInformation("Set cloud availability override for game {GameId} on {Provider}: {Available}",
                gameId, provider, isAvailable);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cloud availability override for game {GameId}", gameId);
            return Result.Failure($"Failed to set override: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears a user override for cloud gaming availability.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="provider">The cloud gaming provider, or null to clear all overrides for the game.</param>
    /// <returns>A result indicating success.</returns>
    public Result ClearCloudAvailabilityOverride(
        Guid gameId,
        CloudGamingProvider? provider = null)
    {
        try
        {
            if (!_userCloudOverrides.TryGetValue(gameId, out var providerOverrides))
            {
                return Result.Success(); // Nothing to clear
            }

            if (provider.HasValue)
            {
                providerOverrides.Remove(provider.Value);
                _logger.LogInformation("Cleared cloud availability override for game {GameId} on {Provider}",
                    gameId, provider.Value);
            }
            else
            {
                _userCloudOverrides.Remove(gameId);
                _logger.LogInformation("Cleared all cloud availability overrides for game {GameId}", gameId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cloud availability override for game {GameId}", gameId);
            return Result.Failure($"Failed to clear override: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all user overrides for cloud gaming availability for a specific game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>A result containing a dictionary of provider overrides, or empty if none exist.</returns>
    public Result<IReadOnlyDictionary<CloudGamingProvider, bool>> GetCloudAvailabilityOverrides(Guid gameId)
    {
        try
        {
            if (_userCloudOverrides.TryGetValue(gameId, out var providerOverrides))
            {
                return Result.Success<IReadOnlyDictionary<CloudGamingProvider, bool>>(
                    new Dictionary<CloudGamingProvider, bool>(providerOverrides));
            }

            return Result.Success<IReadOnlyDictionary<CloudGamingProvider, bool>>(
                new Dictionary<CloudGamingProvider, bool>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cloud availability overrides for game {GameId}", gameId);
            return Result.Failure<IReadOnlyDictionary<CloudGamingProvider, bool>>(
                $"Failed to get overrides: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets network optimization recommendations for the specified cloud gaming provider.
    /// </summary>
    /// <param name="provider">The cloud gaming provider to get recommendations for.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing a list of network optimization recommendations.</returns>
    public async Task<Result<IReadOnlyList<string>>> GetNetworkRecommendationsAsync(
        CloudGamingProvider provider,
        CancellationToken ct = default)
    {
        try
        {
            // Get current network quality
            var qualityResult = await _networkQualityMonitor.GetCurrentQualityAsync(ct)
                .ConfigureAwait(false);

            if (!qualityResult.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<string>>(
                    $"Failed to get network quality: {qualityResult.Error}");
            }

            var quality = qualityResult.Value;
            var recommendations = new List<string>();

            // Generate recommendations based on provider requirements and current quality
            if (quality.LatencyMs > 50)
            {
                recommendations.Add("Reduce latency by connecting to a closer server or using a wired connection");
            }

            if (quality.PacketLossPercent > 1)
            {
                recommendations.Add("Reduce packet loss by checking your internet connection stability");
            }

            if (quality.JitterMs > 20)
            {
                recommendations.Add("Reduce jitter by avoiding bandwidth-intensive activities during gaming");
            }

            if (quality.BandwidthMbps < 25)
            {
                recommendations.Add("Increase bandwidth to at least 25 Mbps for optimal cloud gaming performance");
            }

            // Provider-specific recommendations
            switch (provider)
            {
                case CloudGamingProvider.GeForceNow:
                    recommendations.Add("GeForce Now recommends 50+ Mbps for 4K gaming");
                    break;
                case CloudGamingProvider.XboxCloud:
                    recommendations.Add("Xbox Cloud Gaming works best with 10+ Mbps and <100ms latency");
                    break;
                case CloudGamingProvider.AmazonLuna:
                    recommendations.Add("Amazon Luna requires stable 10+ Mbps connection");
                    break;
            }

            if (!recommendations.Any())
            {
                recommendations.Add("Your network quality is good for cloud gaming");
            }

            return Result.Success<IReadOnlyList<string>>(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get network recommendations for {Provider}", provider);
            return Result.Failure<IReadOnlyList<string>>(
                $"Failed to get recommendations: {ex.Message}");
        }
    }

    private static bool IsGameLikelyAvailableOnProvider(string gameTitle, CloudGamingProvider provider)
    {
        // Placeholder logic - in a real implementation, this would query provider APIs
        // For demo purposes, assume popular games are available
        var popularGames = new[]
        {
            "cyberpunk 2077", "the witcher 3", "red dead redemption 2",
            "god of war", "spider-man", "horizon zero dawn", "death stranding",
            "control", "alan wake 2", "forza horizon", "gears 5"
        };

        var normalizedTitle = gameTitle.ToLowerInvariant();
        var isPopularGame = popularGames.Any(popular =>
            normalizedTitle.Contains(popular) ||
            popular.Contains(normalizedTitle));

        // Xbox Cloud has more Microsoft-published games
        if (provider == CloudGamingProvider.XboxCloud)
        {
            return isPopularGame;
        }

        // GeForce Now has the broadest library
        if (provider == CloudGamingProvider.GeForceNow)
        {
            return true; // Most games are available
        }

        // Amazon Luna has curated selection
        if (provider == CloudGamingProvider.AmazonLuna)
        {
            return isPopularGame;
        }

        // PlayStation Now has Sony games
        if (provider == CloudGamingProvider.PlayStationNow)
        {
            return normalizedTitle.Contains("god of war") ||
                   normalizedTitle.Contains("spider-man") ||
                   normalizedTitle.Contains("horizon") ||
                   normalizedTitle.Contains("death stranding");
        }

        // Boosteroid has good coverage for popular games
        return isPopularGame;
    }
}
