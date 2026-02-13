namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Feedback configuration data.
/// </summary>
public class FeedbackConfiguration
{
    public IEnumerable<string> EnabledMechanics { get; set; } = default!;
    public string Theme { get; set; } = default!;
    public bool AudioEnabled { get; set; } = default!;
    public bool ParticlesEnabled { get; set; } = default!;
    public bool HapticEnabled { get; set; } = default!;
}
