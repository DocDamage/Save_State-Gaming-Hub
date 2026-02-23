using SaveState.Core.Common;
using SaveState.Core.Theme.Models;

namespace SaveState.Core.Theme.Services;

/// <summary>
/// Service for managing themes including creation, application, and customization.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    ThemeDefinition CurrentTheme { get; }

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Gets the current theme asynchronously.
    /// </summary>
    Task<Result<ThemeDefinition>> GetCurrentThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a theme by ID.
    /// </summary>
    Task<Result> ApplyThemeAsync(Guid themeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a theme definition directly.
    /// </summary>
    Task<Result> ApplyThemeAsync(ThemeDefinition theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews a theme without applying it permanently.
    /// </summary>
    Task<Result> PreviewThemeAsync(ThemeDefinition theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available themes including built-in and custom.
    /// </summary>
    Task<Result<IReadOnlyList<ThemeDefinition>>> GetAllThemesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific theme by ID.
    /// </summary>
    Task<Result<ThemeDefinition>> GetThemeAsync(Guid themeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new theme.
    /// </summary>
    Task<Result<ThemeDefinition>> CreateThemeAsync(string name, ThemeDefinition? baseTheme = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing theme.
    /// </summary>
    Task<Result> UpdateThemeAsync(ThemeDefinition theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a theme.
    /// </summary>
    Task<Result> DeleteThemeAsync(Guid themeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates an existing theme.
    /// </summary>
    Task<Result<ThemeDefinition>> DuplicateThemeAsync(Guid themeId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a theme from JSON.
    /// </summary>
    Task<Result<ThemeDefinition>> ImportThemeAsync(string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a theme to JSON.
    /// </summary>
    Task<Result<string>> ExportThemeAsync(Guid themeId, ThemeFormat format = ThemeFormat.Json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a theme from an image by extracting dominant colors.
    /// </summary>
    Task<Result<ThemeDefinition>> ImportFromImageAsync(Stream imageStream, string themeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all built-in themes.
    /// </summary>
    Task<Result<IReadOnlyList<ThemeDefinition>>> GetBuiltInThemesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default light theme.
    /// </summary>
    ThemeDefinition GetDefaultLightTheme();

    /// <summary>
    /// Gets the default dark theme.
    /// </summary>
    ThemeDefinition GetDefaultDarkTheme();

    /// <summary>
    /// Gets the system theme (follows OS preference).
    /// </summary>
    ThemeDefinition GetSystemTheme();

    /// <summary>
    /// Harmonizes a source color towards a target color.
    /// </summary>
    string Harmonize(string source, string target);

    /// <summary>
    /// Generates a color palette from a seed color.
    /// </summary>
    List<string> GeneratePalette(string seedColor, int count = 5);

    /// <summary>
    /// Generates a complete theme from a seed color.
    /// </summary>
    ThemeColors GenerateFromSeedColor(string seedColor, bool isDark);

    /// <summary>
    /// Calculates contrast information between two colors.
    /// </summary>
    ContrastInfo CalculateContrast(string foreground, string background);

    /// <summary>
    /// Simulates color blindness for a given color.
    /// </summary>
    string SimulateColorBlindness(string color, ColorBlindnessType type);

    /// <summary>
    /// Gets the dominant colors from an image.
    /// </summary>
    Task<Result<List<string>>> ExtractColorsFromImageAsync(Stream imageStream, int colorCount = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the theme to default.
    /// </summary>
    Task<Result> ResetToDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all themes to persistent storage.
    /// </summary>
    Task<Result> SaveThemesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads themes from persistent storage.
    /// </summary>
    Task<Result> LoadThemesAsync(CancellationToken cancellationToken = default);
}
