namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Mechanic balance analysis data.
/// </summary>
public class MechanicBalanceAnalysis
{
    public IReadOnlyDictionary<string, float> MechanicPerformance { get; set; } = default!;
    public IReadOnlyDictionary<string, TrendData> BalanceTrends { get; set; } = default!;
    public IReadOnlyList<string> ProblemAreas { get; set; } = default!;
}
