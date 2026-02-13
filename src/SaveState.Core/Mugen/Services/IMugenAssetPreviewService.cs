namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service for listing available MUGEN asset files for a character.
/// </summary>
public interface IMugenAssetPreviewService
{
    Task<Result<IReadOnlyList<MugenAssetEntry>>> GetAssetsAsync(MugenCharacter character, CancellationToken ct = default);
}
