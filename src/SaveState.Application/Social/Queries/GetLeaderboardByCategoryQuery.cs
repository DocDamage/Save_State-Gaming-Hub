using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;
using SaveState.Core.Social.Repositories;

namespace SaveState.Application.Social.Queries;

public record GetLeaderboardByCategoryQuery(LeaderboardCategory Category) : IRequest<Result<Leaderboard>>;

public class GetLeaderboardByCategoryQueryHandler : IRequestHandler<GetLeaderboardByCategoryQuery, Result<Leaderboard>>
{
    private readonly ICommunityRepository _communityRepository;

    public GetLeaderboardByCategoryQueryHandler(ICommunityRepository communityRepository)
    {
        _communityRepository = communityRepository;
    }

    public async Task<Result<Leaderboard>> Handle(GetLeaderboardByCategoryQuery request, CancellationToken cancellationToken)
    {
        return await _communityRepository.GetLeaderboardByCategoryAsync(request.Category, cancellationToken);
    }
}
