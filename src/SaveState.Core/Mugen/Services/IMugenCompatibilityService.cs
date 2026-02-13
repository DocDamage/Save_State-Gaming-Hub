namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service for analyzing and fixing character compatibility issues.
/// </summary>
public interface IMugenCompatibilityService
{
    Task<Result<MugenCompatibilityReport>> AnalyzeAsync(MugenCharacter character, CancellationToken ct = default);
    Task<Result<MugenCompatibilityReport>> FixAsync(MugenCharacter character, CancellationToken ct = default);
}
