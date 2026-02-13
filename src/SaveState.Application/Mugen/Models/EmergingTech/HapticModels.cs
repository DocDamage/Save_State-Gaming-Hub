namespace SaveState.Application.Mugen.Models.EmergingTech;

/// <summary>
/// Haptic feedback device information.
/// </summary>
public class HapticDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<HapticActuator> Actuators { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string FirmwareVersion { get; set; } = string.Empty;
}

/// <summary>
/// Haptic device registration request.
/// </summary>
public class HapticDeviceRegistration
{
    public string DeviceType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int ActuatorCount { get; set; }
    public string FirmwareVersion { get; set; } = string.Empty;
}

/// <summary>
/// Individual haptic actuator.
/// </summary>
public class HapticActuator
{
    public int ActuatorId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public HapticActuatorConfig Config { get; set; } = new();
}

/// <summary>
/// Haptic actuator configuration.
/// </summary>
public class HapticActuatorConfig
{
    public float MinFrequency { get; set; }
    public float MaxFrequency { get; set; }
    public float MinAmplitude { get; set; }
    public float MaxAmplitude { get; set; }
}

/// <summary>
/// Haptic feedback request.
/// </summary>
public class HapticFeedbackRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string ActuatorId { get; set; } = string.Empty;
    public float Intensity { get; set; }
    public float Duration { get; set; }
    public string Pattern { get; set; } = string.Empty;
}

/// <summary>
/// Haptic feedback pattern.
/// </summary>
public class HapticPattern
{
    public string PatternId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<HapticPatternStep> Steps { get; set; } = new();
}

/// <summary>
/// Individual step in a haptic pattern.
/// </summary>
public class HapticPatternStep
{
    public float Intensity { get; set; }
    public float Frequency { get; set; }
    public float Duration { get; set; }
}

/// <summary>
/// Haptic pattern request.
/// </summary>
public class HapticPatternRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string PatternId { get; set; } = string.Empty;
    public float Scale { get; set; } = 1.0f;
}

/// <summary>
/// Haptic sequence (multiple patterns).
/// </summary>
public class HapticSequence
{
    public string SequenceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> PatternIds { get; set; } = new();
    public float DelayBetweenPatterns { get; set; }
}
