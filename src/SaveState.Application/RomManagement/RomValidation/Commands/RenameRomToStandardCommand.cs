using MediatR;
using SaveState.Application.Common;

namespace SaveState.Application.RomManagement.RomValidation.Commands;

/// <summary>
/// Command to rename a ROM file to its standardized DAT name.
/// </summary>
public sealed record RenameRomToStandardCommand(
    Guid RomFileId,
    bool DryRun = false) : IRequest<Result<RenameRomResult>>;

/// <summary>
/// Result of a ROM rename operation.
/// </summary>
public sealed record RenameRomResult(
    Guid RomFileId,
    string OriginalPath,
    string NewPath,
    bool WasRenamed,
    string? Error = null);
