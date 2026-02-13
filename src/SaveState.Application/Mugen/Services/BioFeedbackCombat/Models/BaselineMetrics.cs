namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Baseline physiological metrics for a player.
/// </summary>
public class BaselineMetrics
{
    public float RestingHeartRate { get; set; } = default!;
    public float NormalBreathingRate { get; set; } = default!;
    public float BaselineMuscleTension { get; set; } = default!;
    public float NormalSkinConductance { get; set; } = default!;
    public float BaselineTemperature { get; set; } = default!;
    public DateTime EstablishedAt { get; set; } = default!;
}
