using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Automation;

/// <summary>
/// Implementation of macro recording service.
/// </summary>
public class MacroRecorder : IMacroRecorder, IDisposable
{
    private readonly ILogger<MacroRecorder> _logger;
    private readonly Dictionary<Guid, MacroRecordingSession> _activeSessions = new();
    private readonly Dictionary<Guid, List<MacroAction>> _recordedActions = new();
    private bool _disposed;

    public event EventHandler<RecordingStartedEventArgs>? RecordingStarted;
    public event EventHandler<RecordingStoppedEventArgs>? RecordingStopped;
    public event EventHandler<ActionRecordedEventArgs>? ActionRecorded;

    public MacroRecorder(ILogger<MacroRecorder> logger)
    {
        _logger = logger;
    }

    public Task<Result<MacroRecordingSession>> StartRecordingAsync(
        MacroRecordingConfig config,
        CancellationToken ct = default)
    {
        try
        {
            var sessionId = Guid.NewGuid();
            var session = new MacroRecordingSession(
                Id: sessionId,
                GameId: config.GameId,
                Name: config.Name,
                Description: config.Description,
                Mode: config.Mode,
                StartedAt: DateTime.UtcNow,
                Status: new RecordingStatus(
                    IsRecording: true,
                    IsPaused: false,
                    Duration: TimeSpan.Zero,
                    ActionsRecorded: 0,
                    StartedAt: DateTime.UtcNow),
                RecordedActions: Array.Empty<MacroAction>(),
                Duration: TimeSpan.Zero);

            _activeSessions[sessionId] = session;
            _recordedActions[sessionId] = new List<MacroAction>();

            OnRecordingStarted(session);
            _logger.LogInformation("Started macro recording session {SessionId} for game {GameId}",
                sessionId, config.GameId);

            return Task.FromResult(Result.Success<MacroRecordingSession>(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start macro recording");
            return Task.FromResult(Result.Failure<MacroRecordingSession>($"Failed to start recording: {ex.Message}"));
        }
    }

    public Task<Result<Macro>> StopRecordingAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result.Failure<Macro>("Recording session not found"));
            }

            var actions = _recordedActions[sessionId];
            var duration = DateTime.UtcNow - session.StartedAt;

            // Create the macro
            var macro = new Macro(
                Id: Guid.NewGuid(),
                Name: session.Name,
                Description: session.Description,
                GameId: session.GameId,
                UserId: "current_user", // Placeholder until user context service is integrated
                Actions: actions,
                Metadata: new MacroMetadata(
                    Author: "current_user",
                    Version: "1.0.0",
                    Tags: Array.Empty<string>(),
                    Properties: new Dictionary<string, string>()),
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow);

            _activeSessions.Remove(sessionId);
            _recordedActions.Remove(sessionId);

            OnRecordingStopped(sessionId, macro, duration);
            _logger.LogInformation("Stopped macro recording session {SessionId}, created macro {MacroId}",
                sessionId, macro.Id);

            return Task.FromResult(Result.Success<Macro>(macro));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop macro recording session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure<Macro>($"Failed to stop recording: {ex.Message}"));
        }
    }

    public Task<Result> CancelRecordingAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.Remove(sessionId))
            {
                return Task.FromResult(Result.Failure("Recording session not found"));
            }

            _recordedActions.Remove(sessionId);

            var duration = DateTime.UtcNow - _activeSessions[sessionId].StartedAt;
            OnRecordingStopped(sessionId, null, duration);

            _logger.LogInformation("Cancelled macro recording session {SessionId}", sessionId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel macro recording session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure($"Failed to cancel recording: {ex.Message}"));
        }
    }

    public Task<Result> RecordActionAsync(
        Guid sessionId,
        MacroAction action,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result.Failure("Recording session not found"));
            }

            if (!session.Status.IsRecording || session.Status.IsPaused)
            {
                return Task.FromResult(Result.Failure("Recording session is not active"));
            }

            _recordedActions[sessionId].Add(action);

            // Update session status
            var updatedStatus = session.Status with
            {
                ActionsRecorded = session.Status.ActionsRecorded + 1,
                Duration = DateTime.UtcNow - session.StartedAt
            };

            var updatedSession = session with { Status = updatedStatus };
            _activeSessions[sessionId] = updatedSession;

            OnActionRecorded(sessionId, action);
            _logger.LogDebug("Recorded action {ActionType} in session {SessionId}",
                action.ActionType, sessionId);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record action in session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure($"Failed to record action: {ex.Message}"));
        }
    }

    public Task<Result> PauseRecordingAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result.Failure("Recording session not found"));
            }

            var updatedStatus = session.Status with { IsPaused = true };
            var updatedSession = session with { Status = updatedStatus };
            _activeSessions[sessionId] = updatedSession;

            _logger.LogInformation("Paused macro recording session {SessionId}", sessionId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause macro recording session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure($"Failed to pause recording: {ex.Message}"));
        }
    }

    public Task<Result> ResumeRecordingAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result.Failure("Recording session not found"));
            }

            var updatedStatus = session.Status with { IsPaused = false };
            var updatedSession = session with { Status = updatedStatus };
            _activeSessions[sessionId] = updatedSession;

            _logger.LogInformation("Resumed macro recording session {SessionId}", sessionId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume macro recording session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure($"Failed to resume recording: {ex.Message}"));
        }
    }

    public Task<Result<RecordingStatus>> GetRecordingStatusAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result.Failure<RecordingStatus>("Recording session not found"));
            }

            var currentDuration = DateTime.UtcNow - session.StartedAt;
            var updatedStatus = session.Status with { Duration = currentDuration };

            return Task.FromResult(Result.Success<RecordingStatus>(updatedStatus));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recording status for session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure<RecordingStatus>($"Failed to get status: {ex.Message}"));
        }
    }

    public Task<Result<IReadOnlyList<MacroRecordingSession>>> GetActiveSessionsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var sessions = (IReadOnlyList<MacroRecordingSession>)_activeSessions.Values.ToArray();
            return Task.FromResult(Result.Success<IReadOnlyList<MacroRecordingSession>>(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active recording sessions");
            return Task.FromResult(Result.Failure<IReadOnlyList<MacroRecordingSession>>(
                $"Failed to get sessions: {ex.Message}"));
        }
    }

    private void OnRecordingStarted(MacroRecordingSession session)
    {
        RecordingStarted?.Invoke(this, new RecordingStartedEventArgs { Session = session });
    }

    private void OnRecordingStopped(Guid sessionId, Macro? macro, TimeSpan duration)
    {
        RecordingStopped?.Invoke(this, new RecordingStoppedEventArgs
        {
            SessionId = sessionId,
            RecordedMacro = macro,
            Duration = duration
        });
    }

    private void OnActionRecorded(Guid sessionId, MacroAction action)
    {
        ActionRecorded?.Invoke(this, new ActionRecordedEventArgs
        {
            SessionId = sessionId,
            Action = action
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Cancel all active recording sessions
                foreach (var sessionId in _activeSessions.Keys.ToList())
                {
                    _ = CancelRecordingAsync(sessionId);
                }

                _activeSessions.Clear();
                _recordedActions.Clear();
            }
            _disposed = true;
        }
    }
}

