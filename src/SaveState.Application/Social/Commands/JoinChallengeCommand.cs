using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Repositories;

namespace SaveState.Application.Social.Commands;

public record JoinChallengeCommand(Guid ChallengeId, Guid UserId) : IRequest<Result>;

public class JoinChallengeCommandHandler : IRequestHandler<JoinChallengeCommand, Result>
{
    private readonly ICommunityRepository _communityRepository;

    public JoinChallengeCommandHandler(ICommunityRepository communityRepository)
    {
        _communityRepository = communityRepository;
    }

    public async Task<Result> Handle(JoinChallengeCommand request, CancellationToken cancellationToken)
    {
        return await _communityRepository.JoinChallengeAsync(request.ChallengeId, request.UserId, cancellationToken);
    }
}
