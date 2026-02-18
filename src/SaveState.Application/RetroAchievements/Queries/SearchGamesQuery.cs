using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RetroAchievements;
using SaveState.Core.RetroAchievements.Services;

namespace SaveState.Application.RetroAchievements.Queries;

/// <summary>
/// Query to search for games on RetroAchievements.
/// </summary>
public sealed record SearchGamesQuery(string Query, int? ConsoleId = null) : IRequest<Result<List<RetroGameInfo>>>;

/// <summary>
/// Handler for SearchGamesQuery.
/// </summary>
public sealed class SearchGamesQueryHandler : IRequestHandler<SearchGamesQuery, Result<List<RetroGameInfo>>>
{
    private readonly IRetroAchievementsService _retroAchievementsService;

    public SearchGamesQueryHandler(IRetroAchievementsService retroAchievementsService)
    {
        _retroAchievementsService = retroAchievementsService;
    }

    public async Task<Result<List<RetroGameInfo>>> Handle(SearchGamesQuery request, CancellationToken cancellationToken)
    {
        return await _retroAchievementsService.SearchGamesAsync(
            request.Query, request.ConsoleId, cancellationToken);
    }
}
