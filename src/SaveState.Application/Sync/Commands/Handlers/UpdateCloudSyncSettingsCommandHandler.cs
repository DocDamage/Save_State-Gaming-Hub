using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Sync.Commands.Handlers;

public class UpdateCloudSyncSettingsCommandHandler : IRequestHandler<UpdateCloudSyncSettingsCommand, Result>
{
    private readonly IUserPreferencesService _preferencesService;

    public UpdateCloudSyncSettingsCommandHandler(IUserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    public async Task<Result> Handle(UpdateCloudSyncSettingsCommand request, CancellationToken cancellationToken)
    {
        await _preferencesService.SetPreferredCloudProviderAsync(request.PreferredProvider, cancellationToken);
        await _preferencesService.SetAutoSyncOnExitAsync(request.AutoSyncOnExit, cancellationToken);
        await _preferencesService.SetBackgroundSyncFailureAlertsEnabledAsync(
            request.EnableBackgroundFailureAlerts,
            cancellationToken);
        await _preferencesService.SetBackgroundSyncConflictAlertsEnabledAsync(
            request.EnableBackgroundConflictAlerts,
            cancellationToken);
        await _preferencesService.SetBackgroundSyncAlertCooldownSecondsAsync(
            request.BackgroundAlertCooldownSeconds,
            cancellationToken);

        if (!string.IsNullOrEmpty(request.OneDriveClientId))
        {
            await _preferencesService.SetCloudClientIdAsync("OneDrive", request.OneDriveClientId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(request.GoogleDriveClientId))
        {
            await _preferencesService.SetCloudClientIdAsync("Google Drive", request.GoogleDriveClientId, cancellationToken);
        }

        return Result.Success();
    }
}
