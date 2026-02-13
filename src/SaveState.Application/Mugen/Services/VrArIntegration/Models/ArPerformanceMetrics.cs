namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// AR performance metrics data.
/// </summary>
public class ArPerformanceMetrics
{
    public float FrameRate { get; set; } = default!;
    public float TrackingStability { get; set; } = default!;
    public float PlaneDetectionAccuracy { get; set; } = default!;
    public float CpuUsage { get; set; } = default!;
    public float MemoryUsage { get; set; } = default!;
}
