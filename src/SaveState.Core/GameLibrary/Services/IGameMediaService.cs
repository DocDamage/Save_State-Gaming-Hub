using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Service for managing game media (screenshots, videos, artwork).
/// </summary>
public interface IGameMediaService
{
    /// <summary>
    /// Gets all media for a specific game.
    /// </summary>
    Task<Result<List<GameMedia>>> GetGameMediaAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Adds new media to a game.
    /// </summary>
    Task<Result<GameMedia>> AddMediaAsync(Guid gameId, string filePath, MediaType type, CancellationToken ct = default);

    /// <summary>
    /// Updates media metadata (title, description, favorite status).
    /// </summary>
    Task<Result> UpdateMediaAsync(Guid mediaId, string? title = null, string? description = null, bool? isFavorite = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes media from a game.
    /// </summary>
    Task<Result> DeleteMediaAsync(Guid mediaId, CancellationToken ct = default);

    /// <summary>
    /// Deletes multiple media items.
    /// </summary>
    Task<Result> DeleteMediaBatchAsync(IEnumerable<Guid> mediaIds, CancellationToken ct = default);

    /// <summary>
    /// Sets a media item as favorite.
    /// </summary>
    Task<Result> SetFavoriteAsync(Guid mediaId, CancellationToken ct = default);

    /// <summary>
    /// Removes favorite status from a media item.
    /// </summary>
    Task<Result> UnsetFavoriteAsync(Guid mediaId, CancellationToken ct = default);

    /// <summary>
    /// Gets storage usage for a game's media.
    /// </summary>
    Task<Result<long>> GetStorageUsageAsync(Guid gameId, CancellationToken ct = default);
}
