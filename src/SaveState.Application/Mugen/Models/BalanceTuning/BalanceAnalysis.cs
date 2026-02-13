namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance analysis result data.
/// </summary>
public class BalanceAnalysis
{
    public string SessionId { get; set; } = default!;
    public int MatchCount { get; set; } = default!;
    public IReadOnlyDictionary<string, MechanicUsage> MechanicUsage { get; set; } = default!;
    public IReadOnlyDictionary<string, WinRateData> WinRates { get; set; } = default!;
    public PlaytimeDistribution PlaytimeDistribution { get; set; } = default!;
    public SkillGapAnalysis SkillGapAnalysis { get; set; } = default!;
    public float BalanceScore { get; set; } = default!;
    public IReadOnlyList<BalanceRecommendation> Recommendations { get; set; } = default!;
    public DateTime AnalysisTimestamp { get; set; } = default!;
}
