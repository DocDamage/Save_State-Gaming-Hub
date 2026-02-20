using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.AccessibilityCenter.Models;

/// <summary>
/// Represents accessibility control center configuration.
/// </summary>
public record AccessibilityConfiguration
{
    public bool OneSwitchModeEnabled { get; init; } = false;
    public bool EyeGazeEnabled { get; init; } = false;
    public bool VoiceControlEnabled { get; init; } = false;
    public ColorblindMode ColorblindMode { get; init; } = ColorblindMode.None;
    public float UiScale { get; init; } = 1.0f;
    public bool HighContrastEnabled { get; init; } = false;
    public bool ScreenReaderEnabled { get; init; } = false;
    public bool ReduceMotionEnabled { get; init; } = false;
    public bool AutoPauseOnFocusLoss { get; init; } = true;
    public OneSwitchConfiguration OneSwitchSettings { get; init; } = new();
    public EyeGazeConfiguration EyeGazeSettings { get; init; } = new();
    public VoiceControlConfiguration VoiceControlSettings { get; init; } = new();
}

/// <summary>
/// Colorblind mode options.
/// </summary>
public enum ColorblindMode
{
    None,
    Deuteranopia,
    Protanopia,
    Tritanopia,
    Achromatopsia,
    HighContrast
}

/// <summary>
/// Configuration for one-switch mode.
/// </summary>
public record OneSwitchConfiguration
{
    public int ScanSpeedMs { get; init; } = 1000;
    public ScanPattern ScanPattern { get; init; } = ScanPattern.Linear;
    public bool AutoScanEnabled { get; init; } = true;
    public int DwellTimeMs { get; init; } = 500;
    public bool AudioFeedbackEnabled { get; init; } = true;
    public bool VisualFeedbackEnabled { get; init; } = true;
    public IReadOnlyList<string> CustomScanGroups { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Scan patterns for one-switch mode.
/// </summary>
public enum ScanPattern
{
    Linear,
    RowColumn,
    Group,
    Step,
    Directed
}

/// <summary>
/// Configuration for eye-gaze control.
/// </summary>
public record EyeGazeConfiguration
{
    public float CalibrationX { get; init; } = 0;
    public float CalibrationY { get; init; } = 0;
    public int DwellTimeMs { get; init; } = 800;
    public float SmoothingFactor { get; init; } = 0.5f;
    public bool HeadTrackingEnabled { get; init; } = false;
    public bool BlinkClickEnabled { get; init; } = true;
    public float GazeZoneSize { get; init; } = 100;
    public bool ShowGazeCursor { get; init; } = true;
    public string GazeCursorStyle { get; init; } = "circle";
}

/// <summary>
/// Configuration for voice control.
/// </summary>
public record VoiceControlConfiguration
{
    public string Language { get; init; } = "en-US";
    public float ConfidenceThreshold { get; init; } = 0.7f;
    public bool WakeWordEnabled { get; init; } = true;
    public string WakeWord { get; init; } = "Hey Game";
    public bool ContinuousListening { get; init; } = false;
    public int CommandTimeoutMs { get; init; } = 5000;
    public IReadOnlyList<VoiceCommandMapping> CustomCommands { get; init; } = Array.Empty<VoiceCommandMapping>();
}

/// <summary>
/// Voice command to action mapping.
/// </summary>
public record VoiceCommandMapping
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string VoiceCommand { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public bool Enabled { get; init; } = true;
}

/// <summary>
/// Represents eye-gaze tracking data.
/// </summary>
public record EyeGazeData
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public float GazeX { get; init; }
    public float GazeY { get; init; }
    public float LeftEyeOpenness { get; init; }
    public float RightEyeOpenness { get; init; }
    public float HeadPitch { get; init; }
    public float HeadYaw { get; init; }
    public float HeadRoll { get; init; }
    public float Confidence { get; init; }
    public DateTime Timestamp { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public bool IsBlinkDetected { get; init; } = false;
}

/// <summary>
/// Represents one-switch scan state.
/// </summary>
public record OneSwitchScanState
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public bool IsScanning { get; init; }
    public int CurrentIndex { get; init; }
    public string CurrentElementId { get; init; } = string.Empty;
    public string CurrentGroupId { get; init; } = string.Empty;
    public IReadOnlyList<ScannableElement> Elements { get; init; } = Array.Empty<ScannableElement>();
    public DateTime ScanStartedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Represents a scannable UI element.
/// </summary>
public record ScannableElement
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string ElementId { get; init; } = string.Empty;
    public string ElementType { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string? ParentGroupId { get; init; }
    public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Represents a voice command recognition result.
/// </summary>
public record VoiceCommandResult
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string RecognizedText { get; init; } = string.Empty;
    public string MatchedCommand { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public bool IsMatch { get; init; }
    public IReadOnlyDictionary<string, object>? ExtractedParameters { get; init; }
    public DateTime Timestamp { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Represents an accessibility action.
/// </summary>
public record AccessibilityAction
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public AccessibilityActionType Type { get; init; }
    public string Target { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public DateTime Timestamp { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Types of accessibility actions.
/// </summary>
public enum AccessibilityActionType
{
    Click,
    DoubleClick,
    RightClick,
    Hover,
    Scroll,
    Type,
    Select,
    Navigate,
    Back,
    Menu
}

/// <summary>
/// Represents accessibility profile.
/// </summary>
public record AccessibilityProfile
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = string.Empty;
    public string Name { get; init; } = "Default";
    public bool IsDefault { get; init; } = false;
    public AccessibilityConfiguration Configuration { get; init; } = new();
    public DateTime CreatedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public DateTime ModifiedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Color correction matrix for colorblind modes.
/// </summary>
public record ColorCorrectionMatrix
{
    public float Rr { get; init; } = 1.0f;
    public float Rg { get; init; } = 0.0f;
    public float Rb { get; init; } = 0.0f;
    public float Gr { get; init; } = 0.0f;
    public float Gg { get; init; } = 1.0f;
    public float Gb { get; init; } = 0.0f;
    public float Br { get; init; } = 0.0f;
    public float Bg { get; init; } = 0.0f;
    public float Bb { get; init; } = 1.0f;
}
