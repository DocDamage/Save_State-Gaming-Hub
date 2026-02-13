using System.Text.Json.Serialization;

namespace SaveState.Application.Mugen.Models.CrossPhase;

/// <summary>
/// Integration result data.
/// </summary>
public class IntegrationResult
{
    public string SessionId { get; set; } = default!;
    public MechanicType PrimaryMechanic { get; set; } = default!;
    public MechanicInteraction Interaction { get; set; } = default!;
    public int EffectsApplied { get; set; } = default!;
    public int CrossPhaseSynergies { get; set; } = default!;
    public float PerformanceImpact { get; set; } = default!;
    public DateTime IntegrationTimestamp { get; set; } = default!;
}

/// <summary>
/// Unified game state data combining all mechanic states.
/// </summary>
public class UnifiedGameState
{
    public string SessionId { get; set; } = default!;
    public QuantumState? QuantumState { get; set; } = default!;
    public CrossPhaseEmotionalState? EmotionalState { get; set; } = default!;
    public RealityState? RealityState { get; set; } = default!;
    public BioState? BioState { get; set; } = default!;
    public CombatState? CombatState { get; set; } = default!;
    public MechanicIntegrationState IntegrationState { get; set; } = default!;
    public int ActiveSynergies { get; set; } = default!;
    public UnifiedPerformanceMetrics PerformanceMetrics { get; set; } = default!;
    public DateTime RetrievedAt { get; set; } = default!;
}

/// <summary>
/// Mechanic integration state data.
/// </summary>
public class MechanicIntegrationState
{
    public string SessionId { get; set; } = default!;
    public HashSet<MechanicType> ActiveMechanics { get; set; } = default!;
    public int TotalInteractions { get; set; } = default!;
    public int ActiveEffectCount { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
    public MechanicInteraction? LastInteraction { get; set; } = default!;
}

/// <summary>
/// Mechanic dependency graph data.
/// </summary>
public class MechanicDependencyGraph
{
    public IReadOnlyDictionary<MechanicType, MechanicType[]> Dependencies { get; set; } = default!;
    public IReadOnlyDictionary<MechanicType, MechanicType[]> Conflicts { get; set; } = default!;
}

/// <summary>
/// Unified performance metrics data.
/// </summary>
public class UnifiedPerformanceMetrics
{
    public float AverageResponseTime { get; set; } = default!;
    public float PeakMemoryUsage { get; set; } = default!;
    public float IntegrationEfficiency { get; set; } = default!;
    public float CrossPhaseOverhead { get; set; } = default!;
}

/// <summary>
/// Cross-phase effects tracking data.
/// </summary>
public class CrossPhaseEffects
{
    public string EffectId { get; set; } = default!;
    public MechanicType SourceMechanic { get; set; } = default!;
    public MechanicType TargetMechanic { get; set; } = default!;
    public bool IsActive { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}
