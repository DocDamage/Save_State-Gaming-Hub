using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Services;

namespace SaveState.Application.SaveStates.Queries;

public sealed record GetSaveStateTimelineQuery(Guid GameId) : IRequest<Result<SaveStateTimeline>>;

public sealed class GetSaveStateTimelineQueryHandler : IRequestHandler<GetSaveStateTimelineQuery, Result<SaveStateTimeline>>
{
    private readonly ISaveStateManager _saveStateManager;

    public GetSaveStateTimelineQueryHandler(ISaveStateManager saveStateManager)
    {
        _saveStateManager = saveStateManager;
    }

    public async Task<Result<SaveStateTimeline>> Handle(GetSaveStateTimelineQuery request, CancellationToken ct)
    {
        return await _saveStateManager.GetTimelineAsync(request.GameId, ct);
    }
}