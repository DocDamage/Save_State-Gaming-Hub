using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Managers;

/// <summary>
/// Manager for symbiosis session operations.
/// </summary>
public class SymbiosisManager
{
    private readonly ILogger<SymbiosisManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, SymbioticPartnerServiceSymbiosisSession> _symbiosisSessions = new();

    public SymbiosisManager(ILogger<SymbiosisManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the sessions dictionary for lookup by other managers.
    /// </summary>
    public IReadOnlyDictionary<string, SymbioticPartnerServiceSymbiosisSession> Sessions => _symbiosisSessions;

    public async Task<Result<SymbioticPartnerServiceSymbiosisSession>> InitiateSymbiosisAsync(
        SymbioticPartnerServiceSymbioticPartner partner,
        string playerId,
        SymbioticPartnerServiceSymbiosisRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Initiating symbiosis between player {PlayerId} and partner {PartnerId}", playerId, partner.PartnerId);

            var session = new SymbioticPartnerServiceSymbiosisSession
            {
                SessionId = Guid.NewGuid().ToString(),
                PlayerId = playerId,
                PartnerId = partner.PartnerId,
                SymbioticPartnerServiceSymbiosisType = request.SymbioticPartnerServiceSymbiosisType,
                FusionLevel = CalculateFusionLevel(partner),
                SynergyEffects = GenerateSynergyEffects(partner, request.SymbioticPartnerServiceSymbiosisType),
                StartedAt = _timeProvider.UtcNow,
                Duration = request.Duration,
                Status = SymbioticPartnerServiceSymbiosisStatus.Active,
                PerformanceMetrics = new SymbioticPartnerServiceSymbiosisMetrics
                {
                    Harmony = 0.8f,
                    Efficiency = 0.7f,
                    Stability = 0.9f,
                    PowerOutput = 1.0f
                }
            };

            _symbiosisSessions[session.SessionId] = session;

            await ApplySymbiosisEffectsAsync(session, ct);

            _logger.LogInformation("Symbiosis initiated: {SessionId} with {FusionLevel:F2} fusion level", session.SessionId, session.FusionLevel);
            return Result.Success<SymbioticPartnerServiceSymbiosisSession>(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating symbiosis for partner {PartnerId}", partner.PartnerId);
            return Result.Failure<SymbioticPartnerServiceSymbiosisSession>($"Symbiosis initiation failed: {ex.Message}");
        }
    }

    public async Task<Result<SymbioticPartnerServiceFusionAttack>> GenerateFusionAttackAsync(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServiceSymbiosisSession session,
        SymbioticPartnerServiceFusionAttackRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating fusion attack for session {SessionId}", session.SessionId);

            var fusionAttack = new SymbioticPartnerServiceFusionAttack
            {
                AttackId = Guid.NewGuid().ToString(),
                PartnerId = partner.PartnerId,
                AttackName = $"Fusion {request.BaseAttack} + {partner.Name}",
                Power = CalculateFusionPower(partner, session, request),
                Effects = CombineEffects(partner, request),
                Duration = TimeSpan.FromSeconds(5),
                Cooldown = TimeSpan.FromSeconds(30),
                GeneratedAt = _timeProvider.UtcNow
            };

            session.PerformanceMetrics.PowerOutput *= 1.5f;

            _logger.LogInformation("Fusion attack performed: {AttackName} with {Power} power", fusionAttack.AttackName, fusionAttack.Power);
            return Result.Success<SymbioticPartnerServiceFusionAttack>(fusionAttack);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating fusion attack for session {SessionId}", session.SessionId);
            return Result.Failure<SymbioticPartnerServiceFusionAttack>($"Fusion attack failed: {ex.Message}");
        }
    }

    public async Task<Result<SymbioticPartnerServiceSessionRewards>> EndSymbiosisAsync(
        string sessionId,
        Dictionary<string, SymbioticPartnerServiceSymbioticPartner> partners,
        CancellationToken ct = default)
    {
        try
        {
            if (!_symbiosisSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<SymbioticPartnerServiceSessionRewards>("Symbiosis session not found");
            }

            _logger.LogInformation("Ending symbiosis session {SessionId}", sessionId);

            var rewards = CalculateSessionRewards(session);

            if (partners.TryGetValue(session.PartnerId, out var partner))
            {
                partner.Experience += rewards.ExperienceGained;
                partner.BondStrength = Math.Min(partner.BondStrength + rewards.BondIncrease, 1.0f);
            }

            _symbiosisSessions.Remove(sessionId);

            _logger.LogInformation("Symbiosis session ended: +{Experience} XP, +{Bond:F2} bond", rewards.ExperienceGained, rewards.BondIncrease);
            return Result.Success<SymbioticPartnerServiceSessionRewards>(rewards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending symbiosis session {SessionId}", sessionId);
            return Result.Failure<SymbioticPartnerServiceSessionRewards>($"Session end failed: {ex.Message}");
        }
    }

    private float CalculateFusionLevel(SymbioticPartnerServiceSymbioticPartner partner)
    {
        return partner.BondStrength * (1 + (int)partner.SymbioticPartnerServiceEvolutionStage * 0.2f);
    }

    private List<SymbioticPartnerServicePartnerSynergyEffect> GenerateSynergyEffects(SymbioticPartnerServiceSymbioticPartner partner, SymbioticPartnerServiceSymbiosisType symbiosisType)
    {
        return new List<SymbioticPartnerServicePartnerSynergyEffect>
        {
            new SymbioticPartnerServicePartnerSynergyEffect { EffectType = "power_boost", Magnitude = partner.Level * 0.1f, Duration = TimeSpan.FromMinutes(5) },
            new SymbioticPartnerServicePartnerSynergyEffect { EffectType = "bond_bonus", Magnitude = partner.BondStrength * 0.5f, Duration = TimeSpan.FromMinutes(5) }
        };
    }

    private async Task ApplySymbiosisEffectsAsync(SymbioticPartnerServiceSymbiosisSession session, CancellationToken ct)
    {
        await Task.Delay(50, ct);
    }

    private int CalculateFusionPower(SymbioticPartnerServiceSymbioticPartner partner, SymbioticPartnerServiceSymbiosisSession session, SymbioticPartnerServiceFusionAttackRequest request)
    {
        return (int)(request.BasePower * session.FusionLevel * (1 + partner.Level * 0.1f));
    }

    private List<SymbioticPartnerServiceAttackEffect> CombineEffects(SymbioticPartnerServiceSymbioticPartner partner, SymbioticPartnerServiceFusionAttackRequest request)
    {
        return new List<SymbioticPartnerServiceAttackEffect>
        {
            new SymbioticPartnerServiceAttackEffect { EffectType = "damage_boost", Magnitude = 1.5f, Duration = TimeSpan.FromSeconds(2) },
            new SymbioticPartnerServiceAttackEffect { EffectType = "stun", Magnitude = 0.8f, Duration = TimeSpan.FromSeconds(1) }
        };
    }

    private SymbioticPartnerServiceSessionRewards CalculateSessionRewards(SymbioticPartnerServiceSymbiosisSession session)
    {
        return new SymbioticPartnerServiceSessionRewards
        {
            ExperienceGained = (int)(session.PerformanceMetrics.Harmony * 100),
            BondIncrease = session.PerformanceMetrics.Efficiency * 0.1f,
            AbilitiesUnlocked = session.FusionLevel > 0.8f ? 1 : 0
        };
    }
}
