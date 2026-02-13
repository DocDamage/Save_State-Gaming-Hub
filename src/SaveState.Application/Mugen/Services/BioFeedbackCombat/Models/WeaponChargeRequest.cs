namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Request to charge a heart rate weapon.
/// </summary>
public class WeaponChargeRequest
{
    public string BaseWeapon { get; set; } = default!;
    public float ChargeTime { get; set; } = default!;
    public bool OverchargeAllowed { get; set; } = default!;
}
