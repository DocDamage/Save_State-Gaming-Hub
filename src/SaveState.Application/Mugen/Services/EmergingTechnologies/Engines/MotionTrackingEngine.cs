namespace SaveState.Application.Mugen.Services.EmergingTechnologies.Engines;

using System.Numerics;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.EmergingTech;
using SaveState.Core.Common.Services;
using MugenVector3 = SaveState.Application.Mugen.Vector3;

/// <summary>
/// Engine for motion tracking and gesture detection.
/// </summary>
public class MotionTrackingEngine
{
    private readonly ILogger<MotionTrackingEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public MotionTrackingEngine(ILogger<MotionTrackingEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processes raw motion data from a controller.
    /// </summary>
    public Task<MotionData> ProcessMotionDataAsync(
        MotionController controller,
        RawMotionData rawData,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Processing motion data for controller {ControllerId}", controller.ControllerId);

        // Apply calibration offsets - convert to Mugen.Vector3 for operations
        MugenVector3 accel = rawData.Accelerometer;
        MugenVector3 gyro = rawData.Gyroscope;
        MugenVector3 accelBias = controller.CalibrationData.AccelerometerBias;
        MugenVector3 gyroBias = controller.CalibrationData.GyroscopeBias;
        
        accel -= accelBias;
        gyro -= gyroBias;

        // Calculate position (simplified integration)
        var position = new MugenVector3(
            accel.X * 0.5,
            accel.Y * 0.5,
            accel.Z * 0.5
        );

        // Calculate rotation from gyroscope
        var rotation = new MugenVector3(
            gyro.X * rawData.Timestamp,
            gyro.Y * rawData.Timestamp,
            gyro.Z * rawData.Timestamp
        );

        var motionData = new MotionData
        {
            ControllerId = controller.ControllerId,
            Position = position,
            Rotation = rotation,
            Velocity = accel * rawData.Timestamp,
            Acceleration = accel,
            Confidence = 0.9f,
            Timestamp = _timeProvider.UtcNow
        };

        return Task.FromResult(motionData);
    }

    /// <summary>
    /// Detects gestures from motion history.
    /// </summary>
    public Task<List<MotionGesture>> DetectGesturesAsync(
        List<MotionData> motionHistory,
        CancellationToken ct = default)
    {
        var gestures = new List<MotionGesture>();

        if (motionHistory.Count < 2)
            return Task.FromResult(gestures);

        // Detect punch (fast forward motion)
        var latest = motionHistory[^1];
        var previous = motionHistory[^2];
        var velocity = latest.Velocity.Length();
        var prevVelocity = previous.Velocity.Length();

        if (velocity > 2.0 && prevVelocity < velocity)
        {
            gestures.Add(new MotionGesture
            {
                GestureType = "Punch",
                Confidence = (float)Math.Min(velocity / 5.0, 1.0),
                Duration = 0.2f,
                Direction = latest.Velocity.Normalize()
            });
        }

        // Detect block (defensive position)
        if (latest.Position.Y > 0.5 && velocity < 0.5)
        {
            gestures.Add(new MotionGesture
            {
                GestureType = "Block",
                Confidence = 0.8f,
                Duration = 0.5f,
                Direction = new MugenVector3(0, 0, 1)
            });
        }

        return Task.FromResult(gestures);
    }
}
