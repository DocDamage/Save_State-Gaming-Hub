using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Managers;

/// <summary>
/// Manager for partner evolution operations.
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

    public async Task<Result<SymbioticPartnerServicePartnerEvolution>> ProcessEvolutionAsync(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServiceEvolutionTrigger trigger,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Attempting evolution for partner {PartnerId} with trigger {TriggerType}", partner.PartnerId, trigger.TriggerType);

            var canEvolve = CheckEvolutionConditions(partner, trigger);

            if (!canEvolve)
            {
                return Result.Success(new SymbioticPartnerServicePartnerEvolution
                {
                    PartnerId = partner.PartnerId,
                    Success = false,
                    Reason = "Evolution conditions not met",
                    OldStage = partner.SymbioticPartnerServiceEvolutionStage,
                    NewStage = partner.SymbioticPartnerServiceEvolutionStage
                });
            }

            var oldStage = partner.SymbioticPartnerServiceEvolutionStage;
            var newStage = (SymbioticPartnerServiceEvolutionStage)((int)oldStage + 1);
            var newLevel = partner.Level + 1;

            var evolution = new SymbioticPartnerServicePartnerEvolution
            {
                PartnerId = partner.PartnerId,
                Success = true,
                Reason = "Evolution successful",
                OldStage = oldStage,
                NewStage = newStage,
                NewLevel = newLevel,
                NewAbilities = GenerateEvolvedAbilities(partner, newStage),
                NewStats = EvolveStats(partner.Stats, newStage),
                EvolutionTimestamp = _timeProvider.UtcNow
            };

            _logger.LogInformation("Partner evolved: {PartnerId} -> {NewStage}", partner.PartnerId, newStage);
            return Result.Success(evolution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evolving partner {PartnerId}", partner.PartnerId);
            return Result.Failure<SymbioticPartnerServicePartnerEvolution>($"Partner evolution failed: {ex.Message}");
        }
    }

    public Result CheckEvolutionEligibility(SymbioticPartnerServiceSymbioticPartner partner)
    {
        var evolutionThreshold = (int)partner.SymbioticPartnerServiceEvolutionStage * 1000;
        if (partner.Experience >= evolutionThreshold)
        {
            return Result.Success();
        }
        return Result.Failure("Evolution threshold not met");
    }

    private bool CheckEvolutionConditions(SymbioticPartnerServiceSymbioticPartner partner, SymbioticPartnerServiceEvolutionTrigger trigger)
    {
        return trigger.TriggerType switch
        {
            SymbioticPartnerServiceEvolutionTriggerType.ExperienceThreshold => partner.Experience >= (int)partner.SymbioticPartnerServiceEvolutionStage * 1000,
            SymbioticPartnerServiceEvolutionTriggerType.BondStrength => partner.BondStrength >= 0.8f,
            SymbioticPartnerServiceEvolutionTriggerType.CombatAchievement => partner.Level >= (int)partner.SymbioticPartnerServiceEvolutionStage * 5,
            _ => false
        };
    }

    private List<SymbioticPartnerServicePartnerAbility> GenerateEvolvedAbilities(SymbioticPartnerServiceSymbioticPartner partner, SymbioticPartnerServiceEvolutionStage newStage)
    {
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

    private SymbioticPartnerServicePartnerStats EvolveStats(SymbioticPartnerServicePartnerStats currentStats, SymbioticPartnerServiceEvolutionStage newStage)
    {
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
