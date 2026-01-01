using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Core.Automation.Services;

/// <summary>
/// Service for recording user actions as macros for later playback.
/// </summary>
public interface IMacroRecorder
{
    /// <summary>
    /// Starts recording a macro with the specified configuration.
    /// </summary>
    Task<Result<MacroRecordingSession>> StartRecordingAsync(
        MacroRecordingConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Stops the current recording session and returns the recorded macro.
    /// </summary>
    Task<Result<Macro>> StopRecordingAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels the current recording session without saving.
    /// </summary>
    Task<Result> CancelRecordingAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a custom action to the current recording session.
    /// </summary>
    Task<Result> RecordActionAsync(
        Guid sessionId,
        MacroAction action,
        CancellationToken ct = default);

    /// <summary>
    /// Pauses the current recording session.
    /// </summary>
    Task<Result> PauseRecordingAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Resumes a paused recording session.
    /// </summary>
    Task<Result> ResumeRecordingAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current recording status.
    /// </summary>
    Task<Result<RecordingStatus>> GetRecordingStatusAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active recording sessions.
    /// </summary>
    Task<Result<IReadOnlyList<MacroRecordingSession>>> GetActiveSessionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Event raised when a recording session starts.
    /// </summary>
    event EventHandler<RecordingStartedEventArgs>? RecordingStarted;

    /// <summary>
    /// Event raised when a recording session stops.
    /// </summary>
    event EventHandler<RecordingStoppedEventArgs>? RecordingStopped;

    /// <summary>
    /// Event raised when an action is recorded.
    /// </summary>
    event EventHandler<ActionRecordedEventArgs>? ActionRecorded;
}