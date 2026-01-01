namespace SaveState.Core.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Repository interface for managing MUGEN character collection entities.
/// </summary>
public interface IMugenCollectionRepository
{
    /// <summary>
    /// Retrieves a collection by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the collection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The collection if found, null otherwise.</returns>
    Task<MugenCharacterCollection?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all collections.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all MUGEN character collections.</returns>
    Task<IReadOnlyList<MugenCharacterCollection>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves collections with pagination and filtering support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="isPublic">Optional public status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the collections.</returns>
    Task<PagedResult<MugenCharacterCollection>> GetCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? userId = null,
        bool? isPublic = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets collections for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of collections owned by the user.</returns>
    Task<IReadOnlyList<MugenCharacterCollection>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets public collections.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of public collections.</returns>
    Task<IReadOnlyList<MugenCharacterCollection>> GetPublicCollectionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of collections.
    /// </summary>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="isPublic">Optional public status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total number of collections matching the filters.</returns>
    Task<int> CountAsync(Guid? userId = null, bool? isPublic = null, CancellationToken ct = default);

    /// <summary>
    /// Checks if a character is in a specific collection.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="characterId">The character ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the character is in the collection, false otherwise.</returns>
    Task<bool> IsCharacterInCollectionAsync(Guid collectionId, Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets collections containing a specific character.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of collections containing the character.</returns>
    Task<IReadOnlyList<MugenCharacterCollection>> GetCollectionsByCharacterAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new collection to the repository.
    /// </summary>
    /// <param name="collection">The collection to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(MugenCharacterCollection collection, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing collection.
    /// </summary>
    /// <param name="collection">The collection to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(MugenCharacterCollection collection, CancellationToken ct = default);

    /// <summary>
    /// Deletes a collection.
    /// </summary>
    /// <param name="collection">The collection to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(MugenCharacterCollection collection, CancellationToken ct = default);
}