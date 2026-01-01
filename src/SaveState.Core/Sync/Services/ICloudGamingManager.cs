using SaveState.Core.Common;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Core.Sync.Services;

/// <summary>
/// Service for managing cloud gaming sessions and providers.
/// </summary>
public interface ICloudGamingManager
{
    /// <summary>
    /// Gets all available cloud gaming providers.
    /// </summary>
    Task<Result<IReadOnlyList<CloudGamingProvider>>> GetAvailableProvidersAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Starts a cloud gaming session for the specified game.
    /// </summary>
    Task<Result<CloudSession>> StartSessionAsync(
        Guid gameId,
        CloudGamingProvider provider,
        CancellationToken ct = default);

    /// <summary>
    /// Ends an active cloud gaming session.
    /// </summary>
    Task<Result> EndSessionAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current network quality metrics.
    /// </summary>
    Task<Result<NetworkQuality>> GetNetworkQualityAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active cloud gaming sessions.
    /// </summary>
    Task<Result<IReadOnlyList<CloudSession>>> GetActiveSessionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Optimizes network settings for cloud gaming.
    /// </summary>
    Task<Result> OptimizeNetworkSettingsAsync(
        CloudGamingProvider provider,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a game is available on the specified cloud gaming provider.
    /// </summary>
    Task<Result<bool>> IsGameAvailableAsync(
        Guid gameId,
        CloudGamingProvider provider,
        CancellationToken ct = default);

    /// <summary>
    /// Gets network optimization recommendations for cloud gaming.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetNetworkRecommendationsAsync(
        CloudGamingProvider provider,
        CancellationToken ct = default);
}