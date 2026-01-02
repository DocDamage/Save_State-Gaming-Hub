namespace SaveState.Application.Social.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;
using SaveState.Core.Social.Entities;

/// <summary>
/// Handler for retrieving the user's friend list.
/// Provides access to social connections and friend information.
/// </summary>
public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, Result<IReadOnlyList<Friend>>>
{
    private readonly ISocialService _socialService;

    public GetFriendsQueryHandler(ISocialService socialService)
    {
        _socialService = socialService;
    }

    /// <summary>
    /// Handles the query to get the user's friend list.
    /// </summary>
    /// <param name="request">The get friends query.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of friends or an error.</returns>
    public async Task<Result<IReadOnlyList<Friend>>> Handle(GetFriendsQuery request, CancellationToken ct)
    {
        return await _socialService.GetFriendsAsync(ct);
    }
}