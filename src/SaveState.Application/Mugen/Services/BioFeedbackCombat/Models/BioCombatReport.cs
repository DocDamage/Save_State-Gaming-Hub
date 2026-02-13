namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Bio combat report data.
/// </summary>
public class BioCombatReport
{
    public string SessionId { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public CombatBioMetrics CombatMetrics { get; set; } = default!;
    public PhysiologicalTrends PhysiologicalTrends { get; set; } = default!;
    public BioEffectiveness BioEffectiveness { get; set; } = default!;
    public IReadOnlyList<PeakMoment> PeakPerformanceMoments { get; set; } = default!;
    public FatigueAnalysis FatigueAccumulation { get; set; } = default!;
    public StressAnalysis StressManagement { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}
