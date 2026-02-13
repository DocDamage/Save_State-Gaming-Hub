namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Menu data.
/// </summary>
public class Menu
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public IReadOnlyList<MenuItem> Items { get; set; } = default!;
}
