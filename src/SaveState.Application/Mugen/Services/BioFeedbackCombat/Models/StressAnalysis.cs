namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Stress analysis data.
/// </summary>
public class StressAnalysis
{
    public float PeakStressLevel { get; set; } = default!;
    public float AverageStressLevel { get; set; } = default!;
    public string StressReductionTechniques { get; set; } = default!;
    public float StressManagementEffectiveness { get; set; } = default!;
}
