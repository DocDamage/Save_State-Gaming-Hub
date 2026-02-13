namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance patch containing multiple adjustments.
/// </summary>
public class BalancePatch
{
    public string Version { get; set; } = default!;
    public IReadOnlyList<BalanceAdjustment> Adjustments { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public TestResults TestResults { get; set; } = default!;
    public BalanceRiskAssessment RiskAssessment { get; set; } = default!;
    public RollbackPlan RollbackPlan { get; set; } = default!;
}
