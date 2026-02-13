namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// VR calibration result data.
/// </summary>
public class VrCalibrationResult
{
    public bool Success { get; set; } = default!;
    public Vector3 CalibratedPosition { get; set; } = default!;
    public Quaternion CalibratedRotation { get; set; } = default!;
    public float IpD { get; set; } = default!;
    public float CalibrationQuality { get; set; } = default!;
    public IReadOnlyList<string> Recommendations { get; set; } = default!;
}
