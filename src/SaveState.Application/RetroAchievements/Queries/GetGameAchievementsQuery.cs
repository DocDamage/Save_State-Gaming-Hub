using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RetroAchievements;
using SaveState.Core.RetroAchievements.Services;

namespace SaveState.Application.RetroAchievements.Queries;

/// <summary>
/// Query to get achievements for a game.
/// </summary>
public sealed record GetGameAchievementsQuery(int GameId) : IRequest<Result<List<RetroAchievement>>>;

/// <summary>
/// Handler for GetGameAchievementsQuery.
/// </summary>
public sealed class GetGameAchievementsQueryHandler : IRequestHandler<GetGameAchievementsQuery, Result<List<RetroAchievement>>>
{
    private readonly IRetroAchievementsService _retroAchievementsService;

    public GetGameAchievementsQueryHandler(IRetroAchievementsService retroAchievementsService)
    {
        _retroAchievementsService = retroAchievementsService;
    }

    public async Task<Result<List<RetroAchievement>>> Handle(GetGameAchievementsQuery request, CancellationToken cancellationToken)
    {
        return await _retroAchievementsService.GetGameAchievementsAsync(request.GameId, cancellationToken);
    }
}
