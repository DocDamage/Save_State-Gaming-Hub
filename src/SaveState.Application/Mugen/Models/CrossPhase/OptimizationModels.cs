namespace SaveState.Application.Mugen.Models.CrossPhase;

/// <summary>
/// Integration optimization result data.
/// </summary>
public class IntegrationOptimization
{
    public string SessionId { get; set; } = default!;
    public int BottlenecksIdentified { get; set; } = default!;
    public int OptimizationsApplied { get; set; } = default!;
    public float PerformanceImprovement { get; set; } = default!;
    public DateTime OptimizationTimestamp { get; set; } = default!;
}

/// <summary>
/// Performance bottleneck data for cross-phase integration.
/// </summary>
public class CrossPhasePerformanceBottleneck
{
    public string BottleneckType { get; set; } = default!;
    public float Severity { get; set; } = default!;
    public string Description { get; set; } = default!;
}

/// <summary>
/// Optimization strategy data.
/// </summary>
public class OptimizationStrategy
{
    public string StrategyId { get; set; } = default!;
    public string TargetBottleneck { get; set; } = default!;
    public string OptimizationType { get; set; } = default!;
    public float ExpectedImprovement { get; set; } = default!;
}

/// <summary>
/// Applied optimization data for cross-phase integration.
/// </summary>
public class CrossPhaseAppliedOptimization
{
    public string OptimizationId { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
    public bool Success { get; set; } = default!;
}
