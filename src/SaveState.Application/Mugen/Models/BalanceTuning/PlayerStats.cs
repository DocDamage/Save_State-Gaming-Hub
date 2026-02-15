namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Player statistics for ranking.
/// </summary>
public class PlayerStats
{
    public string PlayerName { get; set; } = default!;
    public int TotalMatches { get; set; }
    public DateTime LastActive { get; set; }
    public float WinRate { get; set; }
    public float ActivityScore { get; set; }
    public string PlayerId { get; set; } = default!;
    public float Rating { get; set; } = default!;
    public float RatingChange { get; set; } = default!;
    public int MatchesPlayed { get; set; } = default!;
    public int Wins { get; set; } = default!;
    public int Losses { get; set; } = default!;
}
