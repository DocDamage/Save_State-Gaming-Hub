using SaveState.Core.OpenMK.Entities;

namespace SaveState.Core.OpenMK.Repositories;

/// <summary>
/// Repository interface for OpenMK match state persistence.
/// </summary>
public interface IOpenMKMatchStateRepository
{
    /// <summary>
    /// Gets a match state by match ID.
    /// </summary>
    Task<OpenMKMatchState?> GetByMatchIdAsync(Guid matchId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new match state record.
    /// </summary>
    Task AddAsync(OpenMKMatchState matchState, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing match state record.
    /// </summary>
    Task UpdateAsync(OpenMKMatchState matchState, CancellationToken ct = default);

    /// <summary>
    /// Deletes a match state record.
    /// </summary>
    Task DeleteAsync(Guid matchId, CancellationToken ct = default);
}
