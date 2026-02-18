using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStateCloudSync;
using SaveState.Core.SaveStateCloudSync.Services;

namespace SaveState.Application.SaveStateCloudSync.Queries;

/// <summary>
/// Query to get cloud sync statistics.
/// </summary>
public sealed record GetSyncStatsQuery : IRequest<Result<CloudSyncStats>>;

/// <summary>
/// Handler for GetSyncStatsQuery.
/// </summary>
public sealed class GetSyncStatsQueryHandler : IRequestHandler<GetSyncStatsQuery, Result<CloudSyncStats>>
{
    private readonly ICloudSyncService _cloudSyncService;

    public GetSyncStatsQueryHandler(ICloudSyncService cloudSyncService)
    {
        _cloudSyncService = cloudSyncService;
    }

    public async Task<Result<CloudSyncStats>> Handle(GetSyncStatsQuery request, CancellationToken cancellationToken)
    {
        return await _cloudSyncService.GetStatsAsync(cancellationToken);
    }
}
