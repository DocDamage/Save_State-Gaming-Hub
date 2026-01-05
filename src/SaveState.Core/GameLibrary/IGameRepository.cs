namespace SaveState.Core.GameLibrary;

using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.DTOs;

/// <summary>
/// Repository interface for managing Game entities in the data store.
/// Provides CRUD operations and queries for games in the library.
/// </summary>
public interface IGameRepository
{
    /// <summary>
    /// Retrieves a game by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the game.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The game if found, null otherwise.</returns>
    Task<Game?> GetByIdAsync(GameId id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all games in the library.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all games.</returns>
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves games with pagination, filtering, and sorting support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchTerm">Optional search term to filter by game title.</param>
    /// <param name="platformId">Optional platform ID to filter by platform.</param>
    /// <param name="statusFilter">Optional status filter.</param>
    /// <param name="platformFilter">Optional platform name filter.</param>
    /// <param name="sortBy">The sorting criteria.</param>
    /// <param name="sortDescending">Whether to sort in descending order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the games.</returns>
    Task<PagedResult<Game>> GetGamesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? searchTerm = null,
        Guid? platformId = null,
        Guid? collectionId = null,
        GameStatus? statusFilter = null,
        string? platformFilter = null,
        GameSortBy sortBy = GameSortBy.Title,
        bool sortDescending = false,
        CancellationToken ct = default);

    /// <summary>
    /// Gets game summaries with database projection for optimal performance.
    /// </summary>
    Task<PagedResult<GameSummaryProjection>> GetGameSummariesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? searchTerm = null,
        GameStatus? statusFilter = null,
        string? platformFilter = null,
        GameSortBy sortBy = GameSortBy.Title,
        bool sortDescending = false,
        CancellationToken ct = default);

    /// <summary>
    /// Finds a game by its title and platform.
    /// </summary>
    /// <param name="title">The title of the game.</param>
    /// <param name="platformId">The platform identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The game if found, null otherwise.</returns>
    Task<Game?> GetByTitleAndPlatformAsync(GameTitle title, Guid platformId, CancellationToken ct = default);

    /// <summary>
    /// Finds a game by its source and source ID.
    /// </summary>
    /// <param name="source">The source of the game (e.g., "Steam", "GOG").</param>
    /// <param name="sourceId">The source-specific identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The game if found, null otherwise.</returns>
    Task<Game?> GetBySourceAndSourceIdAsync(string source, string sourceId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new game to the repository.
    /// </summary>
    /// <param name="game">The game to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(Game game, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing game in the repository.
    /// </summary>
    /// <param name="game">The game to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(Game game, CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of games in the repository.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total number of games.</returns>
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets platform statistics for efficient library statistics calculation.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary mapping platform names to game counts.</returns>
    Task<IReadOnlyDictionary<string, int>> GetPlatformStatisticsAsync(CancellationToken ct = default);
}
