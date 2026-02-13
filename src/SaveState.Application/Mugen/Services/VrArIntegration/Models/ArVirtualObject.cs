namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// AR virtual object data.
/// </summary>
public class ArVirtualObject
{
    public string ObjectId { get; set; } = default!;
    public string ObjectType { get; set; } = default!;
    public Vector3 Position { get; set; } = default!;
    public Quaternion Rotation { get; set; } = default!;
    public Vector3 Scale { get; set; } = default!;
    public bool IsVisible { get; set; } = default!;
}
