using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Queries;
using SaveState.Application.Sync.Commands;
using SaveState.Core.Common;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudSyncViewModel"/> class.
    /// </summary>
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

            // Load cloud providers and active sessions
            await LoadCloudProvidersAsync();
            await LoadActiveCloudSessionsAsync();

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
}
