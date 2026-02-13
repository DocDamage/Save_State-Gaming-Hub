namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// VR performance metrics data.
/// </summary>
public class VrPerformanceMetrics
{
    public float FrameRate { get; set; } = default!;
    public float Latency { get; set; } = default!;
    public float MotionToPhotonLatency { get; set; } = default!;
    public float CpuUsage { get; set; } = default!;
    public float GpuUsage { get; set; } = default!;
    public float MemoryUsage { get; set; } = default!;
}
