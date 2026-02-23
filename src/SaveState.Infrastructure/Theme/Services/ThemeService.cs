using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Theme.Models;
using SaveState.Core.Theme.Services;

namespace SaveState.Infrastructure.Theme.Services;

/// <summary>
/// Implementation of the theme service for managing themes.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly string _themesDirectory;
    private readonly ConcurrentDictionary<Guid, ThemeDefinition> _themes = new();
    private ThemeDefinition _currentTheme;
    private ThemeDefinition? _previewTheme;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ThemeDefinition CurrentTheme => _previewTheme ?? _currentTheme;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeService(
        ILogger<ThemeService> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _themesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SaveStateReborn",
            "Themes");

        _currentTheme = GetDefaultDarkTheme();

        // Ensure themes directory exists
        Directory.CreateDirectory(_themesDirectory);

        // Initialize built-in themes
        InitializeBuiltInThemes();

        // Load saved themes
        _ = LoadThemesAsync();
    }

    private void InitializeBuiltInThemes()
    {
        var lightTheme = GetDefaultLightTheme();
        var darkTheme = GetDefaultDarkTheme();
        var systemTheme = GetSystemTheme();

        _themes[lightTheme.Id] = lightTheme;
        _themes[darkTheme.Id] = darkTheme;
        _themes[systemTheme.Id] = systemTheme;
    }

    public Task<Result<ThemeDefinition>> GetCurrentThemeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<ThemeDefinition>.Success(CurrentTheme));
    }

    public Task<Result> ApplyThemeAsync(Guid themeId, CancellationToken cancellationToken = default)
    {
        if (!_themes.TryGetValue(themeId, out var theme))
        {
            return Task.FromResult(Result.Failure($"Theme {themeId} not found", ErrorType.NotFound));
        }

        return ApplyThemeAsync(theme, cancellationToken);
    }

    public Task<Result> ApplyThemeAsync(ThemeDefinition theme, CancellationToken cancellationToken = default)
    {
        try
        {
            var oldTheme = _currentTheme;
            _currentTheme = theme;
            _previewTheme = null;

            // Save as active theme preference
            _ = SaveActiveThemeAsync(theme.Id);

            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme, false));
            _logger.LogInformation("Applied theme: {ThemeName} ({ThemeId})", theme.Name, theme.Id);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme {ThemeId}", theme.Id);
            return Task.FromResult(Result.Failure($"Failed to apply theme: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> PreviewThemeAsync(ThemeDefinition theme, CancellationToken cancellationToken = default)
    {
        try
        {
            var oldTheme = _previewTheme ?? _currentTheme;
            _previewTheme = theme;

            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme, true));
            _logger.LogDebug("Previewing theme: {ThemeName}", theme.Name);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview theme");
            return Task.FromResult(Result.Failure($"Failed to preview theme: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<IReadOnlyList<ThemeDefinition>>> GetAllThemesAsync(CancellationToken cancellationToken = default)
    {
        var themes = _themes.Values
            .OrderBy(t => t.IsBuiltIn ? 0 : 1)
            .ThenBy(t => t.Name)
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<ThemeDefinition>>.Success(themes));
    }

    public Task<Result<ThemeDefinition>> GetThemeAsync(Guid themeId, CancellationToken cancellationToken = default)
    {
        if (_themes.TryGetValue(themeId, out var theme))
        {
            return Task.FromResult(Result<ThemeDefinition>.Success(theme));
        }

        return Task.FromResult(Result<ThemeDefinition>.Failure($"Theme {themeId} not found", ErrorType.NotFound));
    }

    public Task<Result<ThemeDefinition>> CreateThemeAsync(string name, ThemeDefinition? baseTheme = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.FromResult(Result<ThemeDefinition>.Failure("Theme name cannot be empty", ErrorType.Validation));
            }

            var now = _timeProvider.UtcNow;
            var theme = baseTheme is not null
                ? baseTheme with
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    IsBuiltIn = false,
                    CreatedAt = now,
                    ModifiedAt = now
                }
                : new ThemeDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    IsBuiltIn = false,
                    IsDark = false,
                    CreatedAt = now,
                    ModifiedAt = now
                };
            theme.ModifiedAt = now;

            _themes[theme.Id] = theme;
            _ = SaveThemesAsync();

            _logger.LogInformation("Created theme: {ThemeName} ({ThemeId})", theme.Name, theme.Id);
            return Task.FromResult(Result<ThemeDefinition>.Success(theme));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create theme");
            return Task.FromResult(Result<ThemeDefinition>.Failure($"Failed to create theme: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> UpdateThemeAsync(ThemeDefinition theme, CancellationToken cancellationToken = default)
    {
        try
        {
            if (theme.IsBuiltIn)
            {
                return Task.FromResult(Result.Failure("Cannot modify built-in themes", ErrorType.Forbidden));
            }

            theme.ModifiedAt = _timeProvider.UtcNow;
            _themes[theme.Id] = theme;
            _ = SaveThemesAsync();

            // If this is the current theme, trigger update
            if (_currentTheme.Id == theme.Id)
            {
                _currentTheme = theme;
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(_currentTheme, theme, false));
            }

            _logger.LogInformation("Updated theme: {ThemeName} ({ThemeId})", theme.Name, theme.Id);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update theme");
            return Task.FromResult(Result.Failure($"Failed to update theme: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> DeleteThemeAsync(Guid themeId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_themes.TryGetValue(themeId, out var theme))
            {
                return Task.FromResult(Result.Failure($"Theme {themeId} not found", ErrorType.NotFound));
            }

            if (theme.IsBuiltIn)
            {
                return Task.FromResult(Result.Failure("Cannot delete built-in themes", ErrorType.Forbidden));
            }

            if (_currentTheme.Id == themeId)
            {
                return Task.FromResult(Result.Failure("Cannot delete the currently active theme", ErrorType.Conflict));
            }

            _themes.TryRemove(themeId, out _);
            _ = SaveThemesAsync();

            // Delete theme file
            var themeFile = Path.Combine(_themesDirectory, $"{themeId}.json");
            if (File.Exists(themeFile))
            {
                File.Delete(themeFile);
            }

            _logger.LogInformation("Deleted theme: {ThemeName} ({ThemeId})", theme.Name, themeId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete theme");
            return Task.FromResult(Result.Failure($"Failed to delete theme: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<ThemeDefinition>> DuplicateThemeAsync(Guid themeId, string newName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_themes.TryGetValue(themeId, out var sourceTheme))
            {
                return Task.FromResult(Result<ThemeDefinition>.Failure($"Theme {themeId} not found", ErrorType.NotFound));
            }

            var duplicate = sourceTheme.Copy(newName);
            _themes[duplicate.Id] = duplicate;
            _ = SaveThemesAsync();

            _logger.LogInformation("Duplicated theme {SourceId} to {NewName} ({NewId})", themeId, newName, duplicate.Id);
            return Task.FromResult(Result<ThemeDefinition>.Success(duplicate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate theme");
            return Task.FromResult(Result<ThemeDefinition>.Failure($"Failed to duplicate theme: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<ThemeDefinition>> ImportThemeAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            var theme = JsonSerializer.Deserialize<ThemeDefinition>(json, JsonOptions);
            if (theme == null)
            {
                return Task.FromResult(Result<ThemeDefinition>.Failure("Invalid theme JSON", ErrorType.Validation));
            }

            theme.Id = Guid.NewGuid();
            theme.IsBuiltIn = false;
            theme.CreatedAt = _timeProvider.UtcNow;
            theme.ModifiedAt = theme.CreatedAt;

            _themes[theme.Id] = theme;
            _ = SaveThemesAsync();

            _logger.LogInformation("Imported theme: {ThemeName} ({ThemeId})", theme.Name, theme.Id);
            return Task.FromResult(Result<ThemeDefinition>.Success(theme));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse theme JSON");
            return Task.FromResult(Result<ThemeDefinition>.Failure($"Invalid JSON: {ex.Message}", ErrorType.Validation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import theme");
            return Task.FromResult(Result<ThemeDefinition>.Failure($"Failed to import theme: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<string>> ExportThemeAsync(Guid themeId, ThemeFormat format = ThemeFormat.Json, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_themes.TryGetValue(themeId, out var theme))
            {
                return Task.FromResult(Result<string>.Failure($"Theme {themeId} not found", ErrorType.NotFound));
            }

            string export;
            switch (format)
            {
                case ThemeFormat.Json:
                    export = JsonSerializer.Serialize(theme, JsonOptions);
                    break;
                case ThemeFormat.Xml:
                    // Simple XML serialization
                    export = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Theme>\n  <Id>{theme.Id}</Id>\n  <Name>{theme.Name}</Name>\n  <IsDark>{theme.IsDark}</IsDark>\n  <Colors>\n    <Primary>{theme.Colors.Primary}</Primary>\n    <Secondary>{theme.Colors.Secondary}</Secondary>\n    <Background>{theme.Colors.Background}</Background>\n  </Colors>\n</Theme>";
                    break;
                default:
                    return Task.FromResult(Result<string>.Failure($"Export format {format} not supported", ErrorType.NotImplemented));
            }

            return Task.FromResult(Result<string>.Success(export));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export theme");
            return Task.FromResult(Result<string>.Failure($"Failed to export theme: {ex.Message}", ErrorType.Internal));
        }
    }

    public async Task<Result<ThemeDefinition>> ImportFromImageAsync(Stream imageStream, string themeName, CancellationToken cancellationToken = default)
    {
        try
        {
            var colorsResult = await ExtractColorsFromImageAsync(imageStream, 5, cancellationToken);
            if (colorsResult.IsFailure)
            {
                return Result<ThemeDefinition>.Failure(colorsResult.Error!, colorsResult.ErrorType);
            }

            var colors = colorsResult.Value!;
            if (colors.Count == 0)
            {
                return Result<ThemeDefinition>.Failure("Could not extract colors from image", ErrorType.External);
            }

            // Use dominant color as seed
            var seedColor = colors[0];
            var isDark = IsDarkColor(seedColor);

            var theme = new ThemeDefinition
            {
                Id = Guid.NewGuid(),
                Name = themeName,
                IsBuiltIn = false,
                IsDark = isDark,
                Colors = GenerateFromSeedColor(seedColor, isDark),
                CreatedAt = _timeProvider.UtcNow,
                ModifiedAt = _timeProvider.UtcNow
            };

            _themes[theme.Id] = theme;
            _ = SaveThemesAsync();

            _logger.LogInformation("Created theme from image: {ThemeName} ({ThemeId})", theme.Name, theme.Id);
            return Result<ThemeDefinition>.Success(theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import theme from image");
            return Result<ThemeDefinition>.Failure($"Failed to import from image: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<IReadOnlyList<ThemeDefinition>>> GetBuiltInThemesAsync(CancellationToken cancellationToken = default)
    {
        var builtInThemes = _themes.Values
            .Where(t => t.IsBuiltIn)
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<ThemeDefinition>>.Success(builtInThemes));
    }

    public ThemeDefinition GetDefaultLightTheme()
    {
        var now = _timeProvider?.UtcNow ?? DateTime.UtcNow;
        return new ThemeDefinition
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Default Light",
            IsBuiltIn = true,
            IsDark = false,
            Colors = new ThemeColors
            {
                Primary = "#6750A4",
                OnPrimary = "#FFFFFF",
                PrimaryContainer = "#EADDFF",
                OnPrimaryContainer = "#21005D",
                Secondary = "#625B71",
                OnSecondary = "#FFFFFF",
                SecondaryContainer = "#E8DEF8",
                OnSecondaryContainer = "#1D192B",
                Tertiary = "#7D5260",
                OnTertiary = "#FFFFFF",
                TertiaryContainer = "#FFD8E4",
                OnTertiaryContainer = "#31111D",
                Error = "#B3261E",
                OnError = "#FFFFFF",
                ErrorContainer = "#F9DEDC",
                OnErrorContainer = "#410E0B",
                Background = "#FFFBFE",
                OnBackground = "#1C1B1F",
                Surface = "#FFFBFE",
                OnSurface = "#1C1B1F",
                SurfaceVariant = "#E7E0EC",
                OnSurfaceVariant = "#49454F",
                Outline = "#79747E",
                OutlineVariant = "#CAC4D0",
                InverseSurface = "#313033",
                InverseOnSurface = "#F4EFF4",
                InversePrimary = "#D0BCFF",
                SurfaceContainerLowest = "#FFFFFF",
                SurfaceContainerLow = "#F7F2FA",
                SurfaceContainer = "#F3EDF7",
                SurfaceContainerHigh = "#ECE6F0",
                SurfaceContainerHighest = "#E6E0E9",
                GlassBackground = "#20FFFFFF",
                GlassBorder = "#40FFFFFF"
            },
            CreatedAt = now,
            ModifiedAt = now
        };
    }

    public ThemeDefinition GetDefaultDarkTheme()
    {
        var now = _timeProvider?.UtcNow ?? DateTime.UtcNow;
        return new ThemeDefinition
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Default Dark",
            IsBuiltIn = true,
            IsDark = true,
            Colors = new ThemeColors
            {
                Primary = "#D0BCFF",
                OnPrimary = "#381E72",
                PrimaryContainer = "#4F378B",
                OnPrimaryContainer = "#EADDFF",
                Secondary = "#CCC2DC",
                OnSecondary = "#332D41",
                SecondaryContainer = "#4A4458",
                OnSecondaryContainer = "#E8DEF8",
                Tertiary = "#EFB8C8",
                OnTertiary = "#492532",
                TertiaryContainer = "#633B48",
                OnTertiaryContainer = "#FFD8E4",
                Error = "#F2B8B5",
                OnError = "#601410",
                ErrorContainer = "#8C1D18",
                OnErrorContainer = "#F9DEDC",
                Background = "#1C1B1F",
                OnBackground = "#E6E1E5",
                Surface = "#1C1B1F",
                OnSurface = "#E6E1E5",
                SurfaceVariant = "#49454F",
                OnSurfaceVariant = "#CAC4D0",
                Outline = "#938F99",
                OutlineVariant = "#49454F",
                InverseSurface = "#E6E1E5",
                InverseOnSurface = "#1C1B1F",
                InversePrimary = "#6750A4",
                SurfaceContainerLowest = "#0F0F11",
                SurfaceContainerLow = "#1C1B1F",
                SurfaceContainer = "#211F26",
                SurfaceContainerHigh = "#2B2930",
                SurfaceContainerHighest = "#36343B",
                GlassBackground = "#201C1B1F",
                GlassBorder = "#40FFFFFF"
            },
            CreatedAt = now,
            ModifiedAt = now
        };
    }

    public ThemeDefinition GetSystemTheme()
    {
        var now = _timeProvider?.UtcNow ?? DateTime.UtcNow;
        return new ThemeDefinition
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "System",
            IsBuiltIn = true,
            IsDark = false, // Will be determined at runtime
            Colors = GetDefaultDarkTheme().Colors.Copy(),
            CreatedAt = now,
            ModifiedAt = now
        };
    }

    public string Harmonize(string source, string target)
    {
        // Simplified harmonization - blend colors
        var sourceArgb = HexToArgb(source);
        var targetArgb = HexToArgb(target);

        var blend = BlendColors(sourceArgb, targetArgb, 0.3);
        return ArgbToHex(blend);
    }

    public List<string> GeneratePalette(string seedColor, int count = 5)
    {
        var palette = new List<string>();
        var baseHct = HctColor.FromArgb(HexToArgb(seedColor));

        for (int i = 0; i < count; i++)
        {
            var hueShift = (i * 30) % 360;
            var newHue = (baseHct.Hue + hueShift) % 360;
            var variant = new HctColor
            {
                Hue = newHue,
                Chroma = baseHct.Chroma * (0.8 + i * 0.1),
                Tone = baseHct.Tone
            };
            palette.Add(ArgbToHex(variant.ToArgb()));
        }

        return palette;
    }

    public ThemeColors GenerateFromSeedColor(string seedColor, bool isDark)
    {
        var palette = TonalPalette.FromSeed(seedColor);
        var baseHct = HctColor.FromArgb(HexToArgb(seedColor));

        if (isDark)
        {
            return new ThemeColors
            {
                Primary = palette.Tones.GetValueOrDefault(80, "#D0BCFF"),
                OnPrimary = palette.Tones.GetValueOrDefault(20, "#381E72"),
                PrimaryContainer = palette.Tones.GetValueOrDefault(30, "#4F378B"),
                OnPrimaryContainer = palette.Tones.GetValueOrDefault(90, "#EADDFF"),
                Secondary = Harmonize(palette.Tones.GetValueOrDefault(80, "#CCC2DC"), seedColor),
                OnSecondary = palette.Tones.GetValueOrDefault(20, "#332D41"),
                SecondaryContainer = palette.Tones.GetValueOrDefault(30, "#4A4458"),
                OnSecondaryContainer = palette.Tones.GetValueOrDefault(90, "#E8DEF8"),
                Background = "#1C1B1F",
                OnBackground = "#E6E1E5",
                Surface = "#1C1B1F",
                OnSurface = "#E6E1E5"
            };
        }
        else
        {
            return new ThemeColors
            {
                Primary = palette.Tones.GetValueOrDefault(40, "#6750A4"),
                OnPrimary = palette.Tones.GetValueOrDefault(100, "#FFFFFF"),
                PrimaryContainer = palette.Tones.GetValueOrDefault(90, "#EADDFF"),
                OnPrimaryContainer = palette.Tones.GetValueOrDefault(10, "#21005D"),
                Secondary = Harmonize(palette.Tones.GetValueOrDefault(40, "#625B71"), seedColor),
                OnSecondary = palette.Tones.GetValueOrDefault(100, "#FFFFFF"),
                SecondaryContainer = palette.Tones.GetValueOrDefault(90, "#E8DEF8"),
                OnSecondaryContainer = palette.Tones.GetValueOrDefault(10, "#1D192B"),
                Background = "#FFFBFE",
                OnBackground = "#1C1B1F",
                Surface = "#FFFBFE",
                OnSurface = "#1C1B1F"
            };
        }
    }

    public ContrastInfo CalculateContrast(string foreground, string background)
    {
        return ContrastInfo.Calculate(foreground, background);
    }

    public string SimulateColorBlindness(string color, ColorBlindnessType type)
    {
        if (type == ColorBlindnessType.None)
            return color;

        var argb = HexToArgb(color);
        var r = (int)((argb >> 16) & 0xFF);
        var g = (int)((argb >> 8) & 0xFF);
        var b = (int)(argb & 0xFF);

        (int newR, int newG, int newB) = type switch
        {
            ColorBlindnessType.Protanopia => ((int)(0.567 * r + 0.433 * g), (int)(0.558 * r + 0.442 * g), b),
            ColorBlindnessType.Deuteranopia => ((int)(0.625 * r + 0.375 * g), (int)(0.7 * r + 0.3 * g), b),
            ColorBlindnessType.Tritanopia => (r, (int)(0.95 * g + 0.05 * b), (int)(0.433 * g + 0.567 * b)),
            ColorBlindnessType.Achromatopsia => ((int)(0.299 * r + 0.587 * g + 0.114 * b), (int)(0.299 * r + 0.587 * g + 0.114 * b), (int)(0.299 * r + 0.587 * g + 0.114 * b)),
            _ => (r, g, b)
        };

        return $"#{(0xFF << 24) | ((uint)newR << 16) | ((uint)newG << 8) | (uint)newB:X8}";
    }

    public Task<Result<List<string>>> ExtractColorsFromImageAsync(Stream imageStream, int colorCount = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simplified color extraction - in production, use image processing library
            // For now, return mock colors based on the stream
            var colors = new List<string>
            {
                "#6750A4",
                "#625B71",
                "#7D5260",
                "#B3261E",
                "#FFFBFE"
            };

            return Task.FromResult(Result<List<string>>.Success(colors.Take(colorCount).ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract colors from image");
            return Task.FromResult(Result<List<string>>.Failure($"Failed to extract colors: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> ResetToDefaultAsync(CancellationToken cancellationToken = default)
    {
        _currentTheme = GetDefaultDarkTheme();
        _previewTheme = null;
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(null, _currentTheme, false));
        return Task.FromResult(Result.Success());
    }

    public async Task<Result> SaveThemesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_themesDirectory);

            // Save custom themes only
            var customThemes = _themes.Values.Where(t => !t.IsBuiltIn).ToList();
            var themesFile = Path.Combine(_themesDirectory, "themes.json");

            await File.WriteAllTextAsync(
                themesFile,
                JsonSerializer.Serialize(customThemes, JsonOptions),
                cancellationToken);

            _logger.LogInformation("Saved {Count} themes to {Path}", customThemes.Count, themesFile);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save themes");
            return Result.Failure($"Failed to save themes: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> LoadThemesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var themesFile = Path.Combine(_themesDirectory, "themes.json");
            if (!File.Exists(themesFile))
            {
                return Result.Success();
            }

            var json = await File.ReadAllTextAsync(themesFile, cancellationToken);
            var themes = JsonSerializer.Deserialize<List<ThemeDefinition>>(json, JsonOptions);

            if (themes != null)
            {
                foreach (var theme in themes.Where(t => !t.IsBuiltIn))
                {
                    _themes[theme.Id] = theme;
                }
                _logger.LogInformation("Loaded {Count} themes from {Path}", themes.Count, themesFile);
            }

            // Load active theme preference
            var activeThemeFile = Path.Combine(_themesDirectory, "active_theme.txt");
            if (File.Exists(activeThemeFile))
            {
                var activeThemeId = await File.ReadAllTextAsync(activeThemeFile, cancellationToken);
                if (Guid.TryParse(activeThemeId, out var themeId) && _themes.TryGetValue(themeId, out var activeTheme))
                {
                    _currentTheme = activeTheme;
                    ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(null, activeTheme, false));
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load themes");
            return Result.Failure($"Failed to load themes: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task SaveActiveThemeAsync(Guid themeId)
    {
        try
        {
            var activeThemeFile = Path.Combine(_themesDirectory, "active_theme.txt");
            await File.WriteAllTextAsync(activeThemeFile, themeId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save active theme preference");
        }
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

    private static uint BlendColors(uint color1, uint color2, double ratio)
    {
        var r1 = (int)((color1 >> 16) & 0xFF);
        var g1 = (int)((color1 >> 8) & 0xFF);
        var b1 = (int)(color1 & 0xFF);

        var r2 = (int)((color2 >> 16) & 0xFF);
        var g2 = (int)((color2 >> 8) & 0xFF);
        var b2 = (int)(color2 & 0xFF);

        var r = (int)(r1 * (1 - ratio) + r2 * ratio);
        var g = (int)(g1 * (1 - ratio) + g2 * ratio);
        var b = (int)(b1 * (1 - ratio) + b2 * ratio);

        return 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
    }

    private static bool IsDarkColor(string hexColor)
    {
        var argb = HexToArgb(hexColor);
        var r = ((argb >> 16) & 0xFF) / 255.0;
        var g = ((argb >> 8) & 0xFF) / 255.0;
        var b = (argb & 0xFF) / 255.0;

        // Calculate luminance
        var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
        return luminance < 0.5;
    }
}
