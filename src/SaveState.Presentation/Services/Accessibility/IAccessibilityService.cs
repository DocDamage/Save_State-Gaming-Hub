using Avalonia.Controls;
using Avalonia.Input;

namespace SaveState.Presentation.Services.Accessibility;

/// <summary>
/// Enhanced accessibility service for WCAG 2.1 AA compliance.
/// Provides screen reader support, focus management, and visual accessibility features.
/// </summary>
public interface IAccessibilityService
{
    // Screen Reader Support
    Task AnnounceAsync(string message, AccessibilityPriority priority = AccessibilityPriority.Normal);
    Task AnnounceAlertAsync(string alert);
    Task SetAriaLabelAsync(Control element, string label);
    Task SetAriaDescriptionAsync(Control element, string description);
    Task SetAriaLiveAsync(Control element, AriaLiveMode mode);
    
    // Focus Management
    Task MoveFocusAsync(FocusNavigationDirection direction);
    Task SetFocusAsync(Control element);
    Task TrapFocusAsync(Control container);
    Task ReleaseFocusTrapAsync();
    Task FocusFirstAsync(Control container);
    Task FocusLastAsync(Control container);
    
    // High Contrast
    bool IsHighContrastEnabled { get; }
    event EventHandler<bool>? HighContrastChanged;
    Task EnableHighContrastAsync();
    Task DisableHighContrastAsync();
    
    // Reduced Motion
    bool IsReducedMotionEnabled { get; }
    event EventHandler<bool>? ReducedMotionChanged;
    Task EnableReducedMotionAsync();
    Task DisableReducedMotionAsync();
    
    // Color Filters
    Task EnableColorFilterAsync(ColorFilterType filter);
    Task DisableColorFilterAsync();
    
    // Text Scaling
    double TextScaleFactor { get; }
    Task SetTextScaleAsync(double scale);
    
    // Keyboard Navigation
    bool IsKeyboardNavigationEnabled { get; }
    Task EnableKeyboardNavigationAsync();
    Task DisableKeyboardNavigationAsync();
    
    // Accessibility Tree
    AccessibilityNode? GetAccessibilityTree();
    Task DumpAccessibilityTreeAsync(string outputPath);
}

/// <summary>
/// Priority levels for accessibility announcements.
/// </summary>
public enum AccessibilityPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// ARIA live region modes for screen reader announcements.
/// </summary>
public enum AriaLiveMode
{
    Off,
    Polite,
    Assertive
}

/// <summary>
/// Color filter types for visual accessibility.
/// </summary>
public enum ColorFilterType
{
    Protanopia,    // Red-blind
    Deuteranopia,  // Green-blind
    Tritanopia,    // Blue-blind
    Achromatopsia, // Total color blindness
    HighContrast
}

/// <summary>
/// Represents a node in the accessibility tree.
/// </summary>
public record AccessibilityNode
{
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Value { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsFocused { get; set; }
    public List<AccessibilityNode> Children { get; set; } = new();
}
