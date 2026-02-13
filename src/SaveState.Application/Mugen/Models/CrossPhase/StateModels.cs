namespace SaveState.Application.Mugen.Models.CrossPhase;

/// <summary>
/// Quantum state data.
/// </summary>
public class QuantumState
{
    public string StateId { get; set; } = default!;
    public bool IsCollapsed { get; set; } = default!;
    public float UncertaintyLevel { get; set; } = default!;
}

/// <summary>
/// Emotional state data for cross-phase integration.
/// </summary>
public class CrossPhaseEmotionalState
{
    public string CharacterId { get; set; } = default!;
    public Emotion PrimaryEmotion { get; set; } = default!;
    public float Intensity { get; set; } = default!;
}

/// <summary>
/// Reality state data.
/// </summary>
public class RealityState
{
    public string AreaId { get; set; } = default!;
    public float DistortionLevel { get; set; } = default!;
    public float StabilityIndex { get; set; } = default!;
}

/// <summary>
/// Bio state data.
/// </summary>
public class BioState
{
    public string ProfileId { get; set; } = default!;
    public float CurrentHeartRate { get; set; } = default!;
    public float StressLevel { get; set; } = default!;
}

/// <summary>
/// Combat state data.
/// </summary>
public class CombatState
{
    public string SessionId { get; set; } = default!;
    public float CurrentZPosition { get; set; } = default!;
    public float GravityScale { get; set; } = default!;
}
