using Avalonia;
using Avalonia.Styling;

namespace SaveState.Presentation.Services;

public sealed class ThemeService : IThemeService
{
    private ThemeType _currentTheme = ThemeType.Dark;

    public ThemeType CurrentTheme => _currentTheme;

    public IReadOnlyList<ThemeType> AvailableThemes =>
        Enum.GetValues<ThemeType>();

    public event EventHandler<ThemeType>? ThemeChanged;

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
