namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance report data.
/// </summary>
public class BalanceReport
{
    public string SessionId { get; set; } = default!;
    public DateRange DateRange { get; set; } = default!;
    public ExecutiveSummary ExecutiveSummary { get; set; } = default!;
    public MechanicBalanceAnalysis MechanicAnalysis { get; set; } = default!;
    public PlayerFeedbackSummary PlayerFeedback { get; set; } = default!;
    public TournamentResultsAnalysis TournamentResults { get; set; } = default!;
    public IReadOnlyList<ReportRecommendation> Recommendations { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}
