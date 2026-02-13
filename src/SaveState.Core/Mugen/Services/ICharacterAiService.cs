using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for AI-powered character assistance and analysis.
/// </summary>
public interface ICharacterAiService
{
    /// <summary>
    /// Analyzes a character's strengths and weaknesses.
    /// </summary>
    Task<Result<CharacterAnalysis>> AnalyzeCharacterAsync(string characterName, CancellationToken ct = default);

    /// <summary>
    /// Suggests optimal strategies against specific opponents.
    /// </summary>
    Task<Result<IReadOnlyList<StrategySuggestion>>> GetStrategySuggestionsAsync(
        string playerCharacter,
        string opponentCharacter,
        CancellationToken ct = default);

    /// <summary>
    /// Generates training scenarios for skill improvement.
    /// </summary>
    Task<Result<IReadOnlyList<TrainingScenario>>> GenerateTrainingScenariosAsync(
        string characterName,
        CharacterSkillLevel skillLevel,
        CancellationToken ct = default);

    /// <summary>
    /// Provides real-time combo suggestions during matches.
    /// </summary>
    Task<Result<ComboSuggestion>> GetComboSuggestionAsync(
        string characterName,
        MatchState currentState,
        CancellationToken ct = default);
}

/// <summary>
/// Analysis of a character's capabilities.
/// </summary>
public record CharacterAnalysis(
    string CharacterName,
    CharacterStrengths Strengths,
    CharacterWeaknesses Weaknesses,
    IReadOnlyList<string> OptimalMatchups,
    IReadOnlyList<string> DifficultMatchups,
    CharacterSkillLevel RecommendedSkillLevel);

/// <summary>
/// Character strength categories.
/// </summary>
public record CharacterStrengths(
    int Speed,
    int Power,
    int Range,
    int Mixup,
    int AntiAir,
    IReadOnlyList<string> SpecialAbilities);

/// <summary>
/// Character weakness categories.
/// </summary>
public record CharacterWeaknesses(
    int Vulnerability,
    int LimitedOptions,
    int ResourceDependency,
    IReadOnlyList<string> CommonPunishes);

/// <summary>
/// Strategy suggestion for matchups.
/// </summary>
public record StrategySuggestion(
    string OpponentCharacter,
    string Strategy,
    string KeyMoves,
    string Positioning,
    int EffectivenessRating);

/// <summary>
/// Training scenario for improvement.
/// </summary>
public record TrainingScenario(
    string ScenarioName,
    string Description,
    IReadOnlyList<string> Objectives,
    TrainingDifficulty Difficulty,
    TimeSpan EstimatedDuration);

/// <summary>
/// Character skill levels.
/// </summary>
public enum CharacterSkillLevel
{
    Beginner,
    Intermediate,
    Advanced,
    Expert,
    Master
}

/// <summary>
/// Training difficulty levels.
/// </summary>
public enum TrainingDifficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}

/// <summary>
/// Current match state for AI analysis.
/// </summary>
public record MatchState(
    int PlayerHealth,
    int OpponentHealth,
    int PlayerSuper,
    int OpponentSuper,
    string PlayerPosition,
    string OpponentPosition,
    TimeSpan RoundTimeRemaining);

/// <summary>
/// Combo suggestion from AI.
/// </summary>
public record ComboSuggestion(
    IReadOnlyList<string> InputSequence,
    int ExpectedDamage,
    string FollowUpOptions,
    int SuccessProbability);