namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// AR anchor data.
/// </summary>
public class ArAnchor
{
    public string AnchorId { get; set; } = default!;
    public Vector3 Position { get; set; } = default!;
    public Quaternion Rotation { get; set; } = default!;
    public TrackingState TrackingState { get; set; } = default!;
    public AnchorType AnchorType { get; set; } = default!;
}

/// <summary>
/// Tracking states.
/// </summary>
public enum TrackingState { Tracking, Paused, Lost }

/// <summary>
/// Anchor types.
/// </summary>
public enum AnchorType { Plane, Image, Object, Face }
