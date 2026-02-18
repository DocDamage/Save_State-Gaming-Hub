using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RetroAchievements.Services;

namespace SaveState.Application.RetroAchievements.Commands;

/// <summary>
/// Command to start rich presence monitoring for a game.
/// </summary>
public sealed record StartRichPresenceCommand(int GameId) : IRequest<Result>;

/// <summary>
/// Handler for StartRichPresenceCommand.
/// </summary>
public sealed class StartRichPresenceCommandHandler : IRequestHandler<StartRichPresenceCommand, Result>
{
    private readonly IRetroAchievementsService _retroAchievementsService;

    public StartRichPresenceCommandHandler(IRetroAchievementsService retroAchievementsService)
    {
        _retroAchievementsService = retroAchievementsService;
    }

    public async Task<Result> Handle(StartRichPresenceCommand request, CancellationToken cancellationToken)
    {
        return await _retroAchievementsService.StartRichPresenceAsync(request.GameId, cancellationToken);
    }
}
