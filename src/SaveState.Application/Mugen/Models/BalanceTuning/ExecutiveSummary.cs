namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Executive summary for balance reports.
/// </summary>
public class ExecutiveSummary
{
    public float OverallBalanceScore { get; set; } = default!;
    public IReadOnlyList<string> KeyFindings { get; set; } = default!;
    public string RiskLevel { get; set; } = default!;
    public DateTime NextReviewDate { get; set; } = default!;
}
