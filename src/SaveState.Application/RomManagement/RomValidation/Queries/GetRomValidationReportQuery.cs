using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Application.RomManagement.RomValidation.Queries;

/// <summary>
/// Query to get the validation report for a specific ROM.
/// </summary>
public sealed record GetRomValidationReportQuery(
    Guid RomFileId) : IRequest<Result<RomValidationReport>>;
