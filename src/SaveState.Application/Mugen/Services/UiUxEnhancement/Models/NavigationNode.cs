namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Navigation node data.
/// </summary>
public class NavigationNode
{
    public string Feature { get; set; } = default!;
    public IReadOnlyList<string> Connections { get; set; } = default!;
}
