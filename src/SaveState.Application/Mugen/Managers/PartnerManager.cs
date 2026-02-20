using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Managers;

/// <summary>
/// Manager for partner lifecycle and data operations.
/// </summary>
public class PartnerManager
{
    private readonly ILogger<PartnerManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, SymbioticPartnerServiceSymbioticPartner> _partners = new();

    public PartnerManager(ILogger<PartnerManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the partners dictionary for lookup by other managers.
    /// </summary>
    public IReadOnlyDictionary<string, SymbioticPartnerServiceSymbioticPartner> Partners => _partners;

    public async Task<Result<SymbioticPartnerServiceSymbioticPartner>> CreatePartnerAsync(SymbioticPartnerServicePartnerCreationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating symbiotic partner for player {PlayerId}: {PartnerName}", request.PlayerId, request.PartnerName);

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

            _logger.LogInformation("Symbiotic partner created: {PartnerId} at {SymbioticPartnerServiceEvolutionStage} stage", partner.PartnerId, partner.SymbioticPartnerServiceEvolutionStage);
            return Result.Success<SymbioticPartnerServiceSymbioticPartner>(partner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating symbiotic partner for player {PlayerId}", request.PlayerId);
            return Result.Failure<SymbioticPartnerServiceSymbioticPartner>($"Partner creation failed: {ex.Message}");
        }
    }

    public void InitializeDefaultPartners()
    {
        _logger.LogInformation("Symbiotic partner system initialized");
    }

    private List<SymbioticPartnerServicePartnerAbility> GenerateInitialAbilities(SymbioticPartnerServicePartnerType partnerType)
    {
        return partnerType switch
        {
            SymbioticPartnerServicePartnerType.Combat => new List<SymbioticPartnerServicePartnerAbility>
            {
                new SymbioticPartnerServicePartnerAbility { AbilityId = "basic_attack", Name = "Basic Strike", Power = 10, Cooldown = TimeSpan.FromSeconds(2) },
                new SymbioticPartnerServicePartnerAbility { AbilityId = "defend", Name = "Protect", Power = 5, Cooldown = TimeSpan.FromSeconds(5) }
            },
            SymbioticPartnerServicePartnerType.Support => new List<SymbioticPartnerServicePartnerAbility>
            {
                new SymbioticPartnerServicePartnerAbility { AbilityId = "heal", Name = "Restore", Power = 15, Cooldown = TimeSpan.FromSeconds(10) },
                new SymbioticPartnerServicePartnerAbility { AbilityId = "boost", Name = "Enhance", Power = 8, Cooldown = TimeSpan.FromSeconds(15) }
            },
            _ => new List<SymbioticPartnerServicePartnerAbility>()
        };
    }

    private SymbioticPartnerServicePartnerStats GenerateInitialStats(SymbioticPartnerServicePartnerType partnerType)
    {
        return partnerType switch
        {
            SymbioticPartnerServicePartnerType.Combat => new SymbioticPartnerServicePartnerStats { Attack = 8, Defense = 6, Speed = 7, Intelligence = 4 },
            SymbioticPartnerServicePartnerType.Support => new SymbioticPartnerServicePartnerStats { Attack = 4, Defense = 5, Speed = 6, Intelligence = 9 },
            SymbioticPartnerServicePartnerType.Stealth => new SymbioticPartnerServicePartnerStats { Attack = 6, Defense = 7, Speed = 9, Intelligence = 5 },
            _ => new SymbioticPartnerServicePartnerStats { Attack = 5, Defense = 5, Speed = 5, Intelligence = 5 }
        };
    }
}
