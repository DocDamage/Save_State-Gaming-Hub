using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RetroAchievements.Services;

namespace SaveState.Application.RetroAchievements.Commands;

/// <summary>
/// Command to stop rich presence monitoring.
/// </summary>
public sealed record StopRichPresenceCommand : IRequest<Result>;

/// <summary>
/// Handler for StopRichPresenceCommand.
/// </summary>
public sealed class StopRichPresenceCommandHandler : IRequestHandler<StopRichPresenceCommand, Result>
{
    private readonly IRetroAchievementsService _retroAchievementsService;

    public StopRichPresenceCommandHandler(IRetroAchievementsService retroAchievementsService)
    {
        _retroAchievementsService = retroAchievementsService;
    }

    public async Task<Result> Handle(StopRichPresenceCommand request, CancellationToken cancellationToken)
    {
        return await _retroAchievementsService.StopRichPresenceAsync(cancellationToken);
    }
}
