namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// AR session data.
/// </summary>
public class ArSession
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public ArDeviceType DeviceType { get; set; } = default!;
    public ArCameraType CameraType { get; set; } = default!;
    public ArTrackingQuality TrackingQuality { get; set; } = default!;
    public ArGameState GameState { get; set; } = default!;
    public ArPerformanceMetrics PerformanceMetrics { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public DateTime LastActivity { get; set; } = default!;
}
