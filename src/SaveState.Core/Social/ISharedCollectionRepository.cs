using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Core.Social;

/// <summary>
/// Repository interface for shared collections.
/// </summary>
public interface ISharedCollectionRepository
{
    /// <summary>
    /// Gets a shared collection by its ID.
    /// </summary>
    Task<SharedCollection?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a shared collection by its share code.
    /// </summary>
    Task<SharedCollection?> GetByShareCodeAsync(string shareCode, CancellationToken ct = default);

    /// <summary>
    /// Gets all shared collections with optional filtering.
    /// </summary>
    Task<PagedResult<SharedCollection>> GetCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        bool? isPublic = null,
        string? searchTerm = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets collections created by the current user.
    /// </summary>
    Task<PagedResult<SharedCollection>> GetUserCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a new shared collection.
    /// </summary>
    Task AddAsync(SharedCollection collection, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing shared collection.
    /// </summary>
    Task UpdateAsync(SharedCollection collection, CancellationToken ct = default);

    /// <summary>
    /// Deletes a shared collection.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Adds an item to a shared collection.
    /// </summary>
    Task AddItemAsync(SharedCollectionItem item, CancellationToken ct = default);

    /// <summary>
    /// Removes an item from a shared collection.
    /// </summary>
    Task RemoveItemAsync(Guid collectionId, string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Updates the items in a shared collection.
    /// </summary>
    Task UpdateItemsAsync(Guid collectionId, IReadOnlyList<SharedCollectionItem> items, CancellationToken ct = default);

    /// <summary>
    /// Checks if a share code is already in use.
    /// </summary>
    Task<bool> IsShareCodeUniqueAsync(string shareCode, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets statistics for shared collections.
    /// </summary>
    Task<SharedCollectionStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// Statistics for shared collections.
/// </summary>
public sealed record SharedCollectionStatistics(
    int TotalCollections,
    int PublicCollections,
    int TotalDownloads,
    int AverageItemsPerCollection,
    DateTime? LastCreatedAt);