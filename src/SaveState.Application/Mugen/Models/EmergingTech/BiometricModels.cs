namespace SaveState.Application.Mugen.Models.EmergingTech;

/// <summary>
/// Biometric input data.
/// </summary>
public class BiometricInput
{
    public string UserId { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty;
    public float Timestamp { get; set; }
    public Dictionary<string, float> Metrics { get; set; } = new();
}

/// <summary>
/// Processed biometric data.
/// </summary>
public class BiometricData
{
    public string UserId { get; set; } = string.Empty;
    public float StressLevel { get; set; }
    public float EngagementLevel { get; set; }
    public float FatigueLevel { get; set; }
    public float ExcitementLevel { get; set; }
    public float FocusLevel { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Eye tracking input data.
/// </summary>
public class EyeTrackingInput
{
    public string UserId { get; set; } = string.Empty;
    public float Timestamp { get; set; }
    public EyeData LeftEye { get; set; } = new();
    public EyeData RightEye { get; set; } = new();
    public float PupilDilation { get; set; }
    public float BlinkRate { get; set; }
}

/// <summary>
/// Processed eye tracking data.
/// </summary>
public class EyeTrackingData
{
    public string UserId { get; set; } = string.Empty;
    public float GazeX { get; set; }
    public float GazeY { get; set; }
    public float GazeZ { get; set; }
    public float FixationDuration { get; set; }
    public float SaccadeVelocity { get; set; }
    public string FocusedElement { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Individual eye data.
/// </summary>
public class EyeData
{
    public float X { get; set; }
    public float Y { get; set; }
    public float PupilSize { get; set; }
    public bool IsOpen { get; set; }
}

/// <summary>
/// Brainwave input data.
/// </summary>
public class BrainwaveInput
{
    public string UserId { get; set; } = string.Empty;
    public float Timestamp { get; set; }
    public float AlphaWaves { get; set; }
    public float BetaWaves { get; set; }
    public float ThetaWaves { get; set; }
    public float DeltaWaves { get; set; }
    public float GammaWaves { get; set; }
}

/// <summary>
/// Processed brainwave data.
/// </summary>
public class BrainwaveData
{
    public string UserId { get; set; } = string.Empty;
    public float AttentionLevel { get; set; }
    public float MeditationLevel { get; set; }
    public float MentalWorkload { get; set; }
    public float CognitiveLoad { get; set; }
    public string DominantState { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// User context for adaptive interfaces.
/// </summary>
public class UserContext
{
    public string UserId { get; set; } = string.Empty;
    public BiometricData Biometrics { get; set; } = new();
    public EyeTrackingData EyeTracking { get; set; } = new();
    public BrainwaveData Brainwaves { get; set; } = new();
    public string CurrentActivity { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
