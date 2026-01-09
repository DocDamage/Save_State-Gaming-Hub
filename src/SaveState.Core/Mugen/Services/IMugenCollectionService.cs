namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Service interface for managing MUGEN character collections.
/// Reuses existing VirtualCollection system for MUGEN characters.
/// </summary>
public interface IMugenCollectionService
{
    /// <summary>
    /// Creates a new MUGEN character collection.
    /// </summary>
    /// <param name="name">Collection name.</param>
    /// <param name="icon">Optional icon.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created collection.</returns>
    Task<Result<MugenCharacterCollection>> CreateCollectionAsync(string name, string? icon = null, CancellationToken ct = default);

    /// <summary>
    /// Adds a character to a collection.
    /// </summary>
    /// <param name="collectionId">Collection ID.</param>
    /// <param name="characterId">Character ID to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> AddCharacterToCollectionAsync(Guid collectionId, Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Removes a character from a collection.
    /// </summary>
    /// <param name="collectionId">Collection ID.</param>
    /// <param name="characterId">Character ID to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> RemoveCharacterFromCollectionAsync(Guid collectionId, Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets all MUGEN collections.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of collections.</returns>
    Task<Result<IReadOnlyList<MugenCharacterCollection>>> GetCollectionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets a character as favorite.
    /// </summary>
    /// <param name="characterId">Character ID.</param>
    /// <param name="isFavorite">Whether to mark as favorite.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> SetFavoriteAsync(Guid characterId, bool isFavorite, CancellationToken ct = default);

    /// <summary>
    /// Gets all favorite characters.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of favorite characters.</returns>
    Task<Result<IReadOnlyList<MugenCharacter>>> GetFavoritesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the full roster of characters.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all characters.</returns>
    Task<Result<IReadOnlyList<MugenCharacter>>> GetRosterAsync(CancellationToken ct = default);
}
