namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service for running real MUGEN engine matches and capturing results.
/// </summary>
public interface IMugenDeathMatchService
{
    Task<Result<DeathMatchResult>> RunDeathMatchAsync(
        Guid character1Id,
        Guid character2Id,
        int matchCount = 3,
        CancellationToken ct = default);
}
