namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Trend data for metrics.
/// </summary>
public class TrendData
{
    public float CurrentValue { get; set; } = default!;
    public float TrendDirection { get; set; } = default!;
    public float Volatility { get; set; } = default!;
}
