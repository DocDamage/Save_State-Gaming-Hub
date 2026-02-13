namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Competitive ranking data.
/// </summary>
public class CompetitiveRanking
{
    public IReadOnlyList<PlayerRanking> Players { get; set; } = default!;
    public IReadOnlyList<RankingDivision> Divisions { get; set; } = default!;
    public SeasonStatistics SeasonStats { get; set; } = default!;
    public IReadOnlyDictionary<string, float> BalanceFactors { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = default!;
}
