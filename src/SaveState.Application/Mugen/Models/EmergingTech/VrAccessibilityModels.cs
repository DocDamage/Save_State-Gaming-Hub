namespace SaveState.Application.Mugen.Models.EmergingTech;

/// <summary>
/// Adaptive interface configuration.
/// </summary>
public class AdaptiveInterface
{
    public string UserId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public List<AdaptiveLayout> Layouts { get; set; } = new();
    public List<AdaptiveControl> Controls { get; set; } = new();
    public AdaptiveFeedback Feedback { get; set; } = new();
}

/// <summary>
/// Adaptive layout configuration.
/// </summary>
public class AdaptiveLayout
{
    public string LayoutId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TriggerCondition { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Adaptive control configuration.
/// </summary>
public class AdaptiveControl
{
    public string ControlId { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public float Sensitivity { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Adaptive feedback configuration.
/// </summary>
public class AdaptiveFeedback
{
    public VisualFeedback Visual { get; set; } = new();
    public AudioFeedback Audio { get; set; } = new();
    public VrHapticFeedback Haptic { get; set; } = new();
}

/// <summary>
/// Visual feedback settings.
/// </summary>
public class VisualFeedback
{
    public bool Enabled { get; set; }
    public float Intensity { get; set; }
    public string ColorScheme { get; set; } = string.Empty;
    public float Contrast { get; set; }
}

/// <summary>
/// Audio feedback settings.
/// </summary>
public class AudioFeedback
{
    public bool Enabled { get; set; }
    public float Volume { get; set; }
    public bool SpatialAudio { get; set; }
    public string AudioProfile { get; set; } = string.Empty;
}

/// <summary>
/// VR haptic feedback settings.
/// </summary>
public class VrHapticFeedback
{
    public bool Enabled { get; set; }
    public float Intensity { get; set; }
    public string DeviceType { get; set; } = string.Empty;
}

/// <summary>
/// VR accessibility settings.
/// </summary>
public class VrAccessibilitySettings
{
    public string UserId { get; set; } = string.Empty;
    public bool SeatedMode { get; set; }
    public float HeightAdjustment { get; set; }
    public float InteractionDistance { get; set; }
    public bool ReducedMotion { get; set; }
    public bool HighContrast { get; set; }
    public float TextScale { get; set; }
    public AccessibilityFeatures Features { get; set; } = new();
}

/// <summary>
/// Accessibility features configuration.
/// </summary>
public class AccessibilityFeatures
{
    public bool ScreenReader { get; set; }
    public bool VoiceControl { get; set; }
    public bool EyeTrackingControl { get; set; }
    public bool HeadTrackingControl { get; set; }
    public bool GestureControl { get; set; }
    public bool BrainwaveControl { get; set; }
}
