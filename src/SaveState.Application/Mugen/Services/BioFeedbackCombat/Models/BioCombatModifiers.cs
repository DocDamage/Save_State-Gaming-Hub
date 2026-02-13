namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Combat modifiers derived from bio feedback.
/// </summary>
public class BioCombatModifiers
{
    public float HeartRateDamageBonus { get; set; } = default!;
    public float BreathingComboBonus { get; set; } = default!;
    public float MuscleTensionDefenseBonus { get; set; } = default!;
    public bool AdrenalineBurstEnabled { get; set; } = default!;
}
