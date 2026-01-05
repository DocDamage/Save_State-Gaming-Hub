using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Commands;
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

    public CloudSyncViewModel(
        IMediator mediator,
        ISyncService syncService,
        ICloudGamingManager cloudGamingManager,
        INetworkQualityMonitor networkMonitor,
        INotificationService notificationService,
        ILogger<CloudSyncViewModel> logger)
    {
        _mediator = mediator;
        _syncService = syncService;
        _cloudGamingManager = cloudGamingManager;
        _networkMonitor = networkMonitor;
        _notificationService = notificationService;
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
    private void ConfigureProvider()
    {
        _logger.LogInformation("Opening provider configuration dialog");
        _notificationService.ShowInfo("Provider Configuration", "Cloud provider configuration dialog");
        // Future: Show actual configuration dialog with provider selection and credentials
    }

    /// <summary>
    /// Command to view sync conflicts.
    /// </summary>
    [RelayCommand]
    private void ViewConflicts()
    {
        _logger.LogInformation("Opening conflicts resolution overlay");
        _notificationService.ShowInfo("Sync Conflicts", "Conflict resolution interface");
        // Future: Show actual conflicts list with resolution options
    }

    private async Task RefreshBackupHistoryAsync()
    {
        // TODO: Implement backup history loading
        await Task.CompletedTask;
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e)
    {
        // Update status message with current file being synced
        SyncStatusMessage = e.CurrentFile ?? "Syncing...";
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
