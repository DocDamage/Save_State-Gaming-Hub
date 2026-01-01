namespace SaveState.Core.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Repository interface for managing MUGEN training session entities.
/// </summary>
public interface IMugenTrainingRepository
{
    /// <summary>
    /// Retrieves a training session by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the training session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The training session if found, null otherwise.</returns>
    Task<MugenTrainingSession?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all training sessions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all training sessions.</returns>
    Task<IReadOnlyList<MugenTrainingSession>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves training sessions with pagination and filtering support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="characterId">Optional character ID filter.</param>
    /// <param name="sessionType">Optional session type filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the training sessions.</returns>
    Task<PagedResult<MugenTrainingSession>> GetTrainingSessionsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? userId = null,
        Guid? characterId = null,
        TrainingSessionType? sessionType = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets training sessions for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of training sessions for the user.</returns>
    Task<IReadOnlyList<MugenTrainingSession>> GetByUserAsync(Guid userId, int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets training sessions for a specific character.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of training sessions for the character.</returns>
    Task<IReadOnlyList<MugenTrainingSession>> GetByCharacterAsync(Guid characterId, int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets active (incomplete) training sessions.
    /// </summary>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of active training sessions.</returns>
    Task<IReadOnlyList<MugenTrainingSession>> GetActiveSessionsAsync(Guid? userId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of training sessions.
    /// </summary>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="characterId">Optional character ID filter.</param>
    /// <param name="sessionType">Optional session type filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total number of training sessions matching the filters.</returns>
    Task<int> CountAsync(Guid? userId = null, Guid? characterId = null, TrainingSessionType? sessionType = null, CancellationToken ct = default);

    /// <summary>
    /// Adds a new training session to the repository.
    /// </summary>
    /// <param name="session">The training session to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(MugenTrainingSession session, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing training session.
    /// </summary>
    /// <param name="session">The training session to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(MugenTrainingSession session, CancellationToken ct = default);

    /// <summary>
    /// Deletes a training session.
    /// </summary>
    /// <param name="session">The training session to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(MugenTrainingSession session, CancellationToken ct = default);
}