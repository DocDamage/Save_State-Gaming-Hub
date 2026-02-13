namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// AR input data.
/// </summary>
public class ArInput
{
    public ArInputType InputType { get; set; } = default!;
    public Vector3 Position { get; set; } = default!;
    public Vector2 ScreenPosition { get; set; } = default!;
    public float Pressure { get; set; } = default!;
}

/// <summary>
/// AR input types.
/// </summary>
public enum ArInputType { Touch, SurfaceTap, ObjectPlacement, Gesture, CameraMovement }

/// <summary>
/// 2D vector for screen positions.
/// </summary>
public class Vector2
{
    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
}
