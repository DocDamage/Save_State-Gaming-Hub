namespace SaveState.Application.Social.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

public class RemoveFriendCommandHandler : IRequestHandler<RemoveFriendCommand, Result>
{
    private readonly ISocialService _socialService;

    public RemoveFriendCommandHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    public async Task<Result> Handle(RemoveFriendCommand request, CancellationToken ct)
    {
        return await _socialService.RemoveFriendAsync(request.FriendId, ct);
    }
}