using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.SymbioticPartner;

/// <summary>
/// Manages partner adaptation to player behavior and playstyle preferences.
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

    /// <summary>
    /// Adapts a partner based on observed player behavior, updating preferences, abilities, and bond strength.
    /// </summary>
    /// <param name="partner">The symbiotic partner to adapt.</param>
    /// <param name="behavior">The observed player behavior.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the adaptation details.</returns>
    public async Task<Result<SymbioticPartnerServicePartnerAdaptation>> AdaptPartnerAsync(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServicePlayerBehavior behavior,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adapting partner {PartnerId} to player behavior", partner.PartnerId);

            var adaptation = await AdaptToBehaviorAsync(partner, behavior, ct);

            // Update partner preferences and abilities
            partner.Preferences.PreferredPlaystyle = adaptation.NewPreferredPlaystyle;
            partner.Abilities = adaptation.UpdatedAbilities;

            // Increase bond strength
            partner.BondStrength = Math.Min(partner.BondStrength + adaptation.BondIncrease, 1.0f);

            _logger.LogInformation("Partner adapted: bond strength +{BondIncrease:F2}, new playstyle {NewPlaystyle}",
                adaptation.BondIncrease, adaptation.NewPreferredPlaystyle);

            return Result.Success(adaptation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adapting partner {PartnerId}", partner.PartnerId);
            return Result.Failure<SymbioticPartnerServicePartnerAdaptation>($"Partner adaptation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Adapts partner configuration to match observed player behavior patterns.
    /// </summary>
    /// <param name="partner">The symbiotic partner to adapt.</param>
    /// <param name="behavior">The observed player behavior.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The adaptation details including playstyle changes and ability updates.</returns>
    public Task<SymbioticPartnerServicePartnerAdaptation> AdaptToBehaviorAsync(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServicePlayerBehavior behavior,
        CancellationToken ct)
    {
        // Adapt partner to player behavior
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

        return Task.FromResult(adaptation);
    }

    /// <summary>
    /// Determines the preferred playstyle based on player behavior metrics.
    /// </summary>
    /// <param name="behavior">The observed player behavior.</param>
    /// <returns>The recommended playstyle for the partner.</returns>
    private static SymbioticPartnerServicePlaystyle DeterminePreferredPlaystyle(SymbioticPartnerServicePlayerBehavior behavior)
    {
        // Determine preferred playstyle based on player behavior
        return behavior.Aggressiveness > 0.7f ? SymbioticPartnerServicePlaystyle.Rushdown :
               behavior.Defensiveness > 0.7f ? SymbioticPartnerServicePlaystyle.Zoning :
               SymbioticPartnerServicePlaystyle.Balanced;
    }

    /// <summary>
    /// Adapts partner abilities to align with player behavior patterns.
    /// </summary>
    /// <param name="abilities">The current partner abilities.</param>
    /// <param name="behavior">The observed player behavior.</param>
    /// <returns>The updated list of abilities with adjusted power levels.</returns>
    private static List<SymbioticPartnerServicePartnerAbility> AdaptAbilities(
        IReadOnlyList<SymbioticPartnerServicePartnerAbility> abilities,
        SymbioticPartnerServicePlayerBehavior behavior)
    {
        // Adapt abilities based on player behavior - increase power based on aggressiveness
        return abilities.Select(a => a with { Power = (int)(a.Power * (1 + behavior.Aggressiveness * 0.2f)) }).ToList();
    }
}
