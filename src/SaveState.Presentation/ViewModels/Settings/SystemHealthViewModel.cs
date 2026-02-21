using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Health;
using System.Collections.ObjectModel;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for the System Health Dashboard.
/// Aggregates health checks for database, APIs, cache, and system resources.
/// </summary>
public partial class SystemHealthViewModel : ObservableObject, IDisposable
{
    private readonly System.Timers.Timer _refreshTimer;

    /// <summary>Overall system health summary.</summary>
    [ObservableProperty]
    private OverallHealthSummary _overallStatus = new();

    /// <summary>Database health information.</summary>
    [ObservableProperty]
    private DatabaseHealth _databaseHealth = new();

    /// <summary>Collection of external API health statuses.</summary>
    [ObservableProperty]
    private ObservableCollection<ApiHealthStatus> _apiStatuses = new();

    /// <summary>Cache statistics.</summary>
    [ObservableProperty]
    private CacheStatistics _cacheStats = new();

    /// <summary>System resource utilization.</summary>
    [ObservableProperty]
    private SystemResources _resources = new();

    /// <summary>Recent error log entries.</summary>
    [ObservableProperty]
    private ObservableCollection<ErrorLogEntry> _recentErrors = new();

    /// <summary>Timestamp of the last refresh.</summary>
    [ObservableProperty]
    private DateTime _lastRefresh;

    /// <summary>Whether a refresh is currently in progress.</summary>
    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>Selected error severity filter.</summary>
    [ObservableProperty]
    private string? _selectedErrorFilter;

    /// <summary>Available severity filters.</summary>
    public List<string> SeverityFilters { get; } = new() { "All", "Critical", "Error", "Warning", "Info" };

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHealthViewModel"/> class.
    /// </summary>
    public SystemHealthViewModel()
    {
        _refreshTimer = new System.Timers.Timer(30000); // 30 seconds
        _refreshTimer.Elapsed += async (s, e) => await RefreshAsync();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();

        // Initialize with sample data
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        OverallStatus = new OverallHealthSummary
        {
            OverallStatus = HealthStatus.Healthy,
            HealthyServices = 7,
            DegradedServices = 0,
            UnhealthyServices = 0,
            LastUpdated = DateTime.Now
        };

        DatabaseHealth = new DatabaseHealth
        {
            Status = HealthStatus.Healthy,
            ResponseTime = TimeSpan.FromMilliseconds(12),
            LastBackup = DateTime.Now.AddHours(-2),
            DatabaseSize = 1024 * 1024 * 150 // 150MB
        };

        ApiStatuses = new ObservableCollection<ApiHealthStatus>
        {
            new() { ApiName = "Steam", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(45), LastChecked = DateTime.Now },
            new() { ApiName = "GOG", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(38), LastChecked = DateTime.Now },
            new() { ApiName = "Epic Games", Status = HealthStatus.Degraded, ResponseTime = TimeSpan.FromMilliseconds(1200), LastChecked = DateTime.Now },
            new() { ApiName = "IGDB", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(67), LastChecked = DateTime.Now },
            new() { ApiName = "Discord", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(23), LastChecked = DateTime.Now },
            new() { ApiName = "RetroAchievements", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(89), LastChecked = DateTime.Now }
        };

        CacheStats = new CacheStatistics
        {
            HitRate = 0.94,
            SizeInBytes = 1024 * 1024 * 145,
            EntryCount = 1240,
            EvictionCount = 45
        };

        Resources = new SystemResources
        {
            CpuPercentage = 45,
            MemoryPercentage = 62,
            GpuPercentage = 30,
            DiskPercentage = 85,
            AvailableMemoryBytes = 1024L * 1024 * 1024 * 6,
            TotalMemoryBytes = 1024L * 1024 * 1024 * 16
        };

        RecentErrors = new ObservableCollection<ErrorLogEntry>
        {
            new() { Timestamp = DateTime.Now.AddHours(-2), Component = "Steam API", Message = "Timeout during sync", Severity = ErrorSeverity.Warning },
            new() { Timestamp = DateTime.Now.AddHours(-4), Component = "Cover Downloader", Message = "Failed to download from IGDB", Severity = ErrorSeverity.Info }
        };
    }

    /// <summary>
    /// Refreshes all health check data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;

        // TODO: Call health check services to fetch real data
        await Task.Delay(500);

        LastRefresh = DateTime.Now;
        OverallStatus.LastUpdated = DateTime.Now;

        IsRefreshing = false;
    }

    /// <summary>
    /// Clears the application cache.
    /// </summary>
    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        // TODO: Clear cache through service
        await Task.Delay(300);
        await RefreshAsync();
    }

    /// <summary>
    /// Triggers a database backup.
    /// </summary>
    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        // TODO: Trigger backup through service
        await Task.Delay(1000);
    }

    /// <summary>
    /// Opens the error log viewer dialog.
    /// </summary>
    [RelayCommand]
    private void ViewErrorLog()
    {
        // TODO: Open error log dialog
    }

    /// <summary>
    /// Retries a failed API connection.
    /// </summary>
    /// <param name="api">The API to retry.</param>
    [RelayCommand]
    private async Task RetryApiAsync(ApiHealthStatus? api)
    {
        if (api is null) return;

        // TODO: Retry API connection through service
        await Task.Delay(500);
        await RefreshAsync();
    }

    /// <summary>
    /// Called when the selected error filter changes.
    /// </summary>
    /// <param name="value">The new filter value.</param>
    partial void OnSelectedErrorFilterChanged(string? value)
    {
        // TODO: Filter errors by severity
    }

    /// <summary>
    /// Stops the refresh timer when disposing.
    /// </summary>
    public void Dispose()
    {
        _refreshTimer.Stop();
        _refreshTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
