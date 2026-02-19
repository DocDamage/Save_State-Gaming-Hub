using MediatR;
using SaveState.Application.Common;

namespace SaveState.Application.RomManagement.RomValidation.Commands;

/// <summary>
/// Command to remove duplicate ROM files.
/// </summary>
public sealed record RemoveDuplicateRomsCommand(
    List<Guid> RomFileIdsToRemove,
    bool MoveToRecycleBin = true,
    string? BackupPath = null) : IRequest<Result<RemoveDuplicatesResult>>;

/// <summary>
/// Result of duplicate removal operation.
/// </summary>
public sealed record RemoveDuplicatesResult(
    int RemovedCount,
    long FreedSpace,
    List<Guid> RemovedRomIds,
    List<string> Errors);
