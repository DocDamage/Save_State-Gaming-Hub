namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Mechanic usage statistics.
/// </summary>
public class MechanicUsageStats
{
    public int TotalUses { get; set; } = default!;
    public int SuccessfulUses { get; set; } = default!;
    public float SuccessRate { get; set; } = default!;
}
