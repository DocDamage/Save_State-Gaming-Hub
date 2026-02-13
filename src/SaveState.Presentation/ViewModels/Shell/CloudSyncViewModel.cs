using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Commands;
using SaveState.Application.CloudServices.Queries;
using SaveState.Application.Sync.Commands;
using SaveState.Application.Sync.Queries;
using SaveState.Core.Common;
using SaveState.Core.Common.Enums;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates.Services.DTOs;
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
    private const int MinDaemonAlertCooldownSeconds = 15;
    private const int MaxDaemonAlertCooldownSeconds = 600;
    private const int DefaultDaemonAlertCooldownSeconds = 60;
    private static readonly TimeSpan ManualConflictAlertCooldown = TimeSpan.FromSeconds(15);

    private readonly IMediator _mediator;
    private readonly ISyncService _syncService;
    private readonly ICloudGamingManager _cloudGamingManager;
    private readonly INetworkQualityMonitor _networkMonitor;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<CloudSyncViewModel> _logger;
    private readonly ICloudCatalogService _cloudCatalogService;
    private readonly ITimeProvider _timeProvider;
    private readonly ISaveStateCloudService _saveStateCloudService;
    private readonly IGameRepository _gameRepository;
    private readonly ISaveStateCloudSyncMonitor _saveStateCloudSyncMonitor;
    private SaveStateCloudDaemonStatus? _lastDaemonStatusSnapshot;
    private int _pendingDaemonFailureAlerts;
    private int _pendingDaemonConflictAlerts;
    private DateTime _lastDaemonFailureAlertAtUtc = DateTime.MinValue;
    private DateTime _lastDaemonConflictAlertAtUtc = DateTime.MinValue;
    private DateTime _lastManualConflictAlertAtUtc = DateTime.MinValue;
    private bool _daemonFailureAlertsEnabled = true;
    private bool _daemonConflictAlertsEnabled = true;
    private int _daemonAlertCooldownSeconds = DefaultDaemonAlertCooldownSeconds;

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

    [ObservableProperty]
    private int _availableCloudGamesCount;

    [ObservableProperty]
    private ObservableCollection<CloudCatalogEntry> _topCloudGames = new();

    [ObservableProperty]
    private bool _isBackgroundSyncEnabled;

    [ObservableProperty]
    private string _backgroundDaemonState = "Unknown";

    [ObservableProperty]
    private string _backgroundDaemonLastSync = "Never";

    [ObservableProperty]
    private string _backgroundDaemonSummary = "0 successful | 0 failed | 0 conflicts | 0 skipped";

    [ObservableProperty]
    private string _backgroundDaemonMessage = "Daemon status unavailable.";

    [ObservableProperty]
    private string _backgroundDaemonHealthStatus = "Unknown";

    [ObservableProperty]
    private string _backgroundDaemonHealthCue = "Background sync health unavailable.";

    [ObservableProperty]
    private bool _showResolveConflictsQuickAction;

    [ObservableProperty]
    private bool _showRetrySyncQuickAction;

    [ObservableProperty]
    private bool _showConfigureProviderQuickAction;

    [ObservableProperty]
    private bool _hasBackgroundQuickActions;

    public CloudSyncViewModel(
        IMediator mediator,
        ISyncService syncService,
        ICloudGamingManager cloudGamingManager,
        INetworkQualityMonitor networkMonitor,
        INotificationService notificationService,
        IDialogService dialogService,
        ILogger<CloudSyncViewModel> logger,
        ICloudCatalogService cloudCatalogService,
        ITimeProvider timeProvider,
        ISaveStateCloudService saveStateCloudService,
        IGameRepository gameRepository,
        ISaveStateCloudSyncMonitor saveStateCloudSyncMonitor)
    {
        _mediator = mediator;
        _syncService = syncService;
        _cloudGamingManager = cloudGamingManager;
        _networkMonitor = networkMonitor;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _logger = logger;
        _cloudCatalogService = cloudCatalogService;
        _timeProvider = timeProvider;
        _saveStateCloudService = saveStateCloudService;
        _gameRepository = gameRepository;
        _saveStateCloudSyncMonitor = saveStateCloudSyncMonitor;

        CloudProviders = new ObservableCollection<CloudGamingProvider>();
        ActiveSessions = new ObservableCollection<CloudSession>();
        BackupHistory = new ObservableCollection<BackupHistoryItem>();
        TopCloudGames = new ObservableCollection<CloudCatalogEntry>();

        // Subscribe to sync events
        _syncService.ProgressChanged += OnSyncProgressChanged;
        _syncService.ConflictDetected += OnSyncConflictDetected;
        _networkMonitor.NetworkQualityChanged += OnNetworkQualityChanged;
        _saveStateCloudSyncMonitor.StatusChanged += OnSaveStateCloudDaemonStatusChanged;
        ApplyDaemonStatus(_saveStateCloudSyncMonitor.CurrentStatus);

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
            await LoadCloudSyncSettingsAsync();

            // Load cloud providers
            var providersResult = await _mediator.Send(new GetCloudProvidersQuery());
            if (providersResult.IsSuccess && providersResult.Value is not null)
            {
                CloudProviders.Clear();
                foreach (var provider in providersResult.Value)
                {
                    CloudProviders.Add(provider);
                }
            }

            // Load active sessions
            var sessionsResult = await _mediator.Send(new GetActiveCloudSessionsQuery());
            if (sessionsResult.IsSuccess && sessionsResult.Value is not null)
            {
                ActiveSessions.Clear();
                foreach (var session in sessionsResult.Value)
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

            // Load initial catalog metadata
            await LoadCloudCatalogAsync();

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

            var settingsResult = await _mediator.Send(new GetCloudSyncSettingsQuery());
            var currentSettings = BuildDialogSettings(settingsResult);
            if (settingsResult.IsSuccess && settingsResult.Value is not null)
            {
                ApplyCloudSyncSettings(settingsResult.Value);
            }

            var result = await _dialogService.ShowCloudProviderConfigDialogAsync(currentSettings);
            if (result != null)
            {
                // Update configuration via mediator
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

    [RelayCommand]
    private async Task BrowseCatalogAsync()
    {
        try
        {
            var catalogResult = await _cloudCatalogService.GetCatalogAsync();
            if (!catalogResult.IsSuccess || catalogResult.Value == null)
            {
                _notificationService.ShowError("Failed to load cloud catalog");
                return;
            }

            var popularGames = catalogResult.Value.Games
                .OrderByDescending(g => g.Providers.Count)
                .ThenBy(g => g.Title)
                .Take(8)
                .ToList();

            if (popularGames.Count == 0)
            {
                _notificationService.ShowInfo("No popular cloud games available right now");
                return;
            }

            TopCloudGames.Clear();
            foreach (var entry in popularGames)
            {
                TopCloudGames.Add(entry);
            }

            _notificationService.ShowInfo("Top Cloud Games refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to browse cloud catalog");
            _notificationService.ShowError("Failed to browse catalog");
        }
    }

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

    [RelayCommand]
    private async Task ResolveBackgroundConflictsAsync()
    {
        await ViewConflictsAsync();
    }

    [RelayCommand]
    private async Task RetryBackgroundSyncAsync()
    {
        if (IsSyncing)
        {
            return;
        }

        await SyncAsync();
    }

    [RelayCommand]
    private async Task OpenBackgroundSyncSettingsAsync()
    {
        await ConfigureProviderAsync();
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

    private async Task LoadCloudCatalogAsync()
    {
        try
        {
            var catalogResult = await _cloudCatalogService.GetCatalogAsync();
            if (!catalogResult.IsSuccess || catalogResult.Value == null)
            {
                _notificationService.ShowWarning("Unable to load cloud catalog metadata");
                return;
            }

            var catalog = catalogResult.Value;
            AvailableCloudGamesCount = catalog.Games.Count;

            TopCloudGames.Clear();
            foreach (var entry in catalog.Games
                .OrderByDescending(g => g.Providers.Count)
                .ThenBy(g => g.Title)
                .Take(5))
            {
                TopCloudGames.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cloud catalog metadata");
        }
    }

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

    private static string NormalizeProviderName(string? providerName)
    {
        return (providerName ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static int ClampDaemonAlertCooldownSeconds(int cooldownSeconds)
    {
        if (cooldownSeconds < MinDaemonAlertCooldownSeconds)
        {
            return MinDaemonAlertCooldownSeconds;
        }

        if (cooldownSeconds > MaxDaemonAlertCooldownSeconds)
        {
            return MaxDaemonAlertCooldownSeconds;
        }

        return cooldownSeconds;
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
        var nowUtc = _timeProvider.UtcNow;
        if (nowUtc - _lastManualConflictAlertAtUtc < ManualConflictAlertCooldown)
        {
            return;
        }

        _notificationService.ShowWarning(
            "Sync conflicts detected during transfer. Open 'View Conflicts' to resolve.");
        _lastManualConflictAlertAtUtc = nowUtc;
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

    private void OnSaveStateCloudDaemonStatusChanged(object? sender, SaveStateCloudDaemonStatus status)
    {
        Dispatcher.UIThread.Post(() => ApplyDaemonStatus(status));
    }

    private void ApplyDaemonStatus(SaveStateCloudDaemonStatus status)
    {
        IsBackgroundSyncEnabled = status.Enabled;
        BackgroundDaemonState = !status.Enabled
            ? "Disabled"
            : status.IsRunning ? "Running" : "Stopped";
        BackgroundDaemonLastSync = status.LastSyncAtUtc.HasValue
            ? status.LastSyncAtUtc.Value.ToLocalTime().ToString("g")
            : "Never";
        BackgroundDaemonSummary =
            $"{status.SuccessfulSyncCount} successful | {status.FailedSyncCount} failed | {status.ConflictCount} conflicts | {status.SkippedCount} skipped";
        BackgroundDaemonMessage = status.LastMessage;

        var healthSnapshot = EvaluateDaemonHealth(status);
        BackgroundDaemonHealthStatus = healthSnapshot.Status;
        BackgroundDaemonHealthCue = healthSnapshot.Cue;
        ShowResolveConflictsQuickAction = healthSnapshot.ShowResolveConflictsQuickAction;
        ShowRetrySyncQuickAction = healthSnapshot.ShowRetrySyncQuickAction;
        ShowConfigureProviderQuickAction = healthSnapshot.ShowConfigureProviderQuickAction;
        HasBackgroundQuickActions =
            ShowResolveConflictsQuickAction ||
            ShowRetrySyncQuickAction ||
            ShowConfigureProviderQuickAction;

        ProcessDaemonAlertNotifications(status);
    }

    private async Task<Dictionary<string, SaveStateConflictEntry>> AppendSaveStateConflictsAsync(ICollection<SyncConflictViewModel> conflicts)
    {
        var map = new Dictionary<string, SaveStateConflictEntry>(StringComparer.Ordinal);

        IReadOnlyList<Core.GameLibrary.Entities.Game> games;
        try
        {
            games = await _gameRepository.GetAllAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate games for save-state conflict detection");
            return map;
        }

        foreach (var game in games
                     .OrderByDescending(g => g.LastPlayedAt ?? g.UpdatedAt ?? g.CreatedAt)
                     .Take(25))
        {
            SaveStateConflictResolution? saveConflict = null;
            try
            {
                var detectResult = await _saveStateCloudService.DetectConflictsAsync(game.Id).ConfigureAwait(false);
                if (detectResult.IsFailure || detectResult.Value is null || detectResult.Value.Type == SaveStateConflictType.None)
                {
                    continue;
                }

                saveConflict = detectResult.Value;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to detect save-state conflict for game {GameId}", game.Id);
            }

            if (saveConflict is null)
            {
                continue;
            }

            var displayKey = $"SaveState::{game.Id:N}::{game.Title}";
            if (map.ContainsKey(displayKey))
            {
                continue;
            }

            conflicts.Add(new SyncConflictViewModel(
                displayKey,
                (saveConflict.LocalVersion?.CreatedAtUtc ?? _timeProvider.UtcNow).ToLocalTime(),
                (saveConflict.CloudVersion?.CreatedAtUtc ?? _timeProvider.UtcNow).ToLocalTime(),
                saveConflict.LocalVersion?.FileSizeBytes ?? 0,
                saveConflict.CloudVersion?.FileSizeBytes ?? 0));

            map[displayKey] = new SaveStateConflictEntry(game.Id, saveConflict);
        }

        return map;
    }

    private async Task<ConflictApplyResult> ResolveSaveStateConflictAsync(
        string conflictKey,
        SaveStateConflictEntry conflictEntry,
        IDictionary<string, string> encryptionKeyCache,
        string strategy)
    {
        var normalized = strategy.Trim().ToLowerInvariant();
        if (normalized == "skip")
        {
            return ConflictApplyResult.Failed();
        }

        var conflictStrategy = normalized switch
        {
            "keep local" => SaveStateConflictResolutionStrategy.KeepLocal,
            "keep cloud" => SaveStateConflictResolutionStrategy.KeepCloud,
            "keep both" => SaveStateConflictResolutionStrategy.KeepBoth,
            _ => SaveStateConflictResolutionStrategy.PromptUser
        };

        if (conflictStrategy == SaveStateConflictResolutionStrategy.PromptUser)
        {
            _logger.LogWarning(
                "Unknown save-state conflict strategy '{Strategy}' for game {GameId}",
                strategy,
                conflictEntry.GameId);
            return ConflictApplyResult.Failed($"Unsupported strategy '{strategy}' for save-state conflict '{conflictKey}'.");
        }

        var metadata = new SaveStateCloudMetadata
        {
            DeviceName = Environment.MachineName,
            ForceUpload = conflictStrategy is SaveStateConflictResolutionStrategy.KeepLocal or SaveStateConflictResolutionStrategy.KeepBoth,
            VersionName = conflictStrategy switch
            {
                SaveStateConflictResolutionStrategy.KeepLocal => $"Conflict KeepLocal {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss}",
                SaveStateConflictResolutionStrategy.KeepBoth => $"Conflict KeepBoth {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss}",
                _ => null
            }
        };

        if (conflictStrategy == SaveStateConflictResolutionStrategy.KeepCloud &&
            conflictEntry.Conflict.CloudVersion?.IsEncrypted == true)
        {
            var encryptionCacheKey = BuildEncryptionCacheKey(conflictEntry);
            if (!encryptionKeyCache.TryGetValue(encryptionCacheKey, out var encryptionKey) ||
                string.IsNullOrWhiteSpace(encryptionKey))
            {
                encryptionKey = await _dialogService.ShowInputDialogAsync(
                    "Cloud Save Encryption Key",
                    $"Enter the encryption key to restore cloud save conflict '{conflictKey}'.",
                    "Encryption key",
                    isSensitive: true).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(encryptionKey))
                {
                    return ConflictApplyResult.Failed(
                        $"Skipped encrypted save-state conflict '{conflictKey}' because no encryption key was provided.");
                }

                encryptionKey = encryptionKey.Trim();
                encryptionKeyCache[encryptionCacheKey] = encryptionKey;
            }

            metadata = metadata with
            {
                EncryptionKey = encryptionKey
            };
        }

        var resolveResult = await _saveStateCloudService.ResolveConflictAsync(
            conflictEntry.GameId,
            conflictStrategy,
            metadata).ConfigureAwait(false);
        if (resolveResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to resolve save-state conflict for game {GameId} with strategy {Strategy}: {Error}",
                conflictEntry.GameId,
                strategy,
                resolveResult.Error);
            return ConflictApplyResult.Failed(
                $"Save-state conflict '{conflictKey}' failed with strategy '{strategy}': {resolveResult.Error ?? "unknown error"}.");
        }

        return ConflictApplyResult.Successful();
    }

    private static string BuildEncryptionCacheKey(SaveStateConflictEntry conflictEntry)
    {
        var fingerprint = conflictEntry.Conflict.CloudVersion?.EncryptionKeyFingerprint;
        if (!string.IsNullOrWhiteSpace(fingerprint))
        {
            return $"fingerprint:{fingerprint.Trim()}";
        }

        return $"game:{conflictEntry.GameId:N}";
    }

    private static string BuildFailureSummary(IReadOnlyList<string> failureMessages)
    {
        if (failureMessages.Count == 0)
        {
            return "No failure details were provided.";
        }

        var distinctFailures = failureMessages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.Ordinal)
            .Take(3)
            .ToList();

        if (distinctFailures.Count == 0)
        {
            return "No failure details were provided.";
        }

        var summary = string.Join(" | ", distinctFailures);
        if (failureMessages.Count > distinctFailures.Count)
        {
            summary += $" | +{failureMessages.Count - distinctFailures.Count} more";
        }

        return summary;
    }

    private void ProcessDaemonAlertNotifications(SaveStateCloudDaemonStatus status)
    {
        if (_lastDaemonStatusSnapshot is null)
        {
            _lastDaemonStatusSnapshot = status;
            return;
        }

        var failureDelta = Math.Max(0, status.FailedSyncCount - _lastDaemonStatusSnapshot.FailedSyncCount);
        var conflictDelta = Math.Max(0, status.ConflictCount - _lastDaemonStatusSnapshot.ConflictCount);

        if (_daemonFailureAlertsEnabled && failureDelta > 0)
        {
            _pendingDaemonFailureAlerts += failureDelta;
        }
        else if (!_daemonFailureAlertsEnabled)
        {
            _pendingDaemonFailureAlerts = 0;
        }

        if (_daemonConflictAlertsEnabled && conflictDelta > 0)
        {
            _pendingDaemonConflictAlerts += conflictDelta;
        }
        else if (!_daemonConflictAlertsEnabled)
        {
            _pendingDaemonConflictAlerts = 0;
        }

        var nowUtc = _timeProvider.UtcNow;
        var alertCooldown = TimeSpan.FromSeconds(ClampDaemonAlertCooldownSeconds(_daemonAlertCooldownSeconds));

        if (_daemonFailureAlertsEnabled &&
            _pendingDaemonFailureAlerts > 0 &&
            nowUtc - _lastDaemonFailureAlertAtUtc >= alertCooldown)
        {
            var failureLabel = _pendingDaemonFailureAlerts == 1 ? "failure" : "failures";
            _notificationService.ShowError(
                $"Background save-state sync reported {_pendingDaemonFailureAlerts} new {failureLabel}. {status.LastMessage}");
            _pendingDaemonFailureAlerts = 0;
            _lastDaemonFailureAlertAtUtc = nowUtc;
        }

        if (_daemonConflictAlertsEnabled &&
            _pendingDaemonConflictAlerts > 0 &&
            nowUtc - _lastDaemonConflictAlertAtUtc >= alertCooldown)
        {
            var conflictLabel = _pendingDaemonConflictAlerts == 1 ? "conflict" : "conflicts";
            _notificationService.ShowWarning(
                $"Background save-state sync detected {_pendingDaemonConflictAlerts} new {conflictLabel}. Open 'View Conflicts' to resolve.");
            _pendingDaemonConflictAlerts = 0;
            _lastDaemonConflictAlertAtUtc = nowUtc;
        }

        _lastDaemonStatusSnapshot = status;
    }

    private static DaemonHealthSnapshot EvaluateDaemonHealth(SaveStateCloudDaemonStatus status)
    {
        if (!status.Enabled)
        {
            return new DaemonHealthSnapshot(
                "Disabled",
                "Background sync daemon is disabled. Enable it in cloud sync settings.",
                status.ConflictCount > 0,
                false,
                true);
        }

        if (status.FailedSyncCount > 0)
        {
            var failureLabel = status.FailedSyncCount == 1 ? "failure" : "failures";
            return new DaemonHealthSnapshot(
                "Critical",
                $"Background sync reported {status.FailedSyncCount} {failureLabel}. Retry sync and review provider settings.",
                status.ConflictCount > 0,
                true,
                true);
        }

        if (status.ConflictCount > 0)
        {
            var conflictLabel = status.ConflictCount == 1 ? "conflict" : "conflicts";
            return new DaemonHealthSnapshot(
                "Warning",
                $"Background sync detected {status.ConflictCount} {conflictLabel}. Resolve conflicts to prevent data divergence.",
                true,
                false,
                false);
        }

        if (!status.IsRunning)
        {
            return new DaemonHealthSnapshot(
                "Stopped",
                "Background sync daemon is not running. Retry sync or review daemon settings.",
                false,
                true,
                true);
        }

        return new DaemonHealthSnapshot(
            "Healthy",
            "Background sync is operating normally.",
            false,
            false,
            false);
    }

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

    private sealed record SaveStateConflictEntry(Guid GameId, SaveStateConflictResolution Conflict);

    private sealed record ConflictApplyResult(bool Success, string? Error)
    {
        public static ConflictApplyResult Successful() => new(true, null);
        public static ConflictApplyResult Failed(string? error = null) => new(false, error);
    }

    private sealed record DaemonHealthSnapshot(
        string Status,
        string Cue,
        bool ShowResolveConflictsQuickAction,
        bool ShowRetrySyncQuickAction,
        bool ShowConfigureProviderQuickAction);
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

