namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Win rate data for a mechanic or strategy.
/// </summary>
public class WinRateData
{
    public string MechanicName { get; set; } = default!;
    public int TotalUses { get; set; }
    public float WinRate { get; set; }
    public int Wins { get; set; } = default!;
    public int TotalMatches { get; set; } = default!;
    public float WinPercentage { get; set; } = default!;
}
