namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance recommendation data.
/// </summary>
public class BalanceRecommendation
{
    public string Mechanic { get; set; } = default!;
    public string RecommendationType { get; set; } = default!;
    public float Severity { get; set; } = default!;
    public string Description { get; set; } = default!;
    public RecommendationPriority Priority { get; set; } = default!;
}
