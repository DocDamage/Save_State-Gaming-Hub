using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace SaveState.Infrastructure.Services;

/// <summary>
/// Implementation of accessibility service for WCAG 2.1 AA compliance.
/// Manages screen reader announcements, focus management, and accessibility validation.
/// </summary>
public class AccessibilityService : IAccessibilityService
{
    private readonly ILogger<AccessibilityService> _logger;

    public AccessibilityService(ILogger<AccessibilityService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task AnnounceAsync(string message, AnnouncementPriority priority = AnnouncementPriority.Normal)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("Attempted to announce empty or null message");
            return;
        }

        try
        {
            // Log the announcement for testing and debugging
            _logger.LogInformation("Accessibility announcement: {Message} (Priority: {Priority})",
                message, priority);

            // In a real implementation, this would integrate with platform-specific
            // accessibility APIs (UI Automation on Windows, NSAccessibility on macOS, etc.)
            // For now, we rely on proper AutomationProperties in XAML

            await Task.CompletedTask; // Placeholder for async operation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to announce accessibility message: {Message}", message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetFocusAsync(string automationId)
    {
        if (string.IsNullOrWhiteSpace(automationId))
        {
            _logger.LogWarning("Attempted to set focus with empty or null automation ID");
            return false;
        }

        try
        {
            // In a real implementation, this would find and focus the element
            // with the specified automation ID using platform accessibility APIs
            _logger.LogInformation("Setting focus to element with automation ID: {AutomationId}", automationId);

            await Task.CompletedTask; // Placeholder for async operation
            return true; // Assume success for now
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set focus to element {AutomationId}", automationId);
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsHighContrastEnabled
    {
        get
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var hc = new HIGHCONTRAST
                    {
                        cbSize = Marshal.SizeOf<HIGHCONTRAST>()
                    };

                    if (SystemParametersInfo(SPI_GETHIGHCONTRAST, hc.cbSize, ref hc, 0))
                    {
                        return (hc.dwFlags & HCF_HIGHCONTRASTON) != 0;
                    }

                    return false;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS accessibility detection would be implemented via native bindings.
                    return false;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect high contrast mode");
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool IsScreenReaderActive
    {
        get
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    bool isScreenReader = false;
                    SystemParametersInfo(SPI_GETSCREENREADER, 0, ref isScreenReader, 0);
                    return isScreenReader;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return false;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect screen reader status");
                return false;
            }
        }
    }

    /// <inheritdoc />
    public double RecommendedContrastRatio
    {
        get
        {
            // WCAG 2.1 AA requirements
            const double NormalTextRatio = 4.5;
            const double LargeTextRatio = 3.0;
            const double HighContrastRatio = 7.0; // Enhanced contrast

            // Return enhanced ratio if high contrast is enabled
            return IsHighContrastEnabled ? HighContrastRatio : NormalTextRatio;
        }
    }

    /// <inheritdoc />
    public AccessibilityValidationResult ValidateTextAccessibility(string text, TextAccessibilityContext context)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return AccessibilityValidationResult.Failure(
                new List<string> { "Text is empty or null" },
                new List<string> { "Provide meaningful text content" });
        }

        var issues = new List<string>();
        var suggestions = new List<string>();

        // Length validation based on context
        switch (context)
        {
            case TextAccessibilityContext.Button:
                if (text.Length > 50)
                {
                    issues.Add("Button text is too long (>50 characters)");
                    suggestions.Add("Use concise, descriptive button text (2-4 words)");
                }
                else if (text.Length < 1)
                {
                    issues.Add("Button text is empty");
                    suggestions.Add("Provide clear button text describing the action");
                }
                break;

            case TextAccessibilityContext.Heading:
                if (text.Length > 100)
                {
                    issues.Add("Heading text is too long (>100 characters)");
                    suggestions.Add("Use concise, descriptive headings");
                }
                break;

            case TextAccessibilityContext.Label:
                if (text.Length > 80)
                {
                    issues.Add("Label text is too long (>80 characters)");
                    suggestions.Add("Use concise, clear label text");
                }
                break;

            case TextAccessibilityContext.ErrorMessage:
                if (text.Length < 10)
                {
                    issues.Add("Error message is too short (<10 characters)");
                    suggestions.Add("Provide specific, actionable error messages");
                }
                if (!text.Contains(" ") && text.Length > 20)
                {
                    issues.Add("Error message appears to be an error code without explanation");
                    suggestions.Add("Include human-readable explanation with error codes");
                }
                break;
        }

        // Check for ALL CAPS (WCAG 3.1.5 Reading Level)
        if (text.Length > 5 && text.All(char.IsUpper))
        {
            issues.Add("Text is in ALL CAPS, which can be harder to read");
            suggestions.Add("Use sentence case or mixed case for better readability");
        }

        // Check for potential accessibility issues with special characters
        var specialCharCount = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        if (specialCharCount > text.Length * 0.3 && text.Length > 10)
        {
            issues.Add("Text contains excessive special characters");
            suggestions.Add("Reduce special characters for better screen reader compatibility");
        }

        return issues.Any()
            ? AccessibilityValidationResult.Failure(issues, suggestions)
            : AccessibilityValidationResult.Success();
    }

    private const uint SPI_GETHIGHCONTRAST = 0x0042;
    private const uint SPI_GETSCREENREADER = 0x0046;
    private const uint HCF_HIGHCONTRASTON = 0x00000001;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct HIGHCONTRAST
    {
        public int cbSize;
        public int dwFlags;
        public IntPtr lpszDefaultScheme;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SystemParametersInfo(uint uiAction, int uiParam, ref HIGHCONTRAST pvParam, int fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref bool pvParam, uint fWinIni);

    // Accessibility feature state flags
    private bool _screenReaderEnabled;
    private bool _textToSpeechEnabled;
    private bool _highContrastEnabled;
    private bool _colorBlindModeEnabled;
    private int _colorBlindMode;
    private float _uiScaleFactor = 1.0f;
    private float _fontSizeMultiplier = 1.0f;
    private bool _reduceMotionEnabled;

    /// <inheritdoc />
    public async Task EnableScreenReaderAsync(CancellationToken ct = default)
    {
        try
        {
            _screenReaderEnabled = true;
            _logger.LogInformation("Screen reader enabled");
            await AnnounceAsync("Screen reader enabled", AnnouncementPriority.High);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable screen reader");
        }
    }

    /// <inheritdoc />
    public async Task DisableScreenReaderAsync(CancellationToken ct = default)
    {
        try
        {
            _screenReaderEnabled = false;
            _logger.LogInformation("Screen reader disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable screen reader");
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task EnableTextToSpeechAsync(CancellationToken ct = default)
    {
        try
        {
            _textToSpeechEnabled = true;
            _logger.LogInformation("Text-to-speech enabled");
            await AnnounceAsync("Text-to-speech enabled", AnnouncementPriority.High);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable text-to-speech");
        }
    }

    /// <inheritdoc />
    public async Task DisableTextToSpeechAsync(CancellationToken ct = default)
    {
        try
        {
            _textToSpeechEnabled = false;
            _logger.LogInformation("Text-to-speech disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable text-to-speech");
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task EnableHighContrastAsync(CancellationToken ct = default)
    {
        try
        {
            _highContrastEnabled = true;
            _logger.LogInformation("High contrast mode enabled");
            await AnnounceAsync("High contrast mode enabled", AnnouncementPriority.High);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable high contrast mode");
        }
    }

    /// <inheritdoc />
    public async Task DisableHighContrastAsync(CancellationToken ct = default)
    {
        try
        {
            _highContrastEnabled = false;
            _logger.LogInformation("High contrast mode disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable high contrast mode");
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ApplyColorBlindModeAsync(int mode, CancellationToken ct = default)
    {
        try
        {
            _colorBlindModeEnabled = true;
            _colorBlindMode = mode;
            _logger.LogInformation("Color blind mode applied: {Mode}", mode);
            await AnnounceAsync($"Color blind mode applied: Mode {mode}", AnnouncementPriority.High);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply color blind mode");
        }
    }

    /// <inheritdoc />
    public async Task DisableColorBlindModeAsync(CancellationToken ct = default)
    {
        try
        {
            _colorBlindModeEnabled = false;
            _logger.LogInformation("Color blind mode disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable color blind mode");
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SetUIScaleAsync(float scaleFactor, CancellationToken ct = default)
    {
        try
        {
            if (scaleFactor < 0.5f || scaleFactor > 3.0f)
            {
                _logger.LogWarning("UI scale factor out of range: {Factor}", scaleFactor);
                return;
            }

            _uiScaleFactor = scaleFactor;
            _logger.LogInformation("UI scale set to {Factor}%", scaleFactor * 100);
            await AnnounceAsync($"UI scale changed to {scaleFactor * 100:F0}%", AnnouncementPriority.Normal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set UI scale");
        }
    }

    /// <inheritdoc />
    public async Task SetFontSizeMultiplierAsync(float multiplier, CancellationToken ct = default)
    {
        try
        {
            if (multiplier < 0.8f || multiplier > 2.0f)
            {
                _logger.LogWarning("Font size multiplier out of range: {Multiplier}", multiplier);
                return;
            }

            _fontSizeMultiplier = multiplier;
            _logger.LogInformation("Font size multiplier set to {Multiplier}x", multiplier);
            await AnnounceAsync($"Font size adjusted to {multiplier}x", AnnouncementPriority.Normal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set font size multiplier");
        }
    }

    /// <inheritdoc />
    public async Task EnableReduceMotionAsync(CancellationToken ct = default)
    {
        try
        {
            _reduceMotionEnabled = true;
            _logger.LogInformation("Reduce motion enabled");
            await AnnounceAsync("Reduce motion enabled", AnnouncementPriority.High);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable reduce motion");
        }
    }

    /// <inheritdoc />
    public async Task DisableReduceMotionAsync(CancellationToken ct = default)
    {
        try
        {
            _reduceMotionEnabled = false;
            _logger.LogInformation("Reduce motion disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable reduce motion");
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<SaveState.Core.Common.Result<AccessibilitySettings>> GetCurrentSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var settings = new AccessibilitySettings(
                ScreenReaderEnabled: _screenReaderEnabled || IsScreenReaderActive,
                TextToSpeechEnabled: _textToSpeechEnabled,
                HighContrastEnabled: _highContrastEnabled || IsHighContrastEnabled,
                ColorBlindModeEnabled: _colorBlindModeEnabled,
                UIScaleFactor: _uiScaleFactor,
                FontSizeMultiplier: _fontSizeMultiplier,
                ReduceMotionEnabled: _reduceMotionEnabled);

            return await Task.FromResult(SaveState.Core.Common.Result.Success<AccessibilitySettings>(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get accessibility settings");
            return await Task.FromResult(SaveState.Core.Common.Result.Failure<AccessibilitySettings>(
                $"Failed to get settings: {ex.Message}", SaveState.Core.Common.ErrorType.Internal));
        }
    }
}
