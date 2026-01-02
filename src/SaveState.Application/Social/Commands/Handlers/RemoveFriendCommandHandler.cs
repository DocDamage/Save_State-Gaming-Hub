namespace SaveState.Application.Social.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

/// <summary>
/// Handler for removing friends from the user's social network.
/// Manages friend relationship termination and cleanup.
/// </summary>
public class RemoveFriendCommandHandler : IRequestHandler<RemoveFriendCommand, Result>
{
    private readonly ISocialService _socialService;

    public RemoveFriendCommandHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    /// <summary>
    /// Handles the command to remove a friend.
    /// </summary>
    /// <param name="request">The remove friend command containing the friend ID.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> Handle(RemoveFriendCommand request, CancellationToken ct)
    {
        return await _socialService.RemoveFriendAsync(request.FriendId, ct);
    }
}