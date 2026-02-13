namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Feedback derived from heart rate data.
/// </summary>
public class HeartRateFeedback
{
    public float CurrentHeartRate { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public float DamageBonus { get; set; } = default!;
    public float SpeedBonus { get; set; } = default!;
    public float DefenseBonus { get; set; } = default!;
    public bool AdrenalinePotential { get; set; } = default!;
    public string Feedback { get; set; } = default!;
}
