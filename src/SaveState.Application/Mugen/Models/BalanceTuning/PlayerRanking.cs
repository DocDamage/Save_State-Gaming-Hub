namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Player ranking data.
/// </summary>
public class PlayerRanking
{
    public string PlayerName { get; set; } = default!;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public float WinRate { get; set; }
    public string PlayerId { get; set; } = default!;
    public int Rank { get; set; } = default!;
    public float Rating { get; set; } = default!;
    public float Change { get; set; } = default!;
}
