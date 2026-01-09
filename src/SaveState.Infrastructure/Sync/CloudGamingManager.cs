using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
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
    private readonly ILogger<CloudGamingManager> _logger;

    // In-memory storage for active sessions (can be replaced with repository)
    private readonly Dictionary<Guid, CloudSession> _activeSessions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudGamingManager"/> class.
    /// </summary>
    /// <param name="gameRepository">Repository for accessing game data.</param>
    /// <param name="networkQualityMonitor">Service for monitoring network quality.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public CloudGamingManager(
        IGameRepository gameRepository,
        INetworkQualityMonitor networkQualityMonitor,
        ILogger<CloudGamingManager> logger)
    {
        _gameRepository = gameRepository;
        _networkQualityMonitor = networkQualityMonitor;
        _logger = logger;
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
            // Return all supported providers
            var providers = new[]
            {
                CloudGamingProvider.GeForceNow,
                CloudGamingProvider.XboxCloud,
                CloudGamingProvider.AmazonLuna,
                CloudGamingProvider.PlayStationNow,
                CloudGamingProvider.Boosteroid
            };

            _logger.LogInformation("Retrieved {Count} available cloud gaming providers", providers.Length);
            return Result.Success<IReadOnlyList<CloudGamingProvider>>(providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available cloud gaming providers");
            return Result.Failure<IReadOnlyList<CloudGamingProvider>>(
                $"Failed to get providers: {ex.Message}");
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
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<CloudSession>($"Game with ID {gameId} not found");
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

            // Check if network quality is sufficient
            if (networkQuality.Value.Level == QualityLevel.Poor)
            {
                _logger.LogWarning("Starting cloud gaming session with poor network quality for game {GameId}", gameId);
            }

            // Create new session
            var session = new CloudSession(
                Id: Guid.NewGuid(),
                GameId: gameId,
                Provider: provider,
                StartedAt: DateTime.UtcNow,
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

            var duration = DateTime.UtcNow - session.StartedAt;
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
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<bool>($"Game with ID {gameId} not found");
            }

            // Placeholder logic - in a real implementation, this would query the provider's API
            // For now, assume major games are available on major providers
            var isAvailable = IsGameLikelyAvailableOnProvider(game.Title, provider);

            _logger.LogDebug("Game '{Title}' availability on {Provider}: {Available}",
                game.Title, provider, isAvailable);

            return Result.Success<bool>(isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check game availability for game {GameId} on {Provider}",
                gameId, provider);
            return Result.Failure<bool>($"Failed to check availability: {ex.Message}");
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

