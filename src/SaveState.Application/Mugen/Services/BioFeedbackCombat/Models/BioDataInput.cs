namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Input bio data for processing.
/// </summary>
public class BioDataInput
{
    public float HeartRate { get; set; } = default!;
    public float BreathingRate { get; set; } = default!;
    public float MuscleTension { get; set; } = default!;
    public float SkinConductance { get; set; } = default!;
    public float Temperature { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}
