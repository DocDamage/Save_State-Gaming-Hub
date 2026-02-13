namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Bio calibration data.
/// </summary>
public class BioCalibration
{
    public float RestingHeartRate { get; set; } = default!;
    public float NormalBreathingRate { get; set; } = default!;
    public float BaselineMuscleTension { get; set; } = default!;
    public DateTime CalibratedAt { get; set; } = default!;
}
