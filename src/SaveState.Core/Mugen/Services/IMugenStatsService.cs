namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service interface for tracking and retrieving MUGEN match statistics.
/// </summary>
public interface IMugenStatsService
{
    /// <summary>
    /// Gets statistics for a specific character.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The character statistics.</returns>
    Task<Result<CharacterStats>> GetCharacterStatsAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets matchup statistics for a character against all opponents.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matchup statistics.</returns>
    Task<Result<IReadOnlyList<MatchupStats>>> GetMatchupStatsAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent matches.
    /// </summary>
    /// <param name="count">Number of matches to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The recent matches.</returns>
    Task<Result<IReadOnlyList<MugenMatchHistory>>> GetRecentMatchesAsync(int count = 20, CancellationToken ct = default);

    /// <summary>
    /// Records a new match result.
    /// </summary>
    /// <param name="match">The match to record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> RecordMatchAsync(MugenMatchHistory match, CancellationToken ct = default);
}