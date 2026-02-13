using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;

namespace SaveState.Core.AI.Assistant;

/// <summary>
/// Monitors game sessions in real-time and emits events for analysis.
/// </summary>
public interface IGameSessionMonitor
{
    /// <summary>
    /// Starts monitoring a game session.
    /// </summary>
    Task<Result> StartSessionAsync(
        Guid sessionId,
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Ends monitoring for a game session.
    /// </summary>
    Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Records a gameplay event during the session.
    /// </summary>
    Task<Result> RecordEventAsync(
        Guid sessionId,
        GameplayEvent gameEvent,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current session state.
    /// </summary>
    Task<Result<SessionState>> GetSessionStateAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Event raised when a difficulty suggestion is generated.
    /// </summary>
    event EventHandler<DifficultySuggestionEventArgs>? DifficultySuggestionReceived;

    /// <summary>
    /// Event raised when a break reminder is triggered.
    /// </summary>
    event EventHandler<BreakReminderEventArgs>? BreakReminderTriggered;

    /// <summary>
    /// Event raised when session metrics are updated.
    /// </summary>
    event EventHandler<SessionMetricsUpdatedEventArgs>? SessionMetricsUpdated;
}

/// <summary>
/// Base class for gameplay events.
/// </summary>
public abstract record GameplayEvent
{
    public required DateTime TimestampUtc { get; init; }
}

/// <summary>
/// Event recorded when player dies in-game.
/// </summary>
public sealed record DeathEvent : GameplayEvent
{
    public required string? Location { get; init; }
    public required TimeSpan TimeSinceLastDeath { get; init; }
}

/// <summary>
/// Event recorded when player retries a section.
/// </summary>
public sealed record RetryEvent : GameplayEvent
{
    public required int AttemptNumber { get; init; }
    public required TimeSpan TimeSpentOnAttempt { get; init; }
}

/// <summary>
/// Event recorded for input pattern sampling.
/// </summary>
public sealed record InputSampleEvent : GameplayEvent
{
    public required float ActionsPerMinute { get; init; }
    public required float ErrorRate { get; init; }
    public required bool IsRapidBurst { get; init; }
    public required bool IsIdleSpike { get; init; }
}

/// <summary>
/// Event recorded when game is paused/unpaused.
/// </summary>
public sealed record PauseEvent : GameplayEvent
{
    public required bool IsPaused { get; init; }
    public required TimeSpan? Duration { get; init; }
}

/// <summary>
/// Current state of a monitored session.
/// </summary>
public sealed record SessionState(
    Guid SessionId,
    Guid GameId,
    DateTime StartTimeUtc,
    DateTime? EndTimeUtc,
    int DeathCount,
    int RetryCount,
    TimeSpan TotalPlayTime,
    TimeSpan TimeInCurrentSection,
    SessionMetrics CurrentMetrics,
    bool IsActive);

/// <summary>
/// Real-time session metrics.
/// </summary>
public sealed record SessionMetrics(
    float CurrentActionsPerMinute,
    float AverageErrorRate,
    bool HasRecentRapidBursts,
    bool HasRecentIdleSpikes,
    TimeSpan TimeSinceLastDeath,
    int PauseCount,
    TimeSpan TotalPausedTime);

/// <summary>
/// Event args for difficulty suggestions.
/// </summary>
public sealed class DifficultySuggestionEventArgs : EventArgs
{
    public required Guid SessionId { get; init; }
    public required Guid GameId { get; init; }
    public required SuggestedDifficulty SuggestedDifficulty { get; init; }
    public required float Confidence { get; init; }
    public required string Reasoning { get; init; }
    public required IReadOnlyList<string> ContributingFactors { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

/// <summary>
/// Event args for break reminders.
/// </summary>
public sealed class BreakReminderEventArgs : EventArgs
{
    public required Guid SessionId { get; init; }
    public required Guid GameId { get; init; }
    public required TimeSpan SessionDuration { get; init; }
    public required int BreaksTaken { get; init; }
    public required string Message { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

/// <summary>
/// Event args for session metrics updates.
/// </summary>
public sealed class SessionMetricsUpdatedEventArgs : EventArgs
{
    public required Guid SessionId { get; init; }
    public required SessionMetrics Metrics { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

// Note: SuggestedDifficulty enum is defined in SaveState.Core.Assistant.Services
