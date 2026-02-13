namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Stream of bio data points during combat.
/// </summary>
public class BioDataStream
{
    public IReadOnlyList<BioDataPoint> HeartRateData { get; set; } = default!;
    public IReadOnlyList<BioDataPoint> BreathingData { get; set; } = default!;
    public IReadOnlyList<BioDataPoint> MuscleTensionData { get; set; } = default!;
    public IReadOnlyList<BioDataPoint> SkinConductanceData { get; set; } = default!;
    public IReadOnlyList<BioDataPoint> TemperatureData { get; set; } = default!;
}
