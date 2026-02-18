namespace SaveState.Application.Mugen.Services.BioFeedbackCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for processing heart rate data and creating heart rate powered weapons.
/// </summary>
public class HeartRateEngine
{
    private readonly ILogger<HeartRateEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public HeartRateEngine(ILogger<HeartRateEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processes heart rate data and generates feedback.
    /// </summary>
    public Task<HeartRateFeedback> ProcessHeartRateAsync(
        BioFeedbackCombatSession session,
        float heartRate,
        BioProfile profile,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Processing heart rate: {HeartRate} bpm for session {SessionId}", heartRate, session.SessionId);

        var baseline = profile.BaselineMetrics.RestingHeartRate;
        var elevation = heartRate - baseline;
        var intensity = Math.Min(elevation / baseline, 1.0f);

        var feedback = new HeartRateFeedback
        {
            CurrentHeartRate = heartRate,
            Intensity = intensity,
            DamageBonus = intensity * profile.BioSettings.HeartRateSensitivity * 0.3f,
            SpeedBonus = intensity * profile.BioSettings.HeartRateSensitivity * 0.2f,
            DefenseBonus = intensity > 0.5f ? 0.1f : 0f,
            AdrenalinePotential = elevation > 30,
            Feedback = GenerateHeartRateFeedback(intensity, elevation)
        };

        return Task.FromResult(feedback);
    }

    /// <summary>
    /// Charges a weapon based on heart rate intensity.
    /// </summary>
    public Task<HeartRateWeapon> ChargeWeaponAsync(
        BioFeedbackCombatSession session,
        WeaponChargeRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Charging heart rate weapon for session {SessionId}", session.SessionId);

        var heartRate = session.PhysiologicalState.CurrentHeartRate;
        var baseline = 70f; // Default baseline
        var chargeLevel = Math.Min((heartRate - baseline) / 50f, 1.0f);

        var weapon = new HeartRateWeapon
        {
            WeaponId = Guid.NewGuid().ToString(),
            BaseWeapon = request.BaseWeapon,
            ChargeLevel = chargeLevel,
            Power = 100f * (1 + chargeLevel), // Base power 100
            SpecialEffects = chargeLevel > 0.8f ? "Adrenaline Surge" : "Heart Sync",
            Duration = TimeSpan.FromSeconds(30 + chargeLevel * 60),
            ChargedAt = _timeProvider.UtcNow
        };

        _logger.LogInformation("Weapon charged: {WeaponId} with power {Power:F2}", weapon.WeaponId, weapon.Power);
        return Task.FromResult(weapon);
    }

    private static string GenerateHeartRateFeedback(float intensity, float elevation)
    {
        return intensity switch
        {
            > 0.8f => "Extreme cardio boost activated!",
            > 0.5f => "Elevated heart rate enhancing combat",
            > 0.2f => "Steady rhythm providing focus",
            _ => "Resting state - baseline performance"
        };
    }
}
