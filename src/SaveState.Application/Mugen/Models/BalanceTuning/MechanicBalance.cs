namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance data for a specific mechanic.
/// </summary>
public class MechanicBalance
{
    public MechanicType Mechanic { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public MechanicUsageStats UsageStats { get; set; } = default!;
    public IReadOnlyList<BalanceAdjustment> BalanceHistory { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastAdjusted { get; set; } = default!;
    public int AdjustmentCount { get; set; } = default!;
}
