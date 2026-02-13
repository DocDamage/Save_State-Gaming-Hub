namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Combined bio feedback from all sensors.
/// </summary>
public class BioFeedback
{
    public HeartRateFeedback HeartRateComponent { get; set; } = default!;
    public BreathingFeedback BreathingComponent { get; set; } = default!;
    public MuscleFeedback MuscleComponent { get; set; } = default!;
    public float OverallIntensity { get; set; } = default!;
    public float DamageBonus { get; set; } = default!;
    public float SpeedBonus { get; set; } = default!;
    public float DefenseBonus { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}
