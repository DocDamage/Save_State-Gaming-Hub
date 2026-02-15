namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Mechanic balance analysis data.
/// </summary>
public class MechanicBalanceAnalysis
{
    public IReadOnlyDictionary<string, MechanicPerformanceData> MechanicPerformance { get; set; } = default!;
    public IReadOnlyDictionary<string, TrendData> BalanceTrends { get; set; } = default!;
    public IReadOnlyList<string> ProblemAreas { get; set; } = default!;
    public DateRange AnalysisPeriod { get; set; } = default!;
    public List<MechanicPerformanceData> PerformanceData { get; set; } = default!;
    public List<TrendData> Trends { get; set; } = default!;
    public List<ReportRecommendation> Recommendations { get; set; } = default!;
}
