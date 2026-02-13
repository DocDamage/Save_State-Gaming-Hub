namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Bio feedback effectiveness metrics.
/// </summary>
public class BioEffectiveness
{
    public float HeartRateUtilization { get; set; } = default!;
    public float BreathingSynchronization { get; set; } = default!;
    public float MuscleTensionEfficiency { get; set; } = default!;
    public float OverallBioIntegration { get; set; } = default!;
}
