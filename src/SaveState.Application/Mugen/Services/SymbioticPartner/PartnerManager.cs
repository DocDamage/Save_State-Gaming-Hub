using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.SymbioticPartner;

/// <summary>
/// Manager for symbiotic partner lifecycle operations including creation, evolution eligibility, and stat generation.
/// </summary>
public class PartnerManager
{
    private readonly ILogger<PartnerManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, SymbioticPartnerServiceSymbioticPartner> _partners;

    public PartnerManager(ILogger<PartnerManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _partners = new Dictionary<string, SymbioticPartnerServiceSymbioticPartner>();
        InitializeDefaultPartners();
    }

    /// <summary>
    /// Gets the dictionary of managed partners.
    /// </summary>
    public Dictionary<string, SymbioticPartnerServiceSymbioticPartner> Partners => _partners;

    /// <summary>
    /// Creates a new symbiotic partner for a player.
    /// </summary>
    public async Task<Result<SymbioticPartnerServiceSymbioticPartner>> CreatePartnerAsync(
        SymbioticPartnerServicePartnerCreationRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating symbiotic partner for player {PlayerId}: {PartnerName}", 
                request.PlayerId, 
                request.PartnerName);

            var partner = new SymbioticPartnerServiceSymbioticPartner
            {
                PartnerId = Guid.NewGuid().ToString(),
                PlayerId = request.PlayerId,
                Name = request.PartnerName,
                SymbioticPartnerServicePartnerType = request.SymbioticPartnerServicePartnerType,
                Personality = request.Personality,
                SymbioticPartnerServiceEvolutionStage = SymbioticPartnerServiceEvolutionStage.Egg,
                Experience = 0,
                Level = 1,
                TrustLevel = 0.5f,
                BondStrength = 0.0f,
                Abilities = GenerateInitialAbilities(request.SymbioticPartnerServicePartnerType),
                Stats = GenerateInitialStats(request.SymbioticPartnerServicePartnerType),
                Preferences = new SymbioticPartnerServicePartnerPreferences
                {
                    PreferredPlaystyle = request.Personality switch
                    {
                        SymbioticPartnerServicePartnerPersonality.Aggressive => SymbioticPartnerServicePlaystyle.Rushdown,
                        SymbioticPartnerServicePartnerPersonality.Defensive => SymbioticPartnerServicePlaystyle.Zoning,
                        SymbioticPartnerServicePartnerPersonality.Technical => SymbioticPartnerServicePlaystyle.Mixup,
                        _ => SymbioticPartnerServicePlaystyle.Balanced
                    },
                    SymbioticPartnerServiceCommunicationStyle = SymbioticPartnerServiceCommunicationStyle.Direct,
                    LearningRate = 0.7f
                },
                CreatedAt = _timeProvider.UtcNow,
                LastInteraction = _timeProvider.UtcNow,
                EvolutionHistory = new List<SymbioticPartnerServiceEvolutionEvent>(),
                Status = SymbioticPartnerServicePartnerStatus.Active
            };

            _partners[partner.PartnerId] = partner;

            _logger.LogInformation(
                "Symbiotic partner created: {PartnerId} at {SymbioticPartnerServiceEvolutionStage} stage", 
                partner.PartnerId, 
                partner.SymbioticPartnerServiceEvolutionStage);
            
            return Result.Success(partner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating symbiotic partner for player {PlayerId}", request.PlayerId);
            return Result.Failure<SymbioticPartnerServiceSymbioticPartner>($"Partner creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a partner is eligible for evolution and returns the trigger if eligible.
    /// </summary>
    /// <param name="partner">The partner to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The evolution trigger if eligible, null otherwise.</returns>
    public Task<SymbioticPartnerServiceEvolutionTrigger?> CheckEvolutionEligibilityAsync(
        SymbioticPartnerServiceSymbioticPartner partner, 
        CancellationToken ct = default)
    {
        var evolutionThreshold = (int)partner.SymbioticPartnerServiceEvolutionStage * 1000;
        
        if (partner.Experience >= evolutionThreshold)
        {
            var trigger = new SymbioticPartnerServiceEvolutionTrigger
            {
                TriggerType = SymbioticPartnerServiceEvolutionTriggerType.ExperienceThreshold,
                TriggerData = new { RequiredXP = evolutionThreshold, CurrentXP = partner.Experience }
            };
            
            return Task.FromResult<SymbioticPartnerServiceEvolutionTrigger?>(trigger);
        }

        return Task.FromResult<SymbioticPartnerServiceEvolutionTrigger?>(null);
    }

    /// <summary>
    /// Calculates the evolution progress percentage for a partner.
    /// </summary>
    public float CalculateEvolutionProgress(SymbioticPartnerServiceSymbioticPartner partner)
    {
        var currentThreshold = (int)partner.SymbioticPartnerServiceEvolutionStage * 1000;
        var nextThreshold = ((int)partner.SymbioticPartnerServiceEvolutionStage + 1) * 1000;
        return (partner.Experience - currentThreshold) / (nextThreshold - currentThreshold);
    }

    /// <summary>
    /// Gets a partner by ID.
    /// </summary>
    public SymbioticPartnerServiceSymbioticPartner? GetPartner(string partnerId)
    {
        _partners.TryGetValue(partnerId, out var partner);
        return partner;
    }

    /// <summary>
    /// Removes a partner from management.
    /// </summary>
    public bool RemovePartner(string partnerId)
    {
        return _partners.Remove(partnerId);
    }

    private void InitializeDefaultPartners()
    {
        // Create default partner templates
        _logger.LogInformation("Symbiotic partner system initialized");
    }

    private List<SymbioticPartnerServicePartnerAbility> GenerateInitialAbilities(SymbioticPartnerServicePartnerType partnerType)
    {
        return partnerType switch
        {
            SymbioticPartnerServicePartnerType.Combat => new List<SymbioticPartnerServicePartnerAbility>
            {
                new SymbioticPartnerServicePartnerAbility 
                { 
                    AbilityId = "basic_attack", 
                    Name = "Basic Strike", 
                    Power = 10, 
                    Cooldown = TimeSpan.FromSeconds(2) 
                },
                new SymbioticPartnerServicePartnerAbility 
                { 
                    AbilityId = "defend", 
                    Name = "Protect", 
                    Power = 5, 
                    Cooldown = TimeSpan.FromSeconds(5) 
                }
            },
            SymbioticPartnerServicePartnerType.Support => new List<SymbioticPartnerServicePartnerAbility>
            {
                new SymbioticPartnerServicePartnerAbility 
                { 
                    AbilityId = "heal", 
                    Name = "Restore", 
                    Power = 15, 
                    Cooldown = TimeSpan.FromSeconds(10) 
                },
                new SymbioticPartnerServicePartnerAbility 
                { 
                    AbilityId = "boost", 
                    Name = "Enhance", 
                    Power = 8, 
                    Cooldown = TimeSpan.FromSeconds(15) 
                }
            },
            _ => new List<SymbioticPartnerServicePartnerAbility>()
        };
    }

    private SymbioticPartnerServicePartnerStats GenerateInitialStats(SymbioticPartnerServicePartnerType partnerType)
    {
        return partnerType switch
        {
            SymbioticPartnerServicePartnerType.Combat => new SymbioticPartnerServicePartnerStats 
            { 
                Attack = 8, 
                Defense = 6, 
                Speed = 7, 
                Intelligence = 4 
            },
            SymbioticPartnerServicePartnerType.Support => new SymbioticPartnerServicePartnerStats 
            { 
                Attack = 4, 
                Defense = 5, 
                Speed = 6, 
                Intelligence = 9 
            },
            SymbioticPartnerServicePartnerType.Stealth => new SymbioticPartnerServicePartnerStats 
            { 
                Attack = 6, 
                Defense = 7, 
                Speed = 9, 
                Intelligence = 5 
            },
            _ => new SymbioticPartnerServicePartnerStats 
            { 
                Attack = 5, 
                Defense = 5, 
                Speed = 5, 
                Intelligence = 5 
            }
        };
    }
}
