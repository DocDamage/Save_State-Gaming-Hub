namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Safe zones data for UI layout.
/// </summary>
public class SafeZones
{
    public int Top { get; set; } = default!;
    public int Bottom { get; set; } = default!;
    public int Left { get; set; } = default!;
    public int Right { get; set; } = default!;
}
