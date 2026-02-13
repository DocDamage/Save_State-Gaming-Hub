namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Visual feedback system data.
/// </summary>
public class VisualFeedbackSystem
{
    public string SessionId { get; set; } = default!;
    public FeedbackConfiguration Configuration { get; set; } = default!;
    public IReadOnlyList<FeedbackRule> FeedbackRules { get; set; } = default!;
    public AnimationLibrary AnimationLibrary { get; set; } = default!;
    public SoundLibrary SoundLibrary { get; set; } = default!;
    public ParticleEffectLibrary ParticleEffects { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}
