namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Mechanic usage statistics.
/// </summary>
public class MechanicUsage
{
    public int TotalUses { get; set; } = default!;
    public int Wins { get; set; } = default!;
    public int TotalMatches { get; set; } = default!;
}
