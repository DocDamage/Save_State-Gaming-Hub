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
    public string SessionId { get; set; } = default!;
    public TrendDirection TrendDirection { get; set; } = default!;
    public float TrendStrength { get; set; }
    public List<TrendData> HistoricalData { get; set; } = default!;
    public float ProjectedBalance { get; set; }
}
