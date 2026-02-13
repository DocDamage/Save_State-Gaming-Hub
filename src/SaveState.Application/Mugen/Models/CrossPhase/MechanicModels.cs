namespace SaveState.Application.Mugen.Models.CrossPhase;

/// <summary>
/// Mechanic synergy data.
/// </summary>
public class MechanicSynergy
{
    public MechanicType Mechanic1 { get; set; } = default!;
    public MechanicType Mechanic2 { get; set; } = default!;
    public string Context { get; set; } = default!;
    public float CompatibilityScore { get; set; } = default!;
    public IReadOnlyList<CrossPhaseSynergyEffect> SynergyEffects { get; set; } = default!;
    public float PowerMultiplier { get; set; } = default!;
    public float ComplexityBonus { get; set; } = default!;
    public DateTime CalculatedAt { get; set; } = default!;
}

/// <summary>
/// Synergy effect data for cross-phase integration.
/// </summary>
public class CrossPhaseSynergyEffect
{
    public string EffectType { get; set; } = default!;
    public float Magnitude { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Mechanic effect data.
/// </summary>
public class MechanicEffect
{
    public string EffectId { get; set; } = default!;
    public MechanicType SourceMechanic { get; set; } = default!;
    public MechanicType TargetMechanic { get; set; } = default!;
    public string EffectType { get; set; } = default!;
    public float Magnitude { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public bool IsCrossPhase { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Mechanic interaction data.
/// </summary>
public class MechanicInteraction
{
    public string InteractionId { get; set; } = default!;
    public string InteractionType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public object InteractionData { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}
