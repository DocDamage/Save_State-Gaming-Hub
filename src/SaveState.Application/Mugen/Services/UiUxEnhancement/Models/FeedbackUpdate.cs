namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Feedback update data.
/// </summary>
public class FeedbackUpdate
{
    public string SessionId { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public FeedbackTrigger Trigger { get; set; } = default!;
    public IReadOnlyList<ActiveFeedback> ActiveFeedback { get; set; } = default!;
    public IReadOnlyList<VisualEffect> VisualEffects { get; set; } = default!;
    public IReadOnlyList<AudioCue> AudioCues { get; set; } = default!;
    public IReadOnlyList<HapticFeedback> HapticFeedback { get; set; } = default!;
}

/// <summary>
/// Active feedback data.
/// </summary>
public class ActiveFeedback
{
    public string RuleId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public float Duration { get; set; } = default!;
}
