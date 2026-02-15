namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Player feedback summary data.
/// </summary>
public class PlayerFeedbackSummary
{
    public IReadOnlyList<string> PositiveFeedback { get; set; } = default!;
    public IReadOnlyList<string> NegativeFeedback { get; set; } = default!;
    public IReadOnlyList<string> Suggestions { get; set; } = default!;
    public float OverallSatisfaction { get; set; } = default!;
    public DateRange CollectionPeriod { get; set; } = default!;
    public int TotalResponses { get; set; }
    public float AverageSatisfaction { get; set; }
    public List<string> CommonConcerns { get; set; } = default!;
}
