namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Risk assessment for a balance patch.
/// </summary>
public class BalanceRiskAssessment
{
    public string Level { get; set; } = default!;
    public IReadOnlyList<string> RiskFactors { get; set; } = default!;
    public IReadOnlyList<string> MitigationStrategies { get; set; } = default!;
}
