using Avalonia.Media;

namespace SaveState.Presentation.Services.Accessibility;

/// <summary>
/// Utility class for checking color contrast ratios according to WCAG 2.1 guidelines.
/// </summary>
public static class ColorContrastChecker
{
    /// <summary>
    /// WCAG 2.1 AA contrast ratio requirement for normal text.
    /// </summary>
    public const double WCAG_AA_Normal = 4.5;

    /// <summary>
    /// WCAG 2.1 AA contrast ratio requirement for large text (18pt+ or 14pt+ bold).
    /// </summary>
    public const double WCAG_AA_Large = 3.0;

    /// <summary>
    /// WCAG 2.1 AAA contrast ratio requirement for normal text.
    /// </summary>
    public const double WCAG_AAA_Normal = 7.0;

    /// <summary>
    /// WCAG 2.1 AAA contrast ratio requirement for large text.
    /// </summary>
    public const double WCAG_AAA_Large = 4.5;

    /// <summary>
    /// Calculates the contrast ratio between two colors.
    /// </summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <returns>The contrast ratio, typically between 1 and 21.</returns>
    public static double CalculateContrastRatio(Color foreground, Color background)
    {
        var foregroundLuminance = GetRelativeLuminance(foreground);
        var backgroundLuminance = GetRelativeLuminance(background);

        var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
        var darker = Math.Min(foregroundLuminance, backgroundLuminance);

        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Calculates the contrast ratio between two brush colors.
    /// </summary>
    public static double CalculateContrastRatio(IBrush foreground, IBrush background)
    {
        var fgColor = GetColorFromBrush(foreground);
        var bgColor = GetColorFromBrush(background);
        return CalculateContrastRatio(fgColor, bgColor);
    }

    /// <summary>
    /// Gets the contrast level for a given contrast ratio.
    /// </summary>
    /// <param name="ratio">The contrast ratio.</param>
    /// <returns>The contrast level classification.</returns>
    public static ContrastLevel GetContrastLevel(double ratio)
    {
        return ratio switch
        {
            >= 7.0 => ContrastLevel.AAA,
            >= 4.5 => ContrastLevel.AA,
            >= 3.0 => ContrastLevel.AA_Large,
            _ => ContrastLevel.Fail
        };
    }

    /// <summary>
    /// Checks if the contrast meets WCAG 2.1 AA requirements.
    /// </summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <param name="isLargeText">Whether the text is considered large (18pt+ or 14pt+ bold).</param>
    /// <returns>True if the contrast meets AA requirements.</returns>
    public static bool MeetsWCAGAA(Color foreground, Color background, bool isLargeText = false)
    {
        var ratio = CalculateContrastRatio(foreground, background);
        var threshold = isLargeText ? WCAG_AA_Large : WCAG_AA_Normal;
        return ratio >= threshold;
    }

    /// <summary>
    /// Checks if the contrast meets WCAG 2.1 AA requirements.
    /// </summary>
    public static bool MeetsWCAGAA(IBrush foreground, IBrush background, bool isLargeText = false)
    {
        var fgColor = GetColorFromBrush(foreground);
        var bgColor = GetColorFromBrush(background);
        return MeetsWCAGAA(fgColor, bgColor, isLargeText);
    }

    /// <summary>
    /// Checks if the contrast meets WCAG 2.1 AAA requirements.
    /// </summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <param name="isLargeText">Whether the text is considered large.</param>
    /// <returns>True if the contrast meets AAA requirements.</returns>
    public static bool MeetsWCAGAAA(Color foreground, Color background, bool isLargeText = false)
    {
        var ratio = CalculateContrastRatio(foreground, background);
        var threshold = isLargeText ? WCAG_AAA_Large : WCAG_AAA_Normal;
        return ratio >= threshold;
    }

    /// <summary>
    /// Checks if the contrast meets WCAG 2.1 AAA requirements.
    /// </summary>
    public static bool MeetsWCAGAAA(IBrush foreground, IBrush background, bool isLargeText = false)
    {
        var fgColor = GetColorFromBrush(foreground);
        var bgColor = GetColorFromBrush(background);
        return MeetsWCAGAAA(fgColor, bgColor, isLargeText);
    }

    /// <summary>
    /// Validates contrast and returns detailed results.
    /// </summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <param name="isLargeText">Whether the text is large.</param>
    /// <returns>Detailed contrast validation results.</returns>
    public static ContrastValidationResult ValidateContrast(Color foreground, Color background, bool isLargeText = false)
    {
        var ratio = CalculateContrastRatio(foreground, background);
        var level = GetContrastLevel(ratio);
        
        var aaRequired = isLargeText ? WCAG_AA_Large : WCAG_AA_Normal;
        var aaaRequired = isLargeText ? WCAG_AAA_Large : WCAG_AAA_Normal;

        return new ContrastValidationResult
        {
            ContrastRatio = ratio,
            ContrastLevel = level,
            MeetsAA = ratio >= aaRequired,
            MeetsAAA = ratio >= aaaRequired,
            IsLargeText = isLargeText,
            RequiredForAA = aaRequired,
            RequiredForAAA = aaaRequired,
            Suggestions = GenerateSuggestions(ratio, level, isLargeText)
        };
    }

    /// <summary>
    /// Finds a contrasting color that meets WCAG AA requirements.
    /// </summary>
    /// <param name="background">The background color.</param>
    /// <param name="preferredColor">The preferred foreground color.</param>
    /// <param name="isLargeText">Whether the text is large.</param>
    /// <returns>A color that meets the contrast requirements, or null if none found.</returns>
    public static Color? FindContrastingColor(Color background, Color preferredColor, bool isLargeText = false)
    {
        var threshold = isLargeText ? WCAG_AA_Large : WCAG_AA_Normal;
        
        // Check if preferred color already works
        if (CalculateContrastRatio(preferredColor, background) >= threshold)
        {
            return preferredColor;
        }

        // Try adjusting luminance
        var bgLuminance = GetRelativeLuminance(background);
        var targetLuminance = bgLuminance > 0.5 
            ? 0.0 // Dark text on light background
            : 1.0; // Light text on dark background

        // Binary search for appropriate color
        var result = AdjustColorForContrast(preferredColor, background, threshold);
        return result;
    }

    /// <summary>
    /// Gets a human-readable description of a contrast ratio.
    /// </summary>
    public static string GetContrastDescription(double ratio)
    {
        var level = GetContrastLevel(ratio);
        return level switch
        {
            ContrastLevel.AAA => $"{ratio:F1}:1 - Excellent (WCAG AAA compliant)",
            ContrastLevel.AA => $"{ratio:F1}:1 - Good (WCAG AA compliant)",
            ContrastLevel.AA_Large => $"{ratio:F1}:1 - Acceptable for large text only",
            _ => $"{ratio:F1}:1 - Insufficient (fails WCAG requirements)"
        };
    }

    #region Private Methods

    private static double GetRelativeLuminance(Color color)
    {
        double rsRgb = color.R / 255.0;
        double gsRgb = color.G / 255.0;
        double bsRgb = color.B / 255.0;

        double r = rsRgb <= 0.03928 ? rsRgb / 12.92 : Math.Pow((rsRgb + 0.055) / 1.055, 2.4);
        double g = gsRgb <= 0.03928 ? gsRgb / 12.92 : Math.Pow((gsRgb + 0.055) / 1.055, 2.4);
        double b = bsRgb <= 0.03928 ? bsRgb / 12.92 : Math.Pow((bsRgb + 0.055) / 1.055, 2.4);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    private static Color GetColorFromBrush(IBrush brush)
    {
        if (brush is ISolidColorBrush solidBrush)
        {
            return solidBrush.Color;
        }
        
        // Default to black for non-solid brushes
        return Colors.Black;
    }

    private static List<string> GenerateSuggestions(double ratio, ContrastLevel level, bool isLargeText)
    {
        var suggestions = new List<string>();

        if (level == ContrastLevel.Fail)
        {
            suggestions.Add("Consider using a darker foreground color or lighter background color.");
            suggestions.Add("Increase the font size or weight if possible (large text has lower requirements).");
        }
        else if (level == ContrastLevel.AA_Large && !isLargeText)
        {
            suggestions.Add("This contrast only works for large text (18pt+ or 14pt+ bold).");
            suggestions.Add("For normal text, increase the contrast to at least 4.5:1.");
        }
        else if (level == ContrastLevel.AA)
        {
            suggestions.Add("This meets WCAG AA requirements. For enhanced accessibility, consider aiming for AAA (7:1).");
        }

        return suggestions;
    }

    private static Color AdjustColorForContrast(Color color, Color background, double targetRatio)
    {
        var bgLuminance = GetRelativeLuminance(background);
        var needsDarkening = bgLuminance > 0.5;

        // Simple adjustment: move towards black or white
        for (int i = 0; i <= 100; i++)
        {
            double factor = i / 100.0;
            var adjusted = needsDarkening
                ? DarkenColor(color, factor)
                : LightenColor(color, factor);

            if (CalculateContrastRatio(adjusted, background) >= targetRatio)
            {
                return adjusted;
            }
        }

        // Fallback to black or white
        return needsDarkening ? Colors.Black : Colors.White;
    }

    private static Color DarkenColor(Color color, double factor)
    {
        return new Color(
            color.A,
            (byte)(color.R * (1 - factor)),
            (byte)(color.G * (1 - factor)),
            (byte)(color.B * (1 - factor)));
    }

    private static Color LightenColor(Color color, double factor)
    {
        return new Color(
            color.A,
            (byte)(color.R + (255 - color.R) * factor),
            (byte)(color.G + (255 - color.G) * factor),
            (byte)(color.B + (255 - color.B) * factor));
    }

    #endregion
}

/// <summary>
/// Contrast level classifications.
/// </summary>
public enum ContrastLevel
{
    /// <summary>
    /// Fails WCAG requirements (less than 3:1).
    /// </summary>
    Fail,

    /// <summary>
    /// Meets AA requirements for large text only (3:1 or greater).
    /// </summary>
    AA_Large,

    /// <summary>
    /// Meets AA requirements for all text (4.5:1 or greater).
    /// </summary>
    AA,

    /// <summary>
    /// Meets AAA requirements (7:1 or greater).
    /// </summary>
    AAA
}

/// <summary>
/// Detailed contrast validation results.
/// </summary>
public class ContrastValidationResult
{
    /// <summary>
    /// The calculated contrast ratio.
    /// </summary>
    public double ContrastRatio { get; set; }

    /// <summary>
    /// The contrast level classification.
    /// </summary>
    public ContrastLevel ContrastLevel { get; set; }

    /// <summary>
    /// Whether the contrast meets WCAG AA requirements.
    /// </summary>
    public bool MeetsAA { get; set; }

    /// <summary>
    /// Whether the contrast meets WCAG AAA requirements.
    /// </summary>
    public bool MeetsAAA { get; set; }

    /// <summary>
    /// Whether the text being validated is large.
    /// </summary>
    public bool IsLargeText { get; set; }

    /// <summary>
    /// The required contrast ratio for AA compliance.
    /// </summary>
    public double RequiredForAA { get; set; }

    /// <summary>
    /// The required contrast ratio for AAA compliance.
    /// </summary>
    public double RequiredForAAA { get; set; }

    /// <summary>
    /// Suggestions for improving contrast.
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// Gets a summary of the validation result.
    /// </summary>
    public string Summary => $"Contrast: {ContrastRatio:F1}:1 - {(MeetsAA ? "WCAG AA Pass" : "WCAG AA Fail")}";
}
