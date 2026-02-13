namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Peak performance moment data.
/// </summary>
public class PeakMoment
{
    public DateTime Timestamp { get; set; } = default!;
    public PeakType Type { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public string Trigger { get; set; } = default!;
}
