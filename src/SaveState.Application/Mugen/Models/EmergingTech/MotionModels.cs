using System.Numerics;

namespace SaveState.Application.Mugen.Models.EmergingTech;

/// <summary>
/// Motion controller device information.
/// </summary>
public class MotionController
{
    public string ControllerId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ControllerType { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
    public MotionCalibration CalibrationData { get; set; } = new();
    public MotionSensitivity Sensitivity { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime LastUsed { get; set; }
    public string FirmwareVersion { get; set; } = string.Empty;
}

/// <summary>
/// Motion controller registration request.
/// </summary>
public class MotionControllerRegistration
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ControllerType { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
    public string FirmwareVersion { get; set; } = string.Empty;
}

/// <summary>
/// Raw motion data from sensors.
/// </summary>
public class RawMotionData
{
    public Vector3 Accelerometer { get; set; }
    public Vector3 Gyroscope { get; set; }
    public Vector3 Magnetometer { get; set; }
    public float Timestamp { get; set; }
}

/// <summary>
/// Processed motion data.
/// </summary>
public class MotionData
{
    public string ControllerId { get; set; } = string.Empty;
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Velocity { get; set; }
    public Vector3 Acceleration { get; set; }
    public float Confidence { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Detected motion gesture.
/// </summary>
public class MotionGesture
{
    public string GestureType { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public float Duration { get; set; }
    public Vector3 Direction { get; set; }
}

/// <summary>
/// Motion calibration data.
/// </summary>
public class MotionCalibration
{
    public Vector3 AccelerometerBias { get; set; }
    public Vector3 GyroscopeBias { get; set; }
    public Vector3 MagnetometerBias { get; set; }
    public DateTime CalibrationDate { get; set; }
}

/// <summary>
/// Motion sensitivity configuration.
/// </summary>
public class MotionSensitivity
{
    public float AccelerationThreshold { get; set; }
    public float RotationThreshold { get; set; }
    public float SpeedThreshold { get; set; }
}

/// <summary>
/// Result of a calibration operation.
/// </summary>
public class MotionCalibrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public MotionCalibration Calibration { get; set; } = new();
}

/// <summary>
/// Calibration sequence configuration.
/// </summary>
public class CalibrationSequence
{
    public string SequenceId { get; set; } = string.Empty;
    public string SequenceType { get; set; } = string.Empty;
    public List<CalibrationStep> Steps { get; set; } = new();
    public int CurrentStep { get; set; }
}

/// <summary>
/// Individual calibration step.
/// </summary>
public class CalibrationStep
{
    public int StepNumber { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public float Duration { get; set; }
    public bool IsCompleted { get; set; }
}
