namespace SaveState.Application.Mugen.Services.BioFeedbackCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for processing breathing patterns and enhancing combos.
/// </summary>
public class BreathingEngine
{
    private readonly ILogger<BreathingEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public BreathingEngine(ILogger<BreathingEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processes breathing rate data and generates feedback.
    /// </summary>
    public Task<BreathingFeedback> ProcessBreathingAsync(
        BioFeedbackCombatSession session,
        float breathingRate,
        BioProfile profile,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Processing breathing rate: {BreathingRate} bpm for session {SessionId}", breathingRate, session.SessionId);

        var baseline = profile.BaselineMetrics.NormalBreathingRate;
        var deviation = Math.Abs(breathingRate - baseline);
        var rhythmStability = Math.Max(0, 1 - deviation / baseline);

        var feedback = new BreathingFeedback
        {
            CurrentBreathingRate = breathingRate,
            RhythmStability = rhythmStability,
            Intensity = rhythmStability,
            DamageBonus = rhythmStability * profile.BioSettings.BreathingSensitivity * 0.25f,
            SpeedBonus = rhythmStability > 0.7f ? 0.15f : 0f,
            DefenseBonus = breathingRate < baseline ? 0.1f : 0f,
            ComboEnhancement = rhythmStability > 0.8f
        };

        return Task.FromResult(feedback);
    }

    /// <summary>
    /// Enhances a combo based on breathing synchronization.
    /// </summary>
    public Task<BreathingCombo> EnhanceComboAsync(
        BioFeedbackCombatSession session,
        ComboEnhancementRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Enhancing combo with breathing for session {SessionId}", session.SessionId);

        var breathingRate = session.PhysiologicalState.CurrentBreathingRate;
        var baseline = 14f; // Default baseline
        var syncQuality = 1 - Math.Abs(breathingRate - baseline) / baseline;
        var baseHitCount = request.BaseCombo?.Length ?? 3;
        var baseDamage = baseHitCount * 10f; // Assume 10 damage per hit

        var enhancedCombo = new BreathingCombo
        {
            ComboId = Guid.NewGuid().ToString(),
            BaseCombo = request.BaseCombo,
            HitCount = (int)(baseHitCount * (1 + syncQuality * 0.3f)),
            TotalDamage = baseDamage * (1 + syncQuality * 0.4f),
            BreathingSynchronization = syncQuality,
            SpecialEffects = syncQuality > 0.8f ? "Flow State" : "Synced Strike",
            ExecutedAt = _timeProvider.UtcNow
        };

        _logger.LogInformation("Combo enhanced: {ComboId} with {HitCount} hits, sync {Sync:F2}",
            enhancedCombo.ComboId, enhancedCombo.HitCount, syncQuality);
        return Task.FromResult(enhancedCombo);
    }
}
