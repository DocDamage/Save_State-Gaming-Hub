namespace SaveState.Application.Social.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

/// <summary>
/// Handler for adding friends to the user's social network.
/// Manages friend request sending and relationship establishment.
/// </summary>
public class AddFriendCommandHandler : IRequestHandler<AddFriendCommand, Result>
{
    private readonly ISocialService _socialService;

    public AddFriendCommandHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    /// <summary>
    /// Handles the command to add a friend.
    /// </summary>
    /// <param name="request">The add friend command containing the friend ID.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> Handle(AddFriendCommand request, CancellationToken ct)
    {
        return await _socialService.SendFriendRequestAsync(request.FriendId, ct);
    }
}