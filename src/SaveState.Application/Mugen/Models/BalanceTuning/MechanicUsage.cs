namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Mechanic usage statistics.
/// </summary>
public class MechanicUsage
{
    public string MechanicName { get; set; } = default!;
    public int UsageCount { get; set; }
    public float UsageRate { get; set; }
    public int TotalUses { get; set; } = default!;
    public int Wins { get; set; } = default!;
    public int TotalMatches { get; set; } = default!;
}
