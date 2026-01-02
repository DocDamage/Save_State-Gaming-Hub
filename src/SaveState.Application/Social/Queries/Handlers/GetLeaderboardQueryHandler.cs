namespace SaveState.Application.Social.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

/// <summary>
/// Handler for retrieving leaderboard rankings.
/// Provides competitive rankings for games and achievements.
/// </summary>
public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, Result<IReadOnlyList<LeaderboardEntry>>>
{
    private readonly ISocialService _socialService;

    public GetLeaderboardQueryHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    /// <summary>
    /// Handles the query to get leaderboard rankings.
    /// </summary>
    /// <param name="request">The leaderboard query with type, game ID, and limit.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the leaderboard entries or an error.</returns>
    public async Task<Result<IReadOnlyList<LeaderboardEntry>>> Handle(GetLeaderboardQuery request, CancellationToken ct)
    {
        return await _socialService.GetLeaderboardAsync(request.Type, request.GameId, request.Limit, ct);
    }
}