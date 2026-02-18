namespace SaveState.Application.Mugen.Services.PerformanceOptimization.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen;

/// <summary>
/// Engine for detecting performance bottlenecks.
/// </summary>
public class BottleneckDetectionEngine
{
    private readonly ILogger<BottleneckDetectionEngine> _logger;
    private readonly PerformanceThresholds _thresholds;

    public BottleneckDetectionEngine(ILogger<BottleneckDetectionEngine> logger, PerformanceThresholds thresholds)
    {
        _logger = logger;
        _thresholds = thresholds;
    }

    /// <summary>
    /// Identifies performance bottlenecks from metrics.
    /// </summary>
    public List<PerformanceBottleneck> IdentifyBottlenecks(OptimizationPerformanceMetrics metrics)
    {
        var bottlenecks = new List<PerformanceBottleneck>();

        if (metrics.AverageResponseTime > _thresholds.MaxResponseTime)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "HighResponseTime",
                0.8f, // High severity
                $"Response time exceeds threshold. Current: {metrics.AverageResponseTime:F0}ms, Target: {_thresholds.MaxResponseTime:F0}ms"
            ));
        }

        if (metrics.PeakMemoryUsage > _thresholds.MaxMemoryUsage)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "HighMemoryUsage",
                0.8f, // High severity
                $"Memory usage exceeds threshold. Current: {metrics.PeakMemoryUsage:F0}MB, Target: {_thresholds.MaxMemoryUsage:F0}MB"
            ));
        }

        if (metrics.CpuUtilization > _thresholds.MaxCpuUtilization)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "HighCpuUtilization",
                0.5f, // Medium severity
                $"CPU utilization exceeds threshold. Current: {metrics.CpuUtilization:F0}%, Target: {_thresholds.MaxCpuUtilization:F0}%"
            ));
        }

        return bottlenecks;
    }

    /// <summary>
    /// Checks if metrics indicate an emergency situation.
    /// </summary>
    public bool IsEmergency(OptimizationPerformanceMetrics metrics)
    {
        return metrics.AverageResponseTime > _thresholds.MaxResponseTime * 2
            || metrics.PeakMemoryUsage > _thresholds.MaxMemoryUsage * 1.5f
            || metrics.CpuUtilization > 95;
    }
}
