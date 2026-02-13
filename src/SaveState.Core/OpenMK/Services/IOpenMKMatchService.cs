using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.ValueObjects;

namespace SaveState.Core.OpenMK.Services;

/// <summary>
/// Service for managing OpenMK matches and gameplay mechanics.
/// </summary>
public interface IOpenMKMatchService
{
    /// <summary>
    /// Starts a new OpenMK match between characters.
    /// </summary>
    Task<Result<OpenMKMatch>> StartMatchAsync(
        OpenMKCharacter player1Character,
        OpenMKCharacter player2Character,
        OpenMKMatchType matchType,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a special move in the current match.
    /// </summary>
    Task<Result<OpenMKMoveResult>> ExecuteSpecialMoveAsync(
        Guid matchId,
        Guid characterId,
        string specialMoveName,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a fatality when opponent is defeated.
    /// </summary>
    Task<Result<OpenMKFinisherResult>> ExecuteFatalityAsync(
        Guid matchId,
        Guid winnerCharacterId,
        string fatalityName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current match state.
    /// </summary>
    Task<Result<OpenMKMatchState>> GetMatchStateAsync(Guid matchId, CancellationToken ct = default);

    /// <summary>
    /// Updates super bar level for a character.
    /// </summary>
    Task<Result> UpdateSuperBarAsync(Guid matchId, Guid characterId, int changeAmount, CancellationToken ct = default);

    /// <summary>
    /// Records a combo in the match.
    /// </summary>
    Task<Result> RecordComboAsync(Guid matchId, Guid characterId, int comboLength, CancellationToken ct = default);

    /// <summary>
    /// Ends the current round and determines the winner.
    /// </summary>
    Task<Result<OpenMKRoundResult>> EndRoundAsync(Guid matchId, CancellationToken ct = default);

    /// <summary>
    /// Ends the match and records final results.
    /// </summary>
    Task<Result<OpenMKMatchResult>> EndMatchAsync(Guid matchId, CancellationToken ct = default);

    /// <summary>
    /// Validates if a move can be executed in the current match state.
    /// </summary>
    Task<Result<bool>> CanExecuteMoveAsync(Guid matchId, Guid characterId, string moveName, CancellationToken ct = default);
}

/// <summary>
/// Represents an active OpenMK match.
/// </summary>
public record OpenMKMatch(
    Guid Id,
    OpenMKCharacter Player1Character,
    OpenMKCharacter Player2Character,
    OpenMKMatchType MatchType,
    DateTime StartedAt,
    OpenMKMatchState CurrentState);

/// <summary>
/// Current state of an OpenMK match.
/// </summary>
public record OpenMKMatchState(
    int RoundNumber,
    int Player1Health,
    int Player2Health,
    int Player1SuperBar,
    int Player2SuperBar,
    int Player1Wins,
    int Player2Wins,
    TimeSpan RoundTimeRemaining,
    OpenMKMatchPhase Phase);

/// <summary>
/// Types of OpenMK matches.
/// </summary>
public enum OpenMKMatchType
{
    /// <summary>
    /// Standard 1v1 match.
    /// </summary>
    Versus,

    /// <summary>
    /// Tag team match.
    /// </summary>
    TagTeam,

    /// <summary>
    /// Tournament match.
    /// </summary>
    Tournament,

    /// <summary>
    /// Practice/training match.
    /// </summary>
    Training,

    /// <summary>
    /// Story mode match.
    /// </summary>
    Story
}

/// <summary>
/// Phases of an OpenMK match.
/// </summary>
public enum OpenMKMatchPhase
{
    /// <summary>
    /// Match is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Round is in progress.
    /// </summary>
    Fighting,

    /// <summary>
    /// Round has ended, finisher opportunity.
    /// </summary>
    Finisher,

    /// <summary>
    /// Match has ended.
    /// </summary>
    Finished
}

/// <summary>
/// Result of executing a move.
/// </summary>
public record OpenMKMoveResult(
    bool Success,
    int DamageDealt,
    int SuperBarGained,
    string? AnimationPlayed,
    string? SoundPlayed,
    bool? ComboExtended);

/// <summary>
/// Result of executing a finisher.
/// </summary>
public record OpenMKFinisherResult(
    bool Success,
    OpenMKFinisherType FinisherType,
    string? AnimationPlayed,
    string? SoundPlayed,
    string? VoiceLinePlayed,
    bool MatchEnded);

/// <summary>
/// Types of finishers.
/// </summary>
public enum OpenMKFinisherType
{
    /// <summary>
    /// Standard fatality.
    /// </summary>
    Fatality,

    /// <summary>
    /// Brutality finisher.
    /// </summary>
    Brutality,

    /// <summary>
    /// Friendship finisher.
    /// </summary>
    Friendship,

    /// <summary>
    /// Babality finisher.
    /// </summary>
    Babality
}

/// <summary>
/// Result of a round.
/// </summary>
public record OpenMKRoundResult(
    int RoundNumber,
    Guid WinnerCharacterId,
    Guid LoserCharacterId,
    OpenMKRoundEndReason EndReason,
    TimeSpan RoundDuration);

/// <summary>
/// Reasons a round can end.
/// </summary>
public enum OpenMKRoundEndReason
{
    /// <summary>
    /// Character defeated by health depletion.
    /// </summary>
    HealthDepleted,

    /// <summary>
    /// Character defeated by time over.
    /// </summary>
    TimeOver,

    /// <summary>
    /// Character defeated by throw.
    /// </summary>
    Throw,

    /// <summary>
    /// Round ended by special condition.
    /// </summary>
    Special
}

/// <summary>
/// Final result of a match.
/// </summary>
public record OpenMKMatchResult(
    Guid WinnerCharacterId,
    Guid LoserCharacterId,
    int WinnerRoundsWon,
    int LoserRoundsWon,
    TimeSpan TotalMatchDuration,
    IReadOnlyList<OpenMKRoundResult> Rounds,
    IReadOnlyList<OpenMKMatchStats> Statistics);

/// <summary>
/// Match statistics.
/// </summary>
public record OpenMKMatchStats(
    Guid CharacterId,
    int TotalDamageDealt,
    int TotalDamageReceived,
    int MaxComboLength,
    int SpecialMovesUsed,
    int SuperMovesUsed,
    int FinishersPerformed,
    TimeSpan TotalFightTime);