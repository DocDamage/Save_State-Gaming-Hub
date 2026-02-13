namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Single bio data point with timestamp.
/// </summary>
public class BioDataPoint
{
    public float Value { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}
