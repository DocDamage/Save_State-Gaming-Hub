namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Fatigue analysis data.
/// </summary>
public class FatigueAnalysis
{
    public float TotalFatigue { get; set; } = default!;
    public float FatigueRate { get; set; } = default!;
    public TimeSpan RecoveryTime { get; set; } = default!;
}
