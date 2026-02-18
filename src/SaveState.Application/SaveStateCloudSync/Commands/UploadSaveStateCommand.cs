using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStateCloudSync;
using SaveState.Core.SaveStateCloudSync.Services;

namespace SaveState.Application.SaveStateCloudSync.Commands;

/// <summary>
/// Command to upload a save state to cloud storage.
/// </summary>
public sealed record UploadSaveStateCommand(
    string LocalFilePath,
    string Name,
    CloudUploadOptions Options) : IRequest<Result<CloudSaveState>>;

/// <summary>
/// Handler for UploadSaveStateCommand.
/// </summary>
public sealed class UploadSaveStateCommandHandler : IRequestHandler<UploadSaveStateCommand, Result<CloudSaveState>>
{
    private readonly ICloudSyncService _cloudSyncService;

    public UploadSaveStateCommandHandler(ICloudSyncService cloudSyncService)
    {
        _cloudSyncService = cloudSyncService;
    }

    public async Task<Result<CloudSaveState>> Handle(UploadSaveStateCommand request, CancellationToken cancellationToken)
    {
        return await _cloudSyncService.UploadAsync(
            request.LocalFilePath, 
            request.Name, 
            request.Options, 
            cancellationToken);
    }
}
