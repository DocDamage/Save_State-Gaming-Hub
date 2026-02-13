using System.Numerics;

namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// HUD element data.
/// </summary>
public class HudElement
{
    public string Id { get; set; } = default!;
    public string Type { get; set; } = default!;
    public Vector2 Position { get; set; } = default!;
    public Vector2 Size { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Color { get; set; } = default!;
    public int UpdateFrequency { get; set; } = default!;
}
