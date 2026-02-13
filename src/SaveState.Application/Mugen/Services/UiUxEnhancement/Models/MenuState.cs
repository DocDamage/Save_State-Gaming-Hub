namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Menu state data.
/// </summary>
public class MenuState
{
    public string CurrentMenu { get; set; } = default!;
    public int SelectedIndex { get; set; } = default!;
    public bool HasPrevious { get; set; } = default!;
    public bool HasNext { get; set; } = default!;
    public bool HasParent { get; set; } = default!;
    public bool IsTransitioning { get; set; } = default!;
}
