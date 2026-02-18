using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RetroAchievements.Services;

namespace SaveState.Application.RetroAchievements.Queries;

/// <summary>
/// Query to get user's completion progress across all games.
/// </summary>
public sealed record GetUserCompletionProgressQuery(string Username) : IRequest<Result<List<GameCompletionStatus>>>;

/// <summary>
/// Handler for GetUserCompletionProgressQuery.
/// </summary>
public sealed class GetUserCompletionProgressQueryHandler : IRequestHandler<GetUserCompletionProgressQuery, Result<List<GameCompletionStatus>>>
{
    private readonly IRetroAchievementsService _retroAchievementsService;

    public GetUserCompletionProgressQueryHandler(IRetroAchievementsService retroAchievementsService)
    {
        _retroAchievementsService = retroAchievementsService;
    }

    public async Task<Result<List<GameCompletionStatus>>> Handle(GetUserCompletionProgressQuery request, CancellationToken cancellationToken)
    {
        return await _retroAchievementsService.GetUserCompletionProgressAsync(request.Username, cancellationToken);
    }
}
