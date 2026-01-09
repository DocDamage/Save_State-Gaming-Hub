using MediatR;
using SaveState.Core.Api.DTOs;
using SaveState.Core.Common;

namespace SaveState.Application.Api.Queries;

/// <summary>
/// Query to get library statistics for API access.
/// </summary>
public record GetLibraryStatsApiQuery : IRequest<Result<ApiLibraryStatsDto>>;
