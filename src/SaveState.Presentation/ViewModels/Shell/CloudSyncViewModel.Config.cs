using System.IO;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Commands;
using SaveState.Application.CloudServices.Queries;
using SaveState.Application.Sync.Commands;
using SaveState.Application.Sync.Queries;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Services.DTOs;
using SaveState.Core.Sync.Services.DTOs;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Partial class containing configuration operations for CloudSyncViewModel.
/// </summary>
public partial class CloudSyncViewModel
{
    /// <summary>
    /// Command to configure cloud provider.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureProviderAsync()
    {
        try
        {
            _logger.LogInformation("Opening provider configuration dialog");

            var settingsResult = await _mediator.Send(new GetCloudSyncSettingsQuery());
            var currentSettings = BuildDialogSettings(settingsResult);
            if (settingsResult.IsSuccess && settingsResult.Value is not null)
            {
                ApplyCloudSyncSettings(settingsResult.Value);
            }

            var result = await _dialogService.ShowCloudProviderConfigDialogAsync(currentSettings);
            if (result != null)
            {
                var normalizedProvider = NormalizeProviderName(result.ProviderName);
                var oneDriveClientId = normalizedProvider == "onedrive" ? result.ApiKey : null;
                var googleDriveClientId = normalizedProvider == "googledrive" ? result.ApiKey : null;

                var updateResult = await _mediator.Send(new UpdateCloudSyncSettingsCommand(
                    result.ProviderName,
                    result.EnableAutoSync,
                    oneDriveClientId,
                    googleDriveClientId,
                    result.EnableBackgroundFailureAlerts,
                    result.EnableBackgroundConflictAlerts,
                    result.AlertCooldownSeconds
                ));

                if (updateResult.IsSuccess)
                {
                    _daemonFailureAlertsEnabled = result.EnableBackgroundFailureAlerts;
                    _daemonConflictAlertsEnabled = result.EnableBackgroundConflictAlerts;
                    _daemonAlertCooldownSeconds = ClampDaemonAlertCooldownSeconds(result.AlertCooldownSeconds);
                    CurrentProvider = result.ProviderName;
                    IsProviderConfigured = result.ProviderName != "Not configured";
                    _notificationService.ShowSuccess($"Cloud provider configured: {result.ProviderName}");
                    await InitializeAsync();
                }
                else
                {
                    _notificationService.ShowError("Failed to save cloud sync settings");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure cloud provider");
            _notificationService.ShowError("Failed to configure cloud provider");
        }
    }

    /// <summary>
    /// Command to view sync conflicts.
    /// </summary>
    [RelayCommand]
    private async Task ViewConflictsAsync()
    {
        try
        {
            _logger.LogInformation("Opening conflicts resolution dialog");

            var fileConflicts = await _syncService.GetConflictsAsync();
            var conflictEntries = fileConflicts.Select(c => new Services.SyncConflictViewModel(
                c.RemotePath,
                c.LocalModified,
                c.RemoteModified,
                File.Exists(c.LocalPath) ? new FileInfo(c.LocalPath).Length : 0,
                c.RemoteSize
            )).ToList();

            var saveStateConflictMap = await AppendSaveStateConflictsAsync(conflictEntries);
            if (conflictEntries.Count == 0)
            {
                _notificationService.ShowInfo("No conflicts detected.");
                return;
            }

            var result = await _dialogService.ShowConflictResolutionDialogAsync(conflictEntries.ToArray());
            if (result != null)
            {
                var successCount = 0;
                var saveStateResolvedCount = 0;
                var failureMessages = new List<string>();
                var encryptionKeyCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var resolution in result.Resolutions)
                {
                    if (saveStateConflictMap.TryGetValue(resolution.Key, out var saveStateConflict))
                    {
                        var saveStateResult = await ResolveSaveStateConflictAsync(
                            resolution.Key,
                            saveStateConflict,
                            encryptionKeyCache,
                            resolution.Value);
                        if (saveStateResult.Success)
                        {
                            successCount++;
                            saveStateResolvedCount++;
                        }
                        else if (!string.IsNullOrWhiteSpace(saveStateResult.Error))
                        {
                            failureMessages.Add(saveStateResult.Error);
                        }

                        continue;
                    }

                    var fileConflict = fileConflicts.FirstOrDefault(c => c.RemotePath == resolution.Key);
                    if (fileConflict != null)
                    {
                        var resolved = await _syncService.ResolveConflictAsync(fileConflict.LocalPath, resolution.Value);
                        if (resolved)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureMessages.Add(
                                $"File conflict '{fileConflict.RemotePath}' failed with strategy '{resolution.Value}'.");
                        }
                    }
                }

                var totalCount = result.Resolutions.Count;
                var failureSummary = BuildFailureSummary(failureMessages);

                if (successCount == totalCount)
                {
                    _notificationService.ShowSuccess(
                        $"Successfully resolved {successCount} of {totalCount} conflicts ({saveStateResolvedCount} save-state).");
                }
                else if (successCount > 0)
                {
                    _notificationService.ShowWarning(
                        $"Resolved {successCount} of {totalCount} conflicts ({saveStateResolvedCount} save-state). {failureSummary}");
                }
                else
                {
                    _notificationService.ShowError($"No conflicts were resolved. {failureSummary}");
                }

                CurrentSyncStatus = _syncService.Status;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show conflict resolution dialog");
            _notificationService.ShowError("Failed to show conflict resolution");
        }
    }

    /// <summary>
    /// Loads cloud sync settings from the settings service.
    /// </summary>
    private async Task LoadCloudSyncSettingsAsync()
    {
        try
        {
            var settingsResult = await _mediator.Send(new GetCloudSyncSettingsQuery());
            if (settingsResult.IsSuccess && settingsResult.Value is not null)
            {
                ApplyCloudSyncSettings(settingsResult.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to load cloud sync settings");
        }
    }

    /// <summary>
    /// Applies cloud sync settings to view model state.
    /// </summary>
    private void ApplyCloudSyncSettings(CloudSyncSettingsDto settings)
    {
        _daemonFailureAlertsEnabled = settings.EnableBackgroundFailureAlerts;
        _daemonConflictAlertsEnabled = settings.EnableBackgroundConflictAlerts;
        _daemonAlertCooldownSeconds = ClampDaemonAlertCooldownSeconds(settings.BackgroundAlertCooldownSeconds);

        if (!string.IsNullOrWhiteSpace(settings.PreferredProvider))
        {
            CurrentProvider = settings.PreferredProvider;
            IsProviderConfigured = !string.Equals(
                settings.PreferredProvider,
                "Not configured",
                StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Builds dialog settings from the current settings result.
    /// </summary>
    private CloudProviderConfigResult BuildDialogSettings(Result<CloudSyncSettingsDto> settingsResult)
    {
        var settings = settingsResult.IsSuccess ? settingsResult.Value : null;
        var providerName = settings?.PreferredProvider;
        if (string.IsNullOrWhiteSpace(providerName))
        {
            providerName = string.IsNullOrWhiteSpace(CurrentProvider)
                ? "GoogleDrive"
                : CurrentProvider;
        }

        var normalizedProvider = NormalizeProviderName(providerName);
        var apiKey = normalizedProvider switch
        {
            "onedrive" => settings?.OneDriveClientId ?? string.Empty,
            "googledrive" => settings?.GoogleDriveClientId ?? string.Empty,
            _ => string.Empty
        };

        return new CloudProviderConfigResult(
            providerName,
            apiKey,
            null,
            settings?.AutoSyncOnExit ?? true,
            settings?.EnableBackgroundFailureAlerts ?? _daemonFailureAlertsEnabled,
            settings?.EnableBackgroundConflictAlerts ?? _daemonConflictAlertsEnabled,
            ClampDaemonAlertCooldownSeconds(settings?.BackgroundAlertCooldownSeconds ?? _daemonAlertCooldownSeconds));
    }

    /// <summary>
    /// Normalizes a provider name for comparison.
    /// </summary>
    private static string NormalizeProviderName(string? providerName)
    {
        return (providerName ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
