namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents statistics for a MUGEN character.
/// </summary>
public sealed record CharacterStats(
    Guid CharacterId,
    string CharacterName,
    int TotalMatches,
    int Wins,
    int Losses,
    float WinRate,
    TimeSpan TotalPlaytime,
    Guid? BestMatchupCharacterId,
    Guid? WorstMatchupCharacterId);

/// <summary>
/// Represents matchup statistics between two characters.
/// </summary>
public sealed record MatchupStats(
    Guid OpponentId,
    string OpponentName,
    int Wins,
    int Losses,
    float WinRate);

/// <summary>
/// Represents coaching advice for a matchup.
/// </summary>
public sealed record MatchupAdvice(
    Guid YourCharacter,
    Guid OpponentCharacter,
    float PredictedWinRate,
    IReadOnlyList<string> Tips,
    IReadOnlyList<string> MovesToAvoid,
    IReadOnlyList<string> KeyMoves);

/// <summary>
/// Represents detailed character guide information.
/// </summary>
public sealed record CharacterGuide(
    Guid CharacterId,
    string CharacterName,
    string Overview,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<ComboInfo> BasicCombos,
    IReadOnlyList<string> AdvancedTips);

/// <summary>
/// Represents information about a character combo.
/// </summary>
public sealed record ComboInfo(
    string Name,
    string Input,
    int Damage,
    string Difficulty);

/// <summary>
/// Represents training session statistics.
/// </summary>
public sealed record TrainingStats(
    Guid SessionId,
    TimeSpan Duration,
    int ComboAttempts,
    int SuccessfulCombos,
    int MaxComboHits,
    int MaxComboDamage);

/// <summary>
/// Represents training configuration.
/// </summary>
public sealed record TrainingConfig(
    Guid DummyCharacterId,
    DummyBehavior Behavior,
    bool ShowInputDisplay,
    bool ShowHitboxes,
    bool RandomBlock);

/// <summary>
/// Represents the behavior of a training dummy.
/// </summary>
public enum DummyBehavior
{
    Stand,
    Crouch,
    Jump,
    Block,
    Random,
    Recorded
}