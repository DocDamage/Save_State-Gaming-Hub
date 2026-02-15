namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance metrics data.
/// </summary>
public class BalanceMetrics
{
    public IReadOnlyDictionary<string, int> MechanicUsage { get; set; } = default!;
    public float PlayerSatisfaction { get; set; } = default!;
    public TimeSpan MatchDuration { get; set; } = default!;
    public DateTime CollectedAt { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public float OverallHealth { get; set; }
    public Dictionary<string, float> MetricValues { get; set; } = default!;
}
