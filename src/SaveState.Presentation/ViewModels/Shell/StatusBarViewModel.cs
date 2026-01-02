using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the status bar.
/// </summary>
public partial class StatusBarViewModel : ObservableObject, IDisposable
{
    private readonly INavigationService _navigationService;
    private readonly ILogger<StatusBarViewModel> _logger;
    private readonly System.Timers.Timer _refreshTimer;

    private bool _isOnline = true;
    private int _gameCount;
    private TimeSpan _todayPlaytime;
    private string _syncStatus = "Idle";
    private int _cpuUsage;
    private int _gpuUsage;
    private bool _isSyncing;

    public StatusBarViewModel(
        INavigationService navigationService,
        ILogger<StatusBarViewModel> logger)
    {
        _navigationService = navigationService;
        _logger = logger;

        // Initialize timer for periodic updates
        _refreshTimer = new System.Timers.Timer(5000); // 5 seconds
        _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        _refreshTimer.Start();

        // Initial refresh
        _ = RefreshStatsAsync();
    }

    /// <summary>
    /// Gets whether the application is online.
    /// </summary>
    public bool IsOnline
    {
        get => _isOnline;
        private set => SetProperty(ref _isOnline, value);
    }

    /// <summary>
    /// Gets the connection status text.
    /// </summary>
    public string ConnectionStatusText => IsOnline ? "🟢 Online" : "🔴 Offline";

    /// <summary>
    /// Gets the game count.
    /// </summary>
    public int GameCount
    {
        get => _gameCount;
        private set => SetProperty(ref _gameCount, value);
    }

    /// <summary>
    /// Gets the game count display text.
    /// </summary>
    public string GameCountText => $"🎮 {GameCount} Games";

    /// <summary>
    /// Gets today's playtime.
    /// </summary>
    public TimeSpan TodayPlaytime
    {
        get => _todayPlaytime;
        private set => SetProperty(ref _todayPlaytime, value);
    }

    /// <summary>
    /// Gets the today's playtime display text.
    /// </summary>
    public string TodayPlaytimeText
    {
        get
        {
            if (TodayPlaytime.TotalHours >= 1)
            {
                return $"⏱️ {TodayPlaytime.TotalHours:F1}h Today";
            }
            else if (TodayPlaytime.TotalMinutes >= 1)
            {
                return $"⏱️ {TodayPlaytime.TotalMinutes:F0}m Today";
            }
            else
            {
                return "⏱️ 0m Today";
            }
        }
    }

    /// <summary>
    /// Gets the sync status.
    /// </summary>
    public string SyncStatus
    {
        get => _syncStatus;
        private set => SetProperty(ref _syncStatus, value);
    }

    /// <summary>
    /// Gets whether syncing is in progress.
    /// </summary>
    public bool IsSyncing
    {
        get => _isSyncing;
        private set => SetProperty(ref _isSyncing, value);
    }

    /// <summary>
    /// Gets the sync status display text.
    /// </summary>
    public string SyncStatusText
    {
        get
        {
            if (IsSyncing)
            {
                return "🔄 Syncing...";
            }
            else
            {
                return SyncStatus;
            }
        }
    }

    /// <summary>
    /// Gets the CPU usage.
    /// </summary>
    public int CpuUsage
    {
        get => _cpuUsage;
        private set => SetProperty(ref _cpuUsage, value);
    }

    /// <summary>
    /// Gets the CPU usage display text.
    /// </summary>
    public string CpuUsageText => $"{CpuUsage}%";

    /// <summary>
    /// Gets the GPU usage.
    /// </summary>
    public int GpuUsage
    {
        get => _gpuUsage;
        private set => SetProperty(ref _gpuUsage, value);
    }

    /// <summary>
    /// Gets the GPU usage display text.
    /// </summary>
    public string GpuUsageText => $"{GpuUsage}%";

    /// <summary>
    /// Shows network connection details.
    /// </summary>
    public void ShowNetworkDetails()
    {
        // TODO: Implement network diagnostics overlay
        _logger.LogInformation("Showing network details");
    }

    /// <summary>
    /// Navigates to the library tab.
    /// </summary>
    [RelayCommand]
    public void NavigateToLibrary()
    {
        _navigationService.NavigateTo("Library");
    }

    /// <summary>
    /// Navigates to the analytics tab.
    /// </summary>
    [RelayCommand]
    public void NavigateToAnalytics()
    {
        _navigationService.NavigateTo("Analytics");
    }

    /// <summary>
    /// Shows sync status details.
    /// </summary>
    public void ShowSyncDetails()
    {
        // TODO: Implement sync status overlay
        _logger.LogInformation("Showing sync details");
    }

    /// <summary>
    /// Handles the timer elapsed event by refreshing stats.
    /// Uses fire-and-forget pattern with existing try/catch in RefreshStatsAsync.
    /// </summary>
    private void OnRefreshTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _ = RefreshStatsAsync();
    }

    /// <summary>
    /// Refreshes all status statistics.
    /// </summary>
    private async Task RefreshStatsAsync()
    {
        try
        {
            // TODO: Get real data from services
            // For now, simulate some data
            IsOnline = true; // TODO: Implement network connectivity check
            GameCount = 142; // TODO: Get from IGameRepository
            TodayPlaytime = TimeSpan.FromHours(2.5); // TODO: Get from IAnalyticsService
            SyncStatus = "Idle"; // TODO: Get from ISyncService
            IsSyncing = false;
            CpuUsage = 23; // TODO: Get from IPerformanceMonitor
            GpuUsage = 45; // TODO: Get from IPerformanceMonitor
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh status bar statistics");
        }
    }

    /// <summary>
    /// Disposes the view model.
    /// </summary>
    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}
