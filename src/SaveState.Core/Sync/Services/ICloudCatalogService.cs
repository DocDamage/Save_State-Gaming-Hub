using SaveState.Core.Common;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Core.Sync.Services;

/// <summary>
/// Service for managing the cloud gaming catalog with hybrid local/remote data.
/// </summary>
public interface ICloudCatalogService
{
    /// <summary>
    /// Gets the current cloud gaming catalog.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cloud gaming catalog.</returns>
    Task<Result<CloudCatalog>> GetCatalogAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if a game is available on a specific cloud gaming provider.
    /// </summary>
    /// <param name="gameTitle">The title of the game.</param>
    /// <param name="provider">The cloud gaming provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the game is available on the provider.</returns>
    Task<Result<bool>> IsGameAvailableOnProviderAsync(
        string gameTitle,
        CloudGamingProvider provider,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a game is available on a specific cloud gaming provider by Steam App ID.
    /// </summary>
    /// <param name="steamAppId">The Steam application ID.</param>
    /// <param name="provider">The cloud gaming provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the game is available on the provider.</returns>
    Task<Result<bool>> IsGameAvailableOnProviderBySteamIdAsync(
        int steamAppId,
        CloudGamingProvider provider,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all providers where a game is available.
    /// </summary>
    /// <param name="gameTitle">The title of the game.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of providers where the game is available.</returns>
    Task<Result<IReadOnlyList<CloudGamingProvider>>> GetAvailableProvidersForGameAsync(
        string gameTitle,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all providers where a game is available by Steam App ID.
    /// </summary>
    /// <param name="steamAppId">The Steam application ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of providers where the game is available.</returns>
    Task<Result<IReadOnlyList<CloudGamingProvider>>> GetAvailableProvidersForGameBySteamIdAsync(
        int steamAppId,
        CancellationToken ct = default);

    /// <summary>
    /// Refreshes the catalog from the remote source if available.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the catalog was updated.</returns>
    Task<Result<bool>> RefreshCatalogAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the catalog entry for a specific game.
    /// </summary>
    /// <param name="gameTitle">The title of the game.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The catalog entry if found.</returns>
    Task<Result<CloudCatalogEntry?>> GetCatalogEntryAsync(
        string gameTitle,
        CancellationToken ct = default);
}
