using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Cinematic camera system providing advanced camera control for MUGEN/IKEMEN matches.
/// Coordinates multiple specialized engines for camera movement, sequences, rigs, paths, and transitions.
/// </summary>
public class CinematicCameraSystem : ICinematicCameraSystem
{
    private readonly ILogger<CinematicCameraSystem> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly CameraControllerEngine _cameraController;
    private readonly SequenceDirectorEngine _sequenceDirector;
    private readonly CameraRigSystemEngine _rigSystem;
    private readonly CameraPathEngine _pathEngine;
    private readonly CameraTransitionEngine _transitionEngine;

    public CinematicCameraSystem(
        ILogger<CinematicCameraSystem> logger,
        ITimeProvider timeProvider,
        CameraControllerEngine cameraController,
        SequenceDirectorEngine sequenceDirector,
        CameraRigSystemEngine rigSystem,
        CameraPathEngine pathEngine,
        CameraTransitionEngine transitionEngine)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _cameraController = cameraController;
        _sequenceDirector = sequenceDirector;
        _rigSystem = rigSystem;
        _pathEngine = pathEngine;
        _transitionEngine = transitionEngine;
    }

    public Task<Result<CinematicCameraSystemCameraSequence>> CreateSequenceAsync(
        CinematicCameraSystemCameraSequenceRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating camera sequence: {Name}", request.Name);
            var sequence = _sequenceDirector.CreateSequence(request);
            return Task.FromResult(Result.Success(sequence));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sequence");
            return Task.FromResult(Result.Failure<CinematicCameraSystemCameraSequence>($"Failed to create sequence: {ex.Message}"));
        }
    }

    public async Task<Result> PlaySequenceAsync(string sequenceId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Playing sequence {SequenceId}", sequenceId);

            await _sequenceDirector.PlaySequenceAsync(
                sequenceId,
                async movement =>
                {
                    if (movement.UsePath && !string.IsNullOrEmpty(movement.PathId))
                    {
                        await PlayPathMovementAsync(movement, ct);
                    }
                    else
                    {
                        await PlayDirectMovementAsync(movement, ct);
                    }
                },
                async evt =>
                {
                    _logger.LogDebug("Triggering event {EventId}: {EventType}", evt.EventId, evt.EventType);
                    await Task.CompletedTask;
                },
                ct);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sequence {SequenceId} was cancelled", sequenceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play sequence {SequenceId}", sequenceId);
            return Result.Failure($"Failed to play sequence: {ex.Message}");
        }
    }

    public Task<Result> StopSequenceAsync(string sequenceId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Stopping sequence {SequenceId}", sequenceId);
            _sequenceDirector.StopSequence(sequenceId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop sequence {SequenceId}", sequenceId);
            return Task.FromResult(Result.Failure($"Failed to stop sequence: {ex.Message}"));
        }
    }

    public Task<Result<CinematicCameraSystemCameraPreset>> CreatePresetAsync(
        CinematicCameraSystemCameraPresetRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating camera preset: {Name}", request.Name);

            var preset = new CinematicCameraSystemCameraPreset
            {
                Name = request.Name,
                Category = request.Category,
                Position = request.Position,
                Settings = request.Settings
            };

            return Task.FromResult(Result.Success(preset));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create preset");
            return Task.FromResult(Result.Failure<CinematicCameraSystemCameraPreset>($"Failed to create preset: {ex.Message}"));
        }
    }

    public Task<Result<CinematicCameraSystemCameraPath>> CreatePathAsync(
        CinematicCameraSystemCameraPathRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating camera path: {Name}", request.Name);
            var path = _pathEngine.CreatePath(request);
            return Task.FromResult(Result.Success(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create path");
            return Task.FromResult(Result.Failure<CinematicCameraSystemCameraPath>($"Failed to create path: {ex.Message}"));
        }
    }

    public Task<Result<CinematicCameraSystemCinematicEvent>> TriggerEventAsync(
        CinematicCameraSystemCinematicEventRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Triggering cinematic event: {Name}", request.Name);

            var evt = new CinematicCameraSystemCinematicEvent
            {
                Name = request.Name,
                TriggerTime = request.TriggerTime,
                EventType = request.EventType,
                Parameters = request.Parameters,
                HasTriggered = true
            };

            return Task.FromResult(Result.Success(evt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger event");
            return Task.FromResult(Result.Failure<CinematicCameraSystemCinematicEvent>($"Failed to trigger event: {ex.Message}"));
        }
    }

    public Task<Result<CinematicCameraSystemCameraTransition>> CreateTransitionAsync(
        CinematicCameraSystemCameraTransitionRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating camera transition: {Type}", request.TransitionType);
            var transition = _transitionEngine.CreateTransition(request);
            return Task.FromResult(Result.Success(transition));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create transition");
            return Task.FromResult(Result.Failure<CinematicCameraSystemCameraTransition>($"Failed to create transition: {ex.Message}"));
        }
    }

    public Task<Result<CinematicCameraSystemCameraRig>> CreateRigAsync(
        CinematicCameraSystemCameraRigRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating camera rig: {Name} of type {Type}", request.Name, request.Settings.RigType);
            var rig = _rigSystem.CreateRig(request);
            return Task.FromResult(Result.Success(rig));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create rig");
            return Task.FromResult(Result.Failure<CinematicCameraSystemCameraRig>($"Failed to create rig: {ex.Message}"));
        }
    }

    public Task<Result<CinematicCameraSystemSequenceAnalytics>> GetAnalyticsAsync(
        string sequenceId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting analytics for sequence {SequenceId}", sequenceId);

            var sequence = _sequenceDirector.GetSequence(sequenceId);
            if (sequence == null)
            {
                return Task.FromResult(Result.Failure<CinematicCameraSystemSequenceAnalytics>("Sequence not found"));
            }

            var analytics = new CinematicCameraSystemSequenceAnalytics
            {
                SequenceId = sequenceId,
                PlayCount = 0, // Would track from a real analytics store
                TotalPlayTime = sequence.Duration,
                LastPlayed = _timeProvider.UtcNow
            };

            return Task.FromResult(Result.Success(analytics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analytics for sequence {SequenceId}", sequenceId);
            return Task.FromResult(Result.Failure<CinematicCameraSystemSequenceAnalytics>($"Failed to get analytics: {ex.Message}"));
        }
    }

    private async Task PlayPathMovementAsync(CinematicCameraSystemCameraMovement movement, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(movement.PathId)) return;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.Elapsed < movement.Duration)
        {
            var t = (float)(stopwatch.Elapsed.TotalMilliseconds / movement.Duration.TotalMilliseconds);
            var position = _pathEngine.GetPositionAtTime(movement.PathId, t);
            _cameraController.UpdatePosition("main", position);
            await Task.Delay(16, ct);
        }
    }

    private async Task PlayDirectMovementAsync(CinematicCameraSystemCameraMovement movement, CancellationToken ct)
    {
        var transition = new CinematicCameraSystemCameraTransitionRequest
        {
            TransitionType = CinematicCameraSystemTransitionType.Blend,
            EasingFunction = movement.Easing,
            Duration = movement.Duration,
            StartPosition = movement.StartPosition,
            EndPosition = movement.EndPosition
        };

        var trans = _transitionEngine.CreateTransition(transition);

        await _transitionEngine.ExecuteTransitionAsync(
            trans,
            async pos =>
            {
                _cameraController.UpdatePosition("main", pos);
                await Task.CompletedTask;
            },
            ct);
    }
}
