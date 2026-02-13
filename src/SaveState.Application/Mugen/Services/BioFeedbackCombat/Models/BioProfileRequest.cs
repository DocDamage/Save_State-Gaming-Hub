namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Request to create a bio profile.
/// </summary>
public class BioProfileRequest
{
    public string PlayerId { get; set; } = default!;
    public float HeartRateSensitivity { get; set; } = default!;
    public float BreathingSensitivity { get; set; } = default!;
    public float MuscleSensitivity { get; set; } = default!;
    public float AdrenalineThreshold { get; set; } = default!;
    public bool MeditationEnabled { get; set; } = default!;
}
