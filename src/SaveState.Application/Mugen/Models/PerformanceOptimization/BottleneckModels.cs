namespace SaveState.Application.Mugen.Models.PerformanceOptimization;

// Type aliases to existing types in SharedTypes.cs
using PerformanceBottleneck = SaveState.Application.Mugen.PerformanceBottleneck;

/// <summary>
/// Analysis of performance bottlenecks.
/// </summary>
public record BottleneckAnalysis(
    string SessionId,
    DateTime AnalysisTime,
    IReadOnlyList<PerformanceBottleneck> Bottlenecks,
    PerformanceBottleneck? PrimaryBottleneck,
    float OverallImpact
);

/// <summary>
/// Thresholds for detecting bottlenecks.
/// </summary>
public record PerformanceThresholds(
    float MaxResponseTime,
    float MaxMemoryUsage,
    float MinCacheHitRate,
    float MaxCpuUtilization,
    float MaxNetworkLatency
);

/// <summary>
/// Strategy configuration for optimization.
/// </summary>
public record OptimizationStrategies(
    IReadOnlyList<string> CacheStrategies,
    IReadOnlyList<string> BatchingStrategies,
    IReadOnlyList<string> MemoryStrategies,
    IReadOnlyList<string> LoadBalancingStrategies
);
