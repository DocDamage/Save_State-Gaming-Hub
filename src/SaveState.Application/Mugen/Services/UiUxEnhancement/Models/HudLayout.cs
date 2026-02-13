using System.Numerics;

namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// HUD layout data.
/// </summary>
public class HudLayout
{
    public ScreenResolution ScreenResolution { get; set; } = default!;
    public IReadOnlyDictionary<string, Vector2> ElementPositions { get; set; } = default!;
    public SafeZones SafeZones { get; set; } = default!;
    public float ScalingFactor { get; set; } = default!;
    public string LayoutType { get; set; } = default!;
}
