namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// AR game state data.
/// </summary>
public class ArGameState
{
    public ArStatus Status { get; set; } = default!;
    public IReadOnlyList<ArAnchor> RealWorldAnchors { get; set; } = default!;
    public IReadOnlyList<ArVirtualObject> VirtualObjects { get; set; } = default!;
    public ArLightingConditions LightingConditions { get; set; } = default!;
    public bool SurfaceDetection { get; set; } = default!;
}

/// <summary>
/// AR lighting conditions.
/// </summary>
public enum ArLightingConditions { Poor, Adequate, Good, Excellent, Outdoor }
