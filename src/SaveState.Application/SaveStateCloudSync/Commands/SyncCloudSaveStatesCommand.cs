using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStateCloudSync;
using SaveState.Core.SaveStateCloudSync.Services;

namespace SaveState.Application.SaveStateCloudSync.Commands;

/// <summary>
/// Command to sync cloud save states.
/// </summary>
public sealed record SyncCloudSaveStatesCommand(SyncOptions Options) : IRequest<Result<SyncResult>>;

/// <summary>
/// Handler for SyncCloudSaveStatesCommand.
/// </summary>
public sealed class SyncCloudSaveStatesCommandHandler : IRequestHandler<SyncCloudSaveStatesCommand, Result<SyncResult>>
{
    private readonly ICloudSyncService _cloudSyncService;

    public SyncCloudSaveStatesCommandHandler(ICloudSyncService cloudSyncService)
    {
        _cloudSyncService = cloudSyncService;
    }

    public async Task<Result<SyncResult>> Handle(SyncCloudSaveStatesCommand request, CancellationToken cancellationToken)
    {
        return await _cloudSyncService.SyncAsync(request.Options, cancellationToken);
    }
}
