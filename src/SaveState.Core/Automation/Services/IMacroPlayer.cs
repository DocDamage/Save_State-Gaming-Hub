using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Core.Automation.Services;

/// <summary>
/// Service for playing back recorded macros.
/// </summary>
public interface IMacroPlayer
{
    /// <summary>
    /// Starts playback of a macro with the specified configuration.
    /// </summary>
    Task<Result<MacroPlaybackSession>> StartPlaybackAsync(
        Guid macroId,
        MacroPlaybackConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Stops the current playback session.
    /// </summary>
    Task<Result> StopPlaybackAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Pauses the current playback session.
    /// </summary>
    Task<Result> PausePlaybackAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Resumes a paused playback session.
    /// </summary>
    Task<Result> ResumePlaybackAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current playback status.
    /// </summary>
    Task<Result<PlaybackStatus>> GetPlaybackStatusAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active playback sessions.
    /// </summary>
    Task<Result<IReadOnlyList<MacroPlaybackSession>>> GetActiveSessionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Validates that a macro can be played back successfully.
    /// </summary>
    Task<Result<MacroValidationResult>> ValidateMacroAsync(
        Guid macroId,
        CancellationToken ct = default);

    /// <summary>
    /// Event raised when playback starts.
    /// </summary>
    event EventHandler<PlaybackStartedEventArgs>? PlaybackStarted;

    /// <summary>
    /// Event raised when playback stops.
    /// </summary>
    event EventHandler<PlaybackStoppedEventArgs>? PlaybackStopped;

    /// <summary>
    /// Event raised when an action is executed during playback.
    /// </summary>
    event EventHandler<ActionExecutedEventArgs>? ActionExecuted;

    /// <summary>
    /// Event raised when a playback error occurs.
    /// </summary>
    event EventHandler<PlaybackErrorEventArgs>? PlaybackError;
}