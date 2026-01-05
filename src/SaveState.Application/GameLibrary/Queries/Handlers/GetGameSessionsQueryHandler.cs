namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

/// <summary>
/// Handles the GetGameSessionsQuery.
/// </summary>
public class GetGameSessionsQueryHandler : IRequestHandler<GetGameSessionsQuery, IReadOnlyList<GameSession>>
{
    private readonly ISessionTrackingService _sessionTrackingService;

    /// <summary>
    /// Initializes a new instance of the GetGameSessionsQueryHandler.
    /// </summary>
    /// <param name="sessionTrackingService">The session tracking service.</param>
    public GetGameSessionsQueryHandler(ISessionTrackingService sessionTrackingService)
    {
        _sessionTrackingService = sessionTrackingService;
    }

    /// <summary>
    /// Handles the get game sessions query.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of game sessions.</returns>
    public async Task<IReadOnlyList<GameSession>> Handle(GetGameSessionsQuery request, CancellationToken cancellationToken)
    {
        var result = await _sessionTrackingService.GetSessionHistoryAsync(request.GameId, request.Limit, cancellationToken);

        return result.IsSuccess ? result.Value : new List<GameSession>();
    }
}
