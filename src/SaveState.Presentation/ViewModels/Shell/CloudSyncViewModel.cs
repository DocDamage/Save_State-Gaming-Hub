using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Commands;
using SaveState.Application.CloudServices.Queries;
using SaveState.Application.Sync.Commands;
using SaveState.Application.Sync.Queries;
using SaveState.Core.Common.Enums;
using SaveState.Core.Sync;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for cloud sync and backup management.
/// </summary>
public partial class CloudSyncViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ISyncService _syncService;
    private readonly ICloudGamingManager _cloudGamingManager;
    private readonly INetworkQualityMonitor _networkMonitor;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<CloudSyncViewModel> _logger;

    [ObservableProperty]
    private SyncStatus _currentSyncStatus;

    [ObservableProperty]
    private string _lastSyncTime = "Never";

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private int _syncProgress;

    [ObservableProperty]
    private string _syncStatusMessage = "Ready";

    [ObservableProperty]
    private bool _isProviderConfigured;

    [ObservableProperty]
    private string _currentProvider = "Not configured";

    [ObservableProperty]
    private NetworkQualityInfo? _networkQuality;

    [ObservableProperty]
    private bool _isNetworkMonitoring;

    [ObservableProperty]
    private string _networkQualityLevel = "Unknown";

    [ObservableProperty]
    private string _syncThroughput = "0 KB/s";

    [ObservableProperty]
    private string _syncTimeRemaining = string.Empty;

    public CloudSyncViewModel(
        IMediator mediator,
        ISyncService syncService,
        ICloudGamingManager cloudGamingManager,
        INetworkQualityMonitor networkMonitor,
        INotificationService notificationService,
        IDialogService dialogService,
        ILogger<CloudSyncViewModel> logger)
    {
        _mediator = mediator;
        _syncService = syncService;
        _cloudGamingManager = cloudGamingManager;
        _networkMonitor = networkMonitor;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _logger = logger;

        CloudProviders = new ObservableCollection<CloudGamingProvider>();
        ActiveSessions = new ObservableCollection<CloudSession>();
        BackupHistory = new ObservableCollection<BackupHistoryItem>();

        // Subscribe to sync events
        _syncService.ProgressChanged += OnSyncProgressChanged;
        _syncService.ConflictDetected += OnSyncConflictDetected;
        _networkMonitor.NetworkQualityChanged += OnNetworkQualityChanged;

        // Initialize async
        _ = InitializeAsync();
    }

    /// <summary>
    /// Gets the collection of available cloud gaming providers.
    /// </summary>
    public ObservableCollection<CloudGamingProvider> CloudProviders { get; }

    /// <summary>
    /// Gets the collection of active cloud gaming sessions.
    /// </summary>
    public ObservableCollection<CloudSession> ActiveSessions { get; }

    /// <summary>
    /// Gets the backup history items.
    /// </summary>
    public ObservableCollection<BackupHistoryItem> BackupHistory { get; }

    private async Task InitializeAsync()
    {
        try
        {
            // Check sync service status
            CurrentSyncStatus = _syncService.Status;
            IsProviderConfigured = CurrentSyncStatus != SyncStatus.NotConfigured;
            CurrentProvider = _syncService.ActiveProviderName ?? "Not configured";

            // Load cloud providers
            var providersResult = await _mediator.Send(new GetCloudProvidersQuery());
            if (providersResult.IsSuccess)
            {
                CloudProviders.Clear();
                foreach (var provider in providersResult.Value!)
                {
                    CloudProviders.Add(provider);
                }
            }

            // Load active sessions
            var sessionsResult = await _mediator.Send(new GetActiveCloudSessionsQuery());
            if (sessionsResult.IsSuccess)
            {
                ActiveSessions.Clear();
                foreach (var session in sessionsResult.Value!)
                {
                    ActiveSessions.Add(session);
                }
            }

            // Get current network quality
            var qualityResult = await _mediator.Send(new GetNetworkQualityCommand());
            if (qualityResult.IsSuccess && qualityResult.Value != null)
            {
                NetworkQuality = qualityResult.Value;
                NetworkQualityLevel = qualityResult.Value.QualityLevel;
            }

            IsNetworkMonitoring = _networkMonitor.IsMonitoring;

            // Load backup history
            await RefreshBackupHistoryAsync();

            _logger.LogInformation("CloudSyncViewModel initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize CloudSyncViewModel");
            _notificationService.ShowError("Failed to load cloud sync data");
        }
    }

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
                LastSyncTime = DateTime.Now.ToString("g");
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
                Name = $"Backup_{DateTime.Now:yyyy-MM-dd_HH-mm}",
                IncludeSettings = true
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess("Backup created successfully");
                // Refresh backup history
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
                // Map NetworkTestResult to NetworkQualityInfo for display
                var testResult = result.Value;
                NetworkQuality = new NetworkQualityInfo(
                    testResult.AverageLatency,
                    testResult.PacketLoss,
                    0, // BandwidthMbps not available in test result
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
    /// Command to configure cloud provider.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureProviderAsync()
    {
        try
        {
            _logger.LogInformation("Opening provider configuration dialog");

            var result = await _dialogService.ShowCloudProviderConfigDialogAsync(CurrentProvider);
            if (result != null)
            {
                // Update configuration via mediator
            var oneDriveClientId = result.ProviderName == "OneDrive" ? result.ApiKey : null;
            var googleDriveClientId = result.ProviderName == "GoogleDrive" ? result.ApiKey : null;

            var updateResult = await _mediator.Send(new UpdateCloudSyncSettingsCommand(
                result.ProviderName,
                result.EnableAutoSync,
                oneDriveClientId,
                googleDriveClientId
            ));

                if (updateResult.IsSuccess)
                {
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

            var conflicts = await _syncService.GetConflictsAsync();
            if (conflicts.Count == 0)
            {
                _notificationService.ShowInfo("No conflicts detected.");
                return;
            }

            var viewModels = conflicts.Select(c => new Services.SyncConflictViewModel(
                c.RemotePath,
                c.LocalModified,
                c.RemoteModified,
                File.Exists(c.LocalPath) ? new FileInfo(c.LocalPath).Length : 0,
                c.RemoteSize
            )).ToArray();

            var result = await _dialogService.ShowConflictResolutionDialogAsync(viewModels);
            if (result != null)
            {
                var successCount = 0;
                foreach (var resolution in result.Resolutions)
                {
                    // Map file path back to local path if needed
                    var conflict = conflicts.FirstOrDefault(c => c.RemotePath == resolution.Key);
                    if (conflict != null)
                    {
                        if (await _syncService.ResolveConflictAsync(conflict.LocalPath, resolution.Value))
                        {
                            successCount++;
                        }
                    }
                }

                _notificationService.ShowSuccess($"Successfully resolved {successCount} of {result.Resolutions.Count} conflicts");

                // Refresh status
                CurrentSyncStatus = _syncService.Status;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show conflict resolution dialog");
            _notificationService.ShowError("Failed to show conflict resolution");
        }
    }

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
                        BackupType = "Full", // Could be extended to include different types
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

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e)
    {
        // Update status message with current file being synced
        SyncStatusMessage = e.CurrentFile ?? "Syncing...";

        // Update throughput
        if (e.ThroughputBytesPerSecond > 1024 * 1024)
            SyncThroughput = $"{e.ThroughputBytesPerSecond / (1024 * 1024):F1} MB/s";
        else
            SyncThroughput = $"{e.ThroughputBytesPerSecond / 1024:F1} KB/s";

        // Update time remaining
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

        // SyncProgress is already bound to 0-100 probably, but let's check PercentComplete
        SyncProgress = (int)e.PercentComplete;
    }

    private void OnSyncConflictDetected(object? sender, SyncConflictEventArgs e)
    {
        _notificationService.ShowWarning($"Sync conflict detected: {e.LocalPath}");
    }

    private void OnNetworkQualityChanged(object? sender, NetworkQualityChangedEventArgs e)
    {
        // Map Core DTO to Application DTO
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
}

/// <summary>
/// Represents a backup history item for display.
/// </summary>
public class BackupHistoryItem
{
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required long SizeBytes { get; init; }
    public required string BackupType { get; init; }
    public required string Status { get; init; }
}
