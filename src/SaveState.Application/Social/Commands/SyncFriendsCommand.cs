using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to sync friends from connected platforms.
/// </summary>
public record SyncFriendsCommand(string Platform) : IRequest<Result>;

/// <summary>
/// Handler for syncing friends.
/// </summary>
public class SyncFriendsCommandHandler : IRequestHandler<SyncFriendsCommand, Result>
{
    private readonly Core.Social.Services.IFriendActivityService _friendActivityService;

    public SyncFriendsCommandHandler(Core.Social.Services.IFriendActivityService friendActivityService)
    {
        _friendActivityService = friendActivityService;
    }

    public async Task<Result> Handle(SyncFriendsCommand request, CancellationToken ct)
    {
        return request.Platform.ToLowerInvariant() switch
        {
            "discord" => await _friendActivityService.SyncDiscordFriendsAsync(ct),
            "steam" => await _friendActivityService.SyncSteamFriendsAsync(ct),
            _ => Result.Failure($"Unsupported platform: {request.Platform}", ErrorType.Validation)
        };
    }
}