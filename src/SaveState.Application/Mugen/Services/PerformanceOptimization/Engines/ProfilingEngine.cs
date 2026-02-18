namespace SaveState.Application.Mugen.Services.PerformanceOptimization.Engines;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for profiling performance metrics.
/// </summary>
public class ProfilingEngine
{
    private readonly ILogger<ProfilingEngine> _logger;
    private readonly ConcurrentDictionary<string, OptimizationPerformanceMetrics> _metrics;
    private readonly PerformanceThresholds _thresholds;
    private readonly ITimeProvider _timeProvider;

    public ProfilingEngine(
        ILogger<ProfilingEngine> logger,
        ConcurrentDictionary<string, OptimizationPerformanceMetrics> metrics,
        PerformanceThresholds thresholds,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _metrics = metrics;
        _thresholds = thresholds;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets or creates metrics for a session.
    /// </summary>
    public OptimizationPerformanceMetrics GetOrCreateMetrics(string sessionId)
    {
        return _metrics.GetOrAdd(sessionId, id => new OptimizationPerformanceMetrics(
            id, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0.0f, _timeProvider.UtcNow
        ));
    }

    /// <summary>
    /// Generates optimization suggestions based on metrics.
    /// </summary>
    public List<OptimizationSuggestion> GenerateSuggestions(OptimizationPerformanceMetrics metrics)
    {
        var suggestions = new List<OptimizationSuggestion>();

        if (metrics.AverageResponseTime > _thresholds.MaxResponseTime)
        {
            suggestions.Add(new OptimizationSuggestion(
                "ReduceResponseTime",
                "ResponseTime",
                "Response time exceeds threshold",
                15.0f,
                1 // High priority
            ));
        }

        if (metrics.PeakMemoryUsage > _thresholds.MaxMemoryUsage)
        {
            suggestions.Add(new OptimizationSuggestion(
                "ReduceMemoryUsage",
                "MemoryUsage",
                "Memory usage exceeds threshold",
                20.0f,
                1 // High priority
            ));
        }

        if (metrics.CacheHitRate < _thresholds.MinCacheHitRate)
        {
            suggestions.Add(new OptimizationSuggestion(
                "ImproveCacheHitRate",
                "CacheHitRate",
                "Cache hit rate below threshold",
                10.0f,
                2 // Medium priority
            ));
        }

        return suggestions;
    }

    /// <summary>
    /// Calculates health score from metrics.
    /// </summary>
    public float CalculateHealthScore(OptimizationPerformanceMetrics metrics)
    {
        var responseScore = Math.Max(0, 1 - metrics.AverageResponseTime / _thresholds.MaxResponseTime);
        var memoryScore = Math.Max(0, 1 - metrics.PeakMemoryUsage / _thresholds.MaxMemoryUsage);
        var cacheScore = metrics.CacheHitRate / _thresholds.MinCacheHitRate;
        var cpuScore = Math.Max(0, 1 - metrics.CpuUtilization / _thresholds.MaxCpuUtilization);

        return (responseScore + memoryScore + cacheScore + cpuScore) / 4;
    }

    /// <summary>
    /// Applies a performance event to metrics.
    /// </summary>
    public OptimizationPerformanceMetrics ApplyEvent(OptimizationPerformanceMetrics current, PerformanceEvent evt)
    {
        return current with
        {
            AverageResponseTime = Math.Max(current.AverageResponseTime, evt.Duration),
            PeakMemoryUsage = Math.Max(current.PeakMemoryUsage, evt.MemoryUsage),
            CreatedAt = evt.Timestamp
        };
    }

    /// <summary>
    /// Calculates overall score from multiple optimization results.
    /// </summary>
    public float CalculateOverallScore(
        OptimizationPerformanceAnalysis analysis,
        SaveState.Application.Mugen.CacheOptimization cache,
        SaveState.Application.Mugen.BatchingOptimization batching,
        SaveState.Application.Mugen.MemoryOptimization memory,
        SaveState.Application.Mugen.LoadBalancingResult loadBalancing)
    {
        var analysisScore = analysis.OverallHealthScore;
        var cacheScore = cache.HitRate / _thresholds.MinCacheHitRate;
        var memoryScore = 1 - (memory.MemoryReduction / 1000); // Normalize
        var loadScore = 1 - loadBalancing.LoadVarianceAfter;

        return (analysisScore + cacheScore + memoryScore + loadScore) / 4;
    }
}
