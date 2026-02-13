namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance trend analysis data.
/// </summary>
public class BalanceTrendAnalysis
{
    public IReadOnlyDictionary<string, float> WinRateTrends { get; set; } = default!;
    public IReadOnlyDictionary<string, float> UsageTrends { get; set; } = default!;
    public TimeSpan TrendPeriod { get; set; } = default!;
    public DateTime AnalysisTimestamp { get; set; } = default!;
}
