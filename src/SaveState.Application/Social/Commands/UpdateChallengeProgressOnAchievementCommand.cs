using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to update challenge progress when an achievement is unlocked.
/// </summary>
public record UpdateChallengeProgressOnAchievementCommand(
    Guid UserId,
    Guid GameId,
    string AchievementId
) : IRequest<Result>;

public class UpdateChallengeProgressOnAchievementCommandHandler
    : IRequestHandler<UpdateChallengeProgressOnAchievementCommand, Result>
{
    private readonly IChallengeProgressService _progressService;

    public UpdateChallengeProgressOnAchievementCommandHandler(IChallengeProgressService progressService)
    {
        _progressService = progressService;
    }

    public async Task<Result> Handle(
        UpdateChallengeProgressOnAchievementCommand request,
        CancellationToken cancellationToken)
    {
        return await _progressService.UpdateProgressOnAchievementAsync(
            request.UserId,
            request.GameId,
            request.AchievementId,
            cancellationToken);
    }
}
