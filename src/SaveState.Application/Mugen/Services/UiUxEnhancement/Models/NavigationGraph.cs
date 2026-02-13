namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Navigation graph data.
/// </summary>
public class NavigationGraph
{
    public IReadOnlyList<NavigationNode> Nodes { get; set; } = default!;
    public IReadOnlyDictionary<string, KeyboardShortcut> Shortcuts { get; set; } = default!;
}
