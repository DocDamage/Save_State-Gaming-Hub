using SaveState.Core.Common;

namespace SaveState.Core.Input.Services;

public interface ITouchController
{
    Task<Result<TouchProfile>> CreateProfileAsync(Guid gameId, TouchConfig config, CancellationToken ct = default);
    Task<Result> CalibrateTouchAsync(CancellationToken ct = default);
    Task<Result<TouchProfile?>> GetCurrentProfileAsync(Guid gameId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TouchProfile>>> GetProfilesForGameAsync(Guid gameId, CancellationToken ct = default);
    Task<Result> DeleteProfileAsync(Guid profileId, CancellationToken ct = default);
    bool IsCalibrated { get; }
    event EventHandler<TouchGestureEventArgs>? GestureDetected;
    event EventHandler<TouchCalibrationEventArgs>? CalibrationCompleted;
}

public sealed record TouchProfile(
    Guid Id,
    Guid GameId,
    string Name,
    TouchConfig Config,
    TouchCalibrationData CalibrationData,
    DateTime CreatedAt);

public sealed record TouchConfig(
    TouchControllerSensitivity Sensitivity,
    GestureSet EnabledGestures,
    bool EnableHapticFeedback,
    bool EnablePalmRejection,
    int DeadZoneRadius,
    int MultiTouchThreshold);

public sealed record TouchCalibrationData(
    Point TopLeft,
    Point TopRight,
    Point BottomLeft,
    Point BottomRight,
    Point Center,
    double CalibrationAccuracy,
    DateTime CalibratedAt);

public sealed record Point(double X, double Y);

[Flags]
public enum GestureSet
{
    None = 0,
    Tap = 1 << 0,
    DoubleTap = 1 << 1,
    Swipe = 1 << 2,
    Pinch = 1 << 3,
    LongPress = 1 << 4,
    TwoFingerTap = 1 << 5,
    ThreeFingerSwipe = 1 << 6,
    Rotate = 1 << 7,
    EdgeSwipe = 1 << 8
}

public enum TouchControllerSensitivity { Low, Medium, High, Maximum }

public sealed class TouchGestureEventArgs : EventArgs
{
    public Guid GameId { get; init; }
    public TouchGesture Gesture { get; init; } = default!;
    public required Point Position { get; init; }
    public DateTime Timestamp { get; init; }
}

public sealed record TouchGesture(
    GestureType Type,
    Point StartPosition,
    Point EndPosition,
    TimeSpan Duration,
    double Distance,
    double Angle,
    int FingerCount);

public enum GestureType
{
    Tap,
    DoubleTap,
    SwipeLeft,
    SwipeRight,
    SwipeUp,
    SwipeDown,
    PinchIn,
    PinchOut,
    LongPress,
    TwoFingerTap,
    ThreeFingerSwipe,
    Rotate,
    EdgeSwipeLeft,
    EdgeSwipeRight
}

public sealed class TouchCalibrationEventArgs : EventArgs
{
    public bool Success { get; init; }
    public double Accuracy { get; init; }
    public string? ErrorMessage { get; init; }
}
