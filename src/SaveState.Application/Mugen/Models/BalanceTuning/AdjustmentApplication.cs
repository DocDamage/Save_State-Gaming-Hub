namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Result of applying a balance adjustment.
/// </summary>
public class AdjustmentApplication
{
    public BalanceAdjustment Adjustment { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
    public bool Success { get; set; } = default!;
    public float PerformanceImpact { get; set; } = default!;
    public bool RollbackAvailable { get; set; } = default!;
}
