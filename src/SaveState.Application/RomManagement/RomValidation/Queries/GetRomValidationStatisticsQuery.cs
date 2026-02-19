using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Application.RomManagement.RomValidation.Queries;

/// <summary>
/// Query to get ROM validation statistics.
/// </summary>
public sealed record GetRomValidationStatisticsQuery : IRequest<Result<RomValidationStatistics>>;
