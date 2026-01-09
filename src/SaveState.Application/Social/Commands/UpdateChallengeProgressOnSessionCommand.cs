using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to update challenge progress when a game session ends.
/// </summary>
public record UpdateChallengeProgressOnSessionCommand(
    Guid UserId,
    Guid GameId,
    TimeSpan SessionDuration
) : IRequest<Result>;

public class UpdateChallengeProgressOnSessionCommandHandler
    : IRequestHandler<UpdateChallengeProgressOnSessionCommand, Result>
{
    private readonly IChallengeProgressService _progressService;

    public UpdateChallengeProgressOnSessionCommandHandler(IChallengeProgressService progressService)
    {
        _progressService = progressService;
    }

    public async Task<Result> Handle(
        UpdateChallengeProgressOnSessionCommand request,
        CancellationToken cancellationToken)
    {
        return await _progressService.UpdateProgressOnGameSessionAsync(
            request.UserId,
            request.GameId,
            request.SessionDuration,
            cancellationToken);
    }
}
