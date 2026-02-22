using SaveState.Core.Common;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Emotional Resonance Service interface.
/// </summary>
public interface IEmotionalResonanceService
{
    Task<Result<EmotionalResonanceServiceResonanceEmotionalState>> UpdateEmotionalStateAsync(string characterId, EmotionalResonanceServiceEmotionalTrigger trigger, CancellationToken ct = default);
    Task<Result<EmotionalResonanceServiceResonanceField>> CreateResonanceFieldAsync(EmotionalResonanceServiceResonanceFieldRequest request, CancellationToken ct = default);
    Task<Result<EmotionalResonanceServiceResonanceTransfer>> TransferResonanceAsync(string sourceCharacterId, string targetCharacterId, EmotionalResonanceServiceResonanceTransferRequest request, CancellationToken ct = default);
    Task<Result<EmotionalResonanceServiceSpectatorInfluence>> SendSpectatorSupportAsync(string spectatorId, string characterId, EmotionalResonanceServiceSpectatorSupport support, CancellationToken ct = default);
    Task<Result<EmotionalResonanceServiceBreakingPoint>> CheckBreakingPointAsync(string characterId, CancellationToken ct = default);
    Task<Result<EmotionalResonanceServiceCrowdPsychology>> UpdateCrowdPsychologyAsync(string matchId, EmotionalResonanceServiceCrowdEvent crowdEvent, CancellationToken ct = default);
    Task<Result<EmotionalResonanceServiceEmotionalSynergy>> CalculateEmotionalSynergyAsync(string character1Id, string character2Id, CancellationToken ct = default);
    Task<Result<EmotionalResonanceServiceEmotionalAnalytics>> GetEmotionalAnalyticsAsync(string characterId, TimeSpan period, CancellationToken ct = default);
    Task<Result<EmotionalResonanceServiceEmotionalBuff>> ApplyEmotionalBuffAsync(string characterId, EmotionalResonanceServiceEmotionalBuffRequest request, CancellationToken ct = default);
}

/// <summary>
/// Emotional state data.
/// </summary>
public class EmotionalResonanceServiceResonanceEmotionalState
{
    public string CharacterId { get; set; } = default!;
    public EmotionalResonanceServiceEmotion PrimaryEmotion { get; set; } = default!;
    public EmotionalResonanceServiceEmotion SecondaryEmotion { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public float Stability { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
    public IReadOnlyList<EmotionalResonanceServiceEmotionalEvent> EmotionalHistory { get; set; } = default!;
    public float ResonanceLevel { get; set; } = default!;
    public float CrowdInfluence { get; set; } = default!;
}

/// <summary>
/// Emotional trigger data.
/// </summary>
public class EmotionalResonanceServiceEmotionalTrigger
{
    public EmotionalResonanceServiceResonanceTriggerType TriggerType { get; set; } = default!;
    public object TriggerData { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Emotional event data.
/// </summary>
public class EmotionalResonanceServiceEmotionalEvent
{
    public string EventId { get; set; } = default!;
    public EmotionalResonanceServiceEmotionalTrigger Trigger { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// EmotionalResonanceServiceEmotion change data.
/// </summary>
public class EmotionalResonanceServiceEmotionChange
{
    public float IntensityChange { get; set; } = default!;
    public float StabilityChange { get; set; } = default!;
    public float ResonanceChange { get; set; } = default!;
    public float CrowdInfluenceChange { get; set; } = default!;
}

/// <summary>
/// Resonance field data.
/// </summary>
public class EmotionalResonanceServiceResonanceField
{
    public string FieldId { get; set; } = default!;
    public string Character1Id { get; set; } = default!;
    public string Character2Id { get; set; } = default!;
    public EmotionalResonanceServiceFieldType EmotionalResonanceServiceFieldType { get; set; } = default!;
    public float Strength { get; set; } = default!;
    public float Radius { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public IReadOnlyDictionary<string, float> Effects { get; set; } = default!;
    public bool Active { get; set; } = default!;
}

/// <summary>
/// Resonance field request.
/// </summary>
public class EmotionalResonanceServiceResonanceFieldRequest
{
    public string Character1Id { get; set; } = default!;
    public string Character2Id { get; set; } = default!;
    public EmotionalResonanceServiceFieldType EmotionalResonanceServiceFieldType { get; set; } = default!;
    public float Strength { get; set; } = default!;
    public float Radius { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Resonance transfer data.
/// </summary>
public class EmotionalResonanceServiceResonanceTransfer
{
    public string TransferId { get; set; } = default!;
    public string SourceCharacterId { get; set; } = default!;
    public string TargetCharacterId { get; set; } = default!;
    public float TransferAmount { get; set; } = default!;
    public EmotionalResonanceServiceTransferType EmotionalResonanceServiceTransferType { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public bool Success { get; set; } = default!;
    public IReadOnlyList<EmotionalResonanceServiceTransferEffect> Effects { get; set; } = default!;
}

/// <summary>
/// Transfer effect data.
/// </summary>
public class EmotionalResonanceServiceTransferEffect
{
    public string EffectType { get; set; } = default!;
    public float Magnitude { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Resonance transfer request.
/// </summary>
public class EmotionalResonanceServiceResonanceTransferRequest
{
    public float TransferAmount { get; set; } = default!;
    public EmotionalResonanceServiceTransferType EmotionalResonanceServiceTransferType { get; set; } = default!;
    public string Reason { get; set; } = default!;
}

/// <summary>
/// Spectator influence data.
/// </summary>
public class EmotionalResonanceServiceSpectatorInfluence
{
    public string InfluenceId { get; set; } = default!;
    public string SpectatorId { get; set; } = default!;
    public string CharacterId { get; set; } = default!;
    public EmotionalResonanceServiceSupportType EmotionalResonanceServiceSupportType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyDictionary<string, float> Effects { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Spectator support data.
/// </summary>
public class EmotionalResonanceServiceSpectatorSupport
{
    public EmotionalResonanceServiceSupportType EmotionalResonanceServiceSupportType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public string Message { get; set; } = default!;
}

/// <summary>
/// Breaking point data.
/// </summary>
public class EmotionalResonanceServiceBreakingPoint
{
    public string CharacterId { get; set; } = default!;
    public bool IsTriggered { get; set; } = default!;
    public EmotionalResonanceServiceBreakingPointType EmotionalResonanceServiceBreakingPointType { get; set; } = default!;
    public EmotionalResonanceServiceEmotion TriggerEmotion { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public IReadOnlyList<EmotionalResonanceServiceBreakingPointEffect> Effects { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public DateTime CheckedAt { get; set; } = default!;
}

/// <summary>
/// Breaking point effect data.
/// </summary>
public class EmotionalResonanceServiceBreakingPointEffect
{
    public string EffectType { get; set; } = default!;
    public float Magnitude { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Crowd psychology data.
/// </summary>
public class EmotionalResonanceServiceCrowdPsychology
{
    public string MatchId { get; set; } = default!;
    public EmotionalResonanceServiceCrowdMood CollectiveMood { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public IReadOnlyList<string> AffectedCharacters { get; set; } = default!;
    public IReadOnlyList<string> MoodTriggers { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = default!;
}

/// <summary>
/// Crowd event data.
/// </summary>
public class EmotionalResonanceServiceCrowdEvent
{
    public EmotionalResonanceServiceCrowdEventType EventType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Emotional synergy data.
/// </summary>
public class EmotionalResonanceServiceEmotionalSynergy
{
    public string Character1Id { get; set; } = default!;
    public string Character2Id { get; set; } = default!;
    public float Compatibility { get; set; } = default!;
    public IReadOnlyList<EmotionalResonanceServiceResonanceSynergyEffect> SynergyEffects { get; set; } = default!;
    public float ResonanceMultiplier { get; set; } = default!;
    public EmotionalResonanceServiceEmotionalBond EmotionalResonanceServiceEmotionalBond { get; set; } = default!;
    public DateTime CalculatedAt { get; set; } = default!;
}

/// <summary>
/// Synergy effect data.
/// </summary>
public class EmotionalResonanceServiceResonanceSynergyEffect
{
    public EmotionalResonanceServiceSynergyType EffectType { get; set; } = default!;
    public float Magnitude { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Emotional analytics data.
/// </summary>
public class EmotionalResonanceServiceEmotionalAnalytics
{
    public string CharacterId { get; set; } = default!;
    public TimeSpan Period { get; set; } = default!;
    public IReadOnlyDictionary<EmotionalResonanceServiceEmotion, float> EmotionalDistribution { get; set; } = default!;
    public IReadOnlyList<EmotionalResonanceServiceResonanceEvent> ResonanceEvents { get; set; } = default!;
    public EmotionalResonanceServiceSpectatorInfluenceStats EmotionalResonanceServiceSpectatorInfluence { get; set; } = default!;
    public IReadOnlyList<EmotionalResonanceServiceBreakingPointEvent> BreakingPointHistory { get; set; } = default!;
    public float EmotionalStability { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Resonance event data.
/// </summary>
public class EmotionalResonanceServiceResonanceEvent
{
    public string EventId { get; set; } = default!;
    public string SourceCharacterId { get; set; } = default!;
    public string TargetCharacterId { get; set; } = default!;
    public float ResonanceAmount { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public EmotionalResonanceServiceResonanceEventType EventType { get; set; } = default!;
}

/// <summary>
/// Spectator influence stats data.
/// </summary>
public class EmotionalResonanceServiceSpectatorInfluenceStats
{
    public int TotalSupportReceived { get; set; } = default!;
    public float AverageSupportIntensity { get; set; } = default!;
    public EmotionalResonanceServiceSupportType MostCommonSupportType { get; set; } = default!;
    public float SupportEffectiveness { get; set; } = default!;
}

/// <summary>
/// Breaking point event data.
/// </summary>
public class EmotionalResonanceServiceBreakingPointEvent
{
    public string EventId { get; set; } = default!;
    public string CharacterId { get; set; } = default!;
    public EmotionalResonanceServiceEmotion TriggerEmotion { get; set; } = default!;
    public EmotionalResonanceServiceBreakingPointType EmotionalResonanceServiceBreakingPointType { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public float Intensity { get; set; } = default!;
}

/// <summary>
/// Emotional buff data.
/// </summary>
public class EmotionalResonanceServiceEmotionalBuff
{
    public string BuffId { get; set; } = default!;
    public string CharacterId { get; set; } = default!;
    public EmotionalResonanceServiceBuffType EmotionalResonanceServiceBuffType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyDictionary<string, float> Effects { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Emotional buff request.
/// </summary>
public class EmotionalResonanceServiceEmotionalBuffRequest
{
    public EmotionalResonanceServiceBuffType EmotionalResonanceServiceBuffType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public string Reason { get; set; } = default!;
}

/// <summary>
/// Various enumeration types.
/// </summary>
public enum EmotionalResonanceServiceEmotion { Neutral, Joy, Anger, Fear, Confidence, Despair, Excitement, Calm }
public enum EmotionalResonanceServiceResonanceTriggerType { CombatSuccess, CombatFailure, EmotionalResonanceServiceSpectatorSupport, EmotionalResonanceServiceBreakingPoint, MatchStart, MatchEnd }
public enum EmotionalResonanceServiceFieldType { Empathy, Rivalry, Harmony, Conflict }
public enum EmotionalResonanceServiceTransferType { Direct, Gradual, Burst }
public enum EmotionalResonanceServiceSupportType { Encouragement, Intimidation, Motivation, Distraction }
public enum EmotionalResonanceServiceBreakingPointType { RageMode, DespairMode, ConfidenceBoost, None }
public enum EmotionalResonanceServiceCrowdMood { Excited, Tense, Bored, Angry, Happy }
public enum EmotionalResonanceServiceCrowdEventType { BigHit, Comeback, Perfect, TimeRunningOut, CrowdChant }
public enum EmotionalResonanceServiceSynergyType { PowerBoost, DefenseBoost, SpeedBoost, AccuracyBoost }
public enum EmotionalResonanceServiceEmotionalBond { Resonant, Conflicting, Neutral, Sympathetic }
public enum EmotionalResonanceServiceResonanceEventType { Transfer, Build, Decay, Burst }
public enum EmotionalResonanceServiceBuffType { RageBoost, ConfidenceBoost, FearReduction, JoyMultiplier }
