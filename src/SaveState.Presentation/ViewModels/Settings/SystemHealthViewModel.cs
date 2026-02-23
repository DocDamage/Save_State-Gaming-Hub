using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Health;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// Overall health status of the system.
/// </summary>
public enum HealthStatus
{
    /// <summary>All systems operating normally.</summary>
    Healthy,

    /// <summary>Some systems experiencing issues.</summary>
    Warning,

    /// <summary>Critical system failures.</summary>
    Critical
}

/// <summary>
/// Database health information.
/// </summary>
public class DatabaseHealth
{
    /// <summary>Current health status of the database.</summary>
    public HealthStatus Status { get; set; }

    /// <summary>Database query response time.</summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>Timestamp of the last database backup.</summary>
    public DateTime? LastBackup { get; set; }

    /// <summary>Size of the database in bytes.</summary>
    public long DatabaseSize { get; set; }
}

/// <summary>
/// Health status of an external API.
/// </summary>
public class ApiHealthStatus
{
    /// <summary>Name of the API (Steam, GOG, Epic, etc.).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Current health status.</summary>
    public HealthStatus Status { get; set; }

    /// <summary>Response time of the last check.</summary>
    public TimeSpan? ResponseTime { get; set; }

    /// <summary>Error message if unhealthy.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Timestamp of the last health check.</summary>
    public DateTime? LastChecked { get; set; }
}

/// <summary>
/// Cache statistics.
/// </summary>
public class CacheStatistics
{
    /// <summary>Cache hit rate (0.0 to 1.0).</summary>
    public double HitRate { get; set; }

    /// <summary>Size of the cache in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Number of entries in the cache.</summary>
    public int EntryCount { get; set; }

    /// <summary>Number of entries evicted from the cache.</summary>
    public int EvictionCount { get; set; }
}

/// <summary>
/// System resource utilization.
/// </summary>
public class SystemResources
{
    /// <summary>CPU usage percentage (0-100).</summary>
    public double CpuPercent { get; set; }

    /// <summary>Memory usage percentage (0-100).</summary>
    public double MemoryPercent { get; set; }

    /// <summary>GPU usage percentage (0-100).</summary>
    public double GpuPercent { get; set; }

    /// <summary>Disk usage percentage (0-100).</summary>
    public double DiskPercent { get; set; }
}

/// <summary>
/// Service for system health checks and monitoring.
/// </summary>
public interface ISystemHealthService
{
    /// <summary>
    /// Performs comprehensive health checks on all system components.
    /// </summary>
    Task<Result<SystemHealthReport>> GetHealthReportAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears the application cache.
    /// </summary>
    Task<Result<CacheStatistics>> ClearCacheAsync(CancellationToken ct = default);

    /// <summary>
    /// Triggers a database backup.
    /// </summary>
    Task<Result<string>> BackupDatabaseAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets recent error log entries.
    /// </summary>
    Task<Result<IReadOnlyList<ErrorLogEntry>>> GetRecentErrorsAsync(int count = 50, CancellationToken ct = default);

    /// <summary>
    /// Retries connection to a failed API.
    /// </summary>
    Task<Result<ApiHealthStatus>> RetryApiConnectionAsync(string apiName, CancellationToken ct = default);
}

/// <summary>
/// Complete system health report.
/// </summary>
public class SystemHealthReport
{
    /// <summary>Overall system health status.</summary>
    public HealthStatus OverallStatus { get; set; }

    /// <summary>Database health information.</summary>
    public DatabaseHealth DatabaseHealth { get; set; } = new();

    /// <summary>Collection of external API health statuses.</summary>
    public IReadOnlyList<ApiHealthStatus> ApiStatuses { get; set; } = Array.Empty<ApiHealthStatus>();

    /// <summary>Cache statistics.</summary>
    public CacheStatistics CacheStats { get; set; } = new();

    /// <summary>System resource utilization.</summary>
    public SystemResources Resources { get; set; } = new();

    /// <summary>Recent error log entries.</summary>
    public IReadOnlyList<ErrorLogEntry> RecentErrors { get; set; } = Array.Empty<ErrorLogEntry>();

    /// <summary>Timestamp of the last update.</summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// ViewModel for the System Health Dashboard.
/// Aggregates health checks for database, APIs, cache, and system resources.
/// </summary>
public partial class SystemHealthViewModel : ObservableObject, IDisposable
{
    private readonly System.Timers.Timer _refreshTimer;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemHealthService? _healthService;
    private readonly IDialogService? _dialogService;
    private readonly INotificationService? _notificationService;

    /// <summary>Overall system health status.</summary>
    [ObservableProperty]
    private HealthStatus _overallStatus;

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

    /// <summary>Timestamp of the last update.</summary>
    [ObservableProperty]
    private DateTime _lastUpdated;

    /// <summary>Whether a refresh is currently in progress.</summary>
    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>Gets the overall status message.</summary>
    public string OverallStatusMessage => OverallStatus switch
    {
        HealthStatus.Healthy => "🟢 All Systems Operational",
        HealthStatus.Warning => "🟡 Some Systems Degraded",
        HealthStatus.Critical => "🔴 Critical Issues Detected",
        _ => "⚪ Status Unknown"
    };

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public SystemHealthViewModel()
    {
        _timeProvider = new SystemTimeProvider();
        _refreshTimer = new System.Timers.Timer(30000); // 30 seconds
        _refreshTimer.Elapsed += async (s, e) => await RefreshAsync();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();

        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHealthViewModel"/> class.
    /// </summary>
    public SystemHealthViewModel(
        ITimeProvider timeProvider,
        ISystemHealthService? healthService = null,
        IDialogService? dialogService = null,
        INotificationService? notificationService = null)
    {
        _timeProvider = timeProvider;
        _healthService = healthService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _refreshTimer = new System.Timers.Timer(30000); // 30 seconds
        _refreshTimer.Elapsed += async (s, e) => await RefreshAsync();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();

        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        OverallStatus = HealthStatus.Healthy;

        DatabaseHealth = new DatabaseHealth
        {
            Status = HealthStatus.Healthy,
            ResponseTime = TimeSpan.FromMilliseconds(12),
            LastBackup = _timeProvider.Now.AddHours(-2),
            DatabaseSize = 1024L * 1024 * 150 // 150MB
        };

        ApiStatuses = new ObservableCollection<ApiHealthStatus>
        {
            new() { Name = "Steam", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(45), LastChecked = _timeProvider.Now },
            new() { Name = "GOG", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(38), LastChecked = _timeProvider.Now },
            new() { Name = "Epic Games", Status = HealthStatus.Warning, ResponseTime = TimeSpan.FromMilliseconds(1200), LastChecked = _timeProvider.Now },
            new() { Name = "IGDB", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(67), LastChecked = _timeProvider.Now },
            new() { Name = "Discord", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(23), LastChecked = _timeProvider.Now },
            new() { Name = "RetroAchievements", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(89), LastChecked = _timeProvider.Now }
        };

        CacheStats = new CacheStatistics
        {
            HitRate = 0.94,
            Size = 1024L * 1024 * 145,
            EntryCount = 1240,
            EvictionCount = 45
        };

        Resources = new SystemResources
        {
            CpuPercent = 45,
            MemoryPercent = 62,
            GpuPercent = 30,
            DiskPercent = 85
        };

        RecentErrors = new ObservableCollection<ErrorLogEntry>
        {
            new() { Timestamp = _timeProvider.Now.AddHours(-2), Component = "Steam API", Message = "Timeout during sync", Severity = ErrorSeverity.Warning },
            new() { Timestamp = _timeProvider.Now.AddHours(-4), Component = "Cover Downloader", Message = "Failed to download from IGDB", Severity = ErrorSeverity.Error }
        };

        LastUpdated = _timeProvider.Now;
    }

    /// <summary>
    /// Refreshes all health check data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;

        try
        {
            if (_healthService is not null)
            {
                var result = await _healthService.GetHealthReportAsync();
                if (result.IsSuccess && result.Value is not null)
                {
                    var report = result.Value;
                    OverallStatus = report.OverallStatus;
                    DatabaseHealth = report.DatabaseHealth;
                    ApiStatuses = new ObservableCollection<ApiHealthStatus>(report.ApiStatuses);
                    CacheStats = report.CacheStats;
                    Resources = report.Resources;
                    RecentErrors = new ObservableCollection<ErrorLogEntry>(report.RecentErrors);
                }
                else
                {
                    _notificationService?.ShowError($"Failed to refresh health data: {result.Error}");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error refreshing health data: {ex.Message}");
        }

        LastUpdated = _timeProvider.Now;
        IsRefreshing = false;
    }

    /// <summary>
    /// Clears the application cache.
    /// </summary>
    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        try
        {
            if (_healthService is not null)
            {
                var result = await _healthService.ClearCacheAsync();
                if (result.IsSuccess)
                {
                    _notificationService?.ShowSuccess("Cache cleared successfully");
                    await RefreshAsync();
                }
                else
                {
                    _notificationService?.ShowError($"Failed to clear cache: {result.Error}");
                }
            }
            else
            {
                await Task.Delay(300);
                CacheStats = new CacheStatistics
                {
                    HitRate = 0,
                    Size = 0,
                    EntryCount = 0,
                    EvictionCount = 0
                };
                _notificationService?.ShowSuccess("Cache cleared (demo mode)");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error clearing cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the error log viewer dialog.
    /// </summary>
    [RelayCommand]
    private async Task ViewErrorLogAsync()
    {
        await (_dialogService?.ShowErrorLogViewerAsync() ?? Task.CompletedTask);
    }

    /// <summary>
    /// Triggers a database backup.
    /// </summary>
    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            if (_healthService is not null)
            {
                var result = await _healthService.BackupDatabaseAsync();
                if (result.IsSuccess)
                {
                    _notificationService?.ShowSuccess($"Database backed up to: {result.Value}", "Backup Complete");
                    await RefreshAsync();
                }
                else
                {
                    _notificationService?.ShowError($"Failed to backup database: {result.Error}");
                }
            }
            else
            {
                await Task.Delay(1000);
                DatabaseHealth.LastBackup = _timeProvider.Now;
                _notificationService?.ShowSuccess("Database backup completed (demo mode)", "Backup Complete");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error during backup: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the full logs viewer.
    /// </summary>
    [RelayCommand]
    private async Task ViewLogsAsync()
    {
        await ViewErrorLogAsync();
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
