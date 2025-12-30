namespace SaveState.Presentation.Services;

public interface IThemeService
{
    ThemeType CurrentTheme { get; }
    void SetTheme(ThemeType theme);
    IReadOnlyList<ThemeType> AvailableThemes { get; }
    event EventHandler<ThemeType>? ThemeChanged;
}
