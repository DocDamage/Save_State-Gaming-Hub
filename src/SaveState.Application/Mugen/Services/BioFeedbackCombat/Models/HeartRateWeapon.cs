namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Heart rate powered weapon data.
/// </summary>
public class HeartRateWeapon
{
    public string WeaponId { get; set; } = default!;
    public string BaseWeapon { get; set; } = default!;
    public float ChargeLevel { get; set; } = default!;
    public float Power { get; set; } = default!;
    public string SpecialEffects { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public DateTime ChargedAt { get; set; } = default!;
}
