namespace SaveState.Core.GameLibrary.Models.AiCoach;

/// <summary>
/// Represents a strength identified in the player's strategy.
/// </summary>
public sealed record StrategyStrength(string Description, double Impact);

/// <summary>
/// Represents a weakness identified in the player's strategy.
/// </summary>
public sealed record StrategyWeakness(string Description, double Impact, string Improvement);

/// <summary>
/// A recommendation for strategy improvement.
/// </summary>
public sealed record StrategyRecommendation(string Action, string Rationale, int Priority);

/// <summary>
/// Complete analysis of player strategy.
/// </summary>
public sealed record StrategyAnalysis(
    StrategyRating OverallRating,
    IReadOnlyList<StrategyStrength> Strengths,
    IReadOnlyList<StrategyWeakness> Weaknesses,
    IReadOnlyList<StrategyRecommendation> Recommendations,
    string AnalysisSummary);

/// <summary>
/// Pattern detected in opponent behavior.
/// </summary>
public sealed record OpponentPattern(string Pattern, string Description, double Frequency);

/// <summary>
/// Strategy to counter opponent patterns.
/// </summary>
public sealed record CounterStrategy(string Strategy, string Description, double Effectiveness);

/// <summary>
/// Analysis of opponent behavior and patterns.
/// </summary>
public sealed record OpponentAnalysis(
    OpponentType OpponentType,
    OpponentSkillLevel SkillLevel,
    IReadOnlyList<OpponentPattern> Patterns,
    IReadOnlyList<CounterStrategy> CounterStrategies,
    string AnalysisSummary);

/// <summary>
/// A detected pattern in gameplay data.
/// </summary>
public sealed record PatternDetection(
    string PatternName,
    string Description,
    double Confidence,
    IReadOnlyList<string> Occurrences,
    DateTime DetectedAt);

/// <summary>
/// Comprehensive gameplay analysis result.
/// </summary>
public sealed record GameplayAnalysis(
    Guid SessionId,
    DateTime AnalysisTime,
    AnalysisType Type,
    StrategyAnalysis? StrategyAnalysis,
    OpponentAnalysis? OpponentAnalysis,
    IReadOnlyList<PatternDetection> DetectedPatterns,
    string Summary);
