using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

namespace SaveState.Application.Social.Queries;

/// <summary>
/// Query to get the current progress for a user's active challenges.
/// </summary>
public record GetUserChallengeProgressQuery(Guid UserId) : IRequest<Result<List<ChallengeProgress>>>;

public class GetUserChallengeProgressQueryHandler
    : IRequestHandler<GetUserChallengeProgressQuery, Result<List<ChallengeProgress>>>
{
    private readonly IChallengeProgressService _progressService;

    public GetUserChallengeProgressQueryHandler(IChallengeProgressService progressService)
    {
        _progressService = progressService;
    }

    public async Task<Result<List<ChallengeProgress>>> Handle(
        GetUserChallengeProgressQuery request,
        CancellationToken cancellationToken)
    {
        return await _progressService.GetUserChallengeProgressAsync(request.UserId, cancellationToken);
    }
}
