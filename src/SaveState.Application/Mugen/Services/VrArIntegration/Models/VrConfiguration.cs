namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// VR configuration data.
/// </summary>
public class VrConfiguration
{
    public VrDeviceType DeviceType { get; set; } = default!;
    public VrHmdType HmdType { get; set; } = default!;
    public VrTrackingType TrackingType { get; set; } = default!;
    public bool SnapTurning { get; set; } = default!;
    public VrComfortMode ComfortMode { get; set; } = default!;
    public float MovementSpeed { get; set; } = default!;
    public bool TeleportationEnabled { get; set; } = default!;
}

/// <summary>
/// VR HMD types.
/// </summary>
public enum VrHmdType { Rift, Quest, Vive, Index, Wmr }

/// <summary>
/// VR tracking types.
/// </summary>
public enum VrTrackingType { ThreeDof, SixDof, InsideOut, OutsideIn }

/// <summary>
/// VR comfort modes.
/// </summary>
public enum VrComfortMode { Comfort, Normal, Performance }
