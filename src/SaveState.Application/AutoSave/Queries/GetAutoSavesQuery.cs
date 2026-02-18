using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Queries;

/// <summary>
/// Query to get auto-saves for a game.
/// </summary>
public sealed record GetAutoSavesQuery(
    Guid GameId,
    AutoSaveTriggerType? TriggerType = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool? OnlyLocked = null) : IRequest<Result<List<AutoSaveEntry>>>;

/// <summary>
/// Handler for GetAutoSavesQuery.
/// </summary>
public sealed class GetAutoSavesQueryHandler : IRequestHandler<GetAutoSavesQuery, Result<List<AutoSaveEntry>>>
{
    private readonly IAutoSaveService _autoSaveService;

    public GetAutoSavesQueryHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result<List<AutoSaveEntry>>> Handle(GetAutoSavesQuery request, CancellationToken cancellationToken)
    {
        var filter = new AutoSaveFilter
        {
            GameId = request.GameId,
            TriggerType = request.TriggerType,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            OnlyLocked = request.OnlyLocked
        };

        return await _autoSaveService.GetAutoSavesAsync(request.GameId, filter, cancellationToken);
    }
}
