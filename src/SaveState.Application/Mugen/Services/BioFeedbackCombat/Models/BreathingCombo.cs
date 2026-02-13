namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Breathing enhanced combo data.
/// </summary>
public class BreathingCombo
{
    public string ComboId { get; set; } = default!;
    public string[] BaseCombo { get; set; } = default!;
    public int HitCount { get; set; } = default!;
    public float TotalDamage { get; set; } = default!;
    public float BreathingSynchronization { get; set; } = default!;
    public string SpecialEffects { get; set; } = default!;
    public DateTime ExecutedAt { get; set; } = default!;
}
