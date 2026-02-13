using SaveState.Core.Common;

namespace SaveState.Core.Social.MobileRemote;

/// <summary>
/// Service for processing controller input from mobile devices.
/// </summary>
public interface IControllerInputService
{
    /// <summary>
    /// Maps mobile input to virtual controller state.
    /// </summary>
    Task<Result<VirtualControllerState>> MapInputAsync(ControllerInput input, ControllerMappingProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Gets the current controller state for a device.
    /// </summary>
    Task<Result<VirtualControllerState>> GetControllerStateAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Registers a custom controller mapping profile.
    /// </summary>
    Task<Result<ControllerMappingProfile>> RegisterMappingProfileAsync(CreateMappingProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a controller mapping profile.
    /// </summary>
    Task<Result<ControllerMappingProfile>> GetMappingProfileAsync(string profileId, CancellationToken ct = default);

    /// <summary>
    /// Lists all available mapping profiles.
    /// </summary>
    Task<Result<IReadOnlyList<ControllerMappingProfile>>> GetMappingProfilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the active mapping profile for a device.
    /// </summary>
    Task<Result> SetActiveProfileAsync(string deviceId, string profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets the default mapping profiles for different controller types.
    /// </summary>
    Task<Result<IReadOnlyList<ControllerMappingProfile>>> GetDefaultProfilesAsync(ControllerType type, CancellationToken ct = default);

    /// <summary>
    /// Calibrates motion controls for a device.
    /// </summary>
    Task<Result<MotionCalibration>> CalibrateMotionAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Processes touch input gesture.
    /// </summary>
    Task<Result<GestureResult>> ProcessTouchGestureAsync(TouchGesture gesture, CancellationToken ct = default);

    /// <summary>
    /// Gets input latency statistics.
    /// </summary>
    Task<Result<InputLatencyStats>> GetLatencyStatsAsync(string deviceId, CancellationToken ct = default);
}

/// <summary>
/// Controller input from a mobile device.
/// </summary>
public sealed record ControllerInput(
    string DeviceId,
    InputType Type,
    DateTime Timestamp,
    ButtonState[]? Buttons = null,
    AnalogStick[]? Sticks = null,
    TouchPoint[]? Touches = null,
    MotionData? Motion = null);

/// <summary>
/// Virtual controller state after mapping.
/// </summary>
public sealed record VirtualControllerState(
    string DeviceId,
    DateTime Timestamp,
    ButtonState[] Buttons,
    AnalogStick[] Sticks,
    TriggerState[] Triggers,
    bool HasMotionData,
    MotionData? Motion = null);

/// <summary>
/// Controller mapping profile.
/// </summary>
public sealed record ControllerMappingProfile(
    string Id,
    string Name,
    ControllerType Type,
    InputLayout Layout,
    IReadOnlyList<ButtonMapping> ButtonMappings,
    IReadOnlyList<StickMapping> StickMappings,
    MotionMapping? MotionMapping,
    TouchMapping? TouchMapping,
    bool IsDefault,
    DateTime CreatedAt);

/// <summary>
/// Request to create a mapping profile.
/// </summary>
public sealed record CreateMappingProfileRequest(
    string Name,
    ControllerType Type,
    InputLayout Layout,
    List<ButtonMapping> ButtonMappings,
    List<StickMapping> StickMappings,
    MotionMapping? MotionMapping = null,
    TouchMapping? TouchMapping = null);

/// <summary>
/// Button state.
/// </summary>
public sealed record ButtonState(
    string Id,
    bool IsPressed,
    double Pressure = 1.0);

/// <summary>
/// Analog stick state.
/// </summary>
public sealed record AnalogStick(
    string Id,
    double X,
    double Y,
    double Magnitude,
    double Angle);

/// <summary>
/// Trigger state.
/// </summary>
public sealed record TriggerState(
    string Id,
    double Value);

/// <summary>
/// Touch point.
/// </summary>
public sealed record TouchPoint(
    int Id,
    double X,
    double Y,
    double Pressure,
    TouchPhase Phase);

/// <summary>
/// Motion data from device sensors.
/// </summary>
public sealed record MotionData(
    double AccelerometerX,
    double AccelerometerY,
    double AccelerometerZ,
    double GyroscopeX,
    double GyroscopeY,
    double GyroscopeZ,
    double? MagnetometerX = null,
    double? MagnetometerY = null,
    double? MagnetometerZ = null);

/// <summary>
/// Button mapping.
/// </summary>
public sealed record ButtonMapping(
    string SourceId,
    string TargetButton,
    ButtonAction Action,
    ModifierKey[]? Modifiers = null);

/// <summary>
/// Stick mapping.
/// </summary>
public sealed record StickMapping(
    string SourceId,
    string TargetStick,
    double Deadzone,
    bool InvertX = false,
    bool InvertY = false);

/// <summary>
/// Motion control mapping.
/// </summary>
public sealed record MotionMapping(
    bool Enabled,
    double Sensitivity,
    MotionMode Mode,
    string? TargetStick = null);

/// <summary>
/// Touch mapping configuration.
/// </summary>
public sealed record TouchMapping(
    bool Enabled,
    int MaxTouchPoints,
    bool UseGestures,
    IReadOnlyList<TouchZone>? Zones = null);

/// <summary>
/// Touch zone definition.
/// </summary>
public sealed record TouchZone(
    string Id,
    double X,
    double Y,
    double Width,
    double Height,
    string Action,
    TouchZoneType Type);

/// <summary>
/// Touch gesture.
/// </summary>
public sealed record TouchGesture(
    string DeviceId,
    GestureType Type,
    TouchPoint[] Points,
    double Velocity,
    DateTime Timestamp);

/// <summary>
/// Gesture result.
/// </summary>
public sealed record GestureResult(
    GestureType Type,
    bool Recognized,
    string? MappedAction,
    double Confidence);

/// <summary>
/// Motion calibration data.
/// </summary>
public sealed record MotionCalibration(
    string DeviceId,
    double CenterX,
    double CenterY,
    double CenterZ,
    DateTime CalibratedAt);

/// <summary>
/// Input latency statistics.
/// </summary>
public sealed record InputLatencyStats(
    string DeviceId,
    double AverageLatencyMs,
    double MinLatencyMs,
    double MaxLatencyMs,
    double JitterMs,
    int Samples,
    DateTime CalculatedAt);

/// <summary>
/// Input types.
/// </summary>
public enum InputType
{
    Buttons,
    AnalogSticks,
    Touch,
    Motion,
    Voice
}

/// <summary>
/// Controller types.
/// </summary>
public enum ControllerType
{
    Standard,
    NES,
    SNES,
    Genesis,
    PlayStation,
    Xbox,
    N64,
    GameCube,
    Arcade,
    Custom
}

/// <summary>
/// Input layouts.
/// </summary>
public enum InputLayout
{
    Standard,
    LeftHanded,
    RightHanded,
    Compact,
    Extended
}

/// <summary>
/// Button actions.
/// </summary>
public enum ButtonAction
{
    Press,
    Hold,
    DoubleTap,
    LongPress,
    Toggle
}

/// <summary>
/// Modifier keys.
/// </summary>
public enum ModifierKey
{
    Shift,
    Control,
    Alt,
    Meta
}

/// <summary>
/// Motion control modes.
/// </summary>
public enum MotionMode
{
    Steering,
    Pointer,
    GyroAiming,
    Shake
}

/// <summary>
/// Touch phases.
/// </summary>
public enum TouchPhase
{
    Began,
    Moved,
    Stationary,
    Ended,
    Cancelled
}

/// <summary>
/// Gesture types.
/// </summary>
public enum GestureType
{
    Tap,
    DoubleTap,
    LongPress,
    SwipeLeft,
    SwipeRight,
    SwipeUp,
    SwipeDown,
    Pinch,
    Zoom,
    Rotate
}

/// <summary>
/// Touch zone types.
/// </summary>
public enum TouchZoneType
{
    Button,
    DPad,
    AnalogStick,
    Trigger,
    Special
}
