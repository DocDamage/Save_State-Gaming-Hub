using System.Threading.Tasks;

namespace SaveState.Core.Common.Services;

/// <summary>
/// Service for managing accessibility features and WCAG 2.1 AA compliance.
/// Provides centralized access to accessibility functionality.
/// </summary>
public interface IAccessibilityService
{
    /// <summary>
    /// Announces a message to screen readers (WCAG 4.1.3 Status Messages).
    /// </summary>
    /// <param name="message">The message to announce.</param>
    /// <param name="priority">Priority level for the announcement.</param>
    Task AnnounceAsync(string message, AnnouncementPriority priority = AnnouncementPriority.Normal);

    /// <summary>
    /// Sets focus to a specific UI element by automation ID.
    /// </summary>
    /// <param name="automationId">The automation ID of the element to focus.</param>
    Task<bool> SetFocusAsync(string automationId);

    /// <summary>
    /// Checks if high contrast mode is enabled.
    /// </summary>
    bool IsHighContrastEnabled { get; }

    /// <summary>
    /// Gets the current screen reader status.
    /// </summary>
    bool IsScreenReaderActive { get; }

    /// <summary>
    /// Gets the recommended color contrast ratio for current settings.
    /// </summary>
    double RecommendedContrastRatio { get; }

    /// <summary>
    /// Validates if the given text meets accessibility requirements.
    /// </summary>
    /// <param name="text">Text to validate.</param>
    /// <param name="context">Context where the text is used.</param>
    AccessibilityValidationResult ValidateTextAccessibility(string text, TextAccessibilityContext context);

    /// <summary>
    /// Enables screen reader support.
    /// </summary>
    Task EnableScreenReaderAsync(CancellationToken ct = default);

    /// <summary>
    /// Disables screen reader support.
    /// </summary>
    Task DisableScreenReaderAsync(CancellationToken ct = default);

    /// <summary>
    /// Enables text-to-speech functionality.
    /// </summary>
    Task EnableTextToSpeechAsync(CancellationToken ct = default);

    /// <summary>
    /// Disables text-to-speech functionality.
    /// </summary>
    Task DisableTextToSpeechAsync(CancellationToken ct = default);

    /// <summary>
    /// Enables high contrast mode.
    /// </summary>
    Task EnableHighContrastAsync(CancellationToken ct = default);

    /// <summary>
    /// Disables high contrast mode.
    /// </summary>
    Task DisableHighContrastAsync(CancellationToken ct = default);

    /// <summary>
    /// Applies a color blind mode.
    /// </summary>
    /// <param name="mode">The color blind mode to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ApplyColorBlindModeAsync(int mode, CancellationToken ct = default);

    /// <summary>
    /// Disables color blind mode.
    /// </summary>
    Task DisableColorBlindModeAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the UI scale factor.
    /// </summary>
    /// <param name="scaleFactor">Scale factor (1.0 = 100%).</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetUIScaleAsync(float scaleFactor, CancellationToken ct = default);

    /// <summary>
    /// Sets the font size multiplier.
    /// </summary>
    /// <param name="multiplier">Font size multiplier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetFontSizeMultiplierAsync(float multiplier, CancellationToken ct = default);

    /// <summary>
    /// Enables motion reduction for animations.
    /// </summary>
    Task EnableReduceMotionAsync(CancellationToken ct = default);

    /// <summary>
    /// Disables motion reduction for animations.
    /// </summary>
    Task DisableReduceMotionAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current accessibility settings.
    /// </summary>
    Task<Core.Common.Result<AccessibilitySettings>> GetCurrentSettingsAsync(CancellationToken ct = default);
}

/// <summary>
/// Priority levels for accessibility announcements.
/// </summary>
public enum AnnouncementPriority
{
    /// <summary>
    /// Low priority - general information.
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority - standard announcements.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority - important status changes or errors.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority - immediate user attention required.
    /// </summary>
    Critical
}

/// <summary>
/// Context for text accessibility validation.
/// </summary>
public enum TextAccessibilityContext
{
    /// <summary>
    /// Regular body text.
    /// </summary>
    BodyText,

    /// <summary>
    /// Heading or title text.
    /// </summary>
    Heading,

    /// <summary>
    /// Button or interactive element text.
    /// </summary>
    Button,

    /// <summary>
    /// Label text for form fields.
    /// </summary>
    Label,

    /// <summary>
    /// Error message text.
    /// </summary>
    ErrorMessage,

    /// <summary>
    /// Help or instructional text.
    /// </summary>
    HelpText
}

/// <summary>
/// Result of accessibility text validation.
/// </summary>
public class AccessibilityValidationResult
{
    /// <summary>
    /// Whether the text passes accessibility validation.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of accessibility issues found.
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// Suggestions for improving accessibility.
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static AccessibilityValidationResult Success()
        => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with issues.
    /// </summary>
    public static AccessibilityValidationResult Failure(List<string> issues, List<string> suggestions = null)
        => new()
        {
            IsValid = false,
            Issues = issues,
            Suggestions = suggestions ?? new List<string>()
        };
}

/// <summary>
/// Accessibility settings.
/// </summary>
public record AccessibilitySettings(
    bool ScreenReaderEnabled,
    bool TextToSpeechEnabled,
    bool HighContrastEnabled,
    bool ColorBlindModeEnabled,
    float UIScaleFactor,
    float FontSizeMultiplier,
    bool ReduceMotionEnabled);
