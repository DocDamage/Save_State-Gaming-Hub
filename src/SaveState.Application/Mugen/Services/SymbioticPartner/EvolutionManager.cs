using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.SymbioticPartner;

/// <summary>
/// Manages partner evolution through different stages (Egg→Larva→Pupa→Adult→Ultimate).
/// Handles evolution conditions checking, ability generation, and stat evolution.
/// </summary>
public class EvolutionManager
{
    private readonly ILogger<EvolutionManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public EvolutionManager(ILogger<EvolutionManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processes evolution for a partner based on the specified trigger.
    /// Checks evolution conditions and advances stage if conditions are met.
    /// </summary>
    /// <param name="partner">The partner to evolve</param>
    /// <param name="trigger">The evolution trigger</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The evolution result with new stage, abilities, and stats</returns>
    public async Task<SymbioticPartnerServicePartnerEvolution> ProcessEvolutionAsync(
        SymbioticPartnerServiceSymbioticPartner partner, 
        SymbioticPartnerServiceEvolutionTrigger trigger, 
        CancellationToken ct)
    {
        // Process partner evolution
        var canEvolve = CheckEvolutionConditions(partner, trigger);

        if (!canEvolve)
        {
            return new SymbioticPartnerServicePartnerEvolution
            {
                PartnerId = partner.PartnerId,
                Success = false,
                Reason = "Evolution conditions not met",
                OldStage = partner.SymbioticPartnerServiceEvolutionStage,
                NewStage = partner.SymbioticPartnerServiceEvolutionStage
            };
        }

        var newStage = (SymbioticPartnerServiceEvolutionStage)((int)partner.SymbioticPartnerServiceEvolutionStage + 1);
        var newLevel = partner.Level + 1;

        return new SymbioticPartnerServicePartnerEvolution
        {
            PartnerId = partner.PartnerId,
            Success = true,
            Reason = "Evolution successful",
            OldStage = partner.SymbioticPartnerServiceEvolutionStage,
            NewStage = newStage,
            NewLevel = newLevel,
            NewAbilities = GenerateEvolvedAbilities(partner, newStage),
            NewStats = EvolveStats(partner.Stats, newStage),
            EvolutionTimestamp = _timeProvider.UtcNow
        };
    }

    /// <summary>
    /// Checks if the partner meets the evolution conditions for the specified trigger.
    /// </summary>
    /// <param name="partner">The partner to check</param>
    /// <param name="trigger">The evolution trigger</param>
    /// <returns>True if evolution conditions are met, false otherwise</returns>
    private bool CheckEvolutionConditions(SymbioticPartnerServiceSymbioticPartner partner, SymbioticPartnerServiceEvolutionTrigger trigger)
    {
        // Check if partner meets evolution conditions
        return trigger.TriggerType switch
        {
            SymbioticPartnerServiceEvolutionTriggerType.ExperienceThreshold => partner.Experience >= (int)partner.SymbioticPartnerServiceEvolutionStage * 1000,
            SymbioticPartnerServiceEvolutionTriggerType.BondStrength => partner.BondStrength >= 0.8f,
            SymbioticPartnerServiceEvolutionTriggerType.CombatAchievement => partner.Level >= (int)partner.SymbioticPartnerServiceEvolutionStage * 5,
            _ => false
        };
    }

    /// <summary>
    /// Generates new abilities for the evolved partner based on the new stage.
    /// </summary>
    /// <param name="partner">The partner being evolved</param>
    /// <param name="newStage">The new evolution stage</param>
    /// <returns>List of abilities including new evolved abilities</returns>
    private List<SymbioticPartnerServicePartnerAbility> GenerateEvolvedAbilities(SymbioticPartnerServiceSymbioticPartner partner, SymbioticPartnerServiceEvolutionStage newStage)
    {
        // Generate new abilities for evolved partner
        var newAbilities = new List<SymbioticPartnerServicePartnerAbility>(partner.Abilities);

        switch (newStage)
        {
            case SymbioticPartnerServiceEvolutionStage.Larva:
                newAbilities.Add(new SymbioticPartnerServicePartnerAbility { AbilityId = "evolved_strike", Name = "Power Strike", Power = 20, Cooldown = TimeSpan.FromSeconds(3) });
                break;
            case SymbioticPartnerServiceEvolutionStage.Pupa:
                newAbilities.Add(new SymbioticPartnerServicePartnerAbility { AbilityId = "energy_wave", Name = "Energy Wave", Power = 30, Cooldown = TimeSpan.FromSeconds(8) });
                break;
            case SymbioticPartnerServiceEvolutionStage.Adult:
                newAbilities.Add(new SymbioticPartnerServicePartnerAbility { AbilityId = "ultimate_fusion", Name = "Fusion Blast", Power = 50, Cooldown = TimeSpan.FromSeconds(15) });
                break;
        }

        return newAbilities;
    }

    /// <summary>
    /// Evolves partner stats based on the new evolution stage.
    /// </summary>
    /// <param name="currentStats">The current partner stats</param>
    /// <param name="newStage">The new evolution stage</param>
    /// <returns>The evolved stats with increased values</returns>
    private SymbioticPartnerServicePartnerStats EvolveStats(SymbioticPartnerServicePartnerStats currentStats, SymbioticPartnerServiceEvolutionStage newStage)
    {
        // Evolve partner stats
        var multiplier = 1 + (int)newStage * 0.2f;
        return new SymbioticPartnerServicePartnerStats
        {
            Attack = (int)(currentStats.Attack * multiplier),
            Defense = (int)(currentStats.Defense * multiplier),
            Speed = (int)(currentStats.Speed * multiplier),
            Intelligence = (int)(currentStats.Intelligence * multiplier)
        };
    }
}
