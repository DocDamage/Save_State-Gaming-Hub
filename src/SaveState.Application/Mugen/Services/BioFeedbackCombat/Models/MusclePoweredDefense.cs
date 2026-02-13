namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Muscle powered defense data.
/// </summary>
public class MusclePoweredDefense
{
    public string DefenseId { get; set; } = default!;
    public string BlockType { get; set; } = default!;
    public float BlockStrength { get; set; } = default!;
    public float DamageReduction { get; set; } = default!;
    public float PushbackForce { get; set; } = default!;
    public bool CounterAttackReady { get; set; } = default!;
    public DateTime ExecutedAt { get; set; } = default!;
}
