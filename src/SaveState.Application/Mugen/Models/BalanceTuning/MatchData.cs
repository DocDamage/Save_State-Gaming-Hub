namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Match data for balance analysis.
/// </summary>
public class MatchData
{
    public string MatchId { get; set; } = default!;
    public string Winner { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyList<string> MechanicsUsed { get; set; } = default!;
    public float Player1Rating { get; set; } = default!;
    public float Player2Rating { get; set; } = default!;
    public DateTime MatchDate { get; set; } = default!;

    public bool WinnerUsedMechanic(string mechanic)
    {
        return MechanicsUsed.Contains(mechanic) && Winner == "Player1"; // Simplified
    }
}
