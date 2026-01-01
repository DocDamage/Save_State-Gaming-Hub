namespace SaveState.Application.Social.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

public class ShareAchievementCommandHandler : IRequestHandler<ShareAchievementCommand, Result>
{
    private readonly ISocialService _socialService;

    public ShareAchievementCommandHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    public async Task<Result> Handle(ShareAchievementCommand request, CancellationToken ct)
    {
        return await _socialService.ShareAchievementAsync(
            request.AchievementName,
            request.Description,
            request.Rarity,
            ct);
    }
}