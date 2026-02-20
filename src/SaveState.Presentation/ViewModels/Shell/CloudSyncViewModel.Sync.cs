using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Commands;
using SaveState.Application.CloudServices.Queries;
using SaveState.Application.Sync.Commands;
using SaveState.Application.Sync.Queries;
using SaveState.Core.Common.Enums;
using SaveState.Core.SaveStates.Services.DTOs;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Partial class containing sync operations for CloudSyncViewModel.
/// </summary>
public partial class CloudSyncViewModel
{
    /// <summary>
    /// Command to perform full sync.
    /// </summary>
    [RelayCommand]
    private async Task SyncAsync()
    {
        try
        {
            IsSyncing = true;
            SyncStatusMessage = "Syncing...";

            var result = await _syncService.SyncAsync();

            if (result.Success)
            {
                LastSyncTime = _timeProvider.Now.ToString("g");
                _notificationService.ShowSuccess($"Sync complete: {result.FilesUploaded} uploaded, {result.FilesDownloaded} downloaded");
            }
            else
            {
                _notificationService.ShowError($"Sync failed: {string.Join(", ", result.Errors)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync operation failed");
            _notificationService.ShowError("Sync failed");
        }
        finally
        {
            IsSyncing = false;
            SyncStatusMessage = "Ready";
            SyncProgress = 0;
        }
    }

    /// <summary>
    /// Command to push local changes to cloud.
    /// </summary>
    [RelayCommand]
    private async Task PushAsync()
    {
        try
        {
            IsSyncing = true;
            SyncStatusMessage = "Uploading...";

            var result = await _syncService.PushAsync();

            if (result.Success)
            {
                _notificationService.ShowSuccess($"Push complete: {result.FilesUploaded} files uploaded");
            }
            else
            {
                _notificationService.ShowError($"Push failed: {string.Join(", ", result.Errors)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push operation failed");
            _notificationService.ShowError("Push failed");
        }
        finally
        {
            IsSyncing = false;
            SyncStatusMessage = "Ready";
        }
    }

    /// <summary>
    /// Command to pull cloud changes to local.
    /// </summary>
    [RelayCommand]
    private async Task PullAsync()
    {
        try
        {
            IsSyncing = true;
            SyncStatusMessage = "Downloading...";

            var result = await _syncService.PullAsync();

            if (result.Success)
            {
                _notificationService.ShowSuccess($"Pull complete: {result.FilesDownloaded} files downloaded");
            }
            else
            {
                _notificationService.ShowError($"Pull failed: {string.Join(", ", result.Errors)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull operation failed");
            _notificationService.ShowError("Pull failed");
        }
        finally
        {
            IsSyncing = false;
            SyncStatusMessage = "Ready";
        }
    }

    /// <summary>
    /// Command to create a backup.
    /// </summary>
    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            var command = new CreateBackupCommand
            {
                Type = BackupType.Full,
                Name = $"Backup_{_timeProvider.Now:yyyy-MM-dd_HH-mm}",
                IncludeSettings = true
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess("Backup created successfully");
                await RefreshBackupHistoryAsync();
            }
            else
            {
                _notificationService.ShowError($"Backup failed: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            _notificationService.ShowError("Backup creation failed");
        }
    }

    /// <summary>
    /// Refreshes the backup history collection.
    /// </summary>
    private async Task RefreshBackupHistoryAsync()
    {
        try
        {
            var result = await _mediator.Send(new GetBackupHistoryQuery());
            if (result.IsSuccess && result.Value != null)
            {
                BackupHistory.Clear();
                foreach (var backup in result.Value)
                {
                    BackupHistory.Add(new BackupHistoryItem
                    {
                        Name = backup.Name,
                        CreatedAt = backup.CreatedAt,
                        SizeBytes = backup.TotalSize,
                        BackupType = "Full",
                        Status = "Complete"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh backup history");
        }
    }

    /// <summary>
    /// Command to view background sync details.
    /// </summary>
    [RelayCommand]
    private async Task ViewBackgroundSyncDetailsAsync()
    {
        try
        {
            var status = _saveStateCloudSyncMonitor.CurrentStatus;
            var details = BuildBackgroundSyncDetails(status);

            await _dialogService.ShowInformationAsync("Background Save-State Sync", details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show background sync details");
            _notificationService.ShowError("Failed to show background sync details");
        }
    }

    /// <summary>
    /// Command to resolve background sync conflicts.
    /// </summary>
    [RelayCommand]
    private async Task ResolveBackgroundConflictsAsync()
    {
        await ViewConflictsAsync();
    }

    /// <summary>
    /// Command to retry background sync.
    /// </summary>
    [RelayCommand]
    private async Task RetryBackgroundSyncAsync()
    {
        if (IsSyncing)
        {
            return;
        }

        await SyncAsync();
    }

    /// <summary>
    /// Command to open background sync settings.
    /// </summary>
    [RelayCommand]
    private async Task OpenBackgroundSyncSettingsAsync()
    {
        await ConfigureProviderAsync();
    }

    /// <summary>
    /// Builds the details text for background sync status.
    /// </summary>
    private static string BuildBackgroundSyncDetails(SaveStateCloudDaemonStatus status)
    {
        var lines = new List<string>
        {
            $"Enabled: {status.Enabled}",
            $"Running: {status.IsRunning}",
            $"Updated: {status.UpdatedAtUtc.ToLocalTime():g}",
            $"Last Auto Sync: {(status.LastSyncAtUtc.HasValue ? status.LastSyncAtUtc.Value.ToLocalTime().ToString("g") : "Never")}",
            $"Last Game: {(status.LastGameId.HasValue ? status.LastGameId.Value.ToString() : "None")}",
            $"Successful syncs: {status.SuccessfulSyncCount}",
            $"Failed syncs: {status.FailedSyncCount}",
            $"Conflicts: {status.ConflictCount}",
            $"Skipped: {status.SkippedCount}",
            $"Last message: {status.LastMessage}"
        };

        if (status.ConflictCount > 0)
        {
            lines.Add("Action: Open 'View Conflicts' to resolve save-state conflicts.");
        }

        if (status.FailedSyncCount > 0)
        {
            lines.Add("Action: Review provider settings and network quality if failures persist.");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
