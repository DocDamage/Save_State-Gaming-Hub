using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Commands;
using SaveState.Application.Sync.Commands;
using SaveState.Core.SaveStates.Services.DTOs;
using SaveState.Core.Sync;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Partial class containing network monitoring operations for CloudSyncViewModel.
/// </summary>
public partial class CloudSyncViewModel
{
    /// <summary>
    /// Command to start network monitoring.
    /// </summary>
    [RelayCommand]
    private async Task StartNetworkMonitoringAsync()
    {
        try
        {
            var result = await _mediator.Send(new StartNetworkMonitoringCommand());

            if (result.IsSuccess)
            {
                IsNetworkMonitoring = true;
                _notificationService.ShowInfo("Network monitoring started");
            }
            else
            {
                _notificationService.ShowError("Failed to start network monitoring");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start network monitoring");
            _notificationService.ShowError("Network monitoring failed");
        }
    }

    /// <summary>
    /// Command to stop network monitoring.
    /// </summary>
    [RelayCommand]
    private async Task StopNetworkMonitoringAsync()
    {
        try
        {
            var result = await _mediator.Send(new StopNetworkMonitoringCommand());

            if (result.IsSuccess)
            {
                IsNetworkMonitoring = false;
                _notificationService.ShowInfo("Network monitoring stopped");
            }
            else
            {
                _notificationService.ShowError("Failed to stop network monitoring");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop network monitoring");
            _notificationService.ShowError("Network monitoring failed");
        }
    }

    /// <summary>
    /// Command to perform network quality test.
    /// </summary>
    [RelayCommand]
    private async Task TestNetworkQualityAsync()
    {
        try
        {
            _notificationService.ShowInfo("Testing network quality...");

            var result = await _mediator.Send(new PerformNetworkQualityTestCommand());

            if (result.IsSuccess && result.Value != null)
            {
                var testResult = result.Value;
                NetworkQuality = new NetworkQualityInfo(
                    testResult.AverageLatency,
                    testResult.PacketLoss,
                    0,
                    testResult.OverallQuality);
                NetworkQualityLevel = testResult.OverallQuality;
                _notificationService.ShowSuccess($"Network quality: {testResult.OverallQuality}");
            }
            else
            {
                _notificationService.ShowError("Network quality test failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network quality test failed");
            _notificationService.ShowError("Network test failed");
        }
    }

    /// <summary>
    /// Handles network quality change events.
    /// </summary>
    private void OnNetworkQualityChanged(object? sender, NetworkQualityChangedEventArgs e)
    {
        var coreQuality = e.CurrentQuality;
        NetworkQuality = new NetworkQualityInfo(
            coreQuality.LatencyMs,
            coreQuality.PacketLossPercent,
            coreQuality.BandwidthMbps,
            coreQuality.Level.ToString());
        NetworkQualityLevel = coreQuality.Level.ToString();

        if (e.ChangeType == QualityChangeType.SignificantDrop)
        {
            _notificationService.ShowWarning("Network quality has significantly degraded");
        }
    }

    /// <summary>
    /// Handles sync progress change events.
    /// </summary>
    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e)
    {
        SyncStatusMessage = e.CurrentFile ?? "Syncing...";

        if (e.ThroughputBytesPerSecond > 1024 * 1024)
            SyncThroughput = $"{e.ThroughputBytesPerSecond / (1024 * 1024):F1} MB/s";
        else
            SyncThroughput = $"{e.ThroughputBytesPerSecond / 1024:F1} KB/s";

        if (e.EstimatedRemainingTime.HasValue)
        {
            var remaining = e.EstimatedRemainingTime.Value;
            if (remaining.TotalHours >= 1)
                SyncTimeRemaining = $"{(int)remaining.TotalHours}h {remaining.Minutes}m {remaining.Seconds}s remaining";
            else if (remaining.TotalMinutes >= 1)
                SyncTimeRemaining = $"{remaining.Minutes}m {remaining.Seconds}s remaining";
            else
                SyncTimeRemaining = $"{remaining.Seconds}s remaining";
        }
        else
        {
            SyncTimeRemaining = "Calculating...";
        }

        SyncProgress = (int)e.PercentComplete;
    }

    /// <summary>
    /// Handles sync conflict detection events.
    /// </summary>
    private void OnSyncConflictDetected(object? sender, SyncConflictEventArgs e)
    {
        var nowUtc = _timeProvider.UtcNow;
        if (nowUtc - _lastManualConflictAlertAtUtc < ManualConflictAlertCooldown)
        {
            return;
        }

        _notificationService.ShowWarning(
            "Sync conflicts detected during transfer. Open 'View Conflicts' to resolve.");
        _lastManualConflictAlertAtUtc = nowUtc;
    }

    /// <summary>
    /// Handles save-state cloud daemon status change events.
    /// </summary>
    private void OnSaveStateCloudDaemonStatusChanged(object? sender, SaveStateCloudDaemonStatus status)
    {
        Dispatcher.UIThread.Post(() => ApplyDaemonStatus(status));
    }
}
