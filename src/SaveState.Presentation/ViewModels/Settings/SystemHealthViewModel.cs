using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using PresentationHealth = SaveState.Presentation.Models.Health;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Settings;

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
    Task<Result<PresentationHealth.CacheStatistics>> ClearCacheAsync(CancellationToken ct = default);

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
    public OverallHealthSummary OverallSummary { get; set; } = new();
    public DatabaseHealth DatabaseHealth { get; set; } = new();
    public IReadOnlyList<ApiHealthStatus> ApiStatuses { get; set; } = Array.Empty<ApiHealthStatus>();
    public PresentationHealth.CacheStatistics CacheStats { get; set; } = new();
    public SystemResources Resources { get; set; } = new();
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
    private ObservableCollection<ErrorLogEntry> _allErrors = new();

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
    private PresentationHealth.CacheStatistics _cacheStats = new();

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

        // Initialize with sample data
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
            LastUpdated = DateTimeOffset.UtcNow.DateTime
        };

        DatabaseHealth = new DatabaseHealth
        {
            Status = HealthStatus.Healthy,
            ResponseTime = TimeSpan.FromMilliseconds(12),
            LastBackup = DateTimeOffset.UtcNow.AddHours(-2).DateTime,
            DatabaseSize = 1024 * 1024 * 150 // 150MB
        };

        ApiStatuses = new ObservableCollection<ApiHealthStatus>
        {
            new() { ApiName = "Steam", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(45), LastChecked = DateTimeOffset.UtcNow.DateTime },
            new() { ApiName = "GOG", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(38), LastChecked = DateTimeOffset.UtcNow.DateTime },
            new() { ApiName = "Epic Games", Status = HealthStatus.Degraded, ResponseTime = TimeSpan.FromMilliseconds(1200), LastChecked = DateTimeOffset.UtcNow.DateTime },
            new() { ApiName = "IGDB", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(67), LastChecked = DateTimeOffset.UtcNow.DateTime },
            new() { ApiName = "Discord", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(23), LastChecked = DateTimeOffset.UtcNow.DateTime },
            new() { ApiName = "RetroAchievements", Status = HealthStatus.Healthy, ResponseTime = TimeSpan.FromMilliseconds(89), LastChecked = DateTimeOffset.UtcNow.DateTime }
        };

        CacheStats = new Models.Health.CacheStatistics
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
            new() { Timestamp = DateTimeOffset.UtcNow.AddHours(-2).DateTime, Component = "Steam API", Message = "Timeout during sync", Severity = ErrorSeverity.Warning },
            new() { Timestamp = DateTimeOffset.UtcNow.AddHours(-4).DateTime, Component = "Cover Downloader", Message = "Failed to download from IGDB", Severity = ErrorSeverity.Info }
        };
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
                    OverallStatus = report.OverallSummary;
                    DatabaseHealth = report.DatabaseHealth;
                    ApiStatuses = new ObservableCollection<ApiHealthStatus>(report.ApiStatuses);
                    CacheStats = report.CacheStats;
                    Resources = report.Resources;
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

        LastRefresh = _timeProvider.Now;
        OverallStatus.LastUpdated = _timeProvider.Now;

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
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error clearing cache: {ex.Message}");
        }
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
                _notificationService?.ShowNotificationAsync("Database backup not available - service not configured", "Backup");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error during backup: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the error log viewer dialog.
    /// </summary>
    [RelayCommand]
    private async Task ViewErrorLogAsync()
    {
        if (_healthService is not null)
        {
            var result = await _healthService.GetRecentErrorsAsync(100);
            if (result.IsSuccess && result.Value is not null)
            {
                _allErrors = new ObservableCollection<ErrorLogEntry>(result.Value);
                ApplyErrorFilter();
            }
        }

        await (_dialogService?.ShowInformationAsync("Error Log", $"Total errors: {_allErrors.Count}") ?? Task.CompletedTask);
    }

    /// <summary>
    /// Retries a failed API connection.
    /// </summary>
    /// <param name="api">The API to retry.</param>
    [RelayCommand]
    private async Task RetryApiAsync(ApiHealthStatus? api)
    {
        if (api is null) return;

        try
        {
            if (_healthService is not null)
            {
                var result = await _healthService.RetryApiConnectionAsync(api.ApiName);
                if (result.IsSuccess)
                {
                    _notificationService?.ShowSuccess($"{api.ApiName} connection restored", "API Retry");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to retry {api.ApiName}: {result.Error}");
                }
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error retrying API connection: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when the selected error filter changes.
    /// </summary>
    /// <param name="value">The new filter value.</param>
    partial void OnSelectedErrorFilterChanged(string? value)
    {
        ApplyErrorFilter();
    }

    /// <summary>
    /// Applies the current error filter to the RecentErrors collection.
    /// </summary>
    private void ApplyErrorFilter()
    {
        if (string.IsNullOrEmpty(SelectedErrorFilter) || SelectedErrorFilter == "All")
        {
            // Show all errors
            if (_allErrors.Count == 0)
            {
                // Use sample data if no real data loaded
                return;
            }
            RecentErrors = new ObservableCollection<ErrorLogEntry>(_allErrors);
            return;
        }

        if (!Enum.TryParse<ErrorSeverity>(SelectedErrorFilter, out var severity))
            return;

        var filtered = _allErrors.Where(e => e.Severity == severity).ToList();
        RecentErrors = new ObservableCollection<ErrorLogEntry>(filtered);
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
