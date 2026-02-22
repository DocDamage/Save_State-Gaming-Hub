using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Interface for the cinematic camera system.
/// </summary>
public interface ICinematicCameraSystem
{
    Task<Result<CinematicCameraSystemCameraSequence>> CreateSequenceAsync(CinematicCameraSystemCameraSequenceRequest request, CancellationToken ct = default);
    Task<Result> PlaySequenceAsync(string sequenceId, CancellationToken ct = default);
    Task<Result> StopSequenceAsync(string sequenceId, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCameraPreset>> CreatePresetAsync(CinematicCameraSystemCameraPresetRequest request, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCameraPath>> CreatePathAsync(CinematicCameraSystemCameraPathRequest request, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCinematicEvent>> TriggerEventAsync(CinematicCameraSystemCinematicEventRequest request, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCameraTransition>> CreateTransitionAsync(CinematicCameraSystemCameraTransitionRequest request, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemCameraRig>> CreateRigAsync(CinematicCameraSystemCameraRigRequest request, CancellationToken ct = default);
    Task<Result<CinematicCameraSystemSequenceAnalytics>> GetAnalyticsAsync(string sequenceId, CancellationToken ct = default);
}

// Data Classes - preserving original names for backward compatibility

public class CinematicCameraSystemCameraSequence
{
    public string SequenceId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CinematicCameraSystemCameraMovement> Movements { get; set; } = new();
    public List<CinematicCameraSystemCameraTransition> Transitions { get; set; } = new();
    public List<CinematicCameraSystemCinematicEvent> Events { get; set; } = new();
    public List<CinematicCameraSystemSequenceTrigger> Triggers { get; set; } = new();
    public CinematicCameraSystemCameraSettings CameraSettings { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public bool IsLooping { get; set; }
    public bool IsPlaying { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CinematicCameraSystemCameraSequenceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CinematicCameraSystemCameraMovement> Movements { get; set; } = new();
    public List<CinematicCameraSystemCameraTransition> Transitions { get; set; } = new();
    public List<CinematicCameraSystemCinematicEvent> Events { get; set; } = new();
    public CinematicCameraSystemCameraSettings CameraSettings { get; set; } = new();
    public bool IsLooping { get; set; }
}

public class CinematicCameraSystemCameraPreset
{
    public string PresetId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public CinematicCameraSystemCameraCategory Category { get; set; }
    public CinematicCameraSystemCameraPosition Position { get; set; } = new();
    public CinematicCameraSystemCameraSettings Settings { get; set; } = new();
    public CinematicCameraSystemPostProcessingSettings PostProcessing { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CinematicCameraSystemCameraPresetRequest
{
    public string Name { get; set; } = string.Empty;
    public CinematicCameraSystemCameraCategory Category { get; set; }
    public CinematicCameraSystemCameraPosition Position { get; set; } = new();
    public CinematicCameraSystemCameraSettings Settings { get; set; } = new();
}

public class CinematicCameraSystemCameraPath
{
    public string PathId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<CinematicCameraSystemCameraWaypoint> Waypoints { get; set; } = new();
    public CinematicCameraSystemInterpolationMode InterpolationMode { get; set; }
    public bool IsClosedLoop { get; set; }
    public TimeSpan Duration { get; set; }
}

public class CinematicCameraSystemCameraPathRequest
{
    public string Name { get; set; } = string.Empty;
    public List<CinematicCameraSystemCameraWaypoint> Waypoints { get; set; } = new();
    public CinematicCameraSystemInterpolationMode InterpolationMode { get; set; }
    public bool IsClosedLoop { get; set; }
    public TimeSpan Duration { get; set; }
}

public class CinematicCameraSystemCinematicEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public TimeSpan TriggerTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool HasTriggered { get; set; }
}

public class CinematicCameraSystemCinematicEventRequest
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan TriggerTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class CinematicCameraSystemCameraTransition
{
    public string TransitionId { get; set; } = Guid.NewGuid().ToString();
    public CinematicCameraSystemTransitionType TransitionType { get; set; }
    public CinematicCameraSystemEasingFunction EasingFunction { get; set; }
    public TimeSpan Duration { get; set; }
    public CinematicCameraSystemCameraPosition StartPosition { get; set; } = new();
    public CinematicCameraSystemCameraPosition EndPosition { get; set; } = new();
}

public class CinematicCameraSystemCameraTransitionRequest
{
    public CinematicCameraSystemTransitionType TransitionType { get; set; }
    public CinematicCameraSystemEasingFunction EasingFunction { get; set; }
    public TimeSpan Duration { get; set; }
    public CinematicCameraSystemCameraPosition StartPosition { get; set; } = new();
    public CinematicCameraSystemCameraPosition EndPosition { get; set; } = new();
}

public class CinematicCameraSystemCameraMovement
{
    public string MovementId { get; set; } = Guid.NewGuid().ToString();
    public CinematicCameraSystemCameraPosition StartPosition { get; set; } = new();
    public CinematicCameraSystemCameraPosition EndPosition { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public CinematicCameraSystemEasingFunction Easing { get; set; }
    public bool UsePath { get; set; }
    public string? PathId { get; set; }
}

public class CinematicCameraSystemCameraWaypoint
{
    public int Index { get; set; }
    public CinematicCameraSystemCameraVector3 Position { get; set; } = new();
    public CinematicCameraSystemCameraVector3 Rotation { get; set; } = new();
    public float FieldOfView { get; set; } = 60f;
    public CinematicCameraSystemEasingFunction EasingIn { get; set; }
    public CinematicCameraSystemEasingFunction EasingOut { get; set; }
}

public class CinematicCameraSystemSequenceTrigger
{
    public string TriggerId { get; set; } = Guid.NewGuid().ToString();
    public CinematicCameraSystemCameraTriggerType TriggerType { get; set; }
    public CinematicCameraSystemTriggerCondition Condition { get; set; } = new();
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> ActionParameters { get; set; } = new();
}

public class CinematicCameraSystemTriggerCondition
{
    public string ConditionType { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public object TargetValue { get; set; } = new();
}

public class CinematicCameraSystemAudioSyncPoint
{
    public TimeSpan Timestamp { get; set; }
    public string AudioCueId { get; set; } = string.Empty;
    public float Volume { get; set; } = 1.0f;
    public bool FadeIn { get; set; }
    public TimeSpan FadeDuration { get; set; }
}

public class CinematicCameraSystemCameraSettings
{
    public float FieldOfView { get; set; } = 60f;
    public float NearClipPlane { get; set; } = 0.1f;
    public float FarClipPlane { get; set; } = 1000f;
    public CinematicCameraSystemProjectionMode ProjectionMode { get; set; }
    public float OrthographicSize { get; set; } = 5f;
}

public class CinematicCameraSystemRigSettings
{
    public CinematicCameraSystemRigType RigType { get; set; }
    public List<CinematicCameraSystemRigConstraint> Constraints { get; set; } = new();
    public CinematicCameraSystemRigAutomationSettings Automation { get; set; } = new();
}

public class CinematicCameraSystemPostProcessingSettings
{
    public float BloomIntensity { get; set; }
    public float MotionBlurAmount { get; set; }
    public float DepthOfFieldFocus { get; set; }
    public float DepthOfFieldAperture { get; set; }
    public float VignetteIntensity { get; set; }
    public float ChromaticAberration { get; set; }
}

public class CinematicCameraSystemCameraRig
{
    public string RigId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public CinematicCameraSystemRigSettings Settings { get; set; } = new();
    public CinematicCameraSystemCameraPosition BasePosition { get; set; } = new();
    public List<CinematicCameraSystemCamera> Cameras { get; set; } = new();
}

public class CinematicCameraSystemCameraRigRequest
{
    public string Name { get; set; } = string.Empty;
    public CinematicCameraSystemRigSettings Settings { get; set; } = new();
    public CinematicCameraSystemCameraPosition BasePosition { get; set; } = new();
}

public class CinematicCameraSystemCameraPosition
{
    public CinematicCameraSystemCameraVector3 Position { get; set; } = new();
    public CinematicCameraSystemCameraVector3 Rotation { get; set; } = new();
    public float FieldOfView { get; set; } = 60f;
}

public class CinematicCameraSystemRigConstraint
{
    public CinematicCameraSystemConstraintType ConstraintType { get; set; }
    public string TargetId { get; set; } = string.Empty;
    public CinematicCameraSystemCameraVector3 Offset { get; set; } = new();
    public float Weight { get; set; } = 1.0f;
}

public class CinematicCameraSystemRigAutomationSettings
{
    public bool AutoTrackTarget { get; set; }
    public string? TargetId { get; set; }
    public float TrackingSpeed { get; set; } = 1.0f;
    public bool AutoFocus { get; set; }
    public float FocusSpeed { get; set; } = 1.0f;
}

public class CinematicCameraSystemCameraState
{
    public string CameraId { get; set; } = Guid.NewGuid().ToString();
    public CinematicCameraSystemCameraPosition CurrentPosition { get; set; } = new();
    public CinematicCameraSystemCameraSettings Settings { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime LastUpdate { get; set; }
}

public class CinematicCameraSystemCameraContext
{
    public string SequenceId { get; set; } = string.Empty;
    public TimeSpan CurrentTime { get; set; }
    public CinematicCameraSystemCameraState CurrentState { get; set; } = new();
    public List<CinematicCameraSystemCinematicEvent> PendingEvents { get; set; } = new();
}

public class CinematicCameraSystemSequenceAnalytics
{
    public string SequenceId { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public DateTime LastPlayed { get; set; }
    public List<CinematicCameraSystemCameraAudioCue> AudioCues { get; set; } = new();
    public List<CinematicCameraSystemCameraVisualEffect> VisualEffects { get; set; } = new();
}

public class CinematicCameraSystemCameraAudioCue
{
    public string CueId { get; set; } = Guid.NewGuid().ToString();
    public TimeSpan TriggerTime { get; set; }
    public string AudioClipId { get; set; } = string.Empty;
    public float Volume { get; set; } = 1.0f;
}

public class CinematicCameraSystemCameraVisualEffect
{
    public string EffectId { get; set; } = Guid.NewGuid().ToString();
    public TimeSpan TriggerTime { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public float Intensity { get; set; } = 1.0f;
    public TimeSpan Duration { get; set; }
}

public class CinematicCameraSystemCameraVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public CinematicCameraSystemCameraVector3(float x = 0, float y = 0, float z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public class CinematicCameraSystemCamera
{
    public string CameraId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public CinematicCameraSystemCameraPosition Position { get; set; } = new();
    public CinematicCameraSystemCameraSettings Settings { get; set; } = new();
}

// Enums

public enum CinematicCameraSystemCameraCategory
{
    Static,
    Dynamic,
    Follow,
    Orbit,
    Dolly,
    Crane,
    Handheld,
    Drone,
    Custom
}

public enum CinematicCameraSystemTransitionType
{
    Cut,
    Fade,
    Dissolve,
    Wipe,
    Slide,
    Zoom,
    Pan,
    Tilt,
    Blend
}

public enum CinematicCameraSystemEasingFunction
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    Spring,
    Bounce,
    Elastic
}

public enum CinematicCameraSystemInterpolationMode
{
    Linear,
    Bezier,
    CatmullRom,
    Hermite
}

public enum CinematicCameraSystemCameraTriggerType
{
    Manual,
    Automatic,
    OnEvent,
    OnCondition,
    OnTime,
    OnPosition
}

public enum CinematicCameraSystemProjectionMode
{
    Perspective,
    Orthographic
}

public enum CinematicCameraSystemRigType
{
    Static,
    Dynamic,
    Crane,
    Dolly,
    Steadicam,
    Drone,
    Virtual
}

public enum CinematicCameraSystemConstraintType
{
    None,
    LookAt,
    Follow,
    Orbit,
    Distance,
    Angle
}
