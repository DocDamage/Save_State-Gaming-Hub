namespace SaveState.Application.Social.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;
using SaveState.Core.Social.Entities;

public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, Result<IReadOnlyList<Friend>>>
{
    private readonly ISocialService _socialService;

    public GetFriendsQueryHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    public async Task<Result<IReadOnlyList<Friend>>> Handle(GetFriendsQuery request, CancellationToken ct)
    {
        return await _socialService.GetFriendsAsync(ct);
    }
}