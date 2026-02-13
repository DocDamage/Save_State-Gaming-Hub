namespace SaveState.Application.Mugen.Models.PerformanceOptimization;

/// <summary>
/// Types of optimizations that can be applied.
/// </summary>
public enum OptimizationType
{
    Cache,
    Batching,
    Memory,
    LoadBalancing,
    ResponseTime,
    General
}

/// <summary>
/// Types of performance bottlenecks.
/// </summary>
public enum BottleneckType
{
    ResponseTime,
    CacheEfficiency,
    MemoryUsage,
    CpuUtilization,
    NetworkLatency,
    Unknown
}

/// <summary>
/// Resource types being monitored.
/// </summary>
public enum ResourceType
{
    Cpu,
    Memory,
    Network,
    Disk,
    Cache
}

/// <summary>
/// Status of an optimization operation.
/// </summary>
public enum OptimizationStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Severity levels for performance issues.
/// </summary>
public enum PerformanceSeverity
{
    Low,
    Medium,
    High,
    Critical
}
