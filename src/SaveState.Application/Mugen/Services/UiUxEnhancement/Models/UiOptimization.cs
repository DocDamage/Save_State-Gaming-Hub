namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// UI optimization data.
/// </summary>
public class UiOptimization
{
    public string SessionId { get; set; } = default!;
    public UiPerformanceAnalysis Analysis { get; set; } = default!;
    public int OptimizationsApplied { get; set; } = default!;
    public float PerformanceImprovement { get; set; } = default!;
    public DateTime OptimizedAt { get; set; } = default!;
}

/// <summary>
/// UI performance analysis data.
/// </summary>
public class UiPerformanceAnalysis
{
    public float RenderTime { get; set; } = default!;
    public float MemoryUsage { get; set; } = default!;
    public int DrawCalls { get; set; } = default!;
    public IReadOnlyList<string> Bottlenecks { get; set; } = default!;
}

/// <summary>
/// UI optimization strategy data.
/// </summary>
public class UiOptimizationStrategy
{
    public string Type { get; set; } = default!;
    public float ExpectedImprovement { get; set; } = default!;
}

/// <summary>
/// Applied UI optimization data.
/// </summary>
public class AppliedUiOptimization
{
    public string StrategyId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public float ImprovementAchieved { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
}
