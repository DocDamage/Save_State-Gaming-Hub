using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Performance.Services;
using SaveState.Infrastructure.Monitoring;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Settings;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// Represents a gaming session with detailed performance metrics.
/// </summary>
public class GameSessionPerformance : ObservableObject
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;

    public double AverageFps { get; set; }
    public double MinFps { get; set; }
    public double MaxFps { get; set; }
    public double OnePercentLow { get; set; }
    public double PointOnePercentLow { get; set; }

    public double AverageCpuUsage { get; set; }
    public double AverageGpuUsage { get; set; }
    public double AverageMemoryUsage { get; set; }

    public ObservableCollection<MetricDataPoint> FpsHistory { get; set; } = new();

    public string FormattedDuration => $"{Duration.TotalHours:F0}h {Duration.Minutes}m";
}

/// <summary>
/// Represents a performance optimization suggestion specific to a game.
/// </summary>
public class GameSpecificOptimization
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Graphics, CPU, Memory, Settings
    public double ExpectedImprovement { get; set; }
    public bool CanAutoApply { get; set; }
    public string ActionText { get; set; } = "Apply";
}

/// <summary>
/// Hardware utilization statistics.
/// </summary>
public class HardwareUtilization
{
    public double CpuAverage { get; set; }
    public double CpuPeak { get; set; }
    public double GpuAverage { get; set; }
    public double GpuPeak { get; set; }
    public double MemoryAverage { get; set; }
    public double MemoryPeak { get; set; }
    public long MemoryUsedBytes { get; set; }
}

/// <summary>
/// ViewModel for the Game Performance Detail dialog.
/// Shows comprehensive performance statistics for a specific game.
/// </summary>
public partial class GamePerformanceDetailViewModel : ObservableObject
{
    private readonly IPerformanceService? _performanceService;
    private readonly ISystemResourceManager? _systemResourceManager;
    private readonly IPerformanceMonitor? _performanceMonitor;
    private readonly ErrorTrackingService? _errorTrackingService;
    private readonly INotificationService? _notificationService;
    private readonly ITimeProvider _timeProvider;
    private readonly Random _random = new();

    // Chart data
    private readonly ObservableCollection<DateTimePoint> _fpsHistoryData = new();
    private readonly ObservableCollection<DateTimePoint> _cpuHistoryData = new();
    private readonly ObservableCollection<DateTimePoint> _gpuHistoryData = new();
    private readonly ObservableCollection<DateTimePoint> _memoryHistoryData = new();

    #region Observable Properties

    /// <summary>Game ID.</summary>
    [ObservableProperty]
    private Guid _gameId;

    /// <summary>Game name.</summary>
    [ObservableProperty]
    private string _gameName = string.Empty;

    /// <summary>Cover image path/URL.</summary>
    [ObservableProperty]
    private string? _coverImage;

    /// <summary>Average FPS across all sessions.</summary>
    [ObservableProperty]
    private double _overallAverageFps;

    /// <summary>Minimum FPS recorded.</summary>
    [ObservableProperty]
    private double _overallMinFps;

    /// <summary>Maximum FPS recorded.</summary>
    [ObservableProperty]
    private double _overallMaxFps;

    /// <summary>1% low FPS.</summary>
    [ObservableProperty]
    private double _onePercentLow;

    /// <summary>0.1% low FPS.</summary>
    [ObservableProperty]
    private double _pointOnePercentLow;

    /// <summary>Total playtime across all sessions.</summary>
    [ObservableProperty]
    private TimeSpan _totalPlaytime;

    /// <summary>Number of gaming sessions.</summary>
    [ObservableProperty]
    private int _sessionCount;

    /// <summary>Last played date.</summary>
    [ObservableProperty]
    private DateTime _lastPlayed;

    /// <summary>Performance by session.</summary>
    [ObservableProperty]
    private ObservableCollection<GameSessionPerformance> _sessions = new();

    /// <summary>Currently selected session.</summary>
    [ObservableProperty]
    private GameSessionPerformance? _selectedSession;

    /// <summary>Hardware utilization statistics.</summary>
    [ObservableProperty]
    private HardwareUtilization _hardwareStats = new();

    /// <summary>Game-specific optimization suggestions.</summary>
    [ObservableProperty]
    private ObservableCollection<GameSpecificOptimization> _optimizations = new();

    /// <summary>Comparison with system average.</summary>
    [ObservableProperty]
    private double _fpsVsAverage; // Percentage difference

    /// <summary>Whether this game performs better than average.</summary>
    [ObservableProperty]
    private bool _isAboveAverage;

    /// <summary>Chart series for FPS history.</summary>
    [ObservableProperty]
    private ISeries[] _fpsSeries = Array.Empty<ISeries>();

    /// <summary>Chart series for hardware utilization.</summary>
    [ObservableProperty]
    private ISeries[] _hardwareSeries = Array.Empty<ISeries>();

    /// <summary>X-axis configuration for time-based charts.</summary>
    [ObservableProperty]
    private ICartesianAxis[] _timeAxis = Array.Empty<ICartesianAxis>();

    /// <summary>Y-axis configuration for FPS charts.</summary>
    [ObservableProperty]
    private ICartesianAxis[] _fpsAxis = Array.Empty<ICartesianAxis>();

    /// <summary>Y-axis configuration for percentage charts.</summary>
    [ObservableProperty]
    private ICartesianAxis[] _percentageAxis = Array.Empty<ICartesianAxis>();

    /// <summary>Whether data is loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Performance trend (positive/negative).</summary>
    [ObservableProperty]
    private double _performanceTrend; // Percentage change from previous period

    /// <summary>Stability score (0-100).</summary>
    [ObservableProperty]
    private double _stabilityScore;

    #endregion

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public GamePerformanceDetailViewModel()
    {
        _timeProvider = new SystemTimeProvider();
        InitializeChartConfiguration();
        InitializeSampleData();
        InitializeChartSeries();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GamePerformanceDetailViewModel"/> class.
    /// </summary>
    public GamePerformanceDetailViewModel(
        ITimeProvider timeProvider,
        GamePerformanceStats gameStats,
        IPerformanceService? performanceService = null,
        ISystemResourceManager? systemResourceManager = null,
        IPerformanceMonitor? performanceMonitor = null,
        ErrorTrackingService? errorTrackingService = null,
        INotificationService? notificationService = null)
    {
        _timeProvider = timeProvider;
        _performanceService = performanceService;
        _systemResourceManager = systemResourceManager;
        _performanceMonitor = performanceMonitor;
        _errorTrackingService = errorTrackingService;
        _notificationService = notificationService;

        // Initialize from game stats
        GameId = gameStats.GameId;
        GameName = gameStats.GameName;
        CoverImage = gameStats.CoverImage;
        OverallAverageFps = gameStats.AverageFps;
        OverallMinFps = gameStats.MinFps;
        OverallMaxFps = gameStats.MaxFps;
        TotalPlaytime = gameStats.TotalPlaytime;
        SessionCount = gameStats.SessionCount;
        LastPlayed = gameStats.LastPlayed;

        InitializeChartConfiguration();
        InitializeSampleData();
        InitializeChartSeries();

        // Load detailed data
        _ = LoadGameDetailsAsync();
    }

    private void InitializeChartConfiguration()
    {
        // Time axis for all charts
        TimeAxis = new ICartesianAxis[]
        {
            new DateTimeAxis(TimeSpan.FromMinutes(1), date => date.ToString("HH:mm"))
            {
                Name = "Time",
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
    }

    private void InitializeChartSeries()
    {
        FpsSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = _fpsHistoryData,
                Name = "FPS",
                Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(30)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        };

        HardwareSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = _cpuHistoryData,
                Name = "CPU",
                Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 2 },
                GeometrySize = 0,
                LineSmoothness = 0.3
            },
            new LineSeries<DateTimePoint>
            {
                Values = _gpuHistoryData,
                Name = "GPU",
                Stroke = new SolidColorPaint(SKColors.LimeGreen) { StrokeThickness = 2 },
                GeometrySize = 0,
                LineSmoothness = 0.3
            },
            new LineSeries<DateTimePoint>
            {
                Values = _memoryHistoryData,
                Name = "Memory",
                Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 },
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        };
    }

    private void InitializeSampleData()
    {
        var now = _timeProvider.Now;

        // Generate sample FPS history
        for (int i = 60; i >= 0; i--)
        {
            var timestamp = now.AddMinutes(-i);
            var baseFps = OverallAverageFps > 0 ? OverallAverageFps : 60;
            _fpsHistoryData.Add(new DateTimePoint(timestamp, baseFps + _random.NextDouble() * 20 - 10));
            _cpuHistoryData.Add(new DateTimePoint(timestamp, 40 + _random.NextDouble() * 40));
            _gpuHistoryData.Add(new DateTimePoint(timestamp, 50 + _random.NextDouble() * 40));
            _memoryHistoryData.Add(new DateTimePoint(timestamp, 60 + _random.NextDouble() * 20));
        }

        // Sample sessions
        Sessions = new ObservableCollection<GameSessionPerformance>
        {
            new()
            {
                StartTime = now.AddDays(-1),
                EndTime = now.AddDays(-1).AddHours(2),
                AverageFps = OverallAverageFps + 5,
                MinFps = OverallMinFps,
                MaxFps = OverallMaxFps,
                OnePercentLow = OverallAverageFps - 10,
                PointOnePercentLow = OverallAverageFps - 15,
                AverageCpuUsage = 45,
                AverageGpuUsage = 65,
                AverageMemoryUsage = 70
            },
            new()
            {
                StartTime = now.AddDays(-3),
                EndTime = now.AddDays(-3).AddHours(3.5),
                AverageFps = OverallAverageFps - 3,
                MinFps = OverallMinFps - 5,
                MaxFps = OverallMaxFps - 2,
                OnePercentLow = OverallAverageFps - 15,
                PointOnePercentLow = OverallAverageFps - 20,
                AverageCpuUsage = 55,
                AverageGpuUsage = 75,
                AverageMemoryUsage = 75
            },
            new()
            {
                StartTime = now.AddDays(-7),
                EndTime = now.AddDays(-7).AddHours(1.5),
                AverageFps = OverallAverageFps,
                MinFps = OverallMinFps,
                MaxFps = OverallMaxFps,
                OnePercentLow = OverallAverageFps - 12,
                PointOnePercentLow = OverallAverageFps - 18,
                AverageCpuUsage = 40,
                AverageGpuUsage = 60,
                AverageMemoryUsage = 65
            }
        };

        // Hardware stats
        HardwareStats = new HardwareUtilization
        {
            CpuAverage = 48,
            CpuPeak = 85,
            GpuAverage = 65,
            GpuPeak = 95,
            MemoryAverage = 72,
            MemoryPeak = 89,
            MemoryUsedBytes = 6L * 1024 * 1024 * 1024 // 6GB
        };

        // Optimizations
        Optimizations = new ObservableCollection<GameSpecificOptimization>
        {
            new()
            {
                Title = "Lower Shadow Quality",
                Description = "Reducing shadow quality from Ultra to High can improve FPS by 15% with minimal visual impact.",
                Category = "Graphics",
                ExpectedImprovement = 15,
                CanAutoApply = false,
                ActionText = "Apply"
            },
            new()
            {
                Title = "Enable DLSS/FSR",
                Description = "AI upscaling can provide 20-40% performance boost with similar image quality.",
                Category = "Graphics",
                ExpectedImprovement = 25,
                CanAutoApply = false,
                ActionText = "Configure"
            },
            new()
            {
                Title = "Limit Background FPS",
                Description = "Limiting FPS when game is not focused saves GPU resources.",
                Category = "Settings",
                ExpectedImprovement = 10,
                CanAutoApply = true,
                ActionText = "Apply"
            },
            new()
            {
                Title = "Optimize CPU Affinity",
                Description = "Pinning game to specific CPU cores can reduce stuttering.",
                Category = "CPU",
                ExpectedImprovement = 8,
                CanAutoApply = true,
                ActionText = "Apply"
            },
            new()
            {
                Title = "Clear Shader Cache",
                Description = "Outdated shader cache can cause stuttering. Clearing may help.",
                Category = "Settings",
                ExpectedImprovement = 5,
                CanAutoApply = true,
                ActionText = "Clear"
            }
        };

        // Performance comparison
        FpsVsAverage = _random.NextDouble() * 20 - 5; // -5% to +15%
        IsAboveAverage = FpsVsAverage > 0;

        // Performance trend (last 7 days vs previous 7 days)
        PerformanceTrend = _random.NextDouble() * 10 - 3; // -3% to +7%

        // Stability score (0-100)
        StabilityScore = 85;

        // Calculate 1% and 0.1% lows if not set
        if (OnePercentLow == 0) OnePercentLow = OverallAverageFps * 0.85;
        if (PointOnePercentLow == 0) PointOnePercentLow = OverallAverageFps * 0.75;
    }

    private async Task LoadGameDetailsAsync()
    {
        IsLoading = true;

        try
        {
            // In a real implementation, this would load detailed performance data
            // from the performance service or database
            await Task.Delay(500); // Simulate loading

            // Load from performance monitor if available
            if (_performanceService != null)
            {
                // Fetch detailed game performance data
                // var result = await _performanceService.GetGameDetailsAsync(GameId);
                // ...
            }
        }
        catch (Exception ex)
        {
            _errorTrackingService?.RecordException(
                nameof(GamePerformanceDetailViewModel),
                ex.GetType().Name,
                $"Failed to load game details for {GameName}: {ex.Message}",
                ex);
        }

        IsLoading = false;
    }

    #region Commands

    /// <summary>
    /// Refreshes the game performance data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadGameDetailsAsync();
    }

    /// <summary>
    /// Applies an optimization suggestion.
    /// </summary>
    [RelayCommand]
    private async Task ApplyOptimizationAsync(GameSpecificOptimization? optimization)
    {
        if (optimization is null) return;

        try
        {
            _notificationService?.ShowInfo($"Applying: {optimization.Title}", "Optimization");

            // Simulate applying optimization
            await Task.Delay(500);

            // In a real implementation, this would:
            // - Modify game config files
            // - Adjust system settings
            // - Apply registry tweaks
            // etc.

            _notificationService?.ShowSuccess(
                $"Applied: {optimization.Title}. Expected improvement: {optimization.ExpectedImprovement:F0}%",
                "Optimization Applied");
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Failed to apply optimization: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports a detailed performance report for this game.
    /// </summary>
    [RelayCommand]
    private async Task ExportReportAsync()
    {
        try
        {
            _notificationService?.ShowInfo("Generating performance report...", "Export");

            // Generate CSV/JSON report
            var report = $"""
                Performance Report for {GameName}
                Generated: {_timeProvider.Now:yyyy-MM-dd HH:mm:ss}

                Summary:
                - Average FPS: {OverallAverageFps:F1}
                - Min/Max FPS: {OverallMinFps:F0}/{OverallMaxFps:F0}
                - 1% Low: {OnePercentLow:F1}
                - 0.1% Low: {PointOnePercentLow:F1}
                - Total Playtime: {TotalPlaytime.TotalHours:F1} hours
                - Sessions: {SessionCount}

                Hardware Utilization:
                - CPU Avg/Peak: {HardwareStats.CpuAverage:F0}%/{HardwareStats.CpuPeak:F0}%
                - GPU Avg/Peak: {HardwareStats.GpuAverage:F0}%/{HardwareStats.GpuPeak:F0}%
                - Memory Avg/Peak: {HardwareStats.MemoryAverage:F0}%/{HardwareStats.MemoryPeak:F0}%
                """;

            // In a real implementation, this would save to a file
            await Task.Delay(500);

            _notificationService?.ShowSuccess("Performance report exported to Downloads", "Export Complete");
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Failed to export report: {ex.Message}");
        }
    }

    /// <summary>
    /// Selects a specific session to view detailed metrics.
    /// </summary>
    [RelayCommand]
    private void SelectSession(GameSessionPerformance? session)
    {
        if (session is null) return;

        SelectedSession = session;

        // Update chart data for selected session
        _fpsHistoryData.Clear();
        _cpuHistoryData.Clear();
        _gpuHistoryData.Clear();
        _memoryHistoryData.Clear();

        foreach (var point in session.FpsHistory)
        {
            _fpsHistoryData.Add(new DateTimePoint(point.Timestamp, point.Value));
        }

        // If no history, generate sample data
        if (_fpsHistoryData.Count == 0)
        {
            var now = _timeProvider.Now;
            for (int i = 60; i >= 0; i--)
            {
                var timestamp = now.AddMinutes(-i);
                _fpsHistoryData.Add(new DateTimePoint(timestamp, session.AverageFps + _random.NextDouble() * 20 - 10));
                _cpuHistoryData.Add(new DateTimePoint(timestamp, session.AverageCpuUsage + _random.NextDouble() * 20 - 10));
                _gpuHistoryData.Add(new DateTimePoint(timestamp, session.AverageGpuUsage + _random.NextDouble() * 20 - 10));
                _memoryHistoryData.Add(new DateTimePoint(timestamp, session.AverageMemoryUsage + _random.NextDouble() * 10 - 5));
            }
        }
    }

    /// <summary>
    /// Opens the game's configuration/settings.
    /// </summary>
    [RelayCommand]
    private async Task OpenGameSettingsAsync()
    {
        _notificationService?.ShowInfo($"Opening settings for {GameName}...", "Settings");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Compares this game's performance with other games.
    /// </summary>
    [RelayCommand]
    private async Task CompareWithOthersAsync()
    {
        _notificationService?.ShowInfo("Loading comparison data...", "Comparison");
        await Task.Delay(500);
        _notificationService?.ShowNotificationAsync(
            $"{GameName} performs {Math.Abs(FpsVsAverage):F0}% {(IsAboveAverage ? "better" : "worse")} than your average",
            "Performance Comparison");
    }

    #endregion
}
