namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Adrenaline burst data.
/// </summary>
public class AdrenalineBurst
{
    public string BurstId { get; set; } = default!;
    public BurstTrigger Trigger { get; set; } = default!;
    public float PowerMultiplier { get; set; } = default!;
    public float SpeedMultiplier { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyList<string> Effects { get; set; } = default!;
    public DateTime TriggeredAt { get; set; } = default!;
}
