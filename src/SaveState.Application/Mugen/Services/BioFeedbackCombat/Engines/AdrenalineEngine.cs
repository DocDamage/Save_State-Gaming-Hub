namespace SaveState.Application.Mugen.Services.BioFeedbackCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for managing adrenaline bursts.
/// </summary>
public class AdrenalineEngine
{
    private readonly ILogger<AdrenalineEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public AdrenalineEngine(ILogger<AdrenalineEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Triggers an adrenaline burst based on physiological state and trigger.
    /// </summary>
    public Task<AdrenalineBurst> TriggerBurstAsync(
        BioFeedbackCombatSession session,
        BioProfile profile,
        BurstTrigger trigger,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Triggering adrenaline burst for session {SessionId}, trigger: {TriggerType}",
            session.SessionId, trigger.TriggerType);

        var intensity = Math.Min(trigger.Intensity, 1.0f);
        var duration = CalculateDuration(trigger.TriggerType, intensity);
        var effects = GenerateEffects(intensity);

        var burst = new AdrenalineBurst
        {
            BurstId = Guid.NewGuid().ToString(),
            Trigger = trigger,
            PowerMultiplier = 1 + intensity * 0.5f,
            SpeedMultiplier = 1 + intensity * 0.3f,
            Duration = duration,
            Effects = effects,
            TriggeredAt = _timeProvider.UtcNow
        };

        _logger.LogInformation("Adrenaline burst triggered: {BurstId}, power: {Power:F2}, duration: {Duration}",
            burst.BurstId, burst.PowerMultiplier, burst.Duration);
        return Task.FromResult(burst);
    }

    private static TimeSpan CalculateDuration(BurstTriggerType triggerType, float intensity)
    {
        var baseDuration = triggerType switch
        {
            BurstTriggerType.Physiological => TimeSpan.FromSeconds(15),
            BurstTriggerType.Combat => TimeSpan.FromSeconds(20),
            BurstTriggerType.Emergency => TimeSpan.FromSeconds(30),
            _ => TimeSpan.FromSeconds(10)
        };

        return TimeSpan.FromSeconds(baseDuration.TotalSeconds * (1 + intensity));
    }

    private static List<string> GenerateEffects(float intensity)
    {
        var effects = new List<string> { "Enhanced Reflexes" };

        if (intensity > 0.3f)
            effects.Add("Pain Suppression");
        if (intensity > 0.5f)
            effects.Add("Time Perception Shift");
        if (intensity > 0.7f)
            effects.Add("Superhuman Strength");
        if (intensity > 0.9f)
            effects.Add("Ultimate Focus");

        return effects;
    }
}
