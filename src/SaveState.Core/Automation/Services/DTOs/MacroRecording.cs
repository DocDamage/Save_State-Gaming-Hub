namespace SaveState.Core.Automation.Services.DTOs;

/// <summary>
/// A recording session for creating macros.
/// </summary>
public sealed record MacroRecordingSession(
    Guid Id,
    Guid? GameId,
    string Name,
    string Description,
    RecordingMode Mode,
    DateTime StartedAt,
    RecordingStatus Status,
    IReadOnlyList<MacroAction> RecordedActions,
    TimeSpan Duration,
    bool IsPaused = false);

/// <summary>
/// Status of a recording session.
/// </summary>
public sealed record RecordingStatus(
    bool IsRecording,
    bool IsPaused,
    TimeSpan Duration,
    int ActionsRecorded,
    DateTime StartedAt);

/// <summary>
/// Configuration for macro playback.
/// </summary>
public sealed record MacroPlaybackConfig(
    PlaybackSpeed Speed = PlaybackSpeed.Normal,
    bool Loop = false,
    int? MaxIterations = null,
    TimeSpan? Timeout = null,
    IReadOnlyDictionary<string, object>? Variables = null);

/// <summary>
/// Playback speed options.
/// </summary>
public enum PlaybackSpeed
{
    Slow,
    Normal,
    Fast,
    Instant
}

/// <summary>
/// A playback session for executing macros.
/// </summary>
public sealed record MacroPlaybackSession(
    Guid Id,
    Guid MacroId,
    PlaybackSpeed Speed,
    DateTime StartedAt,
    PlaybackStatus Status,
    int CurrentActionIndex,
    TimeSpan Duration);

/// <summary>
/// Status of a playback session.
/// </summary>
public sealed record PlaybackStatus(
    bool IsPlaying,
    bool IsPaused,
    PlaybackSpeed Speed,
    int CurrentActionIndex,
    int TotalActions,
    TimeSpan Duration,
    TimeSpan EstimatedTimeRemaining);

/// <summary>
/// Result of macro validation.
/// </summary>
public sealed record MacroValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    TimeSpan EstimatedDuration);

/// <summary>
/// Event arguments for recording started.
/// </summary>
public sealed class RecordingStartedEventArgs : EventArgs
{
    public MacroRecordingSession Session { get; init; } = null!;
}

/// <summary>
/// Event arguments for recording stopped.
/// </summary>
public sealed class RecordingStoppedEventArgs : EventArgs
{
    public Guid SessionId { get; init; }
    public Macro? RecordedMacro { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Event arguments for action recorded.
/// </summary>
public sealed class ActionRecordedEventArgs : EventArgs
{
    public Guid SessionId { get; init; }
    public MacroAction Action { get; init; } = null!;
}

/// <summary>
/// Event arguments for playback started.
/// </summary>
public sealed class PlaybackStartedEventArgs : EventArgs
{
    public MacroPlaybackSession Session { get; init; } = null!;
}

/// <summary>
/// Event arguments for playback stopped.
/// </summary>
public sealed class PlaybackStoppedEventArgs : EventArgs
{
    public Guid SessionId { get; init; }
    public bool CompletedSuccessfully { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Event arguments for action executed.
/// </summary>
public sealed class ActionExecutedEventArgs : EventArgs
{
    public Guid SessionId { get; init; }
    public MacroAction Action { get; init; } = null!;
    public bool Success { get; init; }
}

/// <summary>
/// Event arguments for playback error.
/// </summary>
public sealed class PlaybackErrorEventArgs : EventArgs
{
    public Guid SessionId { get; init; }
    public Exception Exception { get; init; } = null!;
    public MacroAction? FailedAction { get; init; }
}
