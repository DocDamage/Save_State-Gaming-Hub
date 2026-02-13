namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// VR game state data.
/// </summary>
public class VrGameState
{
    public VrStatus Status { get; set; } = default!;
    public Vector3 PlayerPosition { get; set; } = default!;
    public Quaternion PlayerRotation { get; set; } = default!;
    public bool IsImmersive { get; set; } = default!;
    public string CurrentEnvironment { get; set; } = default!;
}
