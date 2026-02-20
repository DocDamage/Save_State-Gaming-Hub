using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Managers;

/// <summary>
/// Manager for partner adaptation operations.
/// </summary>
public class AdaptationManager
{
    private readonly ILogger<AdaptationManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public AdaptationManager(ILogger<AdaptationManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SymbioticPartnerServicePartnerAdaptation>> AdaptToBehaviorAsync(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServicePlayerBehavior behavior,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adapting partner {PartnerId} to player behavior", partner.PartnerId);

            var newPlaystyle = DeterminePreferredPlaystyle(behavior);
            var updatedAbilities = AdaptAbilities(partner.Abilities, behavior);

            var adaptation = new SymbioticPartnerServicePartnerAdaptation
            {
                PartnerId = partner.PartnerId,
                OldPreferredPlaystyle = partner.Preferences.PreferredPlaystyle,
                NewPreferredPlaystyle = newPlaystyle,
                UpdatedAbilities = updatedAbilities,
                BondIncrease = 0.05f,
                AdaptationTimestamp = _timeProvider.UtcNow
            };

            _logger.LogInformation("Partner adapted: bond strength +{BondIncrease:F2}, new playstyle {NewPlaystyle}",
                adaptation.BondIncrease, newPlaystyle);

            return Result.Success(adaptation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adapting partner {PartnerId}", partner.PartnerId);
            return Result.Failure<SymbioticPartnerServicePartnerAdaptation>($"Partner adaptation failed: {ex.Message}");
        }
    }

    private SymbioticPartnerServicePlaystyle DeterminePreferredPlaystyle(SymbioticPartnerServicePlayerBehavior behavior)
    {
        return behavior.Aggressiveness > 0.7f ? SymbioticPartnerServicePlaystyle.Rushdown :
               behavior.Defensiveness > 0.7f ? SymbioticPartnerServicePlaystyle.Zoning :
               SymbioticPartnerServicePlaystyle.Balanced;
    }

    private List<SymbioticPartnerServicePartnerAbility> AdaptAbilities(IReadOnlyList<SymbioticPartnerServicePartnerAbility> abilities, SymbioticPartnerServicePlayerBehavior behavior)
    {
        return abilities.Select(a => a with { Power = (int)(a.Power * (1 + behavior.Aggressiveness * 0.2f)) }).ToList();
    }
}
