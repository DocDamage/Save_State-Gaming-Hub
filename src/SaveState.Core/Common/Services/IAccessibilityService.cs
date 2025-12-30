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
