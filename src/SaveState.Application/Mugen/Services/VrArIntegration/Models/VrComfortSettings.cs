namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// VR comfort settings data.
/// </summary>
public class VrComfortSettings
{
    public bool SnapTurning { get; set; } = default!;
    public VrComfortMode ComfortMode { get; set; } = default!;
    public float MovementSpeed { get; set; } = default!;
    public bool TeleportationEnabled { get; set; } = default!;
}
