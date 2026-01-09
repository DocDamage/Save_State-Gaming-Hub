using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;
using SaveState.Core.Social.Repositories;

namespace SaveState.Application.Social.Queries;

public record GetActiveChallengesQuery : IRequest<Result<IReadOnlyList<Challenge>>>;

public class GetActiveChallengesQueryHandler : IRequestHandler<GetActiveChallengesQuery, Result<IReadOnlyList<Challenge>>>
{
    private readonly ICommunityRepository _communityRepository;

    public GetActiveChallengesQueryHandler(ICommunityRepository communityRepository)
    {
        _communityRepository = communityRepository;
    }

    public async Task<Result<IReadOnlyList<Challenge>>> Handle(GetActiveChallengesQuery request, CancellationToken cancellationToken)
    {
        return await _communityRepository.GetActiveChallengesAsync(cancellationToken);
    }
}
