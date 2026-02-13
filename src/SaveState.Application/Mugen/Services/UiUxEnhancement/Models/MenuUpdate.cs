namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Menu update data.
/// </summary>
public class MenuUpdate
{
    public string SessionId { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public string CurrentMenu { get; set; } = default!;
    public IReadOnlyList<MenuItem> MenuItems { get; set; } = default!;
    public IReadOnlyList<NavigationOption> NavigationOptions { get; set; } = default!;
    public MenuVisualState VisualState { get; set; } = default!;
}

/// <summary>
/// Navigation option data.
/// </summary>
public class NavigationOption
{
    public string Action { get; set; } = default!;
    public string Label { get; set; } = default!;
    public bool Enabled { get; set; } = default!;
}

/// <summary>
/// Menu visual state data.
/// </summary>
public class MenuVisualState
{
    public bool IsVisible { get; set; } = default!;
    public float Opacity { get; set; } = default!;
    public int SelectedIndex { get; set; } = default!;
}
