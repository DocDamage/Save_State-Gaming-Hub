using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Application.RomManagement.RomValidation.Commands;

/// <summary>
/// Command to calculate hashes for a ROM file.
/// </summary>
public sealed record CalculateRomHashesCommand(
    Guid RomFileId,
    bool CalculateCrc32 = true,
    bool CalculateMd5 = true,
    bool CalculateSha1 = true,
    bool CalculateSha256 = false) : IRequest<Result<RomHashInfo>>;
