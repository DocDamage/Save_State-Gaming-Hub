using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for advanced match analytics and statistics.
/// </summary>
public interface IMatchAnalyticsService
{
    /// <summary>
    /// Records detailed match data for analysis.
    /// </summary>
    Task<Result> RecordMatchDataAsync(MatchRecording matchData, CancellationToken ct = default);

    /// <summary>
    /// Analyzes match performance and generates insights.
    /// </summary>
    Task<Result<MatchAnalysis>> AnalyzeMatchAsync(Guid matchId, CancellationToken ct = default);

    /// <summary>
    /// Gets comprehensive player statistics.
    /// </summary>
    Task<Result<PlayerStatistics>> GetPlayerStatisticsAsync(Guid playerId, CancellationToken ct = default);

    /// <summary>
    /// Generates performance trends over time.
    /// </summary>
    Task<Result<PerformanceTrends>> GetPerformanceTrendsAsync(Guid playerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);

    /// <summary>
    /// Identifies patterns in player behavior.
    /// </summary>
    Task<Result<IReadOnlyList<PlayerPattern>>> IdentifyPatternsAsync(Guid playerId, CancellationToken ct = default);

    /// <summary>
    /// Provides personalized improvement recommendations.
    /// </summary>
    Task<Result<IReadOnlyList<ImprovementRecommendation>>> GetImprovementRecommendationsAsync(Guid playerId, CancellationToken ct = default);
}

/// <summary>
/// Detailed match recording data.
/// </summary>
public record MatchRecording(
    Guid MatchId,
    Guid Player1Id,
    Guid Player2Id,
    string Player1Character,
    string Player2Character,
    DateTime StartTime,
    DateTime EndTime,
    IReadOnlyList<RoundData> Rounds,
    IReadOnlyList<InputEvent> InputEvents,
    MatchMetadata Metadata);

/// <summary>
/// Data for a single round.
/// </summary>
public record RoundData(
    int RoundNumber,
    Guid WinnerId,
    TimeSpan Duration,
    IReadOnlyList<HitData> Hits,
    IReadOnlyList<SpecialMoveData> SpecialMoves,
    IReadOnlyList<ComboData> Combos);

/// <summary>
/// Individual hit data.
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
public record InputEvent(
    Guid PlayerId,
    string Input,
    TimeSpan Timestamp,
    InputType Type);

/// <summary>
/// Types of input events.
/// </summary>
public enum InputType
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
/// Comprehensive match analysis.
/// </summary>
public record MatchAnalysis(
    Guid MatchId,
    MatchPerformance Player1Performance,
    MatchPerformance Player2Performance,
    IReadOnlyList<string> KeyMoments,
    IReadOnlyList<string> TurningPoints,
    string OverallAnalysis);

/// <summary>
/// Individual player performance in a match.
/// </summary>
public record MatchPerformance(
    Guid PlayerId,
    int TotalDamageDealt,
    int TotalDamageReceived,
    int LongestCombo,
    int SpecialMovesUsed,
    decimal Accuracy,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses);

/// <summary>
/// Comprehensive player statistics.
/// </summary>
public record PlayerStatistics(
    Guid PlayerId,
    int TotalMatches,
    int Wins,
    int Losses,
    decimal WinRate,
    IReadOnlyDictionary<string, CharacterAnalyticsStats> CharacterStats,
    IReadOnlyList<Achievement> Achievements,
    PlayerRanking Ranking);

/// <summary>
/// Detailed performance statistics for a specific character.
/// </summary>
public record CharacterAnalyticsStats(
    string CharacterName,
    int MatchesPlayed,
    int Wins,
    int Losses,
    decimal WinRate,
    int TotalDamageDealt,
    int AverageComboLength,
    IReadOnlyList<string> MostUsedMoves);

/// <summary>
/// Player achievements.
/// </summary>
public record Achievement(
    string Name,
    string Description,
    DateTime UnlockedAt,
    AchievementRarity Rarity);

/// <summary>
/// Achievement rarity levels.
/// </summary>
public enum AchievementRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

/// <summary>
    /// Player ranking information.
/// </summary>
public record PlayerRanking(
    int GlobalRank,
    int RegionalRank,
    int Rating,
    string Tier,
    IReadOnlyList<RankedStats> RankedStats);

/// <summary>
/// Ranked statistics.
/// </summary>
public record RankedStats(
    string GameMode,
    int Rank,
    int Rating,
    int Wins,
    int Losses,
    decimal WinRate);

/// <summary>
/// Performance trends over time.
/// </summary>
public record PerformanceTrends(
    Guid PlayerId,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<TrendDataPoint> WinRateTrend,
    IReadOnlyList<TrendDataPoint> DamageTrend,
    IReadOnlyList<TrendDataPoint> ComboTrend,
    IReadOnlyList<string> NotableChanges);

/// <summary>
/// Data point for trend analysis.
/// </summary>
public record TrendDataPoint(
    DateTime Date,
    decimal Value,
    string Context);

/// <summary>
/// Identified player behavior pattern.
/// </summary>
public record PlayerPattern(
    string PatternType,
    string Description,
    decimal Frequency,
    IReadOnlyList<string> AssociatedMoves,
    string Impact);

/// <summary>
/// Personalized improvement recommendation.
/// </summary>
public record ImprovementRecommendation(
    string Category,
    string Recommendation,
    string Rationale,
    RecommendationPriority Priority,
    IReadOnlyList<string> ActionSteps);

/// <summary>
/// Priority levels for recommendations.
/// </summary>
public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}