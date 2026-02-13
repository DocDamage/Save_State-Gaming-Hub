using SaveState.Application.Mugen;

namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Physiological trends during combat.
/// </summary>
public class PhysiologicalTrends
{
    public TrendDirection HeartRateTrend { get; set; } = default!;
    public TrendDirection BreathingTrend { get; set; } = default!;
    public TrendDirection MuscleTensionTrend { get; set; } = default!;
    public float StressAccumulation { get; set; } = default!;
    public float FatigueAccumulation { get; set; } = default!;
}
