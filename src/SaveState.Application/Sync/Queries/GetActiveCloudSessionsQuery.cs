using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Application.Sync.Queries;

/// <summary>
/// Query to get all active cloud gaming sessions.
/// </summary>
public record GetActiveCloudSessionsQuery : IRequest<Result<IReadOnlyList<CloudSession>>>;