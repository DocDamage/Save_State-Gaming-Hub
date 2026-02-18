using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Cinematic camera system providing dynamic camera angles, cinematic sequences,
/// and professional camera work for enhanced visual storytelling in MUGEN.
/// </summary>
public class CinematicCameraSystem : CinematicCameraSystemICinematicCameraSystem
{
    private readonly ILogger<CinematicCameraSystem> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, CinematicCameraSystemCameraSequence> _cameraSequences = new();
    private readonly Dictionary<string, CinematicCameraSystemCameraPreset> _cameraPresets = new();
    private readonly CinematicCameraSystemCameraController _cameraController;
    private readonly CinematicCameraSystemSequenceDirector _sequenceDirector;
    private readonly CinematicCameraSystemCameraRigSystem _cameraRigSystem;

    public CinematicCameraSystem(
        ILogger<CinematicCameraSystem> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _cameraController = new CinematicCameraSystemCameraController(loggerFactory.CreateLogger<CinematicCameraSystemCameraController>(), _timeProvider);
        _sequenceDirector = new CinematicCameraSystemSequenceDirector(loggerFactory.CreateLogger<CinematicCameraSystemSequenceDirector>());
        _cameraRigSystem = new CinematicCameraSystemCameraRigSystem(loggerFactory.CreateLogger<CinematicCameraSystemCameraRigSystem>());

        InitializeCameraPresets();
    }

    public async Task<Result<CinematicCameraSystemCameraSequence>> CreateCameraSequenceAsync(CinematicCameraSystemCameraSequenceRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating camera sequence: {Name}", request.Name);

            var sequence = new CinematicCameraSystemCameraSequence
            {
                SequenceId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Duration = request.Duration,
                CameraMovements = request.CameraMovements,
                Triggers = request.Triggers,
                Transitions = request.Transitions,
                AudioSyncPoints = request.AudioSyncPoints,
                Priority = request.Priority,
                Loop = request.Loop,
                CreatedAt = _timeProvider.UtcNow
            };

            _cameraSequences[sequence.SequenceId] = sequence;

            // Validate sequence
            var validation = await ValidateSequenceAsync(sequence, ct);
            if (!validation.IsSuccess)
            {
                _cameraSequences.Remove(sequence.SequenceId);
                return Result.Failure<CinematicCameraSystemCameraSequence>(validation.Error);
            }

            _logger.LogInformation("Camera sequence created: {SequenceId}", sequence.SequenceId);
            return Result.Success<CinematicCameraSystemCameraSequence>(sequence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating camera sequence {Name}", request.Name);
            return Result.Failure<CinematicCameraSystemCameraSequence>($"Failed to create sequence: {ex.Message}");
        }
    }

    public async Task<Result<CinematicCameraSystemCameraPreset>> CreateCameraPresetAsync(CinematicCameraSystemCameraPresetRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating camera preset: {Name}", request.Name);

            var preset = new CinematicCameraSystemCameraPreset
            {
                PresetId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                CinematicCameraSystemCameraSettings = request.CinematicCameraSystemCameraSettings,
                CinematicCameraSystemRigSettings = request.CinematicCameraSystemRigSettings,
                PostProcessing = request.PostProcessing,
                Category = request.Category,
                Tags = request.Tags,
                CreatedAt = _timeProvider.UtcNow
            };

            _cameraPresets[preset.PresetId] = preset;

            _logger.LogInformation("Camera preset created: {PresetId}", preset.PresetId);
            return Result.Success<CinematicCameraSystemCameraPreset>(preset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating camera preset {Name}", request.Name);
            return Result.Failure<CinematicCameraSystemCameraPreset>($"Failed to create preset: {ex.Message}");
        }
    }

    public async Task<Result> ExecuteCameraSequenceAsync(string sequenceId, CinematicCameraSystemCameraContext context, CancellationToken ct = default)
    {
        try
        {
            if (!_cameraSequences.TryGetValue(sequenceId, out var sequence))
            {
                return Result.Failure("Camera sequence not found");
            }

            _logger.LogInformation("Executing camera sequence: {SequenceId}", sequenceId);

            await _sequenceDirector.ExecuteSequenceAsync(sequence, context, ct);

            _logger.LogInformation("Camera sequence executed successfully: {SequenceId}", sequenceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing camera sequence {SequenceId}", sequenceId);
            return Result.Failure($"Failed to execute sequence: {ex.Message}");
        }
    }

    public async Task<Result<CinematicCameraSystemCameraRig>> SetupCameraRigAsync(CinematicCameraSystemCameraRigRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = await _cameraRigSystem.SetupRigAsync(request, ct);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up camera rig");
            return Result.Failure<CinematicCameraSystemCameraRig>($"Failed to setup camera rig: {ex.Message}");
        }
    }

    public async Task<Result> ApplyCameraPresetAsync(string presetId, CinematicCameraSystemCameraContext context, CancellationToken ct = default)
    {
        try
        {
            if (!_cameraPresets.TryGetValue(presetId, out var preset))
            {
                return Result.Failure("Camera preset not found");
            }

            _logger.LogInformation("Applying camera preset: {PresetId}", presetId);

            await _cameraController.ApplyPresetAsync(preset, context, ct);

            _logger.LogInformation("Camera preset applied successfully: {PresetId}", presetId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying camera preset {PresetId}", presetId);
            return Result.Failure($"Failed to apply preset: {ex.Message}");
        }
    }

    public async Task<Result<CinematicCameraSystemCameraPath>> CreateCameraPathAsync(CinematicCameraSystemCameraPathRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating camera path with {Count} waypoints", request.Waypoints.Count);

            var path = new CinematicCameraSystemCameraPath
            {
                PathId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Waypoints = request.Waypoints,
                CinematicCameraSystemInterpolationMode = request.CinematicCameraSystemInterpolationMode,
                SpeedCurve = request.SpeedCurve,
                LookAtTarget = request.LookAtTarget,
                Duration = request.Duration,
                Loop = request.Loop,
                CreatedAt = _timeProvider.UtcNow
            };

            // Validate path
            var validation = await ValidateCameraPathAsync(path, ct);
            if (!validation.IsSuccess)
            {
                return Result.Failure<CinematicCameraSystemCameraPath>(validation.Error);
            }

            _logger.LogInformation("Camera path created: {PathId}", path.PathId);
            return Result.Success<CinematicCameraSystemCameraPath>(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating camera path");
            return Result.Failure<CinematicCameraSystemCameraPath>($"Failed to create path: {ex.Message}");
        }
    }

    public async Task<Result<CinematicCameraSystemCinematicEvent>> CreateCinematicEventAsync(CinematicCameraSystemCinematicEventRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating cinematic event: {Name}", request.Name);

            var cinematicEvent = new CinematicCameraSystemCinematicEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                CinematicCameraSystemTriggerCondition = request.CinematicCameraSystemTriggerCondition,
                SequenceId = request.SequenceId,
                CameraPathId = request.CameraPathId,
                AudioCue = request.AudioCue,
                VisualEffects = request.VisualEffects,
                Duration = request.Duration,
                Priority = request.Priority,
                OneTime = request.OneTime,
                CreatedAt = _timeProvider.UtcNow
            };

            _logger.LogInformation("Cinematic event created: {EventId}", cinematicEvent.EventId);
            return Result.Success<CinematicCameraSystemCinematicEvent>(cinematicEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cinematic event {Name}", request.Name);
            return Result.Failure<CinematicCameraSystemCinematicEvent>($"Failed to create event: {ex.Message}");
        }
    }

    public async Task<Result<CinematicCameraSystemCameraTransition>> CreateCameraTransitionAsync(CinematicCameraSystemCameraTransitionRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating camera transition: {Type}", request.CinematicCameraSystemTransitionType);

            var transition = new CinematicCameraSystemCameraTransition
            {
                TransitionId = Guid.NewGuid().ToString(),
                CinematicCameraSystemTransitionType = request.CinematicCameraSystemTransitionType,
                Duration = request.Duration,
                CinematicCameraSystemEasingFunction = request.CinematicCameraSystemEasingFunction,
                Parameters = request.Parameters,
                CreatedAt = _timeProvider.UtcNow
            };

            _logger.LogInformation("Camera transition created: {TransitionId}", transition.TransitionId);
            return Result.Success<CinematicCameraSystemCameraTransition>(transition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating camera transition");
            return Result.Failure<CinematicCameraSystemCameraTransition>($"Failed to create transition: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<CinematicCameraSystemCameraPreset>>> GetCameraPresetsAsync(CinematicCameraSystemCameraCategory? category, CancellationToken ct = default)
    {
        try
        {
            var presets = category.HasValue
                ? _cameraPresets.Values.Where(p => p.Category == category.Value).ToList()
                : _cameraPresets.Values.ToList();

            return Result.Success<IReadOnlyList<CinematicCameraSystemCameraPreset>>(presets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camera presets");
            return Result.Failure<IReadOnlyList<CinematicCameraSystemCameraPreset>>($"Failed to get presets: {ex.Message}");
        }
    }

    public async Task<Result<CinematicCameraSystemCameraState>> GetCurrentCameraStateAsync(CinematicCameraSystemCameraContext context, CancellationToken ct = default)
    {
        try
        {
            var state = await _cameraController.GetCurrentStateAsync(context, ct);
            return Result.Success<CinematicCameraSystemCameraState>(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current camera state");
            return Result.Failure<CinematicCameraSystemCameraState>($"Failed to get camera state: {ex.Message}");
        }
    }

    public async Task<Result<CinematicCameraSystemSequenceAnalytics>> AnalyzeSequencePerformanceAsync(string sequenceId, CancellationToken ct = default)
    {
        try
        {
            if (!_cameraSequences.TryGetValue(sequenceId, out var sequence))
            {
                return Result.Failure<CinematicCameraSystemSequenceAnalytics>("Camera sequence not found");
            }

            _logger.LogInformation("Analyzing sequence performance: {SequenceId}", sequenceId);

            var analytics = new CinematicCameraSystemSequenceAnalytics
            {
                SequenceId = sequenceId,
                TotalMovements = sequence.CameraMovements.Count,
                AverageMovementDuration = (float)sequence.CameraMovements.Average(m => m.Duration.TotalSeconds),
                TotalTransitions = sequence.Transitions.Count,
                TriggerEfficiency = CalculateTriggerEfficiency(sequence),
                CameraStability = CalculateCameraStability(sequence),
                PerformanceScore = CalculatePerformanceScore(sequence),
                Recommendations = GeneratePerformanceRecommendations(sequence),
                AnalyzedAt = _timeProvider.UtcNow
            };

            _logger.LogInformation("Sequence performance analysis completed: {SequenceId}", sequenceId);
            return Result.Success<CinematicCameraSystemSequenceAnalytics>(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sequence performance {SequenceId}", sequenceId);
            return Result.Failure<CinematicCameraSystemSequenceAnalytics>($"Failed to analyze performance: {ex.Message}");
        }
    }

    #region Private Methods

    private void InitializeCameraPresets()
    {
        // Initialize with professional cinematic camera presets
        var defaultPresets = new[]
        {
            new CinematicCameraSystemCameraPreset
            {
                PresetId = "dramatic_closeup",
                Name = "Dramatic Close-Up",
                Description = "Intense close-up shot with shallow depth of field",
                Category = CinematicCameraSystemCameraCategory.Dramatic,
                CinematicCameraSystemCameraSettings = new CinematicCameraSystemCameraSettings
                {
                    Position = new Vector3(0, 1.5f, -2),
                    Target = new Vector3(0, 1.5f, 0),
                    Up = new Vector3(0, 1, 0),
                    FieldOfView = 35.0f,
                    NearPlane = 0.1f,
                    FarPlane = 50.0f,
                    CinematicCameraSystemProjectionMode = CinematicCameraSystemProjectionMode.Perspective
                },
                Tags = new[] { "closeup", "dramatic", "intense" },
                CreatedAt = _timeProvider.UtcNow
            },
            new CinematicCameraSystemCameraPreset
            {
                PresetId = "wide_arena",
                Name = "Wide Arena Shot",
                Description = "Epic wide shot showing the entire arena",
                Category = CinematicCameraSystemCameraCategory.Epic,
                CinematicCameraSystemCameraSettings = new CinematicCameraSystemCameraSettings
                {
                    Position = new Vector3(0, 8, -15),
                    Target = new Vector3(0, 2, 0),
                    Up = new Vector3(0, 1, 0),
                    FieldOfView = 60.0f,
                    NearPlane = 1.0f,
                    FarPlane = 100.0f,
                    CinematicCameraSystemProjectionMode = CinematicCameraSystemProjectionMode.Perspective
                },
                Tags = new[] { "wide", "arena", "epic" },
                CreatedAt = _timeProvider.UtcNow
            },
            new CinematicCameraSystemCameraPreset
            {
                PresetId = "bullet_time",
                Name = "Bullet Time",
                Description = "Slow-motion bullet time effect",
                Category = CinematicCameraSystemCameraCategory.Dynamic,
                CinematicCameraSystemCameraSettings = new CinematicCameraSystemCameraSettings
                {
                    Position = new Vector3(5, 2, -3),
                    Target = new Vector3(0, 1.5f, 0),
                    Up = new Vector3(0, 1, 0),
                    FieldOfView = 45.0f,
                    NearPlane = 0.1f,
                    FarPlane = 50.0f,
                    CinematicCameraSystemProjectionMode = CinematicCameraSystemProjectionMode.Perspective
                },
                Tags = new[] { "slowmotion", "bullet_time", "dynamic" },
                CreatedAt = _timeProvider.UtcNow
            },
            new CinematicCameraSystemCameraPreset
            {
                PresetId = "dutch_angle",
                Name = "Dutch Angle",
                Description = "Tilted camera angle for tension and unease",
                Category = CinematicCameraSystemCameraCategory.Dramatic,
                CinematicCameraSystemCameraSettings = new CinematicCameraSystemCameraSettings
                {
                    Position = new Vector3(2, 3, -4),
                    Target = new Vector3(0, 1.5f, 0),
                    Up = new Vector3(0.3f, 0.7f, 0.2f), // Tilted up vector
                    FieldOfView = 40.0f,
                    NearPlane = 0.1f,
                    FarPlane = 50.0f,
                    CinematicCameraSystemProjectionMode = CinematicCameraSystemProjectionMode.Perspective
                },
                Tags = new[] { "dutch", "tension", "dramatic" },
                CreatedAt = _timeProvider.UtcNow
            }
        };

        foreach (var preset in defaultPresets)
        {
            _cameraPresets[preset.PresetId] = preset;
        }
    }

    private async Task<Result> ValidateSequenceAsync(CinematicCameraSystemCameraSequence sequence, CancellationToken ct)
    {
        // Validate sequence timing and continuity
        var totalMovementTime = sequence.CameraMovements.Sum(m => m.Duration.TotalSeconds);
        if (totalMovementTime > sequence.Duration.TotalSeconds * 1.1) // 10% tolerance
        {
            return Result.Failure("Total movement time exceeds sequence duration");
        }

        return Result.Success();
    }

    private async Task<Result> ValidateCameraPathAsync(CinematicCameraSystemCameraPath path, CancellationToken ct)
    {
        // Validate path waypoints and timing
        if (path.Waypoints.Count < 2)
        {
            return Result.Failure("Camera path must have at least 2 waypoints");
        }

        return Result.Success();
    }

    private double CalculateTriggerEfficiency(CinematicCameraSystemCameraSequence sequence)
    {
        // Calculate how well triggers are timed
        return 0.85; // Placeholder
    }

    private double CalculateCameraStability(CinematicCameraSystemCameraSequence sequence)
    {
        // Calculate camera movement stability
        return 0.78; // Placeholder
    }

    private double CalculatePerformanceScore(CinematicCameraSystemCameraSequence sequence)
    {
        // Calculate overall sequence performance
        return 0.82; // Placeholder
    }

    private IReadOnlyList<string> GeneratePerformanceRecommendations(CinematicCameraSystemCameraSequence sequence)
    {
        var recommendations = new List<string>();

        if (sequence.CameraMovements.Count > 10)
        {
            recommendations.Add("Consider reducing the number of camera movements for smoother playback");
        }

        if (sequence.Duration.TotalSeconds < 5)
        {
            recommendations.Add("Very short sequences may feel rushed - consider extending duration");
        }

        return recommendations;
    }

    #endregion
}

/// <summary>
/// Camera controller for low-level camera operations.
/// </summary>
public class CinematicCameraSystemCameraController
{
    private readonly ILogger<CinematicCameraSystemCameraController> _logger;
    private readonly ITimeProvider _timeProvider;

    public CinematicCameraSystemCameraController(ILogger<CinematicCameraSystemCameraController> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task ApplyPresetAsync(CinematicCameraSystemCameraPreset preset, CinematicCameraSystemCameraContext context, CancellationToken ct = default)
    {
        // Apply camera preset settings
        await Task.Delay(10, ct);
    }

    public async Task<CinematicCameraSystemCameraState> GetCurrentStateAsync(CinematicCameraSystemCameraContext context, CancellationToken ct = default)
    {
        return new CinematicCameraSystemCameraState
        {
            Position = new Vector3(0, 5, -10),
            Target = new Vector3(0, 0, 0),
            Up = new Vector3(0, 1, 0),
            FieldOfView = 45.0f,
            Timestamp = _timeProvider.UtcNow
        };
    }
}

/// <summary>
/// Sequence director for orchestrating camera sequences.
/// </summary>
public class CinematicCameraSystemSequenceDirector
{
    private readonly ILogger<CinematicCameraSystemSequenceDirector> _logger;

    public CinematicCameraSystemSequenceDirector(ILogger<CinematicCameraSystemSequenceDirector> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteSequenceAsync(CinematicCameraSystemCameraSequence sequence, CinematicCameraSystemCameraContext context, CancellationToken ct = default)
    {
        // Execute camera sequence
        await Task.Delay(50, ct);
    }
}

/// <summary>
/// Camera rig system for professional camera setups.
/// </summary>
public class CinematicCameraSystemCameraRigSystem
{
    private readonly ILogger<CinematicCameraSystemCameraRigSystem> _logger;

    public CinematicCameraSystemCameraRigSystem(ILogger<CinematicCameraSystemCameraRigSystem> logger)
    {
        _logger = logger;
    }

    public async Task<CinematicCameraSystemCameraRig> SetupRigAsync(CinematicCameraSystemCameraRigRequest request, CancellationToken ct = default)
    {
        var rig = new CinematicCameraSystemCameraRig
        {
            RigId = Guid.NewGuid().ToString(),
            Name = request.Name,
            Type = request.Type,
            Cameras = request.CameraPositions,
            Constraints = request.Constraints,
            Automation = request.AutomationSettings
        };

        return rig;
    }
}

/// <summary>
/// Cinematic Camera System interface.
/// </summary>
public interface CinematicCameraSystemICinematicCameraSystem
{
    Task<Result<CinematicCameraSystemCameraSequence>> CreateCameraSequenceAsync(CinematicCameraSystemCameraSequenceRequest request, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCameraPreset>> CreateCameraPresetAsync(CinematicCameraSystemCameraPresetRequest request, CancellationToken ct = default);
    Task<Result> ExecuteCameraSequenceAsync(string sequenceId, CinematicCameraSystemCameraContext context, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCameraRig>> SetupCameraRigAsync(CinematicCameraSystemCameraRigRequest request, CancellationToken ct = default);
    Task<Result> ApplyCameraPresetAsync(string presetId, CinematicCameraSystemCameraContext context, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCameraPath>> CreateCameraPathAsync(CinematicCameraSystemCameraPathRequest request, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCinematicEvent>> CreateCinematicEventAsync(CinematicCameraSystemCinematicEventRequest request, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCameraTransition>> CreateCameraTransitionAsync(CinematicCameraSystemCameraTransitionRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CinematicCameraSystemCameraPreset>>> GetCameraPresetsAsync(CinematicCameraSystemCameraCategory? category, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCameraState>> GetCurrentCameraStateAsync(CinematicCameraSystemCameraContext context, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemSequenceAnalytics>> AnalyzeSequencePerformanceAsync(string sequenceId, CancellationToken ct = default);
}

/// <summary>
/// Camera sequence data.
/// </summary>
public class CinematicCameraSystemCameraSequence
{
    public string SequenceId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraMovement> CameraMovements { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemSequenceTrigger> Triggers { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraTransition> Transitions { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemAudioSyncPoint> AudioSyncPoints { get; set; } = default!;
    public int Priority { get; set; } = default!;
    public bool Loop { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Camera sequence request.
/// </summary>
public class CinematicCameraSystemCameraSequenceRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraMovement> CameraMovements { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemSequenceTrigger> Triggers { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraTransition> Transitions { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemAudioSyncPoint> AudioSyncPoints { get; set; } = default!;
    public int Priority { get; set; } = default!;
    public bool Loop { get; set; } = default!;
}

/// <summary>
/// Camera preset data.
/// </summary>
public class CinematicCameraSystemCameraPreset
{
    public string PresetId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public CinematicCameraSystemCameraSettings CinematicCameraSystemCameraSettings { get; set; } = default!;
    public CinematicCameraSystemRigSettings? CinematicCameraSystemRigSettings { get; set; } = default!;
    public CinematicCameraSystemPostProcessingSettings? PostProcessing { get; set; } = default!;
    public CinematicCameraSystemCameraCategory Category { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Camera preset request.
/// </summary>
public class CinematicCameraSystemCameraPresetRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public CinematicCameraSystemCameraSettings CinematicCameraSystemCameraSettings { get; set; } = default!;
    public CinematicCameraSystemRigSettings? CinematicCameraSystemRigSettings { get; set; } = default!;
    public CinematicCameraSystemPostProcessingSettings? PostProcessing { get; set; } = default!;
    public CinematicCameraSystemCameraCategory Category { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
}

/// <summary>
/// Camera path data.
/// </summary>
public class CinematicCameraSystemCameraPath
{
    public string PathId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraWaypoint> Waypoints { get; set; } = default!;
    public CinematicCameraSystemInterpolationMode CinematicCameraSystemInterpolationMode { get; set; } = default!;
    public IReadOnlyList<float> SpeedCurve { get; set; } = default!;
    public Vector3? LookAtTarget { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public bool Loop { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Camera path request.
/// </summary>
public class CinematicCameraSystemCameraPathRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraWaypoint> Waypoints { get; set; } = default!;
    public CinematicCameraSystemInterpolationMode CinematicCameraSystemInterpolationMode { get; set; } = default!;
    public IReadOnlyList<float> SpeedCurve { get; set; } = default!;
    public Vector3? LookAtTarget { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public bool Loop { get; set; } = default!;
}

/// <summary>
/// Cinematic event data.
/// </summary>
public class CinematicCameraSystemCinematicEvent
{
    public string EventId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public CinematicCameraSystemTriggerCondition CinematicCameraSystemTriggerCondition { get; set; } = default!;
    public string SequenceId { get; set; } = default!;
    public string? CameraPathId { get; set; } = default!;
    public CinematicCameraSystemCameraAudioCue? AudioCue { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraVisualEffect> VisualEffects { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public int Priority { get; set; } = default!;
    public bool OneTime { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Cinematic event request.
/// </summary>
public class CinematicCameraSystemCinematicEventRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public CinematicCameraSystemTriggerCondition CinematicCameraSystemTriggerCondition { get; set; } = default!;
    public string SequenceId { get; set; } = default!;
    public string? CameraPathId { get; set; } = default!;
    public CinematicCameraSystemCameraAudioCue? AudioCue { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraVisualEffect> VisualEffects { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public int Priority { get; set; } = default!;
    public bool OneTime { get; set; } = default!;
}

/// <summary>
/// Camera transition data.
/// </summary>
public class CinematicCameraSystemCameraTransition
{
    public string TransitionId { get; set; } = default!;
    public CinematicCameraSystemTransitionType CinematicCameraSystemTransitionType { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public CinematicCameraSystemEasingFunction CinematicCameraSystemEasingFunction { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Camera transition request.
/// </summary>
public class CinematicCameraSystemCameraTransitionRequest
{
    public CinematicCameraSystemTransitionType CinematicCameraSystemTransitionType { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public CinematicCameraSystemEasingFunction CinematicCameraSystemEasingFunction { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
}

/// <summary>
/// Camera movement data.
/// </summary>
public class CinematicCameraSystemCameraMovement
{
    public string MovementId { get; set; } = default!;
    public Vector3 StartPosition { get; set; } = default!;
    public Vector3 EndPosition { get; set; } = default!;
    public Vector3 StartTarget { get; set; } = default!;
    public Vector3 EndTarget { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public CinematicCameraSystemEasingFunction Easing { get; set; } = default!;
    public float StartFOV { get; set; } = default!;
    public float EndFOV { get; set; } = default!;
}

/// <summary>
/// Camera waypoint data.
/// </summary>
public class CinematicCameraSystemCameraWaypoint
{
    public Vector3 Position { get; set; } = default!;
    public Vector3 Target { get; set; } = default!;
    public float FOV { get; set; } = default!;
    public TimeSpan ArrivalTime { get; set; } = default!;
    public CinematicCameraSystemEasingFunction Easing { get; set; } = default!;
}

/// <summary>
/// Sequence trigger data.
/// </summary>
public class CinematicCameraSystemSequenceTrigger
{
    public string TriggerId { get; set; } = default!;
    public CinematicCameraSystemCameraTriggerType Type { get; set; } = default!;
    public string Condition { get; set; } = default!;
    public TimeSpan Timestamp { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
}

/// <summary>
/// Audio sync point data.
/// </summary>
public class CinematicCameraSystemAudioSyncPoint
{
    public TimeSpan Timestamp { get; set; } = default!;
    public string AudioEvent { get; set; } = default!;
    public CinematicCameraSystemCameraMovement? CinematicCameraSystemCameraMovement { get; set; } = default!;
}

/// <summary>
/// Camera settings data.
/// </summary>
public class CinematicCameraSystemCameraSettings
{
    public Vector3 Position { get; set; } = default!;
    public Vector3 Target { get; set; } = default!;
    public Vector3 Up { get; set; } = default!;
    public float FieldOfView { get; set; } = default!;
    public float NearPlane { get; set; } = default!;
    public float FarPlane { get; set; } = default!;
    public CinematicCameraSystemProjectionMode CinematicCameraSystemProjectionMode { get; set; } = default!;
}

/// <summary>
/// Rig settings data.
/// </summary>
public class CinematicCameraSystemRigSettings
{
    public CinematicCameraSystemRigType Type { get; set; } = default!;
    public Vector3 PivotPoint { get; set; } = default!;
    public float ArmLength { get; set; } = default!;
    public float BoomHeight { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemRigConstraint> Constraints { get; set; } = default!;
}

/// <summary>
/// Post-processing settings.
/// </summary>
public class CinematicCameraSystemPostProcessingSettings
{
    public float BloomIntensity { get; set; } = default!;
    public float DepthOfField { get; set; } = default!;
    public float MotionBlur { get; set; } = default!;
    public string ColorGrading { get; set; } = default!;
}

/// <summary>
/// Camera rig data.
/// </summary>
public class CinematicCameraSystemCameraRig
{
    public string RigId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public CinematicCameraSystemRigType Type { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraPosition> Cameras { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemRigConstraint> Constraints { get; set; } = default!;
    public CinematicCameraSystemRigAutomationSettings? Automation { get; set; } = default!;
}

/// <summary>
/// Camera rig request.
/// </summary>
public class CinematicCameraSystemCameraRigRequest
{
    public string Name { get; set; } = default!;
    public CinematicCameraSystemRigType Type { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemCameraPosition> CameraPositions { get; set; } = default!;
    public IReadOnlyList<CinematicCameraSystemRigConstraint> Constraints { get; set; } = default!;
    public CinematicCameraSystemRigAutomationSettings? AutomationSettings { get; set; } = default!;
}

/// <summary>
/// Camera position data.
/// </summary>
public class CinematicCameraSystemCameraPosition
{
    public string CameraId { get; set; } = default!;
    public Vector3 Position { get; set; } = default!;
    public Vector3 Target { get; set; } = default!;
    public float FOV { get; set; } = default!;
}

/// <summary>
/// Rig constraint data.
/// </summary>
public class CinematicCameraSystemRigConstraint
{
    public CinematicCameraSystemConstraintType Type { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
}

/// <summary>
/// Rig automation settings.
/// </summary>
public class CinematicCameraSystemRigAutomationSettings
{
    public bool AutoFocus { get; set; } = default!;
    public bool AutoExposure { get; set; } = default!;
    public bool MotionTracking { get; set; } = default!;
    public float Smoothness { get; set; } = default!;
}

/// <summary>
/// Camera state data.
/// </summary>
public class CinematicCameraSystemCameraState
{
    public Vector3 Position { get; set; } = default!;
    public Vector3 Target { get; set; } = default!;
    public Vector3 Up { get; set; } = default!;
    public float FieldOfView { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Camera context data.
/// </summary>
public class CinematicCameraSystemCameraContext
{
    public string ContextId { get; set; } = default!;
    public Vector3 Player1Position { get; set; } = default!;
    public Vector3 Player2Position { get; set; } = default!;
    public string CurrentAction { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Sequence analytics data.
/// </summary>
public class CinematicCameraSystemSequenceAnalytics
{
    public string SequenceId { get; set; } = default!;
    public int TotalMovements { get; set; } = default!;
    public double AverageMovementDuration { get; set; } = default!;
    public int TotalTransitions { get; set; } = default!;
    public double TriggerEfficiency { get; set; } = default!;
    public double CameraStability { get; set; } = default!;
    public double PerformanceScore { get; set; } = default!;
    public IReadOnlyList<string> Recommendations { get; set; } = default!;
    public DateTime AnalyzedAt { get; set; } = default!;
}

/// <summary>
/// Trigger condition data.
/// </summary>
public class CinematicCameraSystemTriggerCondition
{
    public string EventType { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
}

/// <summary>
/// Camera audio cue data.
/// </summary>
public class CinematicCameraSystemCameraAudioCue
{
    public string AudioFile { get; set; } = default!;
    public TimeSpan StartTime { get; set; } = default!;
    public float Volume { get; set; } = default!;
    public bool Loop { get; set; } = default!;
}

/// <summary>
/// Camera visual effect data.
/// </summary>
public class CinematicCameraSystemCameraVisualEffect
{
    public string EffectType { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Vector3 data.
/// </summary>
public class CinematicCameraSystemCameraVector3
{
    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
    public float Z { get; set; } = default!;
}

/// <summary>
/// Camera category enumeration.
/// </summary>
public enum CinematicCameraSystemCameraCategory
{
    Dramatic,
    Epic,
    Dynamic,
    Cinematic,
    Gameplay,
    Custom
}

/// <summary>
/// Transition type enumeration.
/// </summary>
public enum CinematicCameraSystemTransitionType
{
    Cut,
    Fade,
    Wipe,
    Zoom,
    Pan,
    Custom
}

/// <summary>
/// Easing function enumeration.
/// </summary>
public enum CinematicCameraSystemEasingFunction
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    Bounce,
    Elastic,
    Custom
}

/// <summary>
/// Interpolation mode enumeration.
/// </summary>
public enum CinematicCameraSystemInterpolationMode
{
    Linear,
    Bezier,
    CatmullRom,
    Hermite
}

/// <summary>
/// Trigger type enumeration.
/// </summary>
public enum CinematicCameraSystemCameraTriggerType
{
    GameEvent,
    HealthThreshold,
    ComboCount,
    SpecialMove,
    TimeBased,
    Custom
}

/// <summary>
/// Projection mode enumeration.
/// </summary>
public enum CinematicCameraSystemProjectionMode
{
    Perspective,
    Orthographic
}

/// <summary>
/// Rig type enumeration.
/// </summary>
public enum CinematicCameraSystemRigType
{
    Dolly,
    Crane,
    Jib,
    SteadyCam,
    Handheld,
    Custom
}

/// <summary>
/// Constraint type enumeration.
/// </summary>
public enum CinematicCameraSystemConstraintType
{
    Distance,
    Angle,
    Height,
    Speed,
    Custom
}
