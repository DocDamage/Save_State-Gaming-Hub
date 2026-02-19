using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Application.RomManagement.RomValidation.Queries;

/// <summary>
/// Query to generate a missing games report for a platform.
/// </summary>
public sealed record GetMissingGamesReportQuery(
    Guid PlatformId,
    string ReferenceDatPath) : IRequest<Result<MissingGameReport>>;
