namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service for computing ELO ratings from match history.
/// </summary>
public interface IMugenEloService
{
    Task<Result<IReadOnlyList<MugenEloRating>>> GetRatingsAsync(CancellationToken ct = default);
    Task<Result<MugenEloRating>> GetPlayerRatingAsync(string playerId, CancellationToken ct = default);
}
