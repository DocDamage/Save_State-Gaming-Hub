namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Report recommendation data.
/// </summary>
public class ReportRecommendation
{
    public string Type { get; set; } = default!;
    public string Description { get; set; } = default!;
    public RecommendationPriority Priority { get; set; } = default!;
}
