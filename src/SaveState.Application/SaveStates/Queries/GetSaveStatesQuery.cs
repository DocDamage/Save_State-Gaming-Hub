using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Entities;
using SaveState.Core.SaveStates.Services;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Application.SaveStates.Queries;

public sealed record GetSaveStatesQuery(Guid GameId) : IRequest<Result<IReadOnlyList<SaveStateEntity>>>;

public sealed class GetSaveStatesQueryHandler : IRequestHandler<GetSaveStatesQuery, Result<IReadOnlyList<SaveStateEntity>>>
{
    private readonly ISaveStateManager _saveStateManager;

    public GetSaveStatesQueryHandler(ISaveStateManager saveStateManager)
    {
        _saveStateManager = saveStateManager;
    }

    public async Task<Result<IReadOnlyList<SaveStateEntity>>> Handle(GetSaveStatesQuery request, CancellationToken ct)
    {
        return await _saveStateManager.GetSaveStatesAsync(request.GameId, ct);
    }
}