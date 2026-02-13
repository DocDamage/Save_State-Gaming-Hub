namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service for extracting command and move lists from character data.
/// </summary>
public interface IMugenMoveListService
{
    Task<Result<IReadOnlyList<MugenMoveEntry>>> GetMoveListAsync(MugenCharacter character, CancellationToken ct = default);
}
