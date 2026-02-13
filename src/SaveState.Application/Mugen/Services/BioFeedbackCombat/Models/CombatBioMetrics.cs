namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Combat metrics derived from bio feedback.
/// </summary>
public class CombatBioMetrics
{
    public int TotalCombos { get; set; } = default!;
    public int HeartRatePoweredMoves { get; set; } = default!;
    public int BreathingEnhancedCombos { get; set; } = default!;
    public int MusclePoweredBlocks { get; set; } = default!;
    public int AdrenalineBursts { get; set; } = default!;
    public int MeditationPeriods { get; set; } = default!;
}
