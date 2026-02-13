namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Feedback trigger data.
/// </summary>
public class FeedbackTrigger
{
    public string TriggerType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public int Priority { get; set; } = default!;
}
