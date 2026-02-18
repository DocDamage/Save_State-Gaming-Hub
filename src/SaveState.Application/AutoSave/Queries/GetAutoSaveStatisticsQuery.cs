using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Queries;

/// <summary>
/// Query to get auto-save statistics for a game.
/// </summary>
public sealed record GetAutoSaveStatisticsQuery(Guid GameId) : IRequest<Result<AutoSaveStatistics>>;

/// <summary>
/// Handler for GetAutoSaveStatisticsQuery.
/// </summary>
public sealed class GetAutoSaveStatisticsQueryHandler : IRequestHandler<GetAutoSaveStatisticsQuery, Result<AutoSaveStatistics>>
{
    private readonly IAutoSaveService _autoSaveService;

    public GetAutoSaveStatisticsQueryHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result<AutoSaveStatistics>> Handle(GetAutoSaveStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _autoSaveService.GetStatisticsAsync(request.GameId, cancellationToken);
    }
}
