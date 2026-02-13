namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// VR input data.
/// </summary>
public class VrInput
{
    public VrInputType InputType { get; set; } = default!;
    public Vector3 Position { get; set; } = default!;
    public Quaternion Rotation { get; set; } = default!;
    public float ButtonPressure { get; set; } = default!;
    public bool IsPressed { get; set; } = default!;
}
