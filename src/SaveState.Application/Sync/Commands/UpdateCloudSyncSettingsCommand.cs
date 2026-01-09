using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Sync.Commands;

public record UpdateCloudSyncSettingsCommand(
    string PreferredProvider,
    bool AutoSyncOnExit,
    string? OneDriveClientId,
    string? GoogleDriveClientId) : IRequest<Result>;
