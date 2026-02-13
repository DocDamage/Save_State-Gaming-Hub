using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

public class PerformanceProfiler : IPerformanceProfiler, IDisposable
{
    private readonly ILogger<PerformanceProfiler> _logger;
    private readonly IGameMemoryReader _memoryReader;
    private readonly PerformanceMetricsCollector _metricsCollector;
    private readonly Timer _profilingTimer;
    private readonly List<PerformanceMetrics> _metricsHistory = new();
    private Guid _currentGameId;
    private DateTime _profilingStartTime;
    private bool _isProfiling;

    public event EventHandler<PerformanceMetricsUpdatedEventArgs>? MetricsUpdated;

    public bool IsProfiling => _isProfiling;

    public PerformanceProfiler(
        ILogger<PerformanceProfiler> logger,
        IGameMemoryReader memoryReader)
    {
        _logger = logger;
        _memoryReader = memoryReader;
        _metricsCollector = new PerformanceMetricsCollector(logger);

        // Collect metrics every 100ms for smooth monitoring
        _profilingTimer = new Timer(CollectMetrics, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task<Result> StartProfilingAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            if (_isProfiling)
            {
                return Task.FromResult(Result.Failure("Performance profiling is already running"));
            }

            _logger.LogInformation("Starting performance profiling for game {GameId}", gameId);

            _currentGameId = gameId;
            _profilingStartTime = DateTime.UtcNow;
            _metricsHistory.Clear();
            _isProfiling = true;

            // Start collecting metrics
            _profilingTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

            _logger.LogInformation("Performance profiling started successfully");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting performance profiling");
            return Task.FromResult(Result.Failure($"Failed to start profiling: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> StopProfilingAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_isProfiling)
            {
                return Task.FromResult(Result.Success()); // Already stopped
            }

            _logger.LogInformation("Stopping performance profiling");

            // Stop collecting metrics
            _profilingTimer.Change(Timeout.Infinite, Timeout.Infinite);

            _isProfiling = false;

            _logger.LogInformation("Performance profiling stopped successfully");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping performance profiling");
            return Task.FromResult(Result.Failure($"Failed to stop profiling: {ex.Message}", ErrorType.Internal));
        }
    }

    public async Task<Result<PerformanceMetrics>> GetCurrentMetricsAsync(CancellationToken ct = default)
    {
        if (!_isProfiling)
        {
            return Result.Failure<PerformanceMetrics>("Performance profiling is not running");
        }

        try
        {
            var metrics = await _metricsCollector.CollectMetricsAsync(ct);
            return Result.Success<PerformanceMetrics>(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current performance metrics");
            return Result.Failure<PerformanceMetrics>($"Failed to get metrics: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<PerformanceReport>> GenerateReportAsync(CancellationToken ct = default)
    {
        if (_metricsHistory.Count == 0)
        {
            return Task.FromResult(Result.Failure<PerformanceReport>("No performance data available. Start profiling first."));
        }

        try
        {
            var endTime = DateTime.UtcNow;
            var duration = endTime - _profilingStartTime;

            var averageMetrics = CalculateAverageMetrics();
            var peakMetrics = CalculatePeakMetrics();
            var minMetrics = CalculateMinMetrics();
            var issues = AnalyzePerformanceIssues();
            var recommendations = GenerateRecommendations(issues);

            var report = new PerformanceReport(
                GameId: _currentGameId,
                StartTime: _profilingStartTime,
                EndTime: endTime,
                Duration: duration,
                AverageMetrics: averageMetrics,
                PeakMetrics: peakMetrics,
                MinMetrics: minMetrics,
                Issues: issues,
                Recommendations: recommendations);

            _logger.LogInformation("Generated performance report for {Duration} of profiling",
                duration.ToString(@"hh\:mm\:ss"));

            return Task.FromResult(Result.Success<PerformanceReport>(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance report");
            return Task.FromResult(Result.Failure<PerformanceReport>($"Failed to generate report: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<IReadOnlyList<BottleneckAnalysis>>> AnalyzeBottlenecksAsync(CancellationToken ct = default)
    {
        if (_metricsHistory.Count == 0)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<BottleneckAnalysis>>("No performance data available"));
        }

        try
        {
            var bottlenecks = new List<BottleneckAnalysis>();

            // Analyze CPU bottlenecks
            var cpuBottleneck = AnalyzeCpuBottleneck();
            if (cpuBottleneck != null)
                bottlenecks.Add(cpuBottleneck);

            // Analyze GPU bottlenecks
            var gpuBottleneck = AnalyzeGpuBottleneck();
            if (gpuBottleneck != null)
                bottlenecks.Add(gpuBottleneck);

            // Analyze memory bottlenecks
            var memoryBottleneck = AnalyzeMemoryBottleneck();
            if (memoryBottleneck != null)
                bottlenecks.Add(memoryBottleneck);

            // Analyze frame rate bottlenecks
            var fpsBottleneck = AnalyzeFpsBottleneck();
            if (fpsBottleneck != null)
                bottlenecks.Add(fpsBottleneck);

            return Task.FromResult(Result.Success<IReadOnlyList<BottleneckAnalysis>>(bottlenecks));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing performance bottlenecks");
            return Task.FromResult(Result.Failure<IReadOnlyList<BottleneckAnalysis>>($"Failed to analyze bottlenecks: {ex.Message}", ErrorType.Internal));
        }
    }

    private void CollectMetrics(object? state)
    {
        if (!_isProfiling)
            return;

        // Fire-and-forget async collection with error handling
        _ = CollectMetricsInternalAsync();
    }

    private async Task CollectMetricsInternalAsync()
    {
        try
        {
            var metrics = await _metricsCollector.CollectMetricsAsync();
            _metricsHistory.Add(metrics);

            // Keep only last 10 minutes of history (60 seconds * 10 minutes * 10 samples/sec = 6000 samples)
            const int maxHistorySize = 6000;
            if (_metricsHistory.Count > maxHistorySize)
            {
                _metricsHistory.RemoveRange(0, _metricsHistory.Count - maxHistorySize);
            }

            // Notify listeners
            MetricsUpdated?.Invoke(this, new PerformanceMetricsUpdatedEventArgs { Metrics = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
        }
    }

    private PerformanceMetrics CalculateAverageMetrics()
    {
        if (_metricsHistory.Count == 0)
            return new PerformanceMetrics(DateTime.UtcNow, 0, 0, 0, 0, 0, 0, 0, Array.Empty<SubsystemMetrics>());

        return new PerformanceMetrics(
            Timestamp: DateTime.UtcNow,
            Fps: _metricsHistory.Average(m => m.Fps),
            FrameTimeMs: _metricsHistory.Average(m => m.FrameTimeMs),
            CpuUsagePercent: _metricsHistory.Average(m => m.CpuUsagePercent),
            GpuUsagePercent: _metricsHistory.Average(m => m.GpuUsagePercent),
            MemoryUsageBytes: (long)_metricsHistory.Average(m => m.MemoryUsageBytes),
            GpuMemoryBytes: (long)_metricsHistory.Average(m => m.GpuMemoryBytes),
            NetworkLatencyMs: _metricsHistory.Average(m => m.NetworkLatencyMs),
            Subsystems: CalculateAverageSubsystemMetrics());
    }

    private PerformanceMetrics CalculatePeakMetrics()
    {
        if (_metricsHistory.Count == 0)
            return new PerformanceMetrics(DateTime.UtcNow, 0, 0, 0, 0, 0, 0, 0, Array.Empty<SubsystemMetrics>());

        return new PerformanceMetrics(
            Timestamp: DateTime.UtcNow,
            Fps: _metricsHistory.Max(m => m.Fps),
            FrameTimeMs: _metricsHistory.Min(m => m.FrameTimeMs), // Lower frame time = better
            CpuUsagePercent: _metricsHistory.Max(m => m.CpuUsagePercent),
            GpuUsagePercent: _metricsHistory.Max(m => m.GpuUsagePercent),
            MemoryUsageBytes: _metricsHistory.Max(m => m.MemoryUsageBytes),
            GpuMemoryBytes: _metricsHistory.Max(m => m.GpuMemoryBytes),
            NetworkLatencyMs: _metricsHistory.Max(m => m.NetworkLatencyMs),
            Subsystems: CalculatePeakSubsystemMetrics());
    }

    private PerformanceMetrics CalculateMinMetrics()
    {
        if (_metricsHistory.Count == 0)
            return new PerformanceMetrics(DateTime.UtcNow, 0, 0, 0, 0, 0, 0, 0, Array.Empty<SubsystemMetrics>());

        return new PerformanceMetrics(
            Timestamp: DateTime.UtcNow,
            Fps: _metricsHistory.Min(m => m.Fps),
            FrameTimeMs: _metricsHistory.Max(m => m.FrameTimeMs), // Higher frame time = worse
            CpuUsagePercent: _metricsHistory.Min(m => m.CpuUsagePercent),
            GpuUsagePercent: _metricsHistory.Min(m => m.GpuUsagePercent),
            MemoryUsageBytes: _metricsHistory.Min(m => m.MemoryUsageBytes),
            GpuMemoryBytes: _metricsHistory.Min(m => m.GpuMemoryBytes),
            NetworkLatencyMs: _metricsHistory.Min(m => m.NetworkLatencyMs),
            Subsystems: CalculateMinSubsystemMetrics());
    }

    private IReadOnlyList<SubsystemMetrics> CalculateAverageSubsystemMetrics()
    {
        if (_metricsHistory.Count == 0 || !_metricsHistory[0].Subsystems.Any())
            return Array.Empty<SubsystemMetrics>();

        var subsystemNames = _metricsHistory[0].Subsystems.Select(s => s.SubsystemName).ToList();
        var result = new List<SubsystemMetrics>();

        foreach (var name in subsystemNames)
        {
            var subsystems = _metricsHistory.SelectMany(m => m.Subsystems.Where(s => s.SubsystemName == name));
            result.Add(new SubsystemMetrics(
                SubsystemName: name,
                UsagePercent: subsystems.Average(s => s.UsagePercent),
                TemperatureCelsius: subsystems.Average(s => s.TemperatureCelsius),
                Status: "Average"));
        }

        return result;
    }

    private IReadOnlyList<SubsystemMetrics> CalculatePeakSubsystemMetrics()
    {
        if (_metricsHistory.Count == 0 || !_metricsHistory[0].Subsystems.Any())
            return Array.Empty<SubsystemMetrics>();

        var subsystemNames = _metricsHistory[0].Subsystems.Select(s => s.SubsystemName).ToList();
        var result = new List<SubsystemMetrics>();

        foreach (var name in subsystemNames)
        {
            var subsystems = _metricsHistory.SelectMany(m => m.Subsystems.Where(s => s.SubsystemName == name));
            result.Add(new SubsystemMetrics(
                SubsystemName: name,
                UsagePercent: subsystems.Max(s => s.UsagePercent),
                TemperatureCelsius: subsystems.Max(s => s.TemperatureCelsius),
                Status: "Peak"));
        }

        return result;
    }

    private IReadOnlyList<SubsystemMetrics> CalculateMinSubsystemMetrics()
    {
        if (_metricsHistory.Count == 0 || !_metricsHistory[0].Subsystems.Any())
            return Array.Empty<SubsystemMetrics>();

        var subsystemNames = _metricsHistory[0].Subsystems.Select(s => s.SubsystemName).ToList();
        var result = new List<SubsystemMetrics>();

        foreach (var name in subsystemNames)
        {
            var subsystems = _metricsHistory.SelectMany(m => m.Subsystems.Where(s => s.SubsystemName == name));
            result.Add(new SubsystemMetrics(
                SubsystemName: name,
                UsagePercent: subsystems.Min(s => s.UsagePercent),
                TemperatureCelsius: subsystems.Min(s => s.TemperatureCelsius),
                Status: "Min"));
        }

        return result;
    }

    private IReadOnlyList<PerformanceIssue> AnalyzePerformanceIssues()
    {
        var issues = new List<PerformanceIssue>();

        var avgMetrics = CalculateAverageMetrics();

        // FPS issues
        if (avgMetrics.Fps < 30)
        {
            issues.Add(new PerformanceIssue(
                IssueType: "Low Frame Rate",
                Description: $"Average FPS of {avgMetrics.Fps:F1} is below 30 FPS",
                Severity: PerformanceSeverity.High,
                ImpactPercent: CalculateFpsImpact(avgMetrics.Fps),
                Causes: new[] { "GPU bottleneck", "CPU bottleneck", "Memory pressure", "High game settings" }));
        }

        // CPU issues
        if (avgMetrics.CpuUsagePercent > 90)
        {
            issues.Add(new PerformanceIssue(
                IssueType: "High CPU Usage",
                Description: $"CPU usage at {avgMetrics.CpuUsagePercent:F1}% indicates bottleneck",
                Severity: PerformanceSeverity.High,
                ImpactPercent: 85,
                Causes: new[] { "Background processes", "Outdated drivers", "Thermal throttling" }));
        }

        // Memory issues
        var memoryUsageGB = avgMetrics.MemoryUsageBytes / (1024.0 * 1024.0 * 1024.0);
        if (memoryUsageGB > 8) // Assuming 16GB system
        {
            issues.Add(new PerformanceIssue(
                IssueType: "High Memory Usage",
                Description: $"Memory usage at {memoryUsageGB:F1}GB may cause stuttering",
                Severity: PerformanceSeverity.Medium,
                ImpactPercent: 60,
                Causes: new[] { "Memory leak", "High texture settings", "Background applications" }));
        }

        return issues;
    }

    private IReadOnlyList<Recommendation> GenerateRecommendations(IReadOnlyList<PerformanceIssue> issues)
    {
        var recommendations = new List<Recommendation>();

        foreach (var issue in issues)
        {
            switch (issue.IssueType)
            {
                case "Low Frame Rate":
                    recommendations.Add(new Recommendation(
                        Title: "Optimize Graphics Settings",
                        Description: "Lower texture quality, shadows, and anti-aliasing to improve FPS",
                        Priority: RecommendationPriority.High,
                        Actions: new[]
                        {
                            "Reduce texture resolution to Medium",
                            "Lower shadow quality",
                            "Disable MSAA anti-aliasing",
                            "Reduce draw distance"
                        }));

                    recommendations.Add(new Recommendation(
                        Title: "Update Graphics Drivers",
                        Description: "Outdated GPU drivers can cause significant performance issues",
                        Priority: RecommendationPriority.Critical,
                        Actions: new[]
                        {
                            "Download latest drivers from manufacturer website",
                            "Use DDU to clean install drivers",
                            "Verify driver installation"
                        }));
                    break;

                case "High CPU Usage":
                    recommendations.Add(new Recommendation(
                        Title: "Close Background Applications",
                        Description: "Running applications compete for CPU resources",
                        Priority: RecommendationPriority.Medium,
                        Actions: new[]
                        {
                            "Close unnecessary browser tabs",
                            "Disable background antivirus scans",
                            "Use Task Manager to identify CPU-intensive processes"
                        }));
                    break;

                case "High Memory Usage":
                    recommendations.Add(new Recommendation(
                        Title: "Increase Virtual Memory",
                        Description: "Windows page file may be too small for current usage",
                        Priority: RecommendationPriority.Medium,
                        Actions: new[]
                        {
                            "Set page file to 1.5x system RAM",
                            "Defragment page file",
                            "Close memory-intensive applications"
                        }));
                    break;
            }
        }

        return recommendations;
    }

    private BottleneckAnalysis? AnalyzeCpuBottleneck()
    {
        var avgCpu = _metricsHistory.Average(m => m.CpuUsagePercent);
        var avgGpu = _metricsHistory.Average(m => m.GpuUsagePercent);

        if (avgCpu > 90 && avgGpu < 70)
        {
            return new BottleneckAnalysis(
                Component: "CPU",
                Severity: BottleneckSeverity.Severe,
                Description: "CPU is heavily utilized while GPU has capacity",
                ImpactPercent: 80,
                Solutions: new[]
                {
                    "Close background applications",
                    "Lower CPU-intensive game settings",
                    "Consider CPU upgrade for better performance"
                });
        }

        return null;
    }

    private BottleneckAnalysis? AnalyzeGpuBottleneck()
    {
        var avgCpu = _metricsHistory.Average(m => m.CpuUsagePercent);
        var avgGpu = _metricsHistory.Average(m => m.GpuUsagePercent);

        if (avgGpu > 95 && avgCpu < 80)
        {
            return new BottleneckAnalysis(
                Component: "GPU",
                Severity: BottleneckSeverity.Severe,
                Description: "GPU is at maximum utilization",
                ImpactPercent: 90,
                Solutions: new[]
                {
                    "Lower graphics settings",
                    "Reduce resolution or disable upscaling",
                    "Consider GPU upgrade"
                });
        }

        return null;
    }

    private BottleneckAnalysis? AnalyzeMemoryBottleneck()
    {
        var avgMemoryBytes = _metricsHistory.Average(m => m.MemoryUsageBytes);
        var memoryUsageGB = avgMemoryBytes / (1024.0 * 1024.0 * 1024.0);

        if (memoryUsageGB > 12) // Assuming 16GB system
        {
            return new BottleneckAnalysis(
                Component: "Memory",
                Severity: BottleneckSeverity.Moderate,
                Description: $"High memory usage ({memoryUsageGB:F1}GB) may cause stuttering",
                ImpactPercent: 60,
                Solutions: new[]
                {
                    "Close unnecessary applications",
                    "Increase page file size",
                    "Lower texture quality settings"
                });
        }

        return null;
    }

    private BottleneckAnalysis? AnalyzeFpsBottleneck()
    {
        var avgFps = _metricsHistory.Average(m => m.Fps);
        var fpsVariance = _metricsHistory.Max(m => m.Fps) - _metricsHistory.Min(m => m.Fps);

        if (avgFps < 60 && fpsVariance > avgFps * 0.5) // High variance indicates instability
        {
            return new BottleneckAnalysis(
                Component: "Frame Rate",
                Severity: BottleneckSeverity.Moderate,
                Description: $"Unstable frame rate (avg: {avgFps:F1}, variance: {fpsVariance:F1})",
                ImpactPercent: 70,
                Solutions: new[]
                {
                    "Enable V-Sync for stable FPS",
                    "Lower graphics settings",
                    "Update graphics drivers",
                    "Check for thermal throttling"
                });
        }

        return null;
    }

    private static double CalculateFpsImpact(double fps)
    {
        if (fps >= 60) return 0;
        if (fps >= 30) return 30;
        if (fps >= 15) return 60;
        return 90;
    }

    public void Dispose()
    {
        // Stop profiling timer first (synchronous operation)
        if (_isProfiling)
        {
            _profilingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _isProfiling = false;
        }

        _profilingTimer?.Dispose();
    }
}

