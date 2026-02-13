namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Request to enhance a combo with breathing.
/// </summary>
public class ComboEnhancementRequest
{
    public string[] BaseCombo { get; set; } = default!;
    public float TimingWindow { get; set; } = default!;
    public bool RhythmLock { get; set; } = default!;
}
