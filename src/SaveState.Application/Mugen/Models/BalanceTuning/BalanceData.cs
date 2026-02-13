namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance data for a mechanic.
/// </summary>
public class BalanceData
{
    public float WinRate { get; set; } = default!;
    public int MatchCount { get; set; } = default!;
    public float UsageRate { get; set; } = default!;
    public float PlayerSatisfaction { get; set; } = default!;
}
