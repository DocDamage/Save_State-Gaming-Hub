namespace SaveState.Application.Social.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, Result<IReadOnlyList<LeaderboardEntry>>>
{
    private readonly ISocialService _socialService;

    public GetLeaderboardQueryHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    public async Task<Result<IReadOnlyList<LeaderboardEntry>>> Handle(GetLeaderboardQuery request, CancellationToken ct)
    {
        return await _socialService.GetLeaderboardAsync(request.Type, request.GameId, request.Limit, ct);
    }
}