namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Trigger for an adrenaline burst.
/// </summary>
public class BurstTrigger
{
    public BurstTriggerType TriggerType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public object TriggerData { get; set; } = default!;
}
