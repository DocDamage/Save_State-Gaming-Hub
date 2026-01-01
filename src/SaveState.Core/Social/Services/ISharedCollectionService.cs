using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Core.Social.Services;

/// <summary>
/// Service for managing shared collections.
/// </summary>
public interface ISharedCollectionService
{
    /// <summary>
    /// Creates a new shared collection.
    /// </summary>
    Task<Result<SharedCollection>> CreateCollectionAsync(
        string title,
        string? description = null,
        bool isPublic = false,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing shared collection.
    /// </summary>
    Task<Result<SharedCollection>> UpdateCollectionAsync(
        Guid collectionId,
        string? title = null,
        string? description = null,
        bool? isPublic = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a shared collection by ID.
    /// </summary>
    Task<Result<SharedCollection>> GetCollectionAsync(Guid collectionId, CancellationToken ct = default);

    /// <summary>
    /// Gets a shared collection by share code.
    /// </summary>
    Task<Result<SharedCollection>> GetCollectionByShareCodeAsync(string shareCode, CancellationToken ct = default);

    /// <summary>
    /// Gets collections with optional filtering.
    /// </summary>
    Task<Result<PagedResult<SharedCollection>>> GetCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        bool? isPublic = null,
        string? searchTerm = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets collections created by the current user.
    /// </summary>
    Task<Result<PagedResult<SharedCollection>>> GetUserCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a shared collection.
    /// </summary>
    Task<Result> DeleteCollectionAsync(Guid collectionId, CancellationToken ct = default);

    /// <summary>
    /// Adds games to a shared collection.
    /// </summary>
    Task<Result> AddGamesToCollectionAsync(
        Guid collectionId,
        IReadOnlyList<CollectionGameRequest> games,
        CancellationToken ct = default);

    /// <summary>
    /// Removes games from a shared collection.
    /// </summary>
    Task<Result> RemoveGamesFromCollectionAsync(
        Guid collectionId,
        IReadOnlyList<string> gameTitles,
        CancellationToken ct = default);

    /// <summary>
    /// Imports a shared collection from a share code.
    /// </summary>
    Task<Result<SharedCollection>> ImportCollectionAsync(
        string shareCode,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a collection for sharing.
    /// </summary>
    Task<Result<string>> ExportCollectionAsync(
        Guid collectionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets statistics for shared collections.
    /// </summary>
    Task<Result<SharedCollectionStatistics>> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// Request to add a game to a collection.
/// </summary>
public sealed record CollectionGameRequest(
    string GameTitle,
    string? Notes = null,
    int SortOrder = 0);