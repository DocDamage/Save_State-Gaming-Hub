namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Meditation mode data.
/// </summary>
public class MeditationMode
{
    public string MeditationId { get; set; } = default!;
    public MeditationTechnique Technique { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public float FocusLevel { get; set; } = default!;
    public float StressReduction { get; set; } = default!;
    public float EnergyRecovery { get; set; } = default!;
    public IReadOnlyList<string> SpecialAbilities { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
}
