namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public record GetCoachingReportQuery(Guid SessionId) : IRequest<Result<CoachingReport>>;