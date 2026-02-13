namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// AR calibration result data.
/// </summary>
public class ArCalibrationResult
{
    public bool Success { get; set; } = default!;
    public IReadOnlyList<ArAnchor> DetectedAnchors { get; set; } = default!;
    public float LightingQuality { get; set; } = default!;
    public float TrackingQuality { get; set; } = default!;
    public IReadOnlyList<string> Recommendations { get; set; } = default!;
}
