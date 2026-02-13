namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Bio profile data for a player.
/// </summary>
public class BioProfile
{
    public string ProfileId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public BaselineMetrics BaselineMetrics { get; set; } = default!;
    public BioCalibration CalibrationData { get; set; } = default!;
    public BioSettings BioSettings { get; set; } = default!;
    public BioCombatModifiers CombatModifiers { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastCalibration { get; set; } = default!;
    public BioProfileStatus Status { get; set; } = default!;
}
