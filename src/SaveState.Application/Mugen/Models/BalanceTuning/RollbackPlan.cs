namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Rollback plan for reverting balance changes.
/// </summary>
public class RollbackPlan
{
    public IReadOnlyList<string> Steps { get; set; } = default!;
    public TimeSpan EstimatedTime { get; set; } = default!;
    public bool RequiresRestart { get; set; } = default!;
}
