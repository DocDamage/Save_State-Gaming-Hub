namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Current physiological state during combat.
/// </summary>
public class PhysiologicalState
{
    public float CurrentHeartRate { get; set; } = default!;
    public float CurrentBreathingRate { get; set; } = default!;
    public float CurrentMuscleTension { get; set; } = default!;
    public float StressLevel { get; set; } = default!;
    public float FocusLevel { get; set; } = default!;
    public float FatigueLevel { get; set; } = default!;
}
