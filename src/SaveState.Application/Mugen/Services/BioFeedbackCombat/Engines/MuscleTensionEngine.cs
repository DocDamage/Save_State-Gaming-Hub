namespace SaveState.Application.Mugen.Services.BioFeedbackCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for processing muscle tension data and powering defense.
/// </summary>
public class MuscleTensionEngine
{
    private readonly ILogger<MuscleTensionEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public MuscleTensionEngine(ILogger<MuscleTensionEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processes muscle tension data and generates feedback.
    /// </summary>
    public Task<MuscleFeedback> ProcessMuscleTensionAsync(
        BioFeedbackCombatSession session,
        float muscleTension,
        BioProfile profile,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Processing muscle tension: {MuscleTension} for session {SessionId}", muscleTension, session.SessionId);

        var baseline = profile.BaselineMetrics.BaselineMuscleTension;
        var tensionLevel = Math.Min(muscleTension / baseline, 2.0f);
        var fatigueIndicator = muscleTension > baseline * 1.8f;

        var feedback = new MuscleFeedback
        {
            CurrentMuscleTension = muscleTension,
            TensionLevel = tensionLevel,
            Intensity = Math.Min(tensionLevel, 1.0f),
            DamageBonus = tensionLevel > 1.2f ? (tensionLevel - 1.2f) * 0.3f : 0f,
            SpeedBonus = tensionLevel < 0.8f ? (0.8f - tensionLevel) * 0.2f : 0f,
            DefenseBonus = Math.Min(tensionLevel * profile.BioSettings.MuscleSensitivity * 0.5f, 0.4f),
            BlockingPower = tensionLevel > 1.0f,
            FatigueIndicator = fatigueIndicator
        };

        return Task.FromResult(feedback);
    }

    /// <summary>
    /// Powers defense based on muscle tension.
    /// </summary>
    public Task<MusclePoweredDefense> PowerDefenseAsync(
        BioFeedbackCombatSession session,
        DefenseRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Powering defense with muscles for session {SessionId}", session.SessionId);

        var muscleTension = session.PhysiologicalState.CurrentMuscleTension;
        var baseline = 0.3f; // Default baseline
        var tensionMultiplier = Math.Min(muscleTension / baseline, 2.0f);
        var baseStrength = request.PerfectBlock ? 100f : 50f;

        var defense = new MusclePoweredDefense
        {
            DefenseId = Guid.NewGuid().ToString(),
            BlockType = request.BlockType,
            BlockStrength = baseStrength * tensionMultiplier,
            DamageReduction = Math.Min(tensionMultiplier * 0.3f, 0.8f),
            PushbackForce = tensionMultiplier > 1.5f ? (tensionMultiplier - 1.5f) * 2f : 0f,
            CounterAttackReady = tensionMultiplier > 1.2f && request.BlockType == "Perfect",
            ExecutedAt = _timeProvider.UtcNow
        };

        _logger.LogInformation("Defense powered: {DefenseId} with strength {Strength:F2}",
            defense.DefenseId, defense.BlockStrength);
        return Task.FromResult(defense);
    }
}
