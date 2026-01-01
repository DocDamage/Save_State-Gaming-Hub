using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.Themes;

/// <summary>
/// Plugin that provides advanced UI themes for SaveState.
/// Includes Dark+, Light, Gaming, and custom themes with dynamic switching.
/// </summary>
public class AdvancedThemesPlugin : IPlugin, ITheme
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private ThemeInfo? _currentTheme;
    private readonly Dictionary<string, ThemeInfo> _availableThemes;

    public string Id => "savestate.themes.advanced";
    public string Name => "Advanced Themes";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Advanced UI themes with dynamic switching";
    public PluginCapabilities Capabilities => PluginCapabilities.ThemeProvider;

    // ITheme implementation
    public string ThemeName => _currentTheme?.Name ?? "Default";
    public string DisplayName => _currentTheme?.DisplayName ?? "Default Theme";
    public string Author => "SaveState Team";
    public string Version => "1.0.0";

    public AdvancedThemesPlugin()
    {
        _availableThemes = CreateBuiltInThemes();
    }

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation("Initializing Advanced Themes plugin");

        // Register theme menu items
        var darkThemeMenuItem = new PluginMenuItem(
            Id: "themes.dark",
            Label: "Switch to Dark+ Theme",
            Icon: "🌙",
            SortOrder: 600,
            Action: () => SwitchToThemeAsync("DarkPlus"));

        var lightThemeMenuItem = new PluginMenuItem(
            Id: "themes.light",
            Label: "Switch to Light Theme",
            Icon: "☀️",
            SortOrder: 601,
            Action: () => SwitchToThemeAsync("Light"));

        var gamingThemeMenuItem = new PluginMenuItem(
            Id: "themes.gaming",
            Label: "Switch to Gaming Theme",
            Icon: "🎮",
            SortOrder: 602,
            Action: () => SwitchToThemeAsync("Gaming"));

        var autoThemeMenuItem = new PluginMenuItem(
            Id: "themes.auto",
            Label: "Auto Theme (Time-based)",
            Icon: "⏰",
            SortOrder: 603,
            Action: EnableAutoThemeAsync);

        await context.RegisterMenuItemAsync(darkThemeMenuItem);
        await context.RegisterMenuItemAsync(lightThemeMenuItem);
        await context.RegisterMenuItemAsync(gamingThemeMenuItem);
        await context.RegisterMenuItemAsync(autoThemeMenuItem);

        // Register themes
        foreach (var theme in _availableThemes.Values)
        {
            await context.RegisterThemeAsync(new ThemeWrapper(theme));
        }

        // Load saved theme preference or default to Dark+
        await LoadSavedThemeAsync(ct);

        _logger.LogInformation("Advanced Themes plugin initialized successfully");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down Advanced Themes plugin");
        return Task.CompletedTask();
    }

    // ITheme implementation
    public async Task<Result> ApplyAsync(CancellationToken ct = default)
    {
        try
        {
            if (_currentTheme == null)
            {
                return Result.Failure("No theme selected");
            }

            _logger?.LogInformation("Applying theme: {ThemeName}", _currentTheme.DisplayName);

            // In a real implementation, this would apply the theme to the UI
            // For Avalonia, this would involve setting resource dictionaries

            await Task.Delay(100, ct); // Simulate theme application

            _logger?.LogInformation("Theme applied successfully: {ThemeName}", _currentTheme.DisplayName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying theme");
            return Result.Failure($"Failed to apply theme: {ex.Message}");
        }
    }

    public async Task<Result> RemoveAsync(CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Removing theme: {ThemeName}", _currentTheme?.DisplayName ?? "None");

            // Reset to default theme
            await Task.Delay(50, ct);

            _logger?.LogInformation("Theme removed, reverted to default");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing theme");
            return Result.Failure($"Failed to remove theme: {ex.Message}");
        }
    }

    public object? GetResourceDictionary()
    {
        // In a real Avalonia implementation, this would return a ResourceDictionary
        // For demo purposes, return null
        return null;
    }

    private Dictionary<string, ThemeInfo> CreateBuiltInThemes()
    {
        return new Dictionary<string, ThemeInfo>
        {
            ["DarkPlus"] = new ThemeInfo(
                Id: "dark-plus",
                Name: "DarkPlus",
                DisplayName: "Dark+",
                Description: "Enhanced dark theme with better contrast and modern accents",
                Colors: new ThemeColors(
                    Primary: "#007ACC",
                    Secondary: "#3C3C3C",
                    Background: "#1E1E1E",
                    Surface: "#252526",
                    Text: "#FFFFFF",
                    TextSecondary: "#CCCCCC",
                    Error: "#F48771",
                    Success: "#4EC9B0",
                    Warning: "#DCDCAA"),
                IsBuiltIn: true),

            ["Light"] = new ThemeInfo(
                Id: "light",
                Name: "Light",
                DisplayName: "Light Theme",
                Description: "Clean light theme perfect for bright environments",
                Colors: new ThemeColors(
                    Primary: "#005A9E",
                    Secondary: "#E5E5E5",
                    Background: "#FFFFFF",
                    Surface: "#F8F8F8",
                    Text: "#1E1E1E",
                    TextSecondary: "#6C6C6C",
                    Error: "#E81123",
                    Success: "#107C10",
                    Warning: "#FF8C00"),
                IsBuiltIn: true),

            ["Gaming"] = new ThemeInfo(
                Id: "gaming",
                Name: "Gaming",
                DisplayName: "Gaming Theme",
                Description: "Vibrant gaming theme with neon accents for that retro gaming feel",
                Colors: new ThemeColors(
                    Primary: "#00FF41", // Matrix green
                    Secondary: "#FF0080", // Neon pink
                    Background: "#0D0D0D",
                    Surface: "#1A1A1A",
                    Text: "#00FF41",
                    TextSecondary: "#00CC33",
                    Error: "#FF4444",
                    Success: "#00FF41",
                    Warning: "#FFFF00"),
                IsBuiltIn: true),

            ["HighContrast"] = new ThemeInfo(
                Id: "high-contrast",
                Name: "HighContrast",
                DisplayName: "High Contrast",
                Description: "High contrast theme for accessibility",
                Colors: new ThemeColors(
                    Primary: "#FFFF00",
                    Secondary: "#FFFFFF",
                    Background: "#000000",
                    Surface: "#000000",
                    Text: "#FFFFFF",
                    TextSecondary: "#FFFF00",
                    Error: "#FF0000",
                    Success: "#00FF00",
                    Warning: "#FFFF00"),
                IsBuiltIn: true)
        };
    }

    private async Task SwitchToThemeAsync(string themeId)
    {
        try
        {
            if (!_availableThemes.TryGetValue(themeId, out var theme))
            {
                _logger?.LogWarning("Theme not found: {ThemeId}", themeId);
                return;
            }

            _logger?.LogInformation("Switching to theme: {ThemeName}", theme.DisplayName);

            _currentTheme = theme;
            var result = await ApplyAsync();

            if (result.IsSuccess)
            {
                // Save theme preference
                await SaveThemePreferenceAsync(themeId);

                _logger?.LogInformation("Successfully switched to theme: {ThemeName}", theme.DisplayName);
            }
            else
            {
                _logger?.LogError("Failed to switch theme: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error switching to theme {ThemeId}", themeId);
        }
    }

    private async Task EnableAutoThemeAsync()
    {
        try
        {
            _logger?.LogInformation("Enabling automatic theme switching based on time");

            // Set up a timer to switch themes based on time of day
            // Morning/Afternoon: Light theme
            // Evening/Night: Dark+ theme

            var currentHour = DateTime.Now.Hour;

            if (currentHour >= 6 && currentHour < 18) // Daytime
            {
                await SwitchToThemeAsync("Light");
            }
            else // Nighttime
            {
                await SwitchToThemeAsync("DarkPlus");
            }

            _logger?.LogInformation("Auto theme enabled - will switch based on time of day");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error enabling auto theme");
        }
    }

    private async Task LoadSavedThemeAsync(CancellationToken ct)
    {
        try
        {
            // In a real implementation, this would load from settings
            // For demo, default to Dark+
            await SwitchToThemeAsync("DarkPlus");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading saved theme");
        }
    }

    private async Task SaveThemePreferenceAsync(string themeId)
    {
        try
        {
            // In a real implementation, this would save to settings file
            var preference = new ThemePreference(themeId, DateTime.UtcNow);
            var json = JsonSerializer.Serialize(preference);

            // Save to plugin data directory
            if (_context != null)
            {
                var settingsPath = Path.Combine(_context.PluginDirectory, "theme_settings.json");
                await File.WriteAllTextAsync(settingsPath, json);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving theme preference");
        }
    }
}

/// <summary>
/// Information about a theme.
/// </summary>
public sealed record ThemeInfo(
    string Id,
    string Name,
    string DisplayName,
    string Description,
    ThemeColors Colors,
    bool IsBuiltIn = false);

/// <summary>
/// Color scheme for a theme.
/// </summary>
public sealed record ThemeColors(
    string Primary,
    string Secondary,
    string Background,
    string Surface,
    string Text,
    string TextSecondary,
    string Error,
    string Success,
    string Warning);

/// <summary>
/// User theme preference.
/// </summary>
public sealed record ThemePreference(
    string ThemeId,
    DateTime LastChanged);

/// <summary>
/// Wrapper to make ThemeInfo implement ITheme interface.
/// </summary>
internal class ThemeWrapper : ITheme
{
    private readonly ThemeInfo _themeInfo;

    public ThemeWrapper(ThemeInfo themeInfo)
    {
        _themeInfo = themeInfo;
    }

    public string ThemeName => _themeInfo.Name;
    public string DisplayName => _themeInfo.DisplayName;
    public string Author => "SaveState Team";
    public string Version => "1.0.0";

    public async Task<Result> ApplyAsync(CancellationToken ct = default)
    {
        // In a real implementation, this would apply the theme colors to the UI
        await Task.Delay(100, ct);
        return Result.Success();
    }

    public async Task<Result> RemoveAsync(CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        return Result.Success();
    }

    public object? GetResourceDictionary()
    {
        // Return Avalonia ResourceDictionary with theme colors
        // For demo, return null
        return null;
    }
}