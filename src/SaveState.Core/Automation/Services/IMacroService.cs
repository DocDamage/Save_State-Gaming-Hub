using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Core.Automation.Services;

/// <summary>
/// Service for recording and playing back macros.
/// </summary>
public interface IMacroService
{
    /// <summary>
    /// Gets whether recording is in progress.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Gets the current recording session.
    /// </summary>
    MacroRecording? CurrentRecording { get; }

    /// <summary>
    /// Starts recording a new macro.
    /// </summary>
    /// <param name="macroName">The name of the macro.</param>
    /// <param name="description">A description of the macro.</param>
    /// <param name="gameId">Optional game ID to associate with the macro.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<MacroRecording>> StartRecordingAsync(
        string macroName,
        string description,
        Guid? gameId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Stops recording and saves the macro.
    /// </summary>
    Task<Result<Macro>> StopRecordingAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Records an action during recording.
    /// </summary>
    Task<Result> RecordActionAsync(
        MacroAction action,
        CancellationToken ct = default);

    /// <summary>
    /// Plays back a recorded macro.
    /// </summary>
    Task<Result<MacroExecutionResult>> PlayMacroAsync(
        Guid macroId,
        MacroPlaybackOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all macros.
    /// </summary>
    Task<Result<IReadOnlyList<Macro>>> GetMacrosAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets a macro by ID.
    /// </summary>
    Task<Result<Macro>> GetMacroAsync(
        Guid macroId,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a macro.
    /// </summary>
    Task<Result> DeleteMacroAsync(
        Guid macroId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a macro.
    /// </summary>
    Task<Result> UpdateMacroAsync(
        Guid macroId,
        string name,
        string description,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a macro to a file.
    /// </summary>
    Task<Result<string>> ExportMacroAsync(
        Guid macroId,
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Imports a macro from a file.
    /// </summary>
    Task<Result<Macro>> ImportMacroAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Event raised when recording starts.
    /// </summary>
    event EventHandler<MacroRecordingEventArgs>? RecordingStarted;

    /// <summary>
    /// Event raised when recording stops.
    /// </summary>
    event EventHandler<MacroRecordingEventArgs>? RecordingStopped;

    /// <summary>
    /// Event raised when an action is recorded.
    /// </summary>
    event EventHandler<MacroActionRecordedEventArgs>? ActionRecorded;

    /// <summary>
    /// Event raised when playback starts.
    /// </summary>
    event EventHandler<MacroPlaybackEventArgs>? PlaybackStarted;

    /// <summary>
    /// Event raised when playback completes.
    /// </summary>
    event EventHandler<MacroPlaybackEventArgs>? PlaybackCompleted;
}
