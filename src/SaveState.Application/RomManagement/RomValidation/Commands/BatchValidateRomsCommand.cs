using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Application.RomManagement.RomValidation.Commands;

/// <summary>
/// Command to validate multiple ROMs in a batch job.
/// </summary>
public sealed record BatchValidateRomsCommand(
    string JobName,
    List<Guid>? RomFileIds = null,
    List<Guid>? PlatformIds = null,
    RomValidationOptions? Options = null) : IRequest<Result<RomValidationJob>>;
