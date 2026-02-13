using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Sync.Commands;

public record UpdateCloudSyncSettingsCommand(
    string PreferredProvider,
    bool AutoSyncOnExit,
    string? OneDriveClientId,
    string? GoogleDriveClientId,
    bool EnableBackgroundFailureAlerts,
    bool EnableBackgroundConflictAlerts,
    int BackgroundAlertCooldownSeconds) : IRequest<Result>;
