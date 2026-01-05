namespace SaveState.Core.GameLibrary;

using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Repository interface for managing game media (screenshots, videos, artwork).
/// </summary>
public interface IGameMediaRepository
{
    /// <summary>
    /// Retrieves all media for a specific game and user.
    /// </summary>
    Task<IReadOnlyList<GameMedia>> GetByGameIdAsync(GameId gameId, UserId userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific media item by ID.
    /// </summary>
    Task<GameMedia?> GetByIdAsync(Guid mediaId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new media item.
    /// </summary>
    Task<GameMedia> AddAsync(GameMedia media, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing media item.
    /// </summary>
    Task UpdateAsync(GameMedia media, CancellationToken ct = default);

    /// <summary>
    /// Deletes a media item.
    /// </summary>
    Task DeleteAsync(Guid mediaId, CancellationToken ct = default);

    /// <summary>
    /// Gets media by type for a specific game.
    /// </summary>
    Task<IReadOnlyList<GameMedia>> GetByTypeAsync(GameId gameId, UserId userId, MediaType mediaType, CancellationToken ct = default);

    /// <summary>
    /// Gets favorite media for a specific game.
    /// </summary>
    Task<IReadOnlyList<GameMedia>> GetFavoritesAsync(GameId gameId, UserId userId, CancellationToken ct = default);

    /// <summary>
    /// Gets public media for a specific game.
    /// </summary>
    Task<IReadOnlyList<GameMedia>> GetPublicMediaAsync(GameId gameId, CancellationToken ct = default);
}
