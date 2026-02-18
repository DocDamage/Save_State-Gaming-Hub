using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.NarrativeMemory;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.NarrativeMemory.Engines;

/// <summary>
/// Engine for managing butterfly effects and cascade reactions.
/// </summary>
public class ButterflyEngine
{
    private readonly ILogger<ButterflyEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, ButterflyEffect> _effects;
    private readonly ConcurrentDictionary<string, ButterflyEffectResult> _effectResults;

    /// <summary>
    /// Initializes a new instance of the <see cref="ButterflyEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public ButterflyEngine(ILogger<ButterflyEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _effects = new ConcurrentDictionary<string, ButterflyEffect>();
        _effectResults = new ConcurrentDictionary<string, ButterflyEffectResult>();
    }

    /// <summary>
    /// Triggers a butterfly effect based on a crystal interaction.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The butterfly effect request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The butterfly effect result.</returns>
    public Task<ButterflyEffectResult> TriggerEffectAsync(
        string userId,
        ButterflyEffectRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Triggering butterfly effect for user {UserId} with intensity {Intensity} and cascade depth {CascadeDepth}",
            userId,
            request.Intensity,
            request.CascadeDepth);

        var effectId = Guid.NewGuid().ToString();
        var now = _timeProvider.UtcNow;

        // Generate affected crystals (simulated based on cascade depth)
        var affectedCrystals = GenerateAffectedCrystals(request);

        // Create the butterfly effect
        var effect = new ButterflyEffect
        {
            EffectId = effectId,
            SourceCrystalId = userId, // Using userId as source reference
            AffectedCrystals = affectedCrystals,
            Magnitude = CalculateMagnitude(request),
            CascadeDepth = request.CascadeDepth,
            TriggeredAt = now,
            Duration = CalculateDuration(request)
        };

        _effects[effectId] = effect;

        // Create the result wrapper
        var result = new ButterflyEffectResult
        {
            EffectId = effectId,
            Success = true,
            Effect = effect,
            AffectedCrystalIds = affectedCrystals,
            CompletedAt = now.Add(effect.Duration)
        };

        _effectResults[effectId] = result;

        _logger.LogInformation(
            "Butterfly effect {EffectId} triggered with magnitude {Magnitude:P}, affecting {AffectedCount} crystals",
            effectId,
            effect.Magnitude,
            affectedCrystals.Count);

        return Task.FromResult(result);
    }

    /// <summary>
    /// Gets a butterfly effect by ID.
    /// </summary>
    /// <param name="effectId">The effect ID.</param>
    /// <returns>The butterfly effect if found; otherwise, null.</returns>
    public ButterflyEffect? GetEffect(string effectId)
    {
        _effects.TryGetValue(effectId, out var effect);
        return effect;
    }

    /// <summary>
    /// Gets a butterfly effect result by ID.
    /// </summary>
    /// <param name="effectId">The effect ID.</param>
    /// <returns>The butterfly effect result if found; otherwise, null.</returns>
    public ButterflyEffectResult? GetEffectResult(string effectId)
    {
        _effectResults.TryGetValue(effectId, out var result);
        return result;
    }

    /// <summary>
    /// Gets all butterfly effects for a source crystal.
    /// </summary>
    /// <param name="sourceCrystalId">The source crystal ID.</param>
    /// <returns>The collection of butterfly effects.</returns>
    public IEnumerable<ButterflyEffect> GetEffectsBySource(string sourceCrystalId)
    {
        return _effects.Values.Where(e => e.SourceCrystalId == sourceCrystalId);
    }

    /// <summary>
    /// Gets all active butterfly effects.
    /// </summary>
    /// <returns>The collection of active butterfly effects.</returns>
    public IEnumerable<ButterflyEffect> GetActiveEffects()
    {
        var now = _timeProvider.UtcNow;
        return _effects.Values.Where(e => e.TriggeredAt.Add(e.Duration) > now);
    }

    /// <summary>
    /// Removes a butterfly effect.
    /// </summary>
    /// <param name="effectId">The effect ID.</param>
    /// <returns>True if removed; otherwise, false.</returns>
    public bool RemoveEffect(string effectId)
    {
        _effectResults.TryRemove(effectId, out _);
        return _effects.TryRemove(effectId, out _);
    }

    /// <summary>
    /// Propagates an existing butterfly effect to increase its cascade.
    /// </summary>
    /// <param name="effectId">The effect ID to propagate.</param>
    /// <param name="additionalDepth">Additional cascade depth.</param>
    /// <returns>The updated butterfly effect result.</returns>
    public Task<ButterflyEffectResult> PropagateEffectAsync(
        string effectId,
        int additionalDepth)
    {
        _logger.LogInformation(
            "Propagating butterfly effect {EffectId} with additional depth {AdditionalDepth}",
            effectId,
            additionalDepth);

        if (!_effects.TryGetValue(effectId, out var effect))
        {
            _logger.LogWarning("Butterfly effect {EffectId} not found for propagation", effectId);
            throw new KeyNotFoundException($"Butterfly effect {effectId} not found");
        }

        // Update the effect with increased cascade
        var updatedEffect = new ButterflyEffect
        {
            EffectId = effect.EffectId,
            SourceCrystalId = effect.SourceCrystalId,
            Magnitude = Math.Min(1.0f, effect.Magnitude * 1.2f),
            CascadeDepth = effect.CascadeDepth + additionalDepth,
            TriggeredAt = effect.TriggeredAt,
            Duration = effect.Duration
        };

        // Add more affected crystals
        var additionalCrystals = GenerateAdditionalAffectedCrystals(additionalDepth);
        var allAffectedCrystals = effect.AffectedCrystals.Concat(additionalCrystals).ToList();
        
        updatedEffect.AffectedCrystals = allAffectedCrystals;

        _effects[effectId] = updatedEffect;

        // Update the result
        var updatedResult = new ButterflyEffectResult
        {
            EffectId = effectId,
            Success = true,
            Effect = updatedEffect,
            AffectedCrystalIds = allAffectedCrystals,
            CompletedAt = updatedEffect.TriggeredAt.Add(updatedEffect.Duration)
        };

        _effectResults[effectId] = updatedResult;

        _logger.LogInformation(
            "Propagated butterfly effect {EffectId} to cascade depth {CascadeDepth}, now affecting {AffectedCount} crystals",
            effectId,
            updatedEffect.CascadeDepth,
            allAffectedCrystals.Count);

        return Task.FromResult(updatedResult);
    }

    private static List<string> GenerateAffectedCrystals(ButterflyEffectRequest request)
    {
        var affected = new List<string>();
        var count = request.CascadeDepth * 2 + 1;

        for (var i = 0; i < count; i++)
        {
            affected.Add($"crystal-{Guid.NewGuid().ToString()[..8]}");
        }

        return affected;
    }

    private static List<string> GenerateAdditionalAffectedCrystals(int additionalDepth)
    {
        var affected = new List<string>();
        var count = additionalDepth * 2;

        for (var i = 0; i < count; i++)
        {
            affected.Add($"crystal-{Guid.NewGuid().ToString()[..8]}");
        }

        return affected;
    }

    private static float CalculateMagnitude(ButterflyEffectRequest request)
    {
        // Magnitude is based on intensity and limited by cascade depth
        var baseMagnitude = request.Intensity;
        var depthFactor = Math.Min(request.CascadeDepth / 5.0f, 0.5f);
        
        var calculatedMagnitude = baseMagnitude * (1 + depthFactor);
        
        // Add slight randomness
        var randomFactor = 0.95f + (Random.Shared.NextSingle() * 0.1f);
        
        return Math.Min(1.0f, calculatedMagnitude * randomFactor);
    }

    private static TimeSpan CalculateDuration(ButterflyEffectRequest request)
    {
        // Duration increases with cascade depth
        var baseMinutes = 5;
        var depthMinutes = request.CascadeDepth * 2;
        var intensityMinutes = (int)(request.Intensity * 10);
        
        return TimeSpan.FromMinutes(baseMinutes + depthMinutes + intensityMinutes);
    }
}
