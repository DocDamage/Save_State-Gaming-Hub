using System.Linq;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Quantum move data.
/// </summary>
public class QuantumSuperpositionServiceQuantumMove
{
    public string MoveId { get; set; } = default!;
    public string StateId { get; set; } = default!;
    public string CharacterId { get; set; } = default!;
    public string MoveName { get; set; } = default!;
    public QuantumSuperpositionServiceMoveProperties BaseProperties { get; set; } = default!;
    public QuantumSuperpositionServiceQuantumState QuantumSuperpositionServiceQuantumState { get; set; } = default!;
    public bool IsCollapsed { get; set; } = default!;
    public TimeSpan ObservationWindow { get; set; } = default!;
}

/// <summary>
/// Quantum state data.
/// </summary>
public class QuantumSuperpositionServiceQuantumState
{
    public string StateId { get; set; } = default!;
    public string CharacterId { get; set; } = default!;
    public string MoveName { get; set; } = default!;
    public IReadOnlyList<QuantumSuperpositionServiceSuperpositionState> SuperpositionStates { get; set; } = default!;
    public string? EntanglementId { get; set; } = default!;
    public float UncertaintyLevel { get; set; } = default!;
    public TimeSpan CoherenceTime { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime? LastObserved { get; set; } = default!;
    public bool IsCollapsed { get; set; } = default!;
    public string? CollapsedState { get; set; } = default!;
    public QuantumSuperpositionServiceMeasurementType? LastMeasurement { get; set; } = default!;
}

/// <summary>
/// Superposition state data.
/// </summary>
public record QuantumSuperpositionServiceSuperpositionState
{
    public string StateId { get; set; } = default!;
    public QuantumSuperpositionServiceMoveProperties Properties { get; set; } = default!;
    public float Probability { get; set; } = default!;
    public string Name { get; set; } = default!;
}

/// <summary>
/// Move properties data.
/// </summary>
public record QuantumSuperpositionServiceMoveProperties
{
    public int Damage { get; set; } = default!;
    public int Hitstun { get; set; } = default!;
    public int Blockstun { get; set; } = default!;
    public int Speed { get; set; } = default!;
}

/// <summary>
/// Quantum move request.
/// </summary>
public class QuantumSuperpositionServiceQuantumMoveRequest
{
    public string CharacterId { get; set; } = default!;
    public string MoveName { get; set; } = default!;
    public QuantumSuperpositionServiceMoveProperties BaseProperties { get; set; } = default!;
    public string? EntanglementPartner { get; set; } = default!;
}

/// <summary>
/// Wave function collapse data.
/// </summary>
public class QuantumSuperpositionServiceWaveFunctionCollapse
{
    public string StateId { get; set; } = default!;
    public QuantumSuperpositionServiceCollapseTrigger Trigger { get; set; } = default!;
    public string ResultingState { get; set; } = default!;
    public QuantumSuperpositionServiceMoveProperties ResultingProperties { get; set; } = default!;
    public DateTime CollapseTime { get; set; } = default!;
    public float MeasurementAccuracy { get; set; } = default!;
    public TimeSpan DecoherenceTime { get; set; } = default!;
}

/// <summary>
/// Collapse trigger data.
/// </summary>
public class QuantumSuperpositionServiceCollapseTrigger
{
    public QuantumSuperpositionServiceQuantumTriggerType TriggerType { get; set; } = default!;
    public object TriggerData { get; set; } = default!;
    public DateTime TriggerTime { get; set; } = default!;
}

/// <summary>
/// Quantum entanglement data.
/// </summary>
public class QuantumSuperpositionServiceQuantumEntanglement
{
    public string EntanglementId { get; set; } = default!;
    public string Character1Id { get; set; } = default!;
    public string Character2Id { get; set; } = default!;
    public QuantumSuperpositionServiceEntanglementType QuantumSuperpositionServiceEntanglementType { get; set; } = default!;
    public float Strength { get; set; } = default!;
    public float DecayRate { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastInteraction { get; set; } = default!;
    public int InteractionCount { get; set; } = default!;
}

/// <summary>
/// Entanglement request.
/// </summary>
public class QuantumSuperpositionServiceEntanglementRequest
{
    public string Character1Id { get; set; } = default!;
    public string Character2Id { get; set; } = default!;
    public string State1Id { get; set; } = default!;
    public string State2Id { get; set; } = default!;
    public QuantumSuperpositionServiceEntanglementType QuantumSuperpositionServiceEntanglementType { get; set; } = default!;
    public float Strength { get; set; } = default!;
    public float DecayRate { get; set; } = default!;
}

/// <summary>
/// Uncertainty measurement data.
/// </summary>
public class QuantumSuperpositionServiceUncertaintyMeasurement
{
    public string StateId { get; set; } = default!;
    public QuantumSuperpositionServiceMeasurementType QuantumSuperpositionServiceMeasurementType { get; set; } = default!;
    public float MeasuredValue { get; set; } = default!;
    public float Accuracy { get; set; } = default!;
    public float Uncertainty { get; set; } = default!;
    public DateTime MeasuredAt { get; set; } = default!;
    public string MeasurementDevice { get; set; } = default!;
}

/// <summary>
/// Quantum probability data.
/// </summary>
public class QuantumSuperpositionServiceQuantumProbability
{
    public string StateId { get; set; } = default!;
    public IReadOnlyDictionary<string, float> StateProbabilities { get; set; } = default!;
    public float TotalProbability { get; set; } = default!;
    public bool IsNormalized { get; set; } = default!;
    public double Entropy { get; set; } = default!;
    public DateTime CalculatedAt { get; set; } = default!;
}

/// <summary>
/// Quantum interference data.
/// </summary>
public class QuantumSuperpositionServiceQuantumInterference
{
    public string StateId { get; set; } = default!;
    public QuantumSuperpositionServiceInterferencePattern QuantumSuperpositionServiceInterferencePattern { get; set; } = default!;
    public IReadOnlyList<QuantumSuperpositionServiceSuperpositionState> ModifiedStates { get; set; } = default!;
    public float InterferenceStrength { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
}

/// <summary>
/// Interference pattern data.
/// </summary>
public class QuantumSuperpositionServiceInterferencePattern
{
    public QuantumSuperpositionServiceInterferenceType QuantumSuperpositionServiceInterferenceType { get; set; } = default!;
    public float Amplitude { get; set; } = default!;
    public float Frequency { get; set; } = default!;
    public float Phase { get; set; } = default!;
}

/// <summary>
/// Superposition training data.
/// </summary>
public class QuantumSuperpositionServiceSuperpositionTraining
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public QuantumSuperpositionServiceTrainingDifficulty Difficulty { get; set; } = default!;
    public IReadOnlyList<QuantumSuperpositionServiceQuantumMove> TrainingMoves { get; set; } = default!;
    public bool ShowAllOutcomes { get; set; } = default!;
    public TimeSpan TimeLimit { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public QuantumSuperpositionServiceTrainingProgress Progress { get; set; } = default!;
}

/// <summary>
/// Training request.
/// </summary>
public class QuantumSuperpositionServiceTrainingRequest
{
    public string UserId { get; set; } = default!;
    public QuantumSuperpositionServiceTrainingDifficulty Difficulty { get; set; } = default!;
    public bool ShowProbabilities { get; set; } = default!;
    public TimeSpan TimeLimit { get; set; } = default!;
}

/// <summary>
/// Training progress data.
/// </summary>
public class QuantumSuperpositionServiceTrainingProgress
{
    public int MovesPracticed { get; set; } = default!;
    public int SuccessfulCollapses { get; set; } = default!;
    public float AverageAccuracy { get; set; } = default!;
    public TimeSpan TimeRemaining { get; set; } = default!;
}

/// <summary>
/// Quantum analytics data.
/// </summary>
public class QuantumSuperpositionServiceQuantumAnalytics
{
    public TimeSpan Period { get; set; } = default!;
    public int TotalStatesCreated { get; set; } = default!;
    public int TotalEntanglements { get; set; } = default!;
    public int TotalWaveFunctionCollapses { get; set; } = default!;
    public double AverageUncertaintyLevel { get; set; } = default!;
    public IReadOnlyDictionary<string, float> ProbabilityDistributions { get; set; } = default!;
    public IReadOnlyList<QuantumSuperpositionServiceEntanglementEffect> EntanglementEffects { get; set; } = default!;
    public QuantumSuperpositionServiceTrainingEffectiveness QuantumSuperpositionServiceTrainingEffectiveness { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Entanglement effect data.
/// </summary>
public class QuantumSuperpositionServiceEntanglementEffect
{
    public string EntanglementId { get; set; } = default!;
    public int TotalInteractions { get; set; } = default!;
    public float Strength { get; set; } = default!;
    public float DecayRate { get; set; } = default!;
}

/// <summary>
/// Training effectiveness data.
/// </summary>
public class QuantumSuperpositionServiceTrainingEffectiveness
{
    public float AverageAccuracy { get; set; } = default!;
    public float LearningRate { get; set; } = default!;
    public int TrainingSessions { get; set; } = default!;
    public float SkillImprovement { get; set; } = default!;
}

/// <summary>
/// Various enumeration types.
/// </summary>
public enum QuantumSuperpositionServiceQuantumTriggerType { PlayerTiming, OpponentAction, Environmental, Random }
public enum QuantumSuperpositionServiceEntanglementType { MoveLink, CharacterLink, Universal }
public enum QuantumSuperpositionServiceMeasurementType { Damage, Hitstun, Speed, Range }
public enum QuantumSuperpositionServiceInterferenceType { Constructive, Destructive, Mixed }
public enum QuantumSuperpositionServiceTrainingDifficulty { Beginner, Intermediate, Advanced }
