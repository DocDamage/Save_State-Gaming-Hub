using System.Collections.Generic;
using SaveState.Core.Mugen.Services;

namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Analysis of a character matchup including advantages and strategies.
/// </summary>
public sealed record CharacterMatchupAnalysis(
    string Character1,
    string Character2,
    MatchupAdvantage Advantage,
    double WinRate,
    IReadOnlyList<string> StrongMatchupReasons,
    IReadOnlyList<string> WeakMatchupReasons,
    IReadOnlyList<string> RecommendedStrategies);

/// <summary>
/// Describes opponent tendencies for counter-pick logic.
/// </summary>
public sealed record PlayerTendencies(
    IReadOnlyDictionary<string, double> CharacterUsage,
    IReadOnlyDictionary<string, double> MoveFrequencies,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyDictionary<string, double> PreferredRanges);

/// <summary>
/// Confidence indicator for AI-driven recommendations.
/// </summary>
public enum ConfidenceLevel
{
    Low,
    Medium,
    High
}

/// <summary>
/// Recommendation for a specific character pick.
/// </summary>
public sealed record CharacterRecommendation(
    string CharacterName,
    double MatchupScore,
    IReadOnlyList<string> Advantages,
    IReadOnlyList<string> Strategies);

/// <summary>
/// Counter-pick recommendations.
/// </summary>
public sealed record CounterPickRecommendation(
    IReadOnlyList<CharacterRecommendation> RecommendedCharacters,
    IReadOnlyList<string> StrategicAdvice,
    double ExpectedWinRate,
    ConfidenceLevel Confidence);

/// <summary>
/// Player performance snapshot used for adaptive difficulty.
/// </summary>
public sealed record PlayerPerformance(
    double WinRate,
    double AverageMatchDuration,
    IReadOnlyDictionary<string, double> CharacterWinRates,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    double SkillTrend);

/// <summary>
/// Result of dynamic difficulty calculations.
/// </summary>
public sealed record DifficultyAdjustment(
    DifficultyLevel RecommendedDifficulty,
    IReadOnlyDictionary<string, double> ParameterAdjustments,
    string Reasoning,
    double Confidence);

/// <summary>
/// Size presets for generated stages.
/// </summary>
public enum StageSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Parameters controlling procedural stage generation.
/// </summary>
public sealed record StageGenerationParameters(
    string Theme,
    DifficultyLevel Difficulty,
    IReadOnlyList<string> RequiredElements,
    IReadOnlyList<string> AvoidedElements,
    StageSize Size);

/// <summary>
/// Result of procedural stage generation.
/// </summary>
public sealed record ProceduralStage(
    string Name,
    string Description,
    DifficultyLevel Difficulty,
    StageSize Size,
    IReadOnlyList<StageElement> Elements,
    IReadOnlyDictionary<string, double> Properties,
    double BalanceScore,
    string Theme);
