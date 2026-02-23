using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Monitoring;
using SaveState.Core.Performance.Services;
using SaveState.Infrastructure.Monitoring;
using SaveState.Presentation.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// Service for performance monitoring and optimization.
/// </summary>
public interface IPerformanceService
{
    /// <summary>
    /// Applies an optimization recommendation.
    /// </summary>
    Task<Result> ApplyOptimizationAsync(string optimizationId, CancellationToken ct = default);

    /// <summary>
    /// Runs a comprehensive performance benchmark.
    /// </summary>
    Task<Result<BenchmarkResult>> RunBenchmarkAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets real-time performance metrics.
    /// </summary>
    Task<Result<PerformanceMetrics>> GetMetricsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets per-game performance statistics.
    /// </summary>
    Task<Result<IReadOnlyList<GamePerformanceStats>>> GetGameStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets optimization recommendations based on current system state.
    /// </summary>
    Task<Result<IReadOnlyList<OptimizationRecommendation>>> GetRecommendationsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets cache performance statistics.
    /// </summary>
    Task<Result<CachePerformanceStats>> GetCacheStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears the performance metrics history.
    /// </summary>
    Task<Result> ClearHistoryAsync(CancellationToken ct = default);

    /// <summary>
    /// Exports a performance report.
    /// </summary>
    Task<Result<string>> ExportReportAsync(CancellationToken ct = default);
}

/// <summary>
/// Performance benchmark results.
/// </summary>
public class BenchmarkResult
{
    public double CpuScore { get; set; }
    public double MemoryScore { get; set; }
    public double GpuScore { get; set; }
    public double DiskScore { get; set; }
    public double OverallScore { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Real-time performance metrics.
/// </summary>
public class PerformanceMetrics
{
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double GpuUsage { get; set; }
    public double DiskUsage { get; set; }
    public double Fps { get; set; }
    public long AvailableMemoryBytes { get; set; }
    public long TotalMemoryBytes { get; set; }
}

/// <summary>
/// Cache performance statistics.
/// </summary>
public class CachePerformanceStats
{
    public double HitRate { get; set; }
    public long SizeBytes { get; set; }
    public int EntryCount { get; set; }
    public int EvictionCount { get; set; }
}

/// <summary>
/// Represents a single metric data point for charting.
/// </summary>
public class MetricDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}

/// <summary>
/// Represents performance statistics for a specific game.
/// </summary>
public class GamePerformanceStats : ObservableObject
{
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public double AverageFps { get; set; }
    public double MinFps { get; set; }
    public double MaxFps { get; set; }
    public TimeSpan TotalPlaytime { get; set; }
    public int SessionCount { get; set; }
    public DateTime LastPlayed { get; set; }
    public ObservableCollection<MetricDataPoint> FpsHistory { get; set; } = new();

    // Computed property for display
    public string FormattedPlaytime => $"{TotalPlaytime.TotalHours:F0}h {TotalPlaytime.Minutes}m";
}

/// <summary>
/// Represents an optimization recommendation.
/// </summary>
public class OptimizationRecommendation : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationSeverity Severity { get; set; }
    public RecommendationCategory Category { get; set; }
    public string? ActionText { get; set; }
    public Func<Task<bool>>? Action { get; set; }
    public bool CanAutoApply { get; set; }
    public double EstimatedImprovement { get; set; }

    // Computed properties for UI
    public string SeverityIcon => Severity switch
    {
        RecommendationSeverity.Critical => "🔴",
        RecommendationSeverity.Warning => "🟡",
        _ => "🟢"
    };

    public string CategoryIcon => Category switch
    {
        RecommendationCategory.Cpu => "🖥️",
        RecommendationCategory.Gpu => "🎮",
        RecommendationCategory.Memory => "🧠",
        RecommendationCategory.Disk => "💾",
        RecommendationCategory.Network => "🌐",
        RecommendationCategory.Settings => "⚙️",
        _ => "📊"
    };
}

/// <summary>
/// Severity levels for recommendations.
/// </summary>
public enum RecommendationSeverity { Info, Warning, Critical }

/// <summary>
/// Categories for optimization recommendations.
/// </summary>
public enum RecommendationCategory { Cpu, Gpu, Memory, Disk, Network, Settings }

/// <summary>
/// ViewModel for the Performance Dashboard.
/// Provides real-time monitoring of CPU, memory, GPU metrics, FPS statistics, and game performance.
/// </summary>
public partial class PerformanceDashboardViewModel : ObservableObject, IDisposable
{
    private readonly IPerformanceService? _performanceService;
    private readonly ISystemResourceManager? _systemResourceManager;
    private readonly IPerformanceMonitor? _performanceMonitor;
    private readonly ICachePerformanceMonitor? _cacheMonitor;
    private readonly IApplicationMetrics? _applicationMetrics;
    private readonly ErrorTrackingService? _errorTrackingService;
    private readonly INotificationService? _notificationService;
    private readonly IDialogService? _dialogService;
    private readonly ITimeProvider _timeProvider;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Random _random = new();

    // Chart series
    private readonly ObservableCollection<DateTimePoint> _cpuData = new();
    private readonly ObservableCollection<DateTimePoint> _gpuData = new();
    private readonly ObservableCollection<DateTimePoint> _memoryData = new();
    private readonly ObservableCollection<DateTimePoint> _fpsData = new();

    #region Observable Properties

    /// <summary>Current CPU usage percentage.</summary>
    [ObservableProperty]
    private double _currentCpuPercent;

    /// <summary>Current GPU usage percentage.</summary>
    [ObservableProperty]
    private double _currentGpuPercent;

    /// <summary>Current memory usage percentage.</summary>
    [ObservableProperty]
    private double _currentMemoryPercent;

    /// <summary>Current FPS value.</summary>
    [ObservableProperty]
    private double _currentFps;

    /// <summary>Current disk usage percentage.</summary>
    [ObservableProperty]
    private double _currentDiskUsage;

    /// <summary>Performance statistics for individual games.</summary>
    [ObservableProperty]
    private ObservableCollection<GamePerformanceStats> _gameStats = new();

    /// <summary>Currently selected game for detailed view.</summary>
    [ObservableProperty]
    private GamePerformanceStats? _selectedGame;

    /// <summary>Cache hit rate (0.0 to 1.0).</summary>
    [ObservableProperty]
    private double _cacheHitRate;

    /// <summary>Cache size in bytes.</summary>
    [ObservableProperty]
    private long _cacheSize;

    /// <summary>Number of entries in the cache.</summary>
    [ObservableProperty]
    private int _cacheEntries;

    /// <summary>Number of cache evictions.</summary>
    [ObservableProperty]
    private int _cacheEvictions;

    /// <summary>Optimization recommendations for the system.</summary>
    [ObservableProperty]
    private ObservableCollection<OptimizationRecommendation> _recommendations = new();

    /// <summary>Whether there are any critical recommendations.</summary>
    [ObservableProperty]
    private bool _hasCriticalRecommendations;

    /// <summary>Current session duration.</summary>
    [ObservableProperty]
    private TimeSpan _sessionDuration;

    /// <summary>Number of games launched this session.</summary>
    [ObservableProperty]
    private int _gamesLaunchedThisSession;

    /// <summary>Average FPS this session.</summary>
    [ObservableProperty]
    private double _averageFpsThisSession;

    /// <summary>Whether real-time monitoring is enabled.</summary>
    [ObservableProperty]
    private bool _isRealtimeMonitoring;

    /// <summary>Whether data is currently loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Chart series for CPU history.</summary>
    [ObservableProperty]
    private ISeries[] _cpuSeries = Array.Empty<ISeries>();

    /// <summary>Chart series for GPU history.</summary>
    [ObservableProperty]
    private ISeries[] _gpuSeries = Array.Empty<ISeries>();

    /// <summary>Chart series for memory history.</summary>
    [ObservableProperty]
    private ISeries[] _memorySeries = Array.Empty<ISeries>();

    /// <summary>Chart series for FPS history.</summary>
    [ObservableProperty]
    private ISeries[] _fpsSeries = Array.Empty<ISeries>();

    /// <summary>X-axis configuration for time-based charts.</summary>
    [ObservableProperty]
    private ICartesianAxis[] _timeAxis = Array.Empty<ICartesianAxis>();

    /// <summary>Y-axis configuration for percentage-based charts.</summary>
    [ObservableProperty]
    private ICartesianAxis[] _percentageAxis = Array.Empty<ICartesianAxis>();

    /// <summary>Y-axis configuration for FPS charts.</summary>
    [ObservableProperty]
    private ICartesianAxis[] _fpsAxis = Array.Empty<ICartesianAxis>();

    /// <summary>Session start time for calculating duration.</summary>
    [ObservableProperty]
    private DateTime _sessionStartTime;

    #endregion

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public PerformanceDashboardViewModel()
    {
        _timeProvider = new SystemTimeProvider();
        _updateTimer = new System.Timers.Timer(2000); // 2 seconds
        _updateTimer.Elapsed += async (s, e) => await UpdateMetricsAsync();
        _updateTimer.AutoReset = true;

        InitializeChartConfiguration();
        InitializeSampleData();
        InitializeChartSeries();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceDashboardViewModel"/> class.
    /// </summary>
    public PerformanceDashboardViewModel(
        ITimeProvider timeProvider,
        IPerformanceService? performanceService = null,
        ISystemResourceManager? systemResourceManager = null,
        IPerformanceMonitor? performanceMonitor = null,
        ICachePerformanceMonitor? cacheMonitor = null,
        IApplicationMetrics? applicationMetrics = null,
        ErrorTrackingService? errorTrackingService = null,
        INotificationService? notificationService = null,
        IDialogService? dialogService = null)
    {
        _timeProvider = timeProvider;
        _performanceService = performanceService;
        _systemResourceManager = systemResourceManager;
        _performanceMonitor = performanceMonitor;
        _cacheMonitor = cacheMonitor;
        _applicationMetrics = applicationMetrics;
        _errorTrackingService = errorTrackingService;
        _notificationService = notificationService;
        _dialogService = dialogService;

        _sessionStartTime = timeProvider.Now;
        _updateTimer = new System.Timers.Timer(2000); // 2 seconds
        _updateTimer.Elapsed += async (s, e) => await UpdateMetricsAsync();
        _updateTimer.AutoReset = true;

        InitializeChartConfiguration();
        InitializeSampleData();
        InitializeChartSeries();

        // Start monitoring if performance monitor is available
        if (_performanceMonitor != null)
        {
            _performanceMonitor.SnapshotUpdated += OnPerformanceSnapshotUpdated;
        }
    }

    private void InitializeChartConfiguration()
    {
        // Time axis for all charts
        TimeAxis = new ICartesianAxis[]
        {
            new DateTimeAxis(TimeSpan.FromMinutes(1), date => date.ToString("HH:mm:ss"))
            {
                Name = "Time",
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 0.5f }
            }
        };

        // Percentage axis (0-100%)
        PercentageAxis = new ICartesianAxis[]
        {
            new Axis
            {
                Name = "%",
                MinLimit = 0,
                MaxLimit = 100,
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 0.5f }
            }
        };

        // FPS axis (0-200)
        FpsAxis = new ICartesianAxis[]
        {
            new Axis
            {
                Name = "FPS",
                MinLimit = 0,
                MaxLimit = 200,
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 0.5f }
            }
        };
    }

    private void InitializeChartSeries()
    {
        CpuSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = _cpuData,
                Name = "CPU",
                Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(30)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        };

        GpuSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = _gpuData,
                Name = "GPU",
                Stroke = new SolidColorPaint(SKColors.LimeGreen) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.LimeGreen.WithAlpha(30)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        };

        MemorySeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = _memoryData,
                Name = "Memory",
                Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Orange.WithAlpha(30)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        };

        FpsSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = _fpsData,
                Name = "FPS",
                Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(30)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        };
    }

    private void InitializeSampleData()
    {
        var now = _timeProvider.Now;

        // Generate sample historical data (last 10 minutes, every 10 seconds)
        for (int i = 60; i >= 0; i--)
        {
            var timestamp = now.AddSeconds(-i * 10);
            var cpuValue = 30 + _random.Next(40);
            var gpuValue = 20 + _random.Next(50);
            var memoryValue = 45 + _random.Next(30);
            var fpsValue = 45 + _random.Next(80);

            _cpuData.Add(new DateTimePoint(timestamp, cpuValue));
            _gpuData.Add(new DateTimePoint(timestamp, gpuValue));
            _memoryData.Add(new DateTimePoint(timestamp, memoryValue));
            _fpsData.Add(new DateTimePoint(timestamp, fpsValue));
        }

        // Current values
        CurrentCpuPercent = _cpuData.LastOrDefault()?.Value ?? 45;
        CurrentGpuPercent = _gpuData.LastOrDefault()?.Value ?? 35;
        CurrentMemoryPercent = _memoryData.LastOrDefault()?.Value ?? 62;
        CurrentFps = _fpsData.LastOrDefault()?.Value ?? 60;
        CurrentDiskUsage = 25;

        // Game stats
        GameStats = new ObservableCollection<GamePerformanceStats>
        {
            new()
            {
                GameId = Guid.NewGuid(),
                GameName = "Cyberpunk 2077",
                AverageFps = 58,
                MinFps = 42,
                MaxFps = 72,
                TotalPlaytime = TimeSpan.FromHours(45),
                SessionCount = 12,
                LastPlayed = now.AddDays(-2),
                FpsHistory = GenerateSampleFpsHistory()
            },
            new()
            {
                GameId = Guid.NewGuid(),
                GameName = "Hades",
                AverageFps = 144,
                MinFps = 120,
                MaxFps = 165,
                TotalPlaytime = TimeSpan.FromHours(120),
                SessionCount = 45,
                LastPlayed = now.AddDays(-1),
                FpsHistory = GenerateSampleFpsHistory(120, 165)
            },
            new()
            {
                GameId = Guid.NewGuid(),
                GameName = "Elden Ring",
                AverageFps = 55,
                MinFps = 38,
                MaxFps = 60,
                TotalPlaytime = TimeSpan.FromHours(200),
                SessionCount = 78,
                LastPlayed = now,
                FpsHistory = GenerateSampleFpsHistory(38, 60)
            },
            new()
            {
                GameId = Guid.NewGuid(),
                GameName = "Baldur's Gate 3",
                AverageFps = 85,
                MinFps = 65,
                MaxFps = 120,
                TotalPlaytime = TimeSpan.FromHours(85),
                SessionCount = 23,
                LastPlayed = now.AddDays(-3),
                FpsHistory = GenerateSampleFpsHistory(65, 120)
            }
        };

        // Recommendations
        Recommendations = new ObservableCollection<OptimizationRecommendation>
        {
            new()
            {
                Title = "Close background apps to free 2GB RAM",
                Description = "Chrome and Discord are using significant memory. Closing them could improve performance by 15%.",
                Severity = RecommendationSeverity.Critical,
                Category = RecommendationCategory.Memory,
                CanAutoApply = true,
                EstimatedImprovement = 15.0,
                ActionText = "Close Apps"
            },
            new()
            {
                Title = "Enable game mode for better CPU priority",
                Description = "Windows Game Mode can improve gaming performance by prioritizing game processes.",
                Severity = RecommendationSeverity.Warning,
                Category = RecommendationCategory.Cpu,
                CanAutoApply = true,
                EstimatedImprovement = 8.0,
                ActionText = "Enable"
            },
            new()
            {
                Title = "Update GPU drivers",
                Description = "New NVIDIA drivers available with 15% performance boost for recent games.",
                Severity = RecommendationSeverity.Info,
                Category = RecommendationCategory.Gpu,
                CanAutoApply = false,
                EstimatedImprovement = 15.0,
                ActionText = "Download"
            },
            new()
            {
                Title = "Clear temporary files",
                Description = "15GB of temporary files can be safely removed to free disk space.",
                Severity = RecommendationSeverity.Warning,
                Category = RecommendationCategory.Disk,
                CanAutoApply = true,
                EstimatedImprovement = 5.0,
                ActionText = "Clean Up"
            }
        };

        HasCriticalRecommendations = Recommendations.Any(r => r.Severity == RecommendationSeverity.Critical);

        // Cache stats
        CacheHitRate = 0.94;
        CacheSize = 1024L * 1024 * 145; // 145 MB
        CacheEntries = 1240;
        CacheEvictions = 45;

        // Session stats
        SessionDuration = TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(34));
        GamesLaunchedThisSession = 3;
        AverageFpsThisSession = 56;
    }

    private ObservableCollection<MetricDataPoint> GenerateSampleFpsHistory(int minFps = 40, int maxFps = 80)
    {
        var history = new ObservableCollection<MetricDataPoint>();
        var now = _timeProvider.Now;

        for (int i = 30; i >= 0; i--)
        {
            history.Add(new MetricDataPoint
            {
                Timestamp = now.AddMinutes(-i),
                Value = minFps + _random.Next(maxFps - minFps)
            });
        }

        return history;
    }

    private void OnPerformanceSnapshotUpdated(object? sender, PerformanceSnapshot snapshot)
    {
        // Update current values from snapshot
        CurrentCpuPercent = snapshot.CpuUsagePercent;
        CurrentFps = snapshot.Fps;

        if (snapshot.GpuUsagePercent.HasValue)
            CurrentGpuPercent = snapshot.GpuUsagePercent.Value;

        // Add to history (handled by UpdateMetricsAsync to batch updates)
    }

    private async Task UpdateMetricsAsync()
    {
        try
        {
            var now = _timeProvider.Now;

            // Update session duration
            SessionDuration = now - SessionStartTime;

            if (_performanceService != null)
            {
                var result = await _performanceService.GetMetricsAsync();
                if (result.IsSuccess && result.Value != null)
                {
                    var metrics = result.Value;
                    CurrentCpuPercent = metrics.CpuUsage;
                    CurrentGpuPercent = metrics.GpuUsage;
                    CurrentMemoryPercent = metrics.MemoryUsage;
                    CurrentDiskUsage = metrics.DiskUsage;
                    CurrentFps = metrics.Fps;
                }
            }
            else
            {
                // Demo mode: simulate slight variations
                CurrentCpuPercent = Math.Clamp(CurrentCpuPercent + _random.NextDouble() * 10 - 5, 10, 95);
                CurrentGpuPercent = Math.Clamp(CurrentGpuPercent + _random.NextDouble() * 10 - 5, 5, 90);
                CurrentMemoryPercent = Math.Clamp(CurrentMemoryPercent + _random.NextDouble() * 4 - 2, 30, 95);
                CurrentFps = Math.Clamp(CurrentFps + _random.NextDouble() * 10 - 5, 30, 144);
            }

            // Add to chart data (limit to last 100 points)
            _cpuData.Add(new DateTimePoint(now, CurrentCpuPercent));
            _gpuData.Add(new DateTimePoint(now, CurrentGpuPercent));
            _memoryData.Add(new DateTimePoint(now, CurrentMemoryPercent));
            _fpsData.Add(new DateTimePoint(now, CurrentFps));

            // Trim old data (keep last 10 minutes at 2-second intervals = 300 points)
            TrimChartData(_cpuData, 300);
            TrimChartData(_gpuData, 300);
            TrimChartData(_memoryData, 300);
            TrimChartData(_fpsData, 300);
        }
        catch (Exception ex)
        {
            _errorTrackingService?.RecordException(nameof(PerformanceDashboardViewModel), ex.GetType().Name, ex.Message, ex);
        }
    }

    private static void TrimChartData(ObservableCollection<DateTimePoint> data, int maxPoints)
    {
        while (data.Count > maxPoints)
        {
            data.RemoveAt(0);
        }
    }

    #region Commands

    /// <summary>
    /// Refreshes all performance data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;

        try
        {
            if (_performanceService != null)
            {
                // Refresh recommendations
                var recResult = await _performanceService.GetRecommendationsAsync();
                if (recResult.IsSuccess && recResult.Value != null)
                {
                    Recommendations = new ObservableCollection<OptimizationRecommendation>(recResult.Value);
                    HasCriticalRecommendations = Recommendations.Any(r => r.Severity == RecommendationSeverity.Critical);
                }

                // Refresh game stats
                var gameResult = await _performanceService.GetGameStatsAsync();
                if (gameResult.IsSuccess && gameResult.Value != null)
                {
                    GameStats = new ObservableCollection<GamePerformanceStats>(gameResult.Value);
                }

                // Refresh cache stats
                var cacheResult = await _performanceService.GetCacheStatsAsync();
                if (cacheResult.IsSuccess && cacheResult.Value != null)
                {
                    var cache = cacheResult.Value;
                    CacheHitRate = cache.HitRate;
                    CacheSize = cache.SizeBytes;
                    CacheEntries = cache.EntryCount;
                    CacheEvictions = cache.EvictionCount;
                }
            }
            else
            {
                await Task.Delay(500); // Simulate network call
                _notificationService?.ShowNotificationAsync("Performance data refreshed (demo mode)", "Refresh Complete");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error refreshing data: {ex.Message}");
        }

        IsLoading = false;
    }

    /// <summary>
    /// Applies an optimization recommendation.
    /// </summary>
    [RelayCommand]
    private async Task ApplyRecommendationAsync(OptimizationRecommendation? recommendation)
    {
        if (recommendation is null) return;

        try
        {
            if (recommendation.Action != null)
            {
                var success = await recommendation.Action();
                if (success)
                {
                    Recommendations.Remove(recommendation);
                    _notificationService?.ShowSuccess($"Applied: {recommendation.Title}", "Optimization Applied");
                    HasCriticalRecommendations = Recommendations.Any(r => r.Severity == RecommendationSeverity.Critical);
                }
                else
                {
                    _notificationService?.ShowError($"Failed to apply: {recommendation.Title}");
                }
            }
            else if (_performanceService != null)
            {
                var result = await _performanceService.ApplyOptimizationAsync(recommendation.Id.ToString());
                if (result.IsSuccess)
                {
                    Recommendations.Remove(recommendation);
                    _notificationService?.ShowSuccess($"Applied: {recommendation.Title}", "Optimization Applied");
                    HasCriticalRecommendations = Recommendations.Any(r => r.Severity == RecommendationSeverity.Critical);
                }
                else
                {
                    _notificationService?.ShowError($"Failed: {result.Error}");
                }
            }
            else
            {
                // Demo mode
                await Task.Delay(500);
                Recommendations.Remove(recommendation);
                _notificationService?.ShowNotificationAsync($"Applied: {recommendation.Title} (demo mode)", "Optimization");
                HasCriticalRecommendations = Recommendations.Any(r => r.Severity == RecommendationSeverity.Critical);
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error applying optimization: {ex.Message}");
        }
    }

    /// <summary>
    /// Dismisses a recommendation without applying it.
    /// </summary>
    [RelayCommand]
    private void DismissRecommendationAsync(OptimizationRecommendation? recommendation)
    {
        if (recommendation is null) return;

        Recommendations.Remove(recommendation);
        HasCriticalRecommendations = Recommendations.Any(r => r.Severity == RecommendationSeverity.Critical);
    }

    /// <summary>
    /// Runs a performance benchmark.
    /// </summary>
    [RelayCommand]
    private async Task RunBenchmarkAsync()
    {
        try
        {
            _notificationService?.ShowInfo("Running performance benchmark...", "Benchmark");

            if (_performanceService != null)
            {
                var result = await _performanceService.RunBenchmarkAsync();
                if (result.IsSuccess && result.Value != null)
                {
                    var benchmark = result.Value;
                    await (_dialogService?.ShowMessageAsync(
                        "Benchmark Complete",
                        $"Overall Score: {benchmark.OverallScore:F0}\n\n" +
                        $"CPU: {benchmark.CpuScore:F0}\n" +
                        $"Memory: {benchmark.MemoryScore:F0}\n" +
                        $"GPU: {benchmark.GpuScore:F0}\n" +
                        $"Disk: {benchmark.DiskScore:F0}\n\n" +
                        $"Duration: {benchmark.Duration.TotalSeconds:F1}s") ?? Task.CompletedTask);
                }
                else
                {
                    _notificationService?.ShowError($"Benchmark failed: {result.Error}");
                }
            }
            else
            {
                await Task.Delay(2000);
                await (_dialogService?.ShowMessageAsync(
                    "Benchmark Complete (Demo)",
                    "Overall Score: 8,450\n\n" +
                    "CPU: 9,200\n" +
                    "Memory: 7,800\n" +
                    "GPU: 9,500\n" +
                    "Disk: 7,300\n\n" +
                    "Duration: 2.5s") ?? Task.CompletedTask);
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error running benchmark: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports a performance report.
    /// </summary>
    [RelayCommand]
    private async Task ExportReportAsync()
    {
        try
        {
            if (_performanceService != null)
            {
                var result = await _performanceService.ExportReportAsync();
                if (result.IsSuccess && result.Value != null)
                {
                    _notificationService?.ShowSuccess($"Report exported to: {result.Value}", "Export Complete");
                }
                else
                {
                    _notificationService?.ShowError($"Export failed: {result.Error}");
                }
            }
            else
            {
                await Task.Delay(1000);
                _notificationService?.ShowSuccess("Performance report exported to Downloads (demo mode)", "Export Complete");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error exporting report: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the performance history.
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        try
        {
            var confirmed = await (_dialogService?.ShowConfirmationAsync(
                "Clear History",
                "Are you sure you want to clear all performance history? This cannot be undone.") ?? Task.FromResult(false));

            if (!confirmed) return;

            if (_performanceService != null)
            {
                var result = await _performanceService.ClearHistoryAsync();
                if (result.IsSuccess)
                {
                    _cpuData.Clear();
                    _gpuData.Clear();
                    _memoryData.Clear();
                    _fpsData.Clear();
                    _notificationService?.ShowSuccess("Performance history cleared");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to clear history: {result.Error}");
                }
            }
            else
            {
                _cpuData.Clear();
                _gpuData.Clear();
                _memoryData.Clear();
                _fpsData.Clear();
                _notificationService?.ShowSuccess("Performance history cleared (demo mode)");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error clearing history: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles real-time monitoring on/off.
    /// </summary>
    [RelayCommand]
    private void ToggleRealTimeMonitoring()
    {
        IsRealtimeMonitoring = !IsRealtimeMonitoring;

        if (IsRealtimeMonitoring)
        {
            _updateTimer.Start();
            _notificationService?.ShowNotificationAsync("Real-time monitoring enabled", "Monitoring");
        }
        else
        {
            _updateTimer.Stop();
            _notificationService?.ShowNotificationAsync("Real-time monitoring paused", "Monitoring");
        }
    }

    /// <summary>
    /// Opens detailed view for selected game.
    /// </summary>
    [RelayCommand]
    private async Task OpenGameDetailsAsync(GamePerformanceStats? game)
    {
        if (game is null) return;

        SelectedGame = game;
        await (_dialogService?.ShowGamePerformanceDetailAsync(game) ?? Task.CompletedTask);
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        try
        {
            CacheHitRate = 0;
            CacheSize = 0;
            CacheEntries = 0;
            CacheEvictions = 0;
            _notificationService?.ShowSuccess("Cache cleared successfully");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error clearing cache: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Cleans up resources when the ViewModel is disposed.
    /// </summary>
    public void Dispose()
    {
        _updateTimer.Stop();
        _updateTimer.Dispose();

        if (_performanceMonitor != null)
        {
            _performanceMonitor.SnapshotUpdated -= OnPerformanceSnapshotUpdated;
        }

        GC.SuppressFinalize(this);
    }
}
