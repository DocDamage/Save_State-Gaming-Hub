using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Theme.Models;

namespace SaveState.Infrastructure.Theme.Services;

/// <summary>
/// Service for generating Material Design 3 color schemes (Material You).
/// </summary>
public interface IMaterialYouService
{
    /// <summary>
    /// Generates a complete color scheme from a seed color.
    /// </summary>
    ThemeColors GenerateColorScheme(string seedColor, bool isDark);

    /// <summary>
    /// Generates a tonal palette from a seed color.
    /// </summary>
    TonalPalette GenerateTonalPalette(string seedColor);

    /// <summary>
    /// Harmonizes a color towards another color.
    /// </summary>
    string Harmonize(string source, string target, double amount = 0.5);

    /// <summary>
    /// Generates a content-based color scheme from multiple colors.
    /// </summary>
    ThemeColors GenerateFromColors(List<string> colors, bool isDark);

    /// <summary>
    /// Gets the closest matching Material color.
    /// </summary>
    string GetClosestMaterialColor(string color);

    /// <summary>
    /// Generates an analogous color scheme.
    /// </summary>
    List<string> GenerateAnalogousColors(string seedColor, int count = 3);

    /// <summary>
    /// Generates a complementary color scheme.
    /// </summary>
    List<string> GenerateComplementaryColors(string seedColor);

    /// <summary>
    /// Generates a triadic color scheme.
    /// </summary>
    List<string> GenerateTriadicColors(string seedColor);

    /// <summary>
    /// Checks if a color is suitable for dark mode (sufficient chroma).
    /// </summary>
    bool IsSuitableForDarkMode(string color);

    /// <summary>
    /// Suggests accessibility improvements for a color pair.
    /// </summary>
    List<string> SuggestAccessibilityImprovements(string foreground, string background);
}

/// <summary>
/// Implementation of Material You color scheme generation.
/// </summary>
public sealed class MaterialYouService : IMaterialYouService
{
    private readonly ILogger<MaterialYouService> _logger;

    // Standard Material Design 3 tone values
    private static readonly int[] TonalValues = { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 95, 99, 100 };

    // Tone values for different color roles in light theme
    private const int LightPrimaryTone = 40;
    private const int LightOnPrimaryTone = 100;
    private const int LightPrimaryContainerTone = 90;
    private const int LightOnPrimaryContainerTone = 10;
    private const int LightSecondaryTone = 40;
    private const int LightOnSecondaryTone = 100;
    private const int LightSecondaryContainerTone = 90;
    private const int LightOnSecondaryContainerTone = 10;
    private const int LightTertiaryTone = 40;
    private const int LightOnTertiaryTone = 100;
    private const int LightTertiaryContainerTone = 90;
    private const int LightOnTertiaryContainerTone = 10;
    private const int LightErrorTone = 40;
    private const int LightOnErrorTone = 100;
    private const int LightErrorContainerTone = 90;
    private const int LightOnErrorContainerTone = 10;
    private const int LightBackgroundTone = 99;
    private const int LightOnBackgroundTone = 10;
    private const int LightSurfaceTone = 99;
    private const int LightOnSurfaceTone = 10;
    private const int LightSurfaceVariantTone = 90;
    private const int LightOnSurfaceVariantTone = 30;
    private const int LightOutlineTone = 50;
    private const int LightOutlineVariantTone = 80;
    private const int LightInverseSurfaceTone = 20;
    private const int LightInverseOnSurfaceTone = 95;
    private const int LightInversePrimaryTone = 80;
    private const int LightSurfaceContainerLowestTone = 100;
    private const int LightSurfaceContainerLowTone = 96;
    private const int LightSurfaceContainerTone = 94;
    private const int LightSurfaceContainerHighTone = 92;
    private const int LightSurfaceContainerHighestTone = 90;

    // Tone values for different color roles in dark theme
    private const int DarkPrimaryTone = 80;
    private const int DarkOnPrimaryTone = 20;
    private const int DarkPrimaryContainerTone = 30;
    private const int DarkOnPrimaryContainerTone = 90;
    private const int DarkSecondaryTone = 80;
    private const int DarkOnSecondaryTone = 20;
    private const int DarkSecondaryContainerTone = 30;
    private const int DarkOnSecondaryContainerTone = 90;
    private const int DarkTertiaryTone = 80;
    private const int DarkOnTertiaryTone = 20;
    private const int DarkTertiaryContainerTone = 30;
    private const int DarkOnTertiaryContainerTone = 90;
    private const int DarkErrorTone = 80;
    private const int DarkOnErrorTone = 20;
    private const int DarkErrorContainerTone = 30;
    private const int DarkOnErrorContainerTone = 90;
    private const int DarkBackgroundTone = 6;
    private const int DarkOnBackgroundTone = 90;
    private const int DarkSurfaceTone = 6;
    private const int DarkOnSurfaceTone = 90;
    private const int DarkSurfaceVariantTone = 30;
    private const int DarkOnSurfaceVariantTone = 80;
    private const int DarkOutlineTone = 60;
    private const int DarkOutlineVariantTone = 30;
    private const int DarkInverseSurfaceTone = 90;
    private const int DarkInverseOnSurfaceTone = 20;
    private const int DarkInversePrimaryTone = 40;
    private const int DarkSurfaceContainerLowestTone = 4;
    private const int DarkSurfaceContainerLowTone = 10;
    private const int DarkSurfaceContainerTone = 12;
    private const int DarkSurfaceContainerHighTone = 17;
    private const int DarkSurfaceContainerHighestTone = 22;

    // Error color HCT values (standard Material red)
    private static readonly HctColor ErrorHct = new() { Hue = 25, Chroma = 84, Tone = 50 };

    public MaterialYouService(ILogger<MaterialYouService> logger)
    {
        _logger = logger;
    }

    public ThemeColors GenerateColorScheme(string seedColor, bool isDark)
    {
        try
        {
            var seedHct = HctColor.FromArgb(HexToArgb(seedColor));
            var primaryPalette = GenerateTonalPalette(seedColor);

            // Generate secondary palette (shifted hue, lower chroma)
            var secondaryHct = new HctColor
            {
                Hue = (seedHct.Hue + 30) % 360,
                Chroma = seedHct.Chroma * 0.6,
                Tone = seedHct.Tone
            };
            var secondaryPalette = GenerateTonalPalette(ArgbToHex(secondaryHct.ToArgb()));

            // Generate tertiary palette (complementary-ish)
            var tertiaryHct = new HctColor
            {
                Hue = (seedHct.Hue + 120) % 360,
                Chroma = seedHct.Chroma * 0.8,
                Tone = seedHct.Tone
            };
            var tertiaryPalette = GenerateTonalPalette(ArgbToHex(tertiaryHct.ToArgb()));

            // Generate error palette
            var errorPalette = GenerateTonalPalette(ArgbToHex(ErrorHct.ToArgb()));

            // Generate neutral palette (low chroma version of seed)
            var neutralHct = new HctColor
            {
                Hue = seedHct.Hue,
                Chroma = 4, // Low chroma for neutral
                Tone = seedHct.Tone
            };
            var neutralPalette = GenerateTonalPalette(ArgbToHex(neutralHct.ToArgb()));

            // Generate neutral variant palette
            var neutralVariantHct = new HctColor
            {
                Hue = seedHct.Hue,
                Chroma = 8,
                Tone = seedHct.Tone
            };
            var neutralVariantPalette = GenerateTonalPalette(ArgbToHex(neutralVariantHct.ToArgb()));

            var colors = isDark
                ? CreateDarkThemeColors(
                    primaryPalette,
                    secondaryPalette,
                    tertiaryPalette,
                    errorPalette,
                    neutralPalette,
                    neutralVariantPalette)
                : CreateLightThemeColors(
                    primaryPalette,
                    secondaryPalette,
                    tertiaryPalette,
                    errorPalette,
                    neutralPalette,
                    neutralVariantPalette);

            // Add glassmorphism colors
            colors.GlassBackground = isDark ? "#20FFFFFF" : "#20FFFFFF";
            colors.GlassBorder = isDark ? "#40FFFFFF" : "#40FFFFFF";

            // Generate gradients
            colors.AccentGradient = new List<string>
            {
                primaryPalette.Tones.GetValueOrDefault(isDark ? 70 : 50, seedColor),
                primaryPalette.Tones.GetValueOrDefault(isDark ? 80 : 60, seedColor)
            };

            colors.SuccessGradient = new List<string> { "#4CAF50", "#81C784" };
            colors.WarningGradient = new List<string> { "#FF9800", "#FFB74D" };
            colors.ErrorGradient = new List<string> { "#F44336", "#E57373" };

            _logger.LogInformation("Generated {Mode} color scheme from seed {Seed}",
                isDark ? "dark" : "light", seedColor);

            return colors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate color scheme");
            return isDark ? new ThemeColors() : new ThemeColors(); // Return defaults
        }
    }

    public TonalPalette GenerateTonalPalette(string seedColor)
    {
        return TonalPalette.FromSeed(seedColor);
    }

    public string Harmonize(string source, string target, double amount = 0.5)
    {
        var sourceHct = HctColor.FromArgb(HexToArgb(source));
        var targetHct = HctColor.FromArgb(HexToArgb(target));

        // Blend hue towards target
        var hueDiff = targetHct.Hue - sourceHct.Hue;
        // Handle circular hue
        if (hueDiff > 180) hueDiff -= 360;
        if (hueDiff < -180) hueDiff += 360;

        var blendedHue = (sourceHct.Hue + hueDiff * amount + 360) % 360;
        var blendedChroma = sourceHct.Chroma * (1 - amount) + targetHct.Chroma * amount;

        var resultHct = new HctColor
        {
            Hue = blendedHue,
            Chroma = blendedChroma,
            Tone = sourceHct.Tone
        };

        return ArgbToHex(resultHct.ToArgb());
    }

    public ThemeColors GenerateFromColors(List<string> colors, bool isDark)
    {
        if (colors.Count == 0)
        {
            return isDark ? new ThemeColors() : new ThemeColors();
        }

        // Find the most vibrant color as primary
        var primaryColor = colors.OrderByDescending(GetChroma).First();

        // Find secondary and tertiary by hue distance
        var primaryHct = HctColor.FromArgb(HexToArgb(primaryColor));
        var remainingColors = colors.Where(c => c != primaryColor).ToList();

        string? secondaryColor = null;
        string? tertiaryColor = null;

        if (remainingColors.Count > 0)
        {
            // Pick color with hue closest to primary + 30
            secondaryColor = remainingColors
                .OrderBy(c => Math.Abs(GetHueDifference(HctColor.FromArgb(HexToArgb(c)).Hue, (primaryHct.Hue + 30) % 360)))
                .First();

            remainingColors.Remove(secondaryColor);
        }

        if (remainingColors.Count > 0)
        {
            // Pick color with hue closest to primary + 120
            tertiaryColor = remainingColors
                .OrderBy(c => Math.Abs(GetHueDifference(HctColor.FromArgb(HexToArgb(c)).Hue, (primaryHct.Hue + 120) % 360)))
                .First();
        }

        // Generate scheme from primary
        var scheme = GenerateColorScheme(primaryColor, isDark);

        // Harmonize secondary and tertiary if found
        if (secondaryColor != null)
        {
            scheme.Secondary = Harmonize(secondaryColor, primaryColor, 0.3);
        }

        if (tertiaryColor != null)
        {
            scheme.Tertiary = Harmonize(tertiaryColor, primaryColor, 0.3);
        }

        return scheme;
    }

    public string GetClosestMaterialColor(string color)
    {
        // Material Design color palette
        var materialColors = new Dictionary<string, string>
        {
            ["Red"] = "#F44336",
            ["Pink"] = "#E91E63",
            ["Purple"] = "#9C27B0",
            ["Deep Purple"] = "#673AB7",
            ["Indigo"] = "#3F51B5",
            ["Blue"] = "#2196F3",
            ["Light Blue"] = "#03A9F4",
            ["Cyan"] = "#00BCD4",
            ["Teal"] = "#009688",
            ["Green"] = "#4CAF50",
            ["Light Green"] = "#8BC34A",
            ["Lime"] = "#CDDC39",
            ["Yellow"] = "#FFEB3B",
            ["Amber"] = "#FFC107",
            ["Orange"] = "#FF9800",
            ["Deep Orange"] = "#FF5722",
            ["Brown"] = "#795548",
            ["Grey"] = "#9E9E9E",
            ["Blue Grey"] = "#607D8B"
        };

        var inputHct = HctColor.FromArgb(HexToArgb(color));
        string? closestName = null;
        double closestDistance = double.MaxValue;

        foreach (var (name, hex) in materialColors)
        {
            var materialHct = HctColor.FromArgb(HexToArgb(hex));
            var distance = CalculateHctDistance(inputHct, materialHct);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestName = name;
            }
        }

        return closestName ?? "Unknown";
    }

    public List<string> GenerateAnalogousColors(string seedColor, int count = 3)
    {
        var seedHct = HctColor.FromArgb(HexToArgb(seedColor));
        var colors = new List<string> { seedColor };

        var step = 30.0; // 30 degrees apart
        for (int i = 1; i < count; i++)
        {
            var hue = (seedHct.Hue + step * i) % 360;
            var newHct = new HctColor
            {
                Hue = hue,
                Chroma = seedHct.Chroma,
                Tone = seedHct.Tone
            };
            colors.Add(ArgbToHex(newHct.ToArgb()));
        }

        return colors;
    }

    public List<string> GenerateComplementaryColors(string seedColor)
    {
        var seedHct = HctColor.FromArgb(HexToArgb(seedColor));
        var complementaryHue = (seedHct.Hue + 180) % 360;

        return new List<string>
        {
            seedColor,
            ArgbToHex(new HctColor
            {
                Hue = complementaryHue,
                Chroma = seedHct.Chroma,
                Tone = seedHct.Tone
            }.ToArgb())
        };
    }

    public List<string> GenerateTriadicColors(string seedColor)
    {
        var seedHct = HctColor.FromArgb(HexToArgb(seedColor));
        var colors = new List<string> { seedColor };

        for (int i = 1; i < 3; i++)
        {
            var hue = (seedHct.Hue + 120 * i) % 360;
            colors.Add(ArgbToHex(new HctColor
            {
                Hue = hue,
                Chroma = seedHct.Chroma,
                Tone = seedHct.Tone
            }.ToArgb()));
        }

        return colors;
    }

    public bool IsSuitableForDarkMode(string color)
    {
        var hct = HctColor.FromArgb(HexToArgb(color));
        // Dark mode needs sufficient chroma to be visible
        return hct.Chroma >= 15;
    }

    public List<string> SuggestAccessibilityImprovements(string foreground, string background)
    {
        var suggestions = new List<string>();
        var contrast = ContrastInfo.Calculate(foreground, background);

        if (!contrast.PassesAaNormal)
        {
            var fgHct = HctColor.FromArgb(HexToArgb(foreground));
            var bgHct = HctColor.FromArgb(HexToArgb(background));
            var isBgDark = bgHct.Tone < 50;

            // Suggest lighter or darker foreground
            if (isBgDark)
            {
                var suggestedTone = Math.Min(100, fgHct.Tone + 20);
                suggestions.Add($"Try lightening foreground to tone {suggestedTone:F0}");
            }
            else
            {
                var suggestedTone = Math.Max(0, fgHct.Tone - 20);
                suggestions.Add($"Try darkening foreground to tone {suggestedTone:F0}");
            }

            // Suggest adjusting background
            if (!isBgDark)
            {
                var suggestedBgTone = Math.Max(0, bgHct.Tone - 10);
                suggestions.Add($"Try darkening background to tone {suggestedBgTone:F0}");
            }
            else
            {
                var suggestedBgTone = Math.Min(100, bgHct.Tone + 10);
                suggestions.Add($"Try lightening background to tone {suggestedBgTone:F0}");
            }

            // Suggest larger text
            suggestions.Add("Consider using larger text (18pt+ or 14pt+ bold) which requires less contrast");
        }
        else if (!contrast.PassesAaaNormal)
        {
            suggestions.Add("Contrast passes AA standard. For AAA compliance, increase contrast ratio to 7:1");
        }
        else
        {
            suggestions.Add("Great! This color combination meets AAA accessibility standards.");
        }

        return suggestions;
    }

    private ThemeColors CreateLightThemeColors(
        TonalPalette primary,
        TonalPalette secondary,
        TonalPalette tertiary,
        TonalPalette error,
        TonalPalette neutral,
        TonalPalette neutralVariant)
    {
        return new ThemeColors
        {
            Primary = primary.Tones.GetValueOrDefault(LightPrimaryTone, primary.SeedColor),
            OnPrimary = primary.Tones.GetValueOrDefault(LightOnPrimaryTone, "#FFFFFF"),
            PrimaryContainer = primary.Tones.GetValueOrDefault(LightPrimaryContainerTone, primary.Tones.GetValueOrDefault(90)),
            OnPrimaryContainer = primary.Tones.GetValueOrDefault(LightOnPrimaryContainerTone, primary.Tones.GetValueOrDefault(10)),

            Secondary = secondary.Tones.GetValueOrDefault(LightSecondaryTone, secondary.SeedColor),
            OnSecondary = secondary.Tones.GetValueOrDefault(LightOnSecondaryTone, "#FFFFFF"),
            SecondaryContainer = secondary.Tones.GetValueOrDefault(LightSecondaryContainerTone, secondary.Tones.GetValueOrDefault(90)),
            OnSecondaryContainer = secondary.Tones.GetValueOrDefault(LightOnSecondaryContainerTone, secondary.Tones.GetValueOrDefault(10)),

            Tertiary = tertiary.Tones.GetValueOrDefault(LightTertiaryTone, tertiary.SeedColor),
            OnTertiary = tertiary.Tones.GetValueOrDefault(LightOnTertiaryTone, "#FFFFFF"),
            TertiaryContainer = tertiary.Tones.GetValueOrDefault(LightTertiaryContainerTone, tertiary.Tones.GetValueOrDefault(90)),
            OnTertiaryContainer = tertiary.Tones.GetValueOrDefault(LightOnTertiaryContainerTone, tertiary.Tones.GetValueOrDefault(10)),

            Error = error.Tones.GetValueOrDefault(LightErrorTone, "#B3261E"),
            OnError = error.Tones.GetValueOrDefault(LightOnErrorTone, "#FFFFFF"),
            ErrorContainer = error.Tones.GetValueOrDefault(LightErrorContainerTone, error.Tones.GetValueOrDefault(90)),
            OnErrorContainer = error.Tones.GetValueOrDefault(LightOnErrorContainerTone, error.Tones.GetValueOrDefault(10)),

            Background = neutral.Tones.GetValueOrDefault(LightBackgroundTone, "#FFFBFE"),
            OnBackground = neutral.Tones.GetValueOrDefault(LightOnBackgroundTone, "#1C1B1F"),
            Surface = neutral.Tones.GetValueOrDefault(LightSurfaceTone, "#FFFBFE"),
            OnSurface = neutral.Tones.GetValueOrDefault(LightOnSurfaceTone, "#1C1B1F"),
            SurfaceVariant = neutralVariant.Tones.GetValueOrDefault(LightSurfaceVariantTone, neutralVariant.Tones.GetValueOrDefault(90)),
            OnSurfaceVariant = neutralVariant.Tones.GetValueOrDefault(LightOnSurfaceVariantTone, neutralVariant.Tones.GetValueOrDefault(30)),

            Outline = neutralVariant.Tones.GetValueOrDefault(LightOutlineTone, neutralVariant.Tones.GetValueOrDefault(50)),
            OutlineVariant = neutralVariant.Tones.GetValueOrDefault(LightOutlineVariantTone, neutralVariant.Tones.GetValueOrDefault(80)),

            InverseSurface = neutral.Tones.GetValueOrDefault(LightInverseSurfaceTone, neutral.Tones.GetValueOrDefault(20)),
            InverseOnSurface = neutral.Tones.GetValueOrDefault(LightInverseOnSurfaceTone, neutral.Tones.GetValueOrDefault(95)),
            InversePrimary = primary.Tones.GetValueOrDefault(LightInversePrimaryTone, primary.Tones.GetValueOrDefault(80)),

            SurfaceContainerLowest = neutral.Tones.GetValueOrDefault(LightSurfaceContainerLowestTone, "#FFFFFF"),
            SurfaceContainerLow = neutral.Tones.GetValueOrDefault(LightSurfaceContainerLowTone, neutral.Tones.GetValueOrDefault(96)),
            SurfaceContainer = neutral.Tones.GetValueOrDefault(LightSurfaceContainerTone, neutral.Tones.GetValueOrDefault(94)),
            SurfaceContainerHigh = neutral.Tones.GetValueOrDefault(LightSurfaceContainerHighTone, neutral.Tones.GetValueOrDefault(92)),
            SurfaceContainerHighest = neutral.Tones.GetValueOrDefault(LightSurfaceContainerHighestTone, neutral.Tones.GetValueOrDefault(90))
        };
    }

    private ThemeColors CreateDarkThemeColors(
        TonalPalette primary,
        TonalPalette secondary,
        TonalPalette tertiary,
        TonalPalette error,
        TonalPalette neutral,
        TonalPalette neutralVariant)
    {
        return new ThemeColors
        {
            Primary = primary.Tones.GetValueOrDefault(DarkPrimaryTone, primary.Tones.GetValueOrDefault(80)),
            OnPrimary = primary.Tones.GetValueOrDefault(DarkOnPrimaryTone, primary.Tones.GetValueOrDefault(20)),
            PrimaryContainer = primary.Tones.GetValueOrDefault(DarkPrimaryContainerTone, primary.Tones.GetValueOrDefault(30)),
            OnPrimaryContainer = primary.Tones.GetValueOrDefault(DarkOnPrimaryContainerTone, primary.Tones.GetValueOrDefault(90)),

            Secondary = secondary.Tones.GetValueOrDefault(DarkSecondaryTone, secondary.Tones.GetValueOrDefault(80)),
            OnSecondary = secondary.Tones.GetValueOrDefault(DarkOnSecondaryTone, secondary.Tones.GetValueOrDefault(20)),
            SecondaryContainer = secondary.Tones.GetValueOrDefault(DarkSecondaryContainerTone, secondary.Tones.GetValueOrDefault(30)),
            OnSecondaryContainer = secondary.Tones.GetValueOrDefault(DarkOnSecondaryContainerTone, secondary.Tones.GetValueOrDefault(90)),

            Tertiary = tertiary.Tones.GetValueOrDefault(DarkTertiaryTone, tertiary.Tones.GetValueOrDefault(80)),
            OnTertiary = tertiary.Tones.GetValueOrDefault(DarkOnTertiaryTone, tertiary.Tones.GetValueOrDefault(20)),
            TertiaryContainer = tertiary.Tones.GetValueOrDefault(DarkTertiaryContainerTone, tertiary.Tones.GetValueOrDefault(30)),
            OnTertiaryContainer = tertiary.Tones.GetValueOrDefault(DarkOnTertiaryContainerTone, tertiary.Tones.GetValueOrDefault(90)),

            Error = error.Tones.GetValueOrDefault(DarkErrorTone, error.Tones.GetValueOrDefault(80)),
            OnError = error.Tones.GetValueOrDefault(DarkOnErrorTone, error.Tones.GetValueOrDefault(20)),
            ErrorContainer = error.Tones.GetValueOrDefault(DarkErrorContainerTone, error.Tones.GetValueOrDefault(30)),
            OnErrorContainer = error.Tones.GetValueOrDefault(DarkOnErrorContainerTone, error.Tones.GetValueOrDefault(90)),

            Background = neutral.Tones.GetValueOrDefault(DarkBackgroundTone, "#1C1B1F"),
            OnBackground = neutral.Tones.GetValueOrDefault(DarkOnBackgroundTone, "#E6E1E5"),
            Surface = neutral.Tones.GetValueOrDefault(DarkSurfaceTone, "#1C1B1F"),
            OnSurface = neutral.Tones.GetValueOrDefault(DarkOnSurfaceTone, "#E6E1E5"),
            SurfaceVariant = neutralVariant.Tones.GetValueOrDefault(DarkSurfaceVariantTone, neutralVariant.Tones.GetValueOrDefault(30)),
            OnSurfaceVariant = neutralVariant.Tones.GetValueOrDefault(DarkOnSurfaceVariantTone, neutralVariant.Tones.GetValueOrDefault(80)),

            Outline = neutralVariant.Tones.GetValueOrDefault(DarkOutlineTone, neutralVariant.Tones.GetValueOrDefault(60)),
            OutlineVariant = neutralVariant.Tones.GetValueOrDefault(DarkOutlineVariantTone, neutralVariant.Tones.GetValueOrDefault(30)),

            InverseSurface = neutral.Tones.GetValueOrDefault(DarkInverseSurfaceTone, neutral.Tones.GetValueOrDefault(90)),
            InverseOnSurface = neutral.Tones.GetValueOrDefault(DarkInverseOnSurfaceTone, neutral.Tones.GetValueOrDefault(20)),
            InversePrimary = primary.Tones.GetValueOrDefault(DarkInversePrimaryTone, primary.Tones.GetValueOrDefault(40)),

            SurfaceContainerLowest = neutral.Tones.GetValueOrDefault(DarkSurfaceContainerLowestTone, "#0F0F11"),
            SurfaceContainerLow = neutral.Tones.GetValueOrDefault(DarkSurfaceContainerLowTone, neutral.Tones.GetValueOrDefault(10)),
            SurfaceContainer = neutral.Tones.GetValueOrDefault(DarkSurfaceContainerTone, neutral.Tones.GetValueOrDefault(12)),
            SurfaceContainerHigh = neutral.Tones.GetValueOrDefault(DarkSurfaceContainerHighTone, neutral.Tones.GetValueOrDefault(17)),
            SurfaceContainerHighest = neutral.Tones.GetValueOrDefault(DarkSurfaceContainerHighestTone, neutral.Tones.GetValueOrDefault(22))
        };
    }

    private static double GetChroma(string color)
    {
        var hct = HctColor.FromArgb(HexToArgb(color));
        return hct.Chroma;
    }

    private static double GetHueDifference(double hue1, double hue2)
    {
        var diff = Math.Abs(hue1 - hue2);
        return diff > 180 ? 360 - diff : diff;
    }

    private static double CalculateHctDistance(HctColor a, HctColor b)
    {
        // Simplified distance calculation in HCT space
        var hueDiff = GetHueDifference(a.Hue, b.Hue) / 180.0;
        var chromaDiff = (a.Chroma - b.Chroma) / 150.0;
        var toneDiff = (a.Tone - b.Tone) / 100.0;

        return Math.Sqrt(hueDiff * hueDiff + chromaDiff * chromaDiff + toneDiff * toneDiff);
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
