using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStateCloudSync;
using SaveState.Core.SaveStateCloudSync.Services;

namespace SaveState.Application.SaveStateCloudSync.Queries;

/// <summary>
/// Query to get available cloud providers.
/// </summary>
public sealed record GetCloudProvidersQuery : IRequest<Result<List<CloudProviderInfo>>>;

/// <summary>
/// Handler for GetCloudProvidersQuery.
/// </summary>
public sealed class GetCloudProvidersQueryHandler : IRequestHandler<GetCloudProvidersQuery, Result<List<CloudProviderInfo>>>
{
    private readonly ICloudSyncService _cloudSyncService;

    public GetCloudProvidersQueryHandler(ICloudSyncService cloudSyncService)
    {
        _cloudSyncService = cloudSyncService;
    }

    public async Task<Result<List<CloudProviderInfo>>> Handle(GetCloudProvidersQuery request, CancellationToken cancellationToken)
    {
        return await _cloudSyncService.GetProvidersAsync(cancellationToken);
    }
}
