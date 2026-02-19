using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Application.RomManagement.RomValidation.Commands;

/// <summary>
/// Command to validate a single ROM file with comprehensive checks.
/// </summary>
public sealed record ValidateRomCommand(
    Guid RomFileId,
    RomValidationOptions? Options = null) : IRequest<Result<RomValidationReport>>;
