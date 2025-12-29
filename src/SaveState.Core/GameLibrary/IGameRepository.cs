namespace SaveState.Core.GameLibrary;

using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;

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
}
