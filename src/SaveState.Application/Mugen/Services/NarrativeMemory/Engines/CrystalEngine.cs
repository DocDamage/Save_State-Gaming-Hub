using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.NarrativeMemory;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.NarrativeMemory.Engines;

/// <summary>
/// Engine for managing memory crystals.
/// </summary>
public class CrystalEngine
{
    private readonly ILogger<CrystalEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, MemoryCrystal> _crystals;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrystalEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public CrystalEngine(ILogger<CrystalEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _crystals = new ConcurrentDictionary<string, MemoryCrystal>();
    }

    /// <summary>
    /// Generates a new memory crystal from a match memory.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="memory">The match memory.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated memory crystal.</returns>
    public Task<MemoryCrystal> GenerateCrystalAsync(
        string userId,
        MatchMemory memory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating crystal for user {UserId} from match {MatchId}", userId, memory.MatchId);

        var crystalId = Guid.NewGuid().ToString();
        var now = _timeProvider.UtcNow;

        // Calculate rarity based on match outcome and emotional intensity
        var rarity = CalculateRarity(memory.Outcome, memory.EmotionalContext);

        // Generate key moments from combos and match data
        var keyMoments = new List<string>(memory.CombosUsed);

        // Generate alternate possibilities based on match outcome
        var alternatePossibilities = GenerateAlternatePossibilities(memory);

        var crystal = new MemoryCrystal
        {
            CrystalId = crystalId,
            PlayerId = userId,
            MatchId = memory.MatchId,
            MatchOutcome = memory.Outcome,
            KeyMoments = keyMoments,
            AlternatePossibilities = alternatePossibilities,
            EmotionalContext = memory.EmotionalContext,
            Rarity = rarity,
            Value = CalculateCrystalValue(rarity, memory),
            GeneratedAt = now,
            ExpiresAt = now.AddDays(30)
        };

        _crystals[crystalId] = crystal;

        _logger.LogInformation(
            "Generated {Rarity} crystal {CrystalId} for user {UserId}",
            rarity,
            crystalId,
            userId);

        return Task.FromResult(crystal);
    }

    /// <summary>
    /// Enhances an existing crystal.
    /// </summary>
    /// <param name="crystalId">The crystal ID.</param>
    /// <param name="request">The enhancement request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The enhanced memory crystal.</returns>
    public Task<MemoryCrystal> EnhanceCrystalAsync(
        string crystalId,
        EnhancementRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Enhancing crystal {CrystalId} with {EnhancementType} at strength {Strength}",
            crystalId,
            request.EnhancementType,
            request.EnhancementStrength);

        if (!_crystals.TryGetValue(crystalId, out var crystal))
        {
            _logger.LogWarning("Crystal {CrystalId} not found for enhancement", crystalId);
            throw new KeyNotFoundException($"Crystal {crystalId} not found");
        }

        // Apply enhancement based on type
        crystal = ApplyEnhancement(crystal, request);

        // Update the stored crystal
        _crystals[crystalId] = crystal;

        _logger.LogInformation(
            "Successfully enhanced crystal {CrystalId} to rarity {Rarity} with value {Value}",
            crystalId,
            crystal.Rarity,
            crystal.Value);

        return Task.FromResult(crystal);
    }

    /// <summary>
    /// Gets a crystal by ID.
    /// </summary>
    /// <param name="crystalId">The crystal ID.</param>
    /// <returns>The crystal if found; otherwise, null.</returns>
    public MemoryCrystal? GetCrystal(string crystalId)
    {
        _crystals.TryGetValue(crystalId, out var crystal);
        return crystal;
    }

    /// <summary>
    /// Gets all crystals for a player.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <returns>The collection of crystals.</returns>
    public IEnumerable<MemoryCrystal> GetPlayerCrystals(string playerId)
    {
        return _crystals.Values.Where(c => c.PlayerId == playerId);
    }

    /// <summary>
    /// Removes a crystal.
    /// </summary>
    /// <param name="crystalId">The crystal ID.</param>
    /// <returns>True if removed; otherwise, false.</returns>
    public bool RemoveCrystal(string crystalId)
    {
        return _crystals.TryRemove(crystalId, out _);
    }

    private static CrystalRarity CalculateRarity(MatchOutcome outcome, EmotionalContext emotionalContext)
    {
        var baseScore = outcome switch
        {
            MatchOutcome.Victory => 50,
            MatchOutcome.Defeat => 30,
            MatchOutcome.Draw => 40,
            MatchOutcome.Timeout => 20,
            _ => 25
        };

        // Factor in emotional intensity (0-1)
        var emotionalBonus = (int)(emotionalContext.Intensity * 30);
        var totalScore = baseScore + emotionalBonus;

        return totalScore switch
        {
            >= 80 => CrystalRarity.Legendary,
            >= 65 => CrystalRarity.Epic,
            >= 50 => CrystalRarity.Rare,
            >= 35 => CrystalRarity.Uncommon,
            _ => CrystalRarity.Common
        };
    }

    private static decimal CalculateCrystalValue(CrystalRarity rarity, MatchMemory memory)
    {
        var baseValue = rarity switch
        {
            CrystalRarity.Common => 10m,
            CrystalRarity.Uncommon => 25m,
            CrystalRarity.Rare => 50m,
            CrystalRarity.Epic => 100m,
            CrystalRarity.Legendary => 250m,
            _ => 10m
        };

        // Factor in match duration and damage dealt
        var durationMultiplier = 1 + (memory.Duration.TotalMinutes / 10);
        var damageFactor = memory.DamageDealt / 1000.0;

        return baseValue * (decimal)durationMultiplier + (decimal)damageFactor;
    }

    private static List<AlternatePossibility> GenerateAlternatePossibilities(MatchMemory memory)
    {
        var possibilities = new List<AlternatePossibility>();

        // Generate possibilities based on match outcome
        if (memory.Outcome == MatchOutcome.Defeat)
        {
            possibilities.Add(new AlternatePossibility
            {
                Scenario = "If you had blocked the final combo",
                Probability = 0.6f,
                Outcome = "Victory",
                CrystalValue = 75
            });

            possibilities.Add(new AlternatePossibility
            {
                Scenario = "If you had used your special move earlier",
                Probability = 0.45f,
                Outcome = "Victory",
                CrystalValue = 60
            });
        }
        else if (memory.Outcome == MatchOutcome.Victory)
        {
            possibilities.Add(new AlternatePossibility
            {
                Scenario = "If the opponent had perfect defense",
                Probability = 0.3f,
                Outcome = "Defeat",
                CrystalValue = 40
            });
        }

        return possibilities;
    }

    private static MemoryCrystal ApplyEnhancement(MemoryCrystal crystal, EnhancementRequest request)
    {
        var enhancedCrystal = crystal with
        {
            Value = crystal.Value,
            Rarity = crystal.Rarity
        };

        switch (request.EnhancementType)
        {
            case EnhancementType.PowerBoost:
                enhancedCrystal = enhancedCrystal with
                {
                    Value = enhancedCrystal.Value * (1 + (decimal)request.EnhancementStrength * 0.2m)
                };
                break;

            case EnhancementType.StabilityIncrease:
                // Increase expiration date
                enhancedCrystal = enhancedCrystal with
                {
                    ExpiresAt = enhancedCrystal.ExpiresAt.AddDays(request.EnhancementStrength * 10)
                };
                break;

            case EnhancementType.RarityUpgrade:
                if (request.EnhancementStrength > 0.5f && enhancedCrystal.Rarity < CrystalRarity.Legendary)
                {
                    enhancedCrystal = enhancedCrystal with
                    {
                        Rarity = enhancedCrystal.Rarity + 1
                    };
                }
                break;

            case EnhancementType.EffectEnhancement:
                // Add additional key moments
                var enhancedMoments = new List<string>(enhancedCrystal.KeyMoments)
                {
                    $"Enhanced with {request.EnhancementType}"
                };
                enhancedCrystal = enhancedCrystal with
                {
                    KeyMoments = enhancedMoments
                };
                break;
        }

        return enhancedCrystal;
    }
}
