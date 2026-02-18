using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStateCloudSync.Services;

namespace SaveState.Application.SaveStateCloudSync.Commands;

/// <summary>
/// Command to delete a cloud save state.
/// </summary>
public sealed record DeleteCloudSaveStateCommand(string CloudId) : IRequest<Result>;

/// <summary>
/// Handler for DeleteCloudSaveStateCommand.
/// </summary>
public sealed class DeleteCloudSaveStateCommandHandler : IRequestHandler<DeleteCloudSaveStateCommand, Result>
{
    private readonly ICloudSyncService _cloudSyncService;

    public DeleteCloudSaveStateCommandHandler(ICloudSyncService cloudSyncService)
    {
        _cloudSyncService = cloudSyncService;
    }

    public async Task<Result> Handle(DeleteCloudSaveStateCommand request, CancellationToken cancellationToken)
    {
        return await _cloudSyncService.DeleteAsync(request.CloudId, cancellationToken);
    }
}
