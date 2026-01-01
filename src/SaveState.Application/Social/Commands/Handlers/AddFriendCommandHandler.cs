namespace SaveState.Application.Social.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

public class AddFriendCommandHandler : IRequestHandler<AddFriendCommand, Result>
{
    private readonly ISocialService _socialService;

    public AddFriendCommandHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    public async Task<Result> Handle(AddFriendCommand request, CancellationToken ct)
    {
        return await _socialService.SendFriendRequestAsync(request.FriendId, ct);
    }
}