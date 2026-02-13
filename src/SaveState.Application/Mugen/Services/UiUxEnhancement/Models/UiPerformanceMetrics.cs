namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// UI performance metrics data.
/// </summary>
public class UiPerformanceMetrics
{
    public float RenderTime { get; set; } = default!;
    public float MemoryUsage { get; set; } = default!;
    public int DrawCalls { get; set; } = default!;
}
