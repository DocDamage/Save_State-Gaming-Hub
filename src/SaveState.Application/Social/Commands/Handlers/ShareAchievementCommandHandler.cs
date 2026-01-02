namespace SaveState.Application.Social.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

/// <summary>
/// Handler for sharing achievements with friends.
/// Publishes gaming accomplishments to the social network.
/// </summary>
public class ShareAchievementCommandHandler : IRequestHandler<ShareAchievementCommand, Result>
{
    private readonly ISocialService _socialService;

    public ShareAchievementCommandHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    /// <summary>
    /// Handles the command to share an achievement.
    /// </summary>
    /// <param name="request">The share achievement command with details.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> Handle(ShareAchievementCommand request, CancellationToken ct)
    {
        return await _socialService.ShareAchievementAsync(
            request.AchievementName,
            request.Description,
            request.Rarity,
            ct);
    }
}