using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStateCloudSync;
using SaveState.Core.SaveStateCloudSync.Services;

namespace SaveState.Application.SaveStateCloudSync.Queries;

/// <summary>
/// Query to list cloud save states.
/// </summary>
public sealed record ListCloudSaveStatesQuery(
    string? Provider = null, 
    int? GameId = null) : IRequest<Result<List<CloudSaveState>>>;

/// <summary>
/// Handler for ListCloudSaveStatesQuery.
/// </summary>
public sealed class ListCloudSaveStatesQueryHandler : IRequestHandler<ListCloudSaveStatesQuery, Result<List<CloudSaveState>>>
{
    private readonly ICloudSyncService _cloudSyncService;

    public ListCloudSaveStatesQueryHandler(ICloudSyncService cloudSyncService)
    {
        _cloudSyncService = cloudSyncService;
    }

    public async Task<Result<List<CloudSaveState>>> Handle(ListCloudSaveStatesQuery request, CancellationToken cancellationToken)
    {
        return await _cloudSyncService.ListAsync(request.Provider, request.GameId, cancellationToken);
    }
}
