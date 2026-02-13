namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Menu system data.
/// </summary>
public class MenuSystem
{
    public string SessionId { get; set; } = default!;
    public MenuConfiguration Configuration { get; set; } = default!;
    public IReadOnlyList<Menu> Menus { get; set; } = default!;
    public NavigationGraph NavigationGraph { get; set; } = default!;
    public string Theme { get; set; } = default!;
    public LocalizationSettings LocalizationSettings { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}
