namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Feedback derived from breathing data.
/// </summary>
public class BreathingFeedback
{
    public float CurrentBreathingRate { get; set; } = default!;
    public float RhythmStability { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public float DamageBonus { get; set; } = default!;
    public float SpeedBonus { get; set; } = default!;
    public float DefenseBonus { get; set; } = default!;
    public bool ComboEnhancement { get; set; } = default!;
}
