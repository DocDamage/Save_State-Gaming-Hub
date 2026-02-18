namespace SaveState.Application.Mugen.Services.BioFeedbackCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for managing meditation modes and techniques.
/// </summary>
public class MeditationEngine
{
    private readonly ILogger<MeditationEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public MeditationEngine(ILogger<MeditationEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Starts a meditation session with the specified technique.
    /// </summary>
    public Task<MeditationMode> StartMeditationAsync(
        BioFeedbackCombatSession session,
        MeditationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting meditation for session {SessionId}, technique: {Technique}",
            session.SessionId, request.Technique);

        var technique = request.Technique;
        var (focusLevel, stressReduction, energyRecovery, specialAbilities) = CalculateMeditationEffects(technique);

        var meditation = new MeditationMode
        {
            MeditationId = Guid.NewGuid().ToString(),
            Technique = technique,
            Duration = request.Duration,
            FocusLevel = focusLevel,
            StressReduction = stressReduction,
            EnergyRecovery = energyRecovery,
            SpecialAbilities = specialAbilities,
            StartedAt = _timeProvider.UtcNow
        };

        _logger.LogInformation("Meditation started: {MeditationId}, technique: {Technique}, focus: {Focus:F2}",
            meditation.MeditationId, technique, focusLevel);
        return Task.FromResult(meditation);
    }

    private static (float focusLevel, float stressReduction, float energyRecovery, List<string> abilities)
        CalculateMeditationEffects(MeditationTechnique technique)
    {
        return technique switch
        {
            MeditationTechnique.BreathFocus => (0.8f, 0.6f, 0.3f, new List<string> { "Calm Mind" }),
            MeditationTechnique.BodyScan => (0.6f, 0.8f, 0.5f, new List<string> { "Pain Resistance" }),
            MeditationTechnique.Visualization => (0.9f, 0.4f, 0.4f, new List<string> { "Combat Prediction" }),
            MeditationTechnique.Mantra => (0.7f, 0.7f, 0.4f, new List<string> { "Mental Fortress" }),
            MeditationTechnique.Mindfulness => (0.85f, 0.5f, 0.6f, new List<string> { "Quick Recovery" }),
            MeditationTechnique.Zen => (0.95f, 0.9f, 0.7f, new List<string> { "Perfect Balance", "Flow State" }),
            _ => (0.5f, 0.3f, 0.2f, new List<string>())
        };
    }
}
