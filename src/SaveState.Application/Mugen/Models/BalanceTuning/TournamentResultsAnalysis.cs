namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Tournament results analysis data.
/// </summary>
public class TournamentResultsAnalysis
{
    public int TournamentCount { get; set; } = default!;
    public float AveragePlacement { get; set; } = default!;
    public IReadOnlyList<string> DominantStrategies { get; set; } = default!;
    public IReadOnlyList<string> BalanceIssues { get; set; } = default!;
    public DateRange AnalysisPeriod { get; set; } = default!;
    public int TournamentsAnalyzed { get; set; }
    public List<string> TopWinningMechanics { get; set; } = default!;
    public float MetaDiversity { get; set; }
    public float UpsetRate { get; set; }
}
