namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Feedback rule data.
/// </summary>
public class FeedbackRule
{
    public string Id { get; set; } = default!;
    public string Mechanic { get; set; } = default!;
    public string Trigger { get; set; } = default!;
    public string FeedbackType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public float Duration { get; set; } = default!;
}
