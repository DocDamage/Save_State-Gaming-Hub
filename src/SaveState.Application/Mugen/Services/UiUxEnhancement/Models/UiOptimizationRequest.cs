namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// UI optimization request data.
/// </summary>
public class UiOptimizationRequest
{
    public float TargetMemoryUsage { get; set; } = default!;
    public float TargetRenderTime { get; set; } = default!;
    public bool EnableAdvancedOptimizations { get; set; } = default!;
}
