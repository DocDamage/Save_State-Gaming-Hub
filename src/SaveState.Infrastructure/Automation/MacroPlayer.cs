using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Automation;

/// <summary>
/// Implementation of macro playback service.
/// </summary>
public class MacroPlayer : IMacroPlayer, IDisposable
{
    private readonly ILogger<MacroPlayer> _logger;
    private readonly IMacroManager _macroManager;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<Guid, MacroPlaybackSession> _activeSessions = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _sessionCancellations = new();
    private bool _disposed;

    public event EventHandler<PlaybackStartedEventArgs>? PlaybackStarted;
    public event EventHandler<PlaybackStoppedEventArgs>? PlaybackStopped;
    public event EventHandler<ActionExecutedEventArgs>? ActionExecuted;
    public event EventHandler<PlaybackErrorEventArgs>? PlaybackError;

    public MacroPlayer(
        ILogger<MacroPlayer> logger,
        IMacroManager macroManager,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _macroManager = macroManager;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MacroPlaybackSession>> StartPlaybackAsync(
        Guid macroId,
        MacroPlaybackConfig config,
        CancellationToken ct = default)
    {
        try
        {
            // Get the macro
            var macroResult = await _macroManager.GetMacroAsync(macroId, ct);
            if (!macroResult.IsSuccess || macroResult.Value is null)
            {
                return Result.Failure<MacroPlaybackSession>(macroResult.Error ?? "Macro not found");
            }

            var macro = macroResult.Value;
            var sessionId = Guid.NewGuid();

            var session = new MacroPlaybackSession(
                Id: sessionId,
                MacroId: macroId,
                Speed: config.Speed,
                StartedAt: _timeProvider.UtcNow,
                Status: new PlaybackStatus(
                    IsPlaying: true,
                    IsPaused: false,
                    Speed: config.Speed,
                    CurrentActionIndex: 0,
                    TotalActions: macro.Actions.Count,
                    Duration: TimeSpan.Zero,
                    EstimatedTimeRemaining: EstimateDuration(macro.Actions, config.Speed)),
                CurrentActionIndex: 0,
                Duration: TimeSpan.Zero);

            _activeSessions[sessionId] = session;
            var cts = new CancellationTokenSource();
            _sessionCancellations[sessionId] = cts;

            OnPlaybackStarted(session);

            // Start playback in background
            _ = Task.Run(() => ExecutePlaybackAsync(session, macro, config, cts.Token), ct);

            _logger.LogInformation("Started macro playback session {SessionId} for macro {MacroId}",
                sessionId, macroId);

            return Result.Success<MacroPlaybackSession>(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start macro playback for macro {MacroId}", macroId);
            return Result.Failure<MacroPlaybackSession>($"Failed to start playback: {ex.Message}");
        }
    }

    public Task<Result> StopPlaybackAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (_sessionCancellations.TryGetValue(sessionId, out var cts))
            {
                cts.Cancel();
                _sessionCancellations.Remove(sessionId);
            }

            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                var duration = _timeProvider.UtcNow - session.StartedAt;
                _activeSessions.Remove(sessionId);
                OnPlaybackStopped(sessionId, true, duration);
                _logger.LogInformation("Stopped macro playback session {SessionId}", sessionId);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop macro playback session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure($"Failed to stop playback: {ex.Message}"));
        }
    }

    public Task<Result> PausePlaybackAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result.Failure("Playback session not found"));
            }

            var updatedStatus = session.Status with { IsPaused = true };
            var updatedSession = session with { Status = updatedStatus };
            _activeSessions[sessionId] = updatedSession;

            _logger.LogInformation("Paused macro playback session {SessionId}", sessionId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause macro playback session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure($"Failed to pause playback: {ex.Message}"));
        }
    }

    public Task<Result> ResumePlaybackAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result.Failure("Playback session not found"));
            }

            var updatedStatus = session.Status with { IsPaused = false };
            var updatedSession = session with { Status = updatedStatus };
            _activeSessions[sessionId] = updatedSession;

            _logger.LogInformation("Resumed macro playback session {SessionId}", sessionId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume macro playback session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure($"Failed to resume playback: {ex.Message}"));
        }
    }

    public Task<Result<PlaybackStatus>> GetPlaybackStatusAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result.Failure<PlaybackStatus>("Playback session not found"));
            }

            var currentDuration = _timeProvider.UtcNow - session.StartedAt;
            var updatedStatus = session.Status with { Duration = currentDuration };

            return Task.FromResult(Result.Success<PlaybackStatus>(updatedStatus));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playback status for session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure<PlaybackStatus>($"Failed to get status: {ex.Message}"));
        }
    }

    public Task<Result<IReadOnlyList<MacroPlaybackSession>>> GetActiveSessionsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var sessions = (IReadOnlyList<MacroPlaybackSession>)_activeSessions.Values.ToArray();
            return Task.FromResult(Result.Success<IReadOnlyList<MacroPlaybackSession>>(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active playback sessions");
            return Task.FromResult(Result.Failure<IReadOnlyList<MacroPlaybackSession>>(
                $"Failed to get sessions: {ex.Message}"));
        }
    }

    public async Task<Result<MacroValidationResult>> ValidateMacroAsync(
        Guid macroId,
        CancellationToken ct = default)
    {
        try
        {
            var macroResult = await _macroManager.GetMacroAsync(macroId, ct);
            if (!macroResult.IsSuccess || macroResult.Value is null)
            {
                return Result.Failure<MacroValidationResult>(macroResult.Error ?? "Macro not found");
            }

            var macro = macroResult.Value;
            var errors = new List<string>();
            var warnings = new List<string>();

            // Basic validation
            if (!macro.Actions.Any())
            {
                errors.Add("Macro contains no actions");
            }

            // Check for potentially problematic actions
            foreach (var action in macro.Actions)
            {
                if (action is SystemAction sysAction &&
                    (sysAction.SystemCommand.Contains("del") || sysAction.SystemCommand.Contains("rm")))
                {
                    warnings.Add($"Action may delete files: {sysAction.SystemCommand}");
                }
            }

            var estimatedDuration = EstimateDuration(macro.Actions, PlaybackSpeed.Normal);

            var result = new MacroValidationResult(
                IsValid: !errors.Any(),
                Errors: errors,
                Warnings: warnings,
                EstimatedDuration: estimatedDuration);

            return Result.Success<MacroValidationResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate macro {MacroId}", macroId);
            return Result.Failure<MacroValidationResult>($"Failed to validate macro: {ex.Message}");
        }
    }

    private async Task ExecutePlaybackAsync(
        MacroPlaybackSession session,
        Macro macro,
        MacroPlaybackConfig config,
        CancellationToken ct)
    {
        try
        {
            var startTime = _timeProvider.UtcNow;
            var lastActionTime = startTime;

            for (int i = 0; i < macro.Actions.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var action = macro.Actions[i];

                // Update session status
                var updatedStatus = session.Status with { CurrentActionIndex = i };
                var updatedSession = session with { Status = updatedStatus, CurrentActionIndex = i };
                _activeSessions[session.Id] = updatedSession;

                // Calculate delay based on action timing and playback speed
                var delay = CalculateDelay(action.Timestamp, lastActionTime, config.Speed);
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, ct);
                }

                // Execute the action
                var success = await ExecuteActionAsync(action, ct);

                // Notify of action execution
                OnActionExecuted(session.Id, action, success);

                if (!success)
                {
                    OnPlaybackError(session.Id, new Exception($"Failed to execute action {action.ActionType}"),
                        action);
                    break;
                }

                lastActionTime = _timeProvider.UtcNow;
            }

            var duration = _timeProvider.UtcNow - startTime;
            OnPlaybackStopped(session.Id, true, duration);
        }
        catch (OperationCanceledException)
        {
            var duration = _timeProvider.UtcNow - session.StartedAt;
            OnPlaybackStopped(session.Id, false, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during macro playback session {SessionId}", session.Id);
            OnPlaybackError(session.Id, ex, null);
        }
        finally
        {
            _activeSessions.Remove(session.Id);
            _sessionCancellations.Remove(session.Id);
        }
    }

    private async Task<bool> ExecuteActionAsync(MacroAction action, CancellationToken ct)
    {
        try
        {
            // This is a simplified implementation
            // In a real implementation, this would interact with input simulation libraries
            _logger.LogDebug("Executing action {ActionType}", action.ActionType);

            // Simulate action execution time
            await Task.Delay(10, ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute action {ActionType}", action.ActionType);
            return false;
        }
    }

    private TimeSpan CalculateDelay(
        TimeSpan actionTimestamp,
        DateTime lastActionTime,
        PlaybackSpeed speed)
    {
        // Simplified delay calculation
        var baseDelay = TimeSpan.FromMilliseconds(100);

        return speed switch
        {
            PlaybackSpeed.Slow => baseDelay * 2,
            PlaybackSpeed.Normal => baseDelay,
            PlaybackSpeed.Fast => baseDelay / 2,
            PlaybackSpeed.Instant => TimeSpan.Zero,
            _ => baseDelay
        };
    }

    private TimeSpan EstimateDuration(IReadOnlyList<MacroAction> actions, PlaybackSpeed speed)
    {
        if (!actions.Any()) return TimeSpan.Zero;

        // Rough estimation based on action count and speed
        var baseTimePerAction = TimeSpan.FromMilliseconds(150);
        var totalBaseTime = actions.Count * baseTimePerAction;

        return speed switch
        {
            PlaybackSpeed.Slow => totalBaseTime * 2,
            PlaybackSpeed.Normal => totalBaseTime,
            PlaybackSpeed.Fast => totalBaseTime / 2,
            PlaybackSpeed.Instant => TimeSpan.FromMilliseconds(actions.Count * 10),
            _ => totalBaseTime
        };
    }

    private void OnPlaybackStarted(MacroPlaybackSession session)
    {
        PlaybackStarted?.Invoke(this, new PlaybackStartedEventArgs { Session = session });
    }

    private void OnPlaybackStopped(Guid sessionId, bool completedSuccessfully, TimeSpan duration)
    {
        PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs
        {
            SessionId = sessionId,
            CompletedSuccessfully = completedSuccessfully,
            Duration = duration
        });
    }

    private void OnActionExecuted(Guid sessionId, MacroAction action, bool success)
    {
        ActionExecuted?.Invoke(this, new ActionExecutedEventArgs
        {
            SessionId = sessionId,
            Action = action,
            Success = success
        });
    }

    private void OnPlaybackError(Guid sessionId, Exception exception, MacroAction? failedAction)
    {
        PlaybackError?.Invoke(this, new PlaybackErrorEventArgs
        {
            SessionId = sessionId,
            Exception = exception,
            FailedAction = failedAction
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
                // Cancel all active playback sessions
                foreach (var cts in _sessionCancellations.Values)
                {
                    cts.Cancel();
                }

                _activeSessions.Clear();
                _sessionCancellations.Clear();
            }
            _disposed = true;
        }
    }
}

