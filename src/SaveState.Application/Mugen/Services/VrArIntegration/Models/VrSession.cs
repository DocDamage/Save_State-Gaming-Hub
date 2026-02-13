namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// VR session data.
/// </summary>
public class VrSession
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public VrDeviceType DeviceType { get; set; } = default!;
    public VrHmdType HmdType { get; set; } = default!;
    public VrTrackingType TrackingType { get; set; } = default!;
    public VrGameState GameState { get; set; } = default!;
    public VrPerformanceMetrics PerformanceMetrics { get; set; } = default!;
    public VrComfortSettings ComfortSettings { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public DateTime LastActivity { get; set; } = default!;
}
