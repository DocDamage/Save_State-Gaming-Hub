using SaveState.Core.Common.Services;

namespace SaveState.Core.Theme.Models;

/// <summary>
/// Represents a complete theme definition with colors, typography, and effects.
/// </summary>
public record ThemeDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the theme.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the theme.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is a built-in theme that cannot be deleted.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Gets or sets whether this is a dark theme.
    /// </summary>
    public bool IsDark { get; set; }

    /// <summary>
    /// Gets or sets the color palette for the theme.
    /// </summary>
    public ThemeColors Colors { get; set; } = new();

    /// <summary>
    /// Gets or sets the typography settings for the theme.
    /// </summary>
    public ThemeTypography Typography { get; set; } = new();

    /// <summary>
    /// Gets or sets the visual effects settings for the theme.
    /// </summary>
    public ThemeEffects Effects { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Creates a copy of this theme with a new ID.
    /// </summary>
    public ThemeDefinition Copy(string newName)
    {
        var now = DateTime.UtcNow;
        return new ThemeDefinition
        {
            Id = Guid.NewGuid(),
            Name = newName,
            IsBuiltIn = false,
            IsDark = IsDark,
            Colors = Colors.Copy(),
            Typography = Typography.Copy(),
            Effects = Effects.Copy(),
            CreatedAt = now,
            ModifiedAt = now
        };
    }
}

/// <summary>
/// Represents the color palette for a theme following Material Design 3 principles.
/// </summary>
public record ThemeColors
{
    // Primary
    public string Primary { get; set; } = "#6750A4";
    public string OnPrimary { get; set; } = "#FFFFFF";
    public string PrimaryContainer { get; set; } = "#EADDFF";
    public string OnPrimaryContainer { get; set; } = "#21005D";

    // Secondary
    public string Secondary { get; set; } = "#625B71";
    public string OnSecondary { get; set; } = "#FFFFFF";
    public string SecondaryContainer { get; set; } = "#E8DEF8";
    public string OnSecondaryContainer { get; set; } = "#1D192B";

    // Tertiary
    public string Tertiary { get; set; } = "#7D5260";
    public string OnTertiary { get; set; } = "#FFFFFF";
    public string TertiaryContainer { get; set; } = "#FFD8E4";
    public string OnTertiaryContainer { get; set; } = "#31111D";

    // Error
    public string Error { get; set; } = "#B3261E";
    public string OnError { get; set; } = "#FFFFFF";
    public string ErrorContainer { get; set; } = "#F9DEDC";
    public string OnErrorContainer { get; set; } = "#410E0B";

    // Background
    public string Background { get; set; } = "#FFFBFE";
    public string OnBackground { get; set; } = "#1C1B1F";
    public string Surface { get; set; } = "#FFFBFE";
    public string OnSurface { get; set; } = "#1C1B1F";
    public string SurfaceVariant { get; set; } = "#E7E0EC";
    public string OnSurfaceVariant { get; set; } = "#49454F";

    // Outline
    public string Outline { get; set; } = "#79747E";
    public string OutlineVariant { get; set; } = "#CAC4D0";

    // Inverse
    public string InverseSurface { get; set; } = "#313033";
    public string InverseOnSurface { get; set; } = "#F4EFF4";
    public string InversePrimary { get; set; } = "#D0BCFF";

    // Surface containers
    public string SurfaceContainerLowest { get; set; } = "#FFFFFF";
    public string SurfaceContainerLow { get; set; } = "#F7F2FA";
    public string SurfaceContainer { get; set; } = "#F3EDF7";
    public string SurfaceContainerHigh { get; set; } = "#ECE6F0";
    public string SurfaceContainerHighest { get; set; } = "#E6E0E9";

    // Glassmorphism
    public string GlassBackground { get; set; } = "#20FFFFFF";
    public string GlassBorder { get; set; } = "#40FFFFFF";

    // Gradients
    public List<string> AccentGradient { get; set; } = new() { "#6750A4", "#7C6DB8" };
    public List<string> SuccessGradient { get; set; } = new() { "#4CAF50", "#81C784" };
    public List<string> WarningGradient { get; set; } = new() { "#FF9800", "#FFB74D" };
    public List<string> ErrorGradient { get; set; } = new() { "#F44336", "#E57373" };

    /// <summary>
    /// Creates a deep copy of the colors.
    /// </summary>
    public ThemeColors Copy()
    {
        return new ThemeColors
        {
            Primary = Primary,
            OnPrimary = OnPrimary,
            PrimaryContainer = PrimaryContainer,
            OnPrimaryContainer = OnPrimaryContainer,
            Secondary = Secondary,
            OnSecondary = OnSecondary,
            SecondaryContainer = SecondaryContainer,
            OnSecondaryContainer = OnSecondaryContainer,
            Tertiary = Tertiary,
            OnTertiary = OnTertiary,
            TertiaryContainer = TertiaryContainer,
            OnTertiaryContainer = OnTertiaryContainer,
            Error = Error,
            OnError = OnError,
            ErrorContainer = ErrorContainer,
            OnErrorContainer = OnErrorContainer,
            Background = Background,
            OnBackground = OnBackground,
            Surface = Surface,
            OnSurface = OnSurface,
            SurfaceVariant = SurfaceVariant,
            OnSurfaceVariant = OnSurfaceVariant,
            Outline = Outline,
            OutlineVariant = OutlineVariant,
            InverseSurface = InverseSurface,
            InverseOnSurface = InverseOnSurface,
            InversePrimary = InversePrimary,
            SurfaceContainerLowest = SurfaceContainerLowest,
            SurfaceContainerLow = SurfaceContainerLow,
            SurfaceContainer = SurfaceContainer,
            SurfaceContainerHigh = SurfaceContainerHigh,
            SurfaceContainerHighest = SurfaceContainerHighest,
            GlassBackground = GlassBackground,
            GlassBorder = GlassBorder,
            AccentGradient = new List<string>(AccentGradient),
            SuccessGradient = new List<string>(SuccessGradient),
            WarningGradient = new List<string>(WarningGradient),
            ErrorGradient = new List<string>(ErrorGradient)
        };
    }
}

/// <summary>
/// Represents typography settings for a theme.
/// </summary>
public record ThemeTypography
{
    /// <summary>
    /// Gets or sets the display/heading font family.
    /// </summary>
    public string DisplayFont { get; set; } = "Inter";

    /// <summary>
    /// Gets or sets the body text font family.
    /// </summary>
    public string BodyFont { get; set; } = "Inter";

    /// <summary>
    /// Gets or sets the monospace font family for code.
    /// </summary>
    public string MonoFont { get; set; } = "JetBrains Mono";

    /// <summary>
    /// Gets or sets the base font size in pixels.
    /// </summary>
    public double BaseFontSize { get; set; } = 14;

    /// <summary>
    /// Creates a deep copy of the typography settings.
    /// </summary>
    public ThemeTypography Copy()
    {
        return new ThemeTypography
        {
            DisplayFont = DisplayFont,
            BodyFont = BodyFont,
            MonoFont = MonoFont,
            BaseFontSize = BaseFontSize
        };
    }
}

/// <summary>
/// Represents visual effects settings for a theme.
/// </summary>
public record ThemeEffects
{
    /// <summary>
    /// Gets or sets the blur amount for glassmorphism effects.
    /// </summary>
    public double GlassBlur { get; set; } = 20;

    /// <summary>
    /// Gets or sets the opacity for glassmorphism backgrounds.
    /// </summary>
    public double GlassOpacity { get; set; } = 0.2;

    /// <summary>
    /// Gets or sets the shadow opacity.
    /// </summary>
    public double ShadowOpacity { get; set; } = 0.3;

    /// <summary>
    /// Gets or sets the border radius in pixels.
    /// </summary>
    public double BorderRadius { get; set; } = 12;

    /// <summary>
    /// Gets or sets the border width in pixels.
    /// </summary>
    public double BorderWidth { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether animations are enabled.
    /// </summary>
    public bool UseAnimations { get; set; } = true;

    /// <summary>
    /// Gets or sets the animation speed multiplier.
    /// </summary>
    public double AnimationSpeed { get; set; } = 1.0;

    /// <summary>
    /// Creates a deep copy of the effects settings.
    /// </summary>
    public ThemeEffects Copy()
    {
        return new ThemeEffects
        {
            GlassBlur = GlassBlur,
            GlassOpacity = GlassOpacity,
            ShadowOpacity = ShadowOpacity,
            BorderRadius = BorderRadius,
            BorderWidth = BorderWidth,
            UseAnimations = UseAnimations,
            AnimationSpeed = AnimationSpeed
        };
    }
}

/// <summary>
/// Represents a color in HCT (Hue, Chroma, Tone) color space used by Material You.
/// </summary>
public record HctColor
{
    /// <summary>
    /// Gets or sets the hue (0-360).
    /// </summary>
    public double Hue { get; set; }

    /// <summary>
    /// Gets or sets the chroma (0-150+).
    /// </summary>
    public double Chroma { get; set; }

    /// <summary>
    /// Gets or sets the tone/lightness (0-100).
    /// </summary>
    public double Tone { get; set; }

    /// <summary>
    /// Converts HCT to ARGB color.
    /// </summary>
    public uint ToArgb()
    {
        // Simplified HCT to ARGB conversion
        // In a full implementation, this would use the Material Color Utilities library
        return HctToArgb(Hue, Chroma, Tone);
    }

    /// <summary>
    /// Creates an HCT color from ARGB.
    /// </summary>
    public static HctColor FromArgb(uint argb)
    {
        // Simplified ARGB to HCT conversion
        var (h, c, t) = ArgbToHct(argb);
        return new HctColor { Hue = h, Chroma = c, Tone = t };
    }

    private static uint HctToArgb(double hue, double chroma, double tone)
    {
        // Simplified conversion - in production, use Google's Material Color Utilities
        // This is a placeholder that converts through HSL
        var h = hue / 360.0;
        var s = Math.Min(chroma / 100.0, 1.0);
        var l = tone / 100.0;

        var (r, g, b) = HslToRgb(h, s, l);
        return (0xFFu << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
    }

    private static (double h, double c, double t) ArgbToHct(uint argb)
    {
        var r = (int)((argb >> 16) & 0xFF);
        var g = (int)((argb >> 8) & 0xFF);
        var b = (int)(argb & 0xFF);

        var (h, s, l) = RgbToHsl(r, g, b);
        return (h * 360, s * 100, l * 100);
    }

    private static (int r, int g, int b) HslToRgb(double h, double s, double l)
    {
        double r, g, b;

        if (s == 0)
        {
            r = g = b = l;
        }
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            r = HueToRgb(p, q, h + 1.0 / 3);
            g = HueToRgb(p, q, h);
            b = HueToRgb(p, q, h - 1.0 / 3);
        }

        return ((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }

    private static (double h, double s, double l) RgbToHsl(int r, int g, int b)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double h = 0, s = 0, l = (max + min) / 2;

        if (max != min)
        {
            double d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);

            if (max == rd)
                h = ((gd - bd) / d + (gd < bd ? 6 : 0)) / 6;
            else if (max == gd)
                h = ((bd - rd) / d + 2) / 6;
            else
                h = ((rd - gd) / d + 4) / 6;
        }

        return (h, s, l);
    }
}

/// <summary>
/// Represents a tonal palette generated from a seed color.
/// </summary>
public record TonalPalette
{
    /// <summary>
    /// Gets or sets the seed color in hex format.
    /// </summary>
    public string SeedColor { get; set; } = "#6750A4";

    /// <summary>
    /// Gets or sets the tonal values (0-100).
    /// </summary>
    public Dictionary<int, string> Tones { get; set; } = new();

    /// <summary>
    /// Generates a tonal palette from a seed color.
    /// </summary>
    public static TonalPalette FromSeed(string seedColor)
    {
        var palette = new TonalPalette { SeedColor = seedColor };
        var hct = HctColor.FromArgb(HexToArgb(seedColor));

        // Generate tones at standard Material Design intervals
        foreach (var tone in new[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 95, 99, 100 })
        {
            var toneHct = new HctColor
            {
                Hue = hct.Hue,
                Chroma = hct.Chroma,
                Tone = tone
            };
            palette.Tones[tone] = ArgbToHex(toneHct.ToArgb());
        }

        return palette;
    }

    private static uint HexToArgb(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex[1..];

        if (hex.Length == 6)
            return 0xFF000000 | uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);

        if (hex.Length == 8)
            return uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);

        return 0xFF000000;
    }

    private static string ArgbToHex(uint argb)
    {
        return $"#{argb:X8}";
    }
}

/// <summary>
/// Represents contrast ratio information for accessibility.
/// </summary>
public record ContrastInfo
{
    /// <summary>
    /// Gets or sets the contrast ratio (1-21).
    /// </summary>
    public double Ratio { get; set; }

    /// <summary>
    /// Gets or sets whether the contrast passes WCAG AA normal text.
    /// </summary>
    public bool PassesAaNormal { get; set; }

    /// <summary>
    /// Gets or sets whether the contrast passes WCAG AA large text.
    /// </summary>
    public bool PassesAaLarge { get; set; }

    /// <summary>
    /// Gets or sets whether the contrast passes WCAG AAA normal text.
    /// </summary>
    public bool PassesAaaNormal { get; set; }

    /// <summary>
    /// Gets or sets whether the contrast passes WCAG AAA large text.
    /// </summary>
    public bool PassesAaaLarge { get; set; }

    /// <summary>
    /// Gets the WCAG compliance level.
    /// </summary>
    public string ComplianceLevel
    {
        get
        {
            if (PassesAaaNormal) return "AAA";
            if (PassesAaNormal) return "AA";
            if (PassesAaLarge) return "AA Large";
            return "Fail";
        }
    }

    /// <summary>
    /// Calculates contrast information between two colors.
    /// </summary>
    public static ContrastInfo Calculate(string foreground, string background)
    {
        var fgLuminance = GetRelativeLuminance(foreground);
        var bgLuminance = GetRelativeLuminance(background);

        var lighter = Math.Max(fgLuminance, bgLuminance);
        var darker = Math.Min(fgLuminance, bgLuminance);
        var ratio = (lighter + 0.05) / (darker + 0.05);

        return new ContrastInfo
        {
            Ratio = ratio,
            PassesAaNormal = ratio >= 4.5,
            PassesAaLarge = ratio >= 3.0,
            PassesAaaNormal = ratio >= 7.0,
            PassesAaaLarge = ratio >= 4.5
        };
    }

    private static double GetRelativeLuminance(string hexColor)
    {
        if (hexColor.StartsWith("#"))
            hexColor = hexColor[1..];

        if (hexColor.Length == 8)
            hexColor = hexColor[2..];

        var r = int.Parse(hexColor[..2], System.Globalization.NumberStyles.HexNumber) / 255.0;
        var g = int.Parse(hexColor[2..4], System.Globalization.NumberStyles.HexNumber) / 255.0;
        var b = int.Parse(hexColor[4..6], System.Globalization.NumberStyles.HexNumber) / 255.0;

        r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
        g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
        b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }
}

/// <summary>
/// Represents color blindness simulation types.
/// </summary>
public enum ColorBlindnessType
{
    None,
    Protanopia,    // Red-blind
    Deuteranopia,  // Green-blind
    Tritanopia,    // Blue-blind
    Achromatopsia  // Total color blindness
}

/// <summary>
/// Represents theme export/import formats.
/// </summary>
public enum ThemeFormat
{
    Json,
    Xml,
    Ase,    // Adobe Swatch Exchange
    Clr     // Color palette file
}

/// <summary>
/// Represents a theme change event.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous theme.
    /// </summary>
    public ThemeDefinition? OldTheme { get; }

    /// <summary>
    /// Gets the new theme.
    /// </summary>
    public ThemeDefinition NewTheme { get; }

    /// <summary>
    /// Gets whether this is a preview change (not yet applied).
    /// </summary>
    public bool IsPreview { get; }

    public ThemeChangedEventArgs(ThemeDefinition? oldTheme, ThemeDefinition newTheme, bool isPreview = false)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
        IsPreview = isPreview;
    }
}
