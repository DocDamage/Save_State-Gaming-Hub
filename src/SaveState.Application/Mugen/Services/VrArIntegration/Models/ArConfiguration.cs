namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// AR configuration data.
/// </summary>
public class ArConfiguration
{
    public ArDeviceType DeviceType { get; set; } = default!;
    public ArCameraType CameraType { get; set; } = default!;
    public ArTrackingQuality TrackingQuality { get; set; } = default!;
}

/// <summary>
/// AR camera types.
/// </summary>
public enum ArCameraType { WorldFacing, UserFacing, TrueDepth }

/// <summary>
/// AR tracking quality levels.
/// </summary>
public enum ArTrackingQuality { Low, Medium, High }
