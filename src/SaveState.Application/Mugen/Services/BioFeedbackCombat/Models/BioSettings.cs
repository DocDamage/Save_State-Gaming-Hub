namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Bio feedback settings.
/// </summary>
public class BioSettings
{
    public float HeartRateSensitivity { get; set; } = default!;
    public float BreathingSensitivity { get; set; } = default!;
    public float MuscleSensitivity { get; set; } = default!;
    public float AdrenalineThreshold { get; set; } = default!;
    public bool MeditationEnabled { get; set; } = default!;
}
