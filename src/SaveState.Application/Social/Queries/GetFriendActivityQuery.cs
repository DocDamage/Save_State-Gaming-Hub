using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Application.Social.Queries;

/// <summary>
/// Query to get the friend activity feed.
/// </summary>
public record GetFriendActivityQuery(int Limit = 50) : IRequest<Result<IReadOnlyList<FriendActivity>>>;

/// <summary>
/// Handler for getting friend activity.
/// </summary>
public class GetFriendActivityQueryHandler : IRequestHandler<GetFriendActivityQuery, Result<IReadOnlyList<FriendActivity>>>
{
    private readonly Core.Social.Services.IFriendActivityService _friendActivityService;

    public GetFriendActivityQueryHandler(Core.Social.Services.IFriendActivityService friendActivityService)
    {
        _friendActivityService = friendActivityService;
    }

    public async Task<Result<IReadOnlyList<FriendActivity>>> Handle(GetFriendActivityQuery request, CancellationToken ct)
    {
        return await _friendActivityService.GetActivityFeedAsync(request.Limit, ct);
    }
}