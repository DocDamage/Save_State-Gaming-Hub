namespace SaveState.Application.Mugen.Models.PerformanceOptimization;

/// <summary>
/// Resource usage metrics.
/// </summary>
public record ResourceUsage(
    ResourceType Type,
    float CurrentUsage,
    float PeakUsage,
    float AverageUsage,
    DateTime Timestamp
);

/// <summary>
/// Comprehensive resource metrics.
/// </summary>
public record ResourceMetrics(
    string SessionId,
    DateTime CollectedAt,
    float CpuPercent,
    float MemoryMB,
    float NetworkLatencyMs,
    int ThreadCount,
    int HandleCount
);

/// <summary>
/// Cache analysis data.
/// </summary>
public record CacheAnalysis(
    int CacheHits,
    int CacheMisses,
    float HitRate,
    int TotalEntries,
    float MemoryUsageMB
);

/// <summary>
/// Strategy for cache optimization.
/// </summary>
public record CacheOptimizationStrategy(
    string StrategyId,
    string Type,
    float ExpectedMemoryIncrease,
    float ExpectedHitRateImprovement
);

/// <summary>
/// Result of applying a cache optimization.
/// </summary>
public record AppliedCacheOptimization(
    string StrategyId,
    float HitRateImprovement,
    float MemoryIncrease,
    DateTime AppliedAt
);

/// <summary>
/// Batching analysis data.
/// </summary>
public record BatchingAnalysis(
    int OpportunitiesFound,
    int CurrentBatchSize,
    int OptimalBatchSize,
    int NetworkCallsPerMinute,
    int EstimatedSavings
);

/// <summary>
/// Strategy for batching optimization.
/// </summary>
public record BatchingStrategy(
    string StrategyId,
    int BatchSize,
    float ExpectedNetworkReduction,
    float ExpectedLatencyReduction
);

/// <summary>
/// Result of batching optimization.
/// </summary>
public record BatchingResult(
    string StrategyId,
    int BatchSize,
    int NetworkCallsSaved,
    float LatencyReduction,
    DateTime AppliedAt
);

/// <summary>
/// Memory analysis data.
/// </summary>
public record MemoryAnalysis(
    float CurrentUsage,
    float MaxAllowed,
    int GcCyclesPerMinute,
    int ObjectCount,
    int LargeObjectCount
);

/// <summary>
/// Strategy for memory optimization.
/// </summary>
public record MemoryStrategy(
    string StrategyId,
    string Type,
    float ExpectedMemorySavings,
    int ExpectedGcReduction
);

/// <summary>
/// Result of memory optimization.
/// </summary>
public record MemoryOptimizationResult(
    string StrategyId,
    float MemorySaved,
    int GcCyclesReduced,
    DateTime AppliedAt
);

/// <summary>
/// Load analysis data.
/// </summary>
public record LoadAnalysis(
    int ThreadCount,
    IReadOnlyList<float> ThreadUtilization,
    float LoadVariance,
    IReadOnlyList<int> BottleneckThreads
);

/// <summary>
/// Strategy for load balancing.
/// </summary>
public record LoadBalancingStrategy(
    string StrategyId,
    string Type,
    IReadOnlyList<int> TargetThreads,
    float ExpectedVarianceReduction
);

/// <summary>
/// Item result from load balancing.
/// </summary>
public record LoadBalancingResultItem(
    string StrategyId,
    int ThreadsAdjusted,
    bool CpuBalanced,
    float VarianceReduction,
    DateTime AppliedAt
);
