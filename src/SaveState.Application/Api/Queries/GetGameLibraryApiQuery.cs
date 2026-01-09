using MediatR;
using SaveState.Core.Api.DTOs;
using SaveState.Core.Common;

namespace SaveState.Application.Api.Queries;

/// <summary>
/// Query to get game library for API access.
/// </summary>
public record GetGameLibraryApiQuery : IRequest<Result<List<ApiGameDto>>>;
