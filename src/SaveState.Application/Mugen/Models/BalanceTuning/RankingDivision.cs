namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Ranking division data.
/// </summary>
public class RankingDivision
{
    public string Name { get; set; } = default!;
    public int MinRating { get; set; } = default!;
    public int MaxRating { get; set; } = default!;
    public int PlayerCount { get; set; } = default!;
}
