using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RetroAchievements;
using SaveState.Core.RetroAchievements.Services;

namespace SaveState.Application.RetroAchievements.Queries;

/// <summary>
/// Query to get RetroAchievements user summary.
/// </summary>
public sealed record GetUserSummaryQuery(string Username) : IRequest<Result<RetroUserSummary>>;

/// <summary>
/// Handler for GetUserSummaryQuery.
/// </summary>
public sealed class GetUserSummaryQueryHandler : IRequestHandler<GetUserSummaryQuery, Result<RetroUserSummary>>
{
    private readonly IRetroAchievementsService _retroAchievementsService;

    public GetUserSummaryQueryHandler(IRetroAchievementsService retroAchievementsService)
    {
        _retroAchievementsService = retroAchievementsService;
    }

    public async Task<Result<RetroUserSummary>> Handle(GetUserSummaryQuery request, CancellationToken cancellationToken)
    {
        return await _retroAchievementsService.GetUserSummaryAsync(request.Username, cancellationToken);
    }
}
