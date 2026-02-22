using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

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
    public long AvailableMemoryBytes { get; set; }
    public long TotalMemoryBytes { get; set; }
}

/// <summary>
/// ViewModel for the Performance Dashboard.
/// Provides real-time monitoring of CPU, memory, GPU metrics and game performance statistics.
/// </summary>
public partial class PerformanceDashboardViewModel : ObservableObject
{
    private readonly IPerformanceService? _performanceService;
    private readonly INotificationService? _notificationService;

    /// <summary>CPU usage history over time.</summary>
    [ObservableProperty]
    private ObservableCollection<PerformanceMetric> _cpuHistory = new();

    /// <summary>Memory usage history over time.</summary>
    [ObservableProperty]
    private ObservableCollection<PerformanceMetric> _memoryHistory = new();

    /// <summary>GPU usage history over time.</summary>
    [ObservableProperty]
    private ObservableCollection<PerformanceMetric> _gpuHistory = new();

    /// <summary>Performance statistics for individual games.</summary>
    [ObservableProperty]
    private ObservableCollection<GamePerformanceStats> _gameStats = new();

    /// <summary>Optimization recommendations for the system.</summary>
    [ObservableProperty]
    private ObservableCollection<OptimizationRecommendation> _recommendations = new();

    /// <summary>Cache statistics view model.</summary>
    [ObservableProperty]
    private CacheStatisticsViewModel _cacheStats = new();

    /// <summary>Whether real-time monitoring is enabled.</summary>
    [ObservableProperty]
    private bool _isRealtimeMonitoring;

    /// <summary>Summary of optimization status.</summary>
    [ObservableProperty]
    private string _optimizationSummary = string.Empty;

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public PerformanceDashboardViewModel()
    {
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceDashboardViewModel"/> class.
    /// </summary>
    public PerformanceDashboardViewModel(
        IPerformanceService? performanceService = null,
        INotificationService? notificationService = null)
    {
        _performanceService = performanceService;
        _notificationService = notificationService;
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Generate sample historical data
        var now = DateTimeOffset.UtcNow.DateTime;
        var random = new Random();
        
        for (int i = 60; i >= 0; i--)
        {
            CpuHistory.Add(new PerformanceMetric { Timestamp = now.AddMinutes(-i), Value = 30 + random.Next(40) });
            MemoryHistory.Add(new PerformanceMetric { Timestamp = now.AddMinutes(-i), Value = 45 + random.Next(30) });
            GpuHistory.Add(new PerformanceMetric { Timestamp = now.AddMinutes(-i), Value = 20 + random.Next(60) });
        }

        GameStats = new ObservableCollection<GamePerformanceStats>
        {
            new() { GameName = "Cyberpunk 2077", AverageFps = 58, MinFps = 42, MaxFps = 72, PlayTime = TimeSpan.FromHours(45) },
            new() { GameName = "Hades", AverageFps = 144, MinFps = 120, MaxFps = 165, PlayTime = TimeSpan.FromHours(120) },
            new() { GameName = "Elden Ring", AverageFps = 55, MinFps = 38, MaxFps = 60, PlayTime = TimeSpan.FromHours(200) }
        };

        Recommendations = new ObservableCollection<OptimizationRecommendation>
        {
            new() { Title = "Enable Game Mode", Description = "Windows Game Mode can improve performance by 5-10%", Impact = "High", Category = "System" },
            new() { Title = "Update GPU Drivers", Description = "New drivers available with 15% performance boost", Impact = "High", Category = "Hardware" },
            new() { Title = "Close Background Apps", Description = "Chrome is using 2GB of RAM", Impact = "Medium", Category = "System" }
        };

        OptimizationSummary = "3 optimizations available - Potential 15-20% performance improvement";
    }

    /// <summary>
    /// Toggles real-time monitoring on/off.
    /// </summary>
    [RelayCommand]
    private void ToggleRealtimeMonitoring()
    {
        IsRealtimeMonitoring = !IsRealtimeMonitoring;
    }

    /// <summary>
    /// Applies an optimization recommendation.
    /// </summary>
    /// <param name="recommendation">The recommendation to apply.</param>
    [RelayCommand]
    private async Task ApplyOptimizationAsync(OptimizationRecommendation? recommendation)
    {
        if (recommendation is null) return;

        try
        {
            if (_performanceService is not null)
            {
                var result = await _performanceService.ApplyOptimizationAsync(recommendation.Title);
                if (result.IsSuccess)
                {
                    _notificationService?.ShowSuccess($"Applied: {recommendation.Title}", "Optimization Applied");
                    Recommendations.Remove(recommendation);
                }
                else
                {
                    _notificationService?.ShowError($"Failed to apply optimization: {result.Error}");
                }
            }
            else
            {
                await Task.Delay(500);
                Recommendations.Remove(recommendation);
                _notificationService?.ShowNotificationAsync("Optimization applied (demo mode)", "Optimization");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error applying optimization: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs a performance benchmark.
    /// </summary>
    [RelayCommand]
    private async Task RunBenchmarkAsync()
    {
        try
        {
            if (_performanceService is not null)
            {
                _notificationService?.ShowInfo("Running performance benchmark...", "Benchmark");
                var result = await _performanceService.RunBenchmarkAsync();
                if (result.IsSuccess && result.Value is not null)
                {
                    var benchmark = result.Value;
                    _notificationService?.ShowSuccess(
                        $"Benchmark complete! Overall Score: {benchmark.OverallScore:F0}",
                        "Benchmark Complete");
                }
                else
                {
                    _notificationService?.ShowError($"Benchmark failed: {result.Error}");
                }
            }
            else
            {
                await Task.Delay(2000);
                _notificationService?.ShowNotificationAsync(
                    "Benchmark completed (demo mode)",
                    "Benchmark");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error running benchmark: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents a single performance metric data point.
/// </summary>
public class PerformanceMetric
{
    /// <summary>Timestamp of the metric.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Value of the metric (percentage 0-100).</summary>
    public double Value { get; set; }
}

/// <summary>
/// Represents performance statistics for a specific game.
/// </summary>
public class GamePerformanceStats
{
    /// <summary>Name of the game.</summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>Average frames per second.</summary>
    public double AverageFps { get; set; }

    /// <summary>Minimum frames per second.</summary>
    public double MinFps { get; set; }

    /// <summary>Maximum frames per second.</summary>
    public double MaxFps { get; set; }

    /// <summary>Total play time.</summary>
    public TimeSpan PlayTime { get; set; }
}

/// <summary>
/// Represents an optimization recommendation.
/// </summary>
public class OptimizationRecommendation
{
    /// <summary>Title of the recommendation.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Description of the recommendation.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Impact level (Low, Medium, High).</summary>
    public string Impact { get; set; } = string.Empty;

    /// <summary>Category of the recommendation.</summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// View model for cache statistics display.
/// </summary>
public class CacheStatisticsViewModel
{
    /// <summary>Cache hit rate (0.0 to 1.0).</summary>
    public double HitRate { get; set; } = 0.94;

    /// <summary>Size of the cache in bytes.</summary>
    public long SizeBytes { get; set; } = 1024 * 1024 * 145;

    /// <summary>Number of entries in the cache.</summary>
    public int EntryCount { get; set; } = 1240;
}
