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
}
