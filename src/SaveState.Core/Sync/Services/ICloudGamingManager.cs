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

    /// <summary>
    /// Sets a user override for cloud gaming availability.
    /// This allows users to manually mark games as available or unavailable on specific providers.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="provider">The cloud gaming provider.</param>
    /// <param name="isAvailable">Whether the game is available on the provider.</param>
    /// <returns>A result indicating success.</returns>
    Result SetCloudAvailabilityOverride(
        Guid gameId,
        CloudGamingProvider provider,
        bool isAvailable);

    /// <summary>
    /// Clears a user override for cloud gaming availability.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="provider">The cloud gaming provider, or null to clear all overrides for the game.</param>
    /// <returns>A result indicating success.</returns>
    Result ClearCloudAvailabilityOverride(
        Guid gameId,
        CloudGamingProvider? provider = null);

    /// <summary>
    /// Gets all user overrides for cloud gaming availability for a specific game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>A result containing a dictionary of provider overrides, or empty if none exist.</returns>
    Result<IReadOnlyDictionary<CloudGamingProvider, bool>> GetCloudAvailabilityOverrides(Guid gameId);
}