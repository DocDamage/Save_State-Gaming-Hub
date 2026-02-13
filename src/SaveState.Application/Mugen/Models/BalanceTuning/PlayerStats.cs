namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Player statistics for ranking.
/// </summary>
public class PlayerStats
{
    public string PlayerId { get; set; } = default!;
    public float Rating { get; set; } = default!;
    public float RatingChange { get; set; } = default!;
    public int MatchesPlayed { get; set; } = default!;
    public int Wins { get; set; } = default!;
    public int Losses { get; set; } = default!;
}
