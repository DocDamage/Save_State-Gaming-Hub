namespace SaveState.Application.Mugen.Services.CrossPhase.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.CrossPhase;
using SaveState.Core.Common.Services;

public class IntegrationEngine
{
    private readonly ILogger<IntegrationEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public IntegrationEngine(ILogger<IntegrationEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<MechanicInteraction> AnalyzeInteractionEffectsAsync(
        MechanicType sourceMechanic,
        MechanicType targetMechanic,
        MechanicInteraction interaction,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Analyzing interaction effects from {Source} to {Target}",
            sourceMechanic, targetMechanic);

        // Analyze and enhance the interaction with effect data
        var analyzedInteraction = new MechanicInteraction
        {
            InteractionId = interaction.InteractionId,
            InteractionType = interaction.InteractionType,
            Intensity = interaction.Intensity * CalculateInteractionMultiplier(sourceMechanic, targetMechanic),
            InteractionData = new
            {
                Source = sourceMechanic,
                Target = targetMechanic,
                OriginalData = interaction.InteractionData,
                EffectMagnitude = interaction.Intensity
            },
            Timestamp = _timeProvider.UtcNow
        };

        return Task.FromResult(analyzedInteraction);
    }

    public Task<IReadOnlyList<MechanicEffect>> ApplyCascadingEffectsAsync(
        MechanicInteraction initialInteraction,
        IReadOnlyList<MechanicType> affectedMechanics,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Applying cascading effects to {Count} mechanics", affectedMechanics.Count);

        var effects = new List<MechanicEffect>();
        var cascadeIntensity = initialInteraction.Intensity;

        foreach (var mechanic in affectedMechanics)
        {
            cascadeIntensity *= 0.8f; // Reduce intensity for each cascade step

            if (cascadeIntensity < 0.1f)
                break;

            effects.Add(new MechanicEffect
            {
                EffectId = Guid.NewGuid().ToString(),
                SourceMechanic = mechanic,
                TargetMechanic = mechanic,
                EffectType = "Cascade",
                Magnitude = cascadeIntensity,
                Duration = TimeSpan.FromSeconds(5),
                IsCrossPhase = true,
                GeneratedAt = _timeProvider.UtcNow
            });
        }

        return Task.FromResult<IReadOnlyList<MechanicEffect>>(effects);
    }

    private static float CalculateInteractionMultiplier(MechanicType source, MechanicType target)
    {
        return source == target ? 1.0f : 0.8f;
    }
}
