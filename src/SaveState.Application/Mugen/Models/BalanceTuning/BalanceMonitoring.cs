namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance monitoring data.
/// </summary>
public class BalanceMonitoring
{
    public string SessionId { get; set; } = default!;
    public BalanceMetrics CurrentMetrics { get; set; } = default!;
    public BalanceTrendAnalysis TrendAnalysis { get; set; } = default!;
    public IReadOnlyList<BalanceAlert> Alerts { get; set; } = default!;
    public float HealthScore { get; set; } = default!;
    public DateTime MonitoringTimestamp { get; set; } = default!;
}
