namespace SaveState.Core.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Repository interface for managing MUGEN tournament entities.
/// </summary>
public interface IMugenTournamentRepository
{
    /// <summary>
    /// Retrieves a MUGEN tournament by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the tournament.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tournament if found, as a Result.</returns>
    Task<Result<MugenTournament>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all MUGEN tournaments.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all MUGEN tournaments.</returns>
    Task<IReadOnlyList<MugenTournament>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves tournaments with pagination and filtering support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="statusFilter">Optional status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the tournaments.</returns>
    Task<PagedResult<MugenTournament>> GetTournamentsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        TournamentStatus? statusFilter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of MUGEN tournaments.
    /// </summary>
    /// <param name="statusFilter">Optional status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total number of tournaments matching the filters.</returns>
    Task<int> CountAsync(TournamentStatus? statusFilter = null, CancellationToken ct = default);

    /// <summary>
    /// Finds tournaments by status.
    /// </summary>
    /// <param name="status">The tournament status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of tournaments with the specified status.</returns>
    Task<IReadOnlyList<MugenTournament>> GetByStatusAsync(TournamentStatus status, CancellationToken ct = default);

    /// <summary>
    /// Adds a new MUGEN tournament to the repository.
    /// </summary>
    /// <param name="tournament">The tournament to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(MugenTournament tournament, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing MUGEN tournament.
    /// </summary>
    /// <param name="tournament">The tournament to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(MugenTournament tournament, CancellationToken ct = default);

    /// <summary>
    /// Deletes a MUGEN tournament.
    /// </summary>
    /// <param name="tournament">The tournament to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(MugenTournament tournament, CancellationToken ct = default);
}
