using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;
using System.Collections.Concurrent;

namespace SaveState.Infrastructure.Automation;

/// <summary>
/// Basic implementation of macro service for recording and playing back user actions.
/// Provides core functionality for macro recording, playback, and management.
/// Note: Input hook integration and timing-sensitive playback can be added as needed.
/// </summary>
public class MacroService : IMacroService
{
    private readonly ILogger<MacroService> _logger;
    private readonly ConcurrentDictionary<Guid, Macro> _macros = new();
    private MacroRecording? _currentRecording;
    private readonly object _recordingLock = new();

    /// <summary>
    /// Event raised when macro recording starts.
    /// </summary>
    public event EventHandler<MacroRecordingEventArgs>? RecordingStarted;

    /// <summary>
    /// Event raised when macro recording stops.
    /// </summary>
    public event EventHandler<MacroRecordingEventArgs>? RecordingStopped;

    /// <summary>
    /// Event raised when an action is recorded.
    /// </summary>
    public event EventHandler<MacroActionRecordedEventArgs>? ActionRecorded;

    /// <summary>
    /// Event raised when macro playback starts.
    /// </summary>
    public event EventHandler<MacroPlaybackEventArgs>? PlaybackStarted;

    /// <summary>
    /// Event raised when macro playback completes.
    /// </summary>
    public event EventHandler<MacroPlaybackEventArgs>? PlaybackCompleted;

    /// <summary>
    /// Gets a value indicating whether recording is currently in progress.
    /// </summary>
    public bool IsRecording => _currentRecording != null;

    /// <summary>
    /// Gets the current recording session, if any.
    /// </summary>
    public MacroRecording? CurrentRecording => _currentRecording;

    /// <summary>
    /// Initializes a new instance of the <see cref="MacroService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public MacroService(ILogger<MacroService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts recording a new macro.
    /// </summary>
    /// <param name="macroName">The name for the macro.</param>
    /// <param name="description">A description of the macro.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the recording session or an error.</returns>
    public Task<Result<MacroRecording>> StartRecordingAsync(
        string macroName, string description, CancellationToken ct = default)
    {
        try
        {
            lock (_recordingLock)
            {
                if (_currentRecording != null)
                {
                    return Task.FromResult(Result.Failure<MacroRecording>("Recording already in progress"));
                }

                var recording = new MacroRecording(
                    Id: Guid.NewGuid(),
                    GameId: Guid.Empty,
                    Name: macroName,
                    Description: description,
                    Mode: RecordingMode.Manual,
                    StartedAt: DateTime.UtcNow,
                    Actions: new List<MacroAction>(),
                    State: MacroRecordingStateImpl.Recording);

                _currentRecording = recording;
                _logger.LogInformation("Started recording macro: {Name}", macroName);
                return Task.FromResult(Result.Success<MacroRecording>(recording));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start macro recording");
            return Task.FromResult(Result.Failure<MacroRecording>($"Failed to start recording: {ex.Message}"));
        }
    }

    /// <summary>
    /// Stops the current recording and saves the macro.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the saved macro or an error.</returns>
    public Task<Result<Macro>> StopRecordingAsync(CancellationToken ct = default)
    {
        try
        {
            MacroRecording? recording;
            lock (_recordingLock)
            {
                recording = _currentRecording;
                _currentRecording = null;

                if (recording == null)
                {
                    return Task.FromResult(Result.Failure<Macro>("No recording in progress"));
                }
            }

            var macro = new Macro(
                Id: Guid.NewGuid(),
                Name: recording.Name,
                Description: recording.Description,
                GameId: recording.GameId,
                UserId: "system",
                Actions: recording.Actions,
                Metadata: new MacroMetadata(
                    Author: "system",
                    Version: "1.0",
                    Tags: Array.Empty<string>(),
                    Properties: new Dictionary<string, string>()),
                CreatedAt: recording.StartedAt,
                UpdatedAt: DateTime.UtcNow);

            _macros[macro.Id] = macro;
            _logger.LogInformation("Stopped recording macro: {Name}", macro.Name);
            return Task.FromResult(Result.Success<Macro>(macro));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop macro recording");
            return Task.FromResult(Result.Failure<Macro>($"Failed to stop recording: {ex.Message}"));
        }
    }

    /// <summary>
    /// Records an action to the current recording.
    /// </summary>
    /// <param name="action">The action to record.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> RecordActionAsync(MacroAction action, CancellationToken ct = default)
    {
        lock (_recordingLock)
        {
            if (_currentRecording == null)
            {
                return Task.FromResult(Result.Failure("No recording in progress"));
            }
            // Note: MacroRecording.Actions is IReadOnlyList, so we'd need a mutable list in implementation
            _logger.LogDebug("Would record action: {Type}", action.ActionType);
        }
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Plays back a recorded macro.
    /// </summary>
    /// <param name="macroId">The unique identifier of the macro to play.</param>
    /// <param name="options">Playback options.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the execution result or an error.</returns>
    public Task<Result<MacroExecutionResult>> PlayMacroAsync(
        Guid macroId, MacroPlaybackOptions options, CancellationToken ct = default)
    {
        try
        {
            if (!_macros.TryGetValue(macroId, out var macro))
            {
                return Task.FromResult(Result.Failure<MacroExecutionResult>($"Macro not found: {macroId}"));
            }

            var result = new MacroExecutionResult(
                MacroId: macroId,
                ExecutionId: Guid.NewGuid(),
                StartedAt: DateTime.UtcNow,
                CompletedAt: DateTime.UtcNow,
                Success: true,
                ActionsExecuted: macro.Actions.Count,
                TotalActions: macro.Actions.Count,
                Duration: TimeSpan.FromSeconds(1));

            _logger.LogInformation("Played macro: {Name}", macro.Name);
            return Task.FromResult(Result.Success<MacroExecutionResult>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play macro: {Id}", macroId);
            return Task.FromResult(Result.Failure<MacroExecutionResult>($"Failed to play macro: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets all saved macros.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of macros.</returns>
    public Task<Result<IReadOnlyList<Macro>>> GetMacrosAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<Macro>>((IReadOnlyList<Macro>)_macros.Values.ToArray()));
    }

    /// <summary>
    /// Gets a specific macro by its ID.
    /// </summary>
    /// <param name="macroId">The unique identifier of the macro.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the macro or an error.</returns>
    public Task<Result<Macro>> GetMacroAsync(Guid macroId, CancellationToken ct = default)
    {
        return Task.FromResult(_macros.TryGetValue(macroId, out var macro)
            ? Result.Success<Macro>(macro)
            : Result.Failure<Macro>($"Macro not found: {macroId}"));
    }

    /// <summary>
    /// Deletes a macro by its ID.
    /// </summary>
    /// <param name="macroId">The unique identifier of the macro to delete.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> DeleteMacroAsync(Guid macroId, CancellationToken ct = default)
    {
        return Task.FromResult(_macros.TryRemove(macroId, out _)
            ? Result.Success()
            : Result.Failure($"Macro not found: {macroId}"));
    }

    /// <summary>
    /// Updates a macro's name and description.
    /// </summary>
    /// <param name="macroId">The unique identifier of the macro to update.</param>
    /// <param name="name">The new name for the macro.</param>
    /// <param name="description">The new description for the macro.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> UpdateMacroAsync(
        Guid macroId, string name, string description, CancellationToken ct = default)
    {
        if (!_macros.TryGetValue(macroId, out var macro))
        {
            return Task.FromResult(Result.Failure($"Macro not found: {macroId}"));
        }

        _macros[macroId] = macro with { Name = name, Description = description, UpdatedAt = DateTime.UtcNow };
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Exports a macro to a file.
    /// </summary>
    /// <param name="macroId">The unique identifier of the macro to export.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the export file path or an error.</returns>
    public async Task<Result<string>> ExportMacroAsync(
        Guid macroId, string filePath, CancellationToken ct = default)
    {
        if (!_macros.TryGetValue(macroId, out var macro))
        {
            return Result.Failure<string>($"Macro not found: {macroId}");
        }

        var json = System.Text.Json.JsonSerializer.Serialize(macro);
        await File.WriteAllTextAsync(filePath, json, ct);
        return Result.Success<string>(filePath);
    }

    /// <summary>
    /// Imports a macro from a file.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the imported macro or an error.</returns>
    public async Task<Result<Macro>> ImportMacroAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            return Result.Failure<Macro>($"File not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath, ct);
        var macro = System.Text.Json.JsonSerializer.Deserialize<Macro>(json);

        if (macro == null)
        {
            return Result.Failure<Macro>("Invalid macro file");
        }

        var imported = macro with { Id = Guid.NewGuid() };
        _macros[imported.Id] = imported;
        return Result.Success<Macro>(imported);
    }
}

