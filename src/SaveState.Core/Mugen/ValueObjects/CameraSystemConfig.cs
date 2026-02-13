namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Configuration for dynamic camera angles and cinematic sequences.
/// </summary>
public record CameraSystemConfig
{
    /// <summary>
    /// Enables dynamic camera movement.
    /// </summary>
    public bool EnableDynamicCamera { get; init; } = true;

    /// <summary>
    /// Default camera behavior mode.
    /// </summary>
    public CameraMode DefaultMode { get; init; } = CameraMode.Follow;

    /// <summary>
    /// Smoothing factor for camera movement (0.0 to 1.0).
    /// </summary>
    public float SmoothingFactor { get; init; } = 0.1f;

    /// <summary>
    /// Dead zone where camera doesn't move.
    /// </summary>
    public CameraSize DeadZone { get; init; } = new(50.0f, 30.0f);

    /// <summary>
    /// Camera bounds to keep within.
    /// </summary>
    public CameraBounds Bounds { get; init; } = new();

    /// <summary>
    /// Cinematic sequences triggered by events.
    /// </summary>
    public IReadOnlyList<CinematicSequence> Sequences { get; init; } = Array.Empty<CinematicSequence>();

    /// <summary>
    /// Special camera effects.
    /// </summary>
    public IReadOnlyList<CameraEffect> Effects { get; init; } = Array.Empty<CameraEffect>();

    /// <summary>
    /// Zoom and field of view settings.
    /// </summary>
    public CameraZoom ZoomSettings { get; init; } = new();
}

/// <summary>
/// Camera behavior modes.
/// </summary>
public enum CameraMode
{
    /// <summary>
    /// Camera follows players dynamically.
    /// </summary>
    Follow,

    /// <summary>
    /// Camera stays centered between players.
    /// </summary>
    Center,

    /// <summary>
    /// Camera focuses on action hotspots.
    /// </summary>
    ActionFocus,

    /// <summary>
    /// Fixed camera position.
    /// </summary>
    Fixed,

    /// <summary>
    /// Cinematic camera for special moments.
    /// </summary>
    Cinematic
}

/// <summary>
/// Camera movement bounds.
/// </summary>
public record CameraBounds
{
    /// <summary>
    /// Minimum X position.
    /// </summary>
    public float MinX { get; init; } = -1000.0f;

    /// <summary>
    /// Maximum X position.
    /// </summary>
    public float MaxX { get; init; } = 1000.0f;

    /// <summary>
    /// Minimum Y position.
    /// </summary>
    public float MinY { get; init; } = -500.0f;

    /// <summary>
    /// Maximum Y position.
    /// </summary>
    public float MaxY { get; init; } = 500.0f;

    /// <summary>
    /// Whether to enforce bounds strictly.
    /// </summary>
    public bool EnforceBounds { get; init; } = true;
}

/// <summary>
/// Cinematic camera sequences.
/// </summary>
public record CinematicSequence
{
    /// <summary>
    /// Name of the sequence for identification.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Trigger event that starts this sequence.
    /// </summary>
    public SequenceTrigger Trigger { get; init; }

    /// <summary>
    /// Sequence of camera movements.
    /// </summary>
    public IReadOnlyList<CameraMovement> Movements { get; init; } = Array.Empty<CameraMovement>();

    /// <summary>
    /// Total duration of the sequence in seconds.
    /// </summary>
    public float Duration { get; init; } = 3.0f;

    /// <summary>
    /// Whether this sequence can be interrupted.
    /// </summary>
    public bool Interruptible { get; init; } = false;
}

/// <summary>
/// Triggers for cinematic sequences.
/// </summary>
public record SequenceTrigger
{
    /// <summary>
    /// Type of trigger event.
    /// </summary>
    public TriggerType EventType { get; init; }

    /// <summary>
    /// Specific conditions for triggering.
    /// </summary>
    public string Condition { get; init; } = string.Empty;

    /// <summary>
    /// Delay before sequence starts after trigger.
    /// </summary>
    public float Delay { get; init; } = 0.0f;
}

/// <summary>
/// Individual camera movement in a sequence.
/// </summary>
public record CameraMovement
{
    /// <summary>
    /// Target position for the camera.
    /// </summary>
    public CameraPosition TargetPosition { get; init; } = new();

    /// <summary>
    /// Target zoom level.
    /// </summary>
    public float TargetZoom { get; init; } = 1.0f;

    /// <summary>
    /// Duration of this movement segment.
    /// </summary>
    public float Duration { get; init; } = 1.0f;

    /// <summary>
    /// Easing function for smooth movement.
    /// </summary>
    public EasingFunction Easing { get; init; } = EasingFunction.Linear;

    /// <summary>
    /// Focus target (player, position, etc.).
    /// </summary>
    public CameraFocus Focus { get; init; } = new();
}

/// <summary>
/// Easing functions for camera movement.
/// </summary>
public enum EasingFunction
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    Bounce,
    Elastic,
    Cubic
}

/// <summary>
/// Camera focus target.
/// </summary>
public record CameraFocus
{
    /// <summary>
    /// Type of focus target.
    /// </summary>
    public FocusType Type { get; init; } = FocusType.Position;

    /// <summary>
    /// Target identifier (player number, position, etc.).
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Offset from the target.
    /// </summary>
    public CameraPosition Offset { get; init; } = new();
}

/// <summary>
/// Types of camera focus targets.
/// </summary>
public enum FocusType
{
    Position,
    Player1,
    Player2,
    Center,
    Action
}

/// <summary>
/// Special camera effects.
/// </summary>
public record CameraEffect
{
    /// <summary>
    /// Type of camera effect.
    /// </summary>
    public CameraEffectType Type { get; init; }

    /// <summary>
    /// Intensity of the effect.
    /// </summary>
    public float Intensity { get; init; } = 1.0f;

    /// <summary>
    /// Duration of the effect.
    /// </summary>
    public float Duration { get; init; } = 1.0f;

    /// <summary>
    /// Effect-specific parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Types of camera effects.
/// </summary>
public enum CameraEffectType
{
    Shake,
    Zoom,
    Rotate,
    Blur,
    ChromaticAberration,
    SlowMotion,
    SpeedRamp
}

/// <summary>
/// Camera zoom and field of view settings.
/// </summary>
public record CameraZoom
{
    /// <summary>
    /// Default zoom level.
    /// </summary>
    public float DefaultZoom { get; init; } = 1.0f;

    /// <summary>
    /// Minimum allowed zoom level.
    /// </summary>
    public float MinZoom { get; init; } = 0.5f;

    /// <summary>
    /// Maximum allowed zoom level.
    /// </summary>
    public float MaxZoom { get; init; } = 2.0f;

    /// <summary>
    /// Zoom speed for smooth transitions.
    /// </summary>
    public float ZoomSpeed { get; init; } = 2.0f;

    /// <summary>
    /// Field of view angle in degrees.
    /// </summary>
    public float FieldOfView { get; init; } = 60.0f;

    /// <summary>
    /// Whether zoom affects field of view.
    /// </summary>
    public bool ZoomAffectsFov { get; init; } = true;
}

/// <summary>
/// Position for camera.
/// </summary>
public record CameraPosition(float X = 0, float Y = 0);

/// <summary>
/// Size for camera components.
/// </summary>
public record CameraSize(float Width = 0, float Height = 0);
