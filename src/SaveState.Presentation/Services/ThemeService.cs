using Avalonia;
using Avalonia.Styling;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for managing application themes.
/// Handles theme switching and Avalonia theme variant application.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private ThemeType _currentTheme = ThemeType.Dark;

    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    public ThemeType CurrentTheme => _currentTheme;

    /// <summary>
    /// Gets all available theme types.
    /// </summary>
    public IReadOnlyList<ThemeType> AvailableThemes =>
        Enum.GetValues<ThemeType>();

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    public event EventHandler<ThemeType>? ThemeChanged;

    /// <summary>
    /// Sets the application theme and applies it to Avalonia.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    public void SetTheme(ThemeType theme)
    {
        if (_currentTheme == theme) return;

        _currentTheme = theme;

        if (Avalonia.Application.Current is { } app)
        {
            app.RequestedThemeVariant = theme switch
            {
                ThemeType.Light => ThemeVariant.Light,
                ThemeType.Dark => ThemeVariant.Dark,
                ThemeType.System => ThemeVariant.Default,
                _ => ThemeVariant.Dark
            };
        }

        ThemeChanged?.Invoke(this, theme);
    }
}
