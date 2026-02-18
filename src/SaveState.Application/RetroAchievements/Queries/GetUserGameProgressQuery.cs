using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RetroAchievements;
using SaveState.Core.RetroAchievements.Services;

namespace SaveState.Application.RetroAchievements.Queries;

/// <summary>
/// Query to get user's progress for a specific game.
/// </summary>
public sealed record GetUserGameProgressQuery(string Username, int GameId) : IRequest<Result<List<UserRetroAchievementProgress>>>;

/// <summary>
/// Handler for GetUserGameProgressQuery.
/// </summary>
public sealed class GetUserGameProgressQueryHandler : IRequestHandler<GetUserGameProgressQuery, Result<List<UserRetroAchievementProgress>>>
{
    private readonly IRetroAchievementsService _retroAchievementsService;

    public GetUserGameProgressQueryHandler(IRetroAchievementsService retroAchievementsService)
    {
        _retroAchievementsService = retroAchievementsService;
    }

    public async Task<Result<List<UserRetroAchievementProgress>>> Handle(GetUserGameProgressQuery request, CancellationToken cancellationToken)
    {
        return await _retroAchievementsService.GetUserGameProgressAsync(
            request.Username, request.GameId, cancellationToken);
    }
}
