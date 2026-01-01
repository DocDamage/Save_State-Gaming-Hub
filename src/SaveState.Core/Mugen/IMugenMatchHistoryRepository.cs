namespace SaveState.Core.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Repository interface for managing MUGEN match history entities.
/// </summary>
public interface IMugenMatchHistoryRepository
{
    /// <summary>
    /// Retrieves a match history by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the match history.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The match history if found, as a Result.</returns>
    Task<Result<MugenMatchHistory>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all match histories.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all match histories.</returns>
    Task<IReadOnlyList<MugenMatchHistory>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves match histories with pagination and filtering support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="characterId">Optional character ID filter.</param>
    /// <param name="gameMode">Optional game mode filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the match histories.</returns>
    Task<PagedResult<MugenMatchHistory>> GetMatchHistoriesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        Guid? characterId = null,
        GameMode? gameMode = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets match histories for a specific character.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of match histories for the character.</returns>
    Task<IReadOnlyList<MugenMatchHistory>> GetByCharacterAsync(Guid characterId, int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets match statistics between two characters.
    /// </summary>
    /// <param name="character1Id">First character ID.</param>
    /// <param name="character2Id">Second character ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matchup statistics between the two characters.</returns>
    Task<Result<MugenMatchupStats>> GetMatchupStatsAsync(Guid character1Id, Guid character2Id, CancellationToken ct = default);

    /// <summary>
    /// Records a new match result.
    /// </summary>
    /// <param name="match">The match history to record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<MugenMatchHistory>> RecordMatchAsync(MugenMatchHistory match, CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of match histories.
    /// </summary>
    /// <param name="characterId">Optional character ID filter.</param>
    /// <param name="gameMode">Optional game mode filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total number of match histories matching the filters.</returns>
    Task<int> CountAsync(Guid? characterId = null, GameMode? gameMode = null, CancellationToken ct = default);

    /// <summary>
    /// Adds a new match history to the repository.
    /// </summary>
    /// <param name="matchHistory">The match history to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(MugenMatchHistory matchHistory, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing match history.
    /// </summary>
    /// <param name="matchHistory">The match history to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(MugenMatchHistory matchHistory, CancellationToken ct = default);

    /// <summary>
    /// Deletes a match history.
    /// </summary>
    /// <param name="matchHistory">The match history to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(MugenMatchHistory matchHistory, CancellationToken ct = default);
}
