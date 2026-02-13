namespace SaveState.Application.Mugen.Services.MatchAnalytics;

/// <summary>
/// Core match data container for analytics processing.
/// </summary>
public record MatchData(
    Guid MatchId,
    Guid Player1Id,
    Guid Player2Id,
    string Player1Character,
    string Player2Character,
    DateTime StartTime,
    DateTime EndTime,
    IReadOnlyList<RoundData> Rounds,
    IReadOnlyList<InputEventData> InputEvents,
    MatchMetadata Metadata);

/// <summary>
/// Data for a single round in a match.
/// </summary>
public record RoundData(
    int RoundNumber,
    Guid WinnerId,
    TimeSpan Duration,
    IReadOnlyList<HitData> Hits,
    IReadOnlyList<SpecialMoveData> SpecialMoves,
    IReadOnlyList<ComboData> Combos);

/// <summary>
/// Individual hit/combo data.
/// </summary>
public record HitData(
    Guid AttackerId,
    Guid DefenderId,
    string MoveName,
    int Damage,
    bool CounterHit,
    TimeSpan Timestamp);

/// <summary>
/// Special move usage data.
/// </summary>
public record SpecialMoveData(
    Guid PlayerId,
    string MoveName,
    int Damage,
    TimeSpan Timestamp);

/// <summary>
/// Combo sequence data.
/// </summary>
public record ComboData(
    Guid PlayerId,
    int Length,
    int TotalDamage,
    TimeSpan Duration,
    IReadOnlyList<string> Moves);

/// <summary>
/// Individual input event.
/// </summary>
public record InputEventData(
    Guid PlayerId,
    string Input,
    TimeSpan Timestamp,
    AnalyticsInputType Type);

/// <summary>
/// Types of input events for analytics.
/// </summary>
public enum AnalyticsInputType
{
    ButtonPress,
    Direction,
    SpecialMove,
    ThrowAttempt,
    Block,
    Jump,
    Crouch
}

/// <summary>
/// Additional match metadata.
/// </summary>
public record MatchMetadata(
    string GameMode,
    string Stage,
    bool OnlineMatch,
    IReadOnlyDictionary<string, string> CustomData);

/// <summary>
/// Player statistics specific to a match.
/// </summary>
public record PlayerMatchStats(
    Guid PlayerId,
    int TotalDamageDealt,
    int TotalDamageReceived,
    int LongestCombo,
    int SpecialMovesUsed,
    decimal Accuracy,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses);

/// <summary>
/// Validation result for match data.
/// </summary>
public record MatchValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);
