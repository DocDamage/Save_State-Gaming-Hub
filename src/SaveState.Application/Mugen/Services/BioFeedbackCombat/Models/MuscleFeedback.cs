namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Feedback derived from muscle tension data.
/// </summary>
public class MuscleFeedback
{
    public float CurrentMuscleTension { get; set; } = default!;
    public float TensionLevel { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public float DamageBonus { get; set; } = default!;
    public float SpeedBonus { get; set; } = default!;
    public float DefenseBonus { get; set; } = default!;
    public bool BlockingPower { get; set; } = default!;
    public bool FatigueIndicator { get; set; } = default!;
}
