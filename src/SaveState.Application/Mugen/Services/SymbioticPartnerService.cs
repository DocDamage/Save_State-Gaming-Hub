using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Managers;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Symbiotic partner service providing evolving AI companions that adapt to player behavior,
/// symbiotic relationships, and dynamic partnership mechanics.
/// Coordinates operations across specialized managers.
/// </summary>
public class SymbioticPartnerService : SymbioticPartnerServiceISymbioticPartnerService
{
    private readonly ILogger<SymbioticPartnerService> _logger;
    private readonly ICacheService _cache;
    private readonly PartnerManager _partnerManager;
    private readonly SymbiosisManager _symbiosisManager;
    private readonly EvolutionManager _evolutionManager;
    private readonly AdaptationManager _adaptationManager;
    private readonly CommunicationManager _communicationManager;
    private readonly PartnerAnalyticsManager _analyticsManager;

    public SymbioticPartnerService(
        ILogger<SymbioticPartnerService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider,
        PartnerManager partnerManager,
        SymbiosisManager symbiosisManager,
        EvolutionManager evolutionManager,
        AdaptationManager adaptationManager,
        CommunicationManager communicationManager,
        PartnerAnalyticsManager analyticsManager)
    {
        _logger = logger;
        _cache = cache;
        _partnerManager = partnerManager;
        _symbiosisManager = symbiosisManager;
        _evolutionManager = evolutionManager;
        _adaptationManager = adaptationManager;
        _communicationManager = communicationManager;
        _analyticsManager = analyticsManager;

        _partnerManager.InitializeDefaultPartners();
    }

    public Task<Result<SymbioticPartnerServiceSymbioticPartner>> CreatePartnerAsync(SymbioticPartnerServicePartnerCreationRequest request, CancellationToken ct = default)
        => _partnerManager.CreatePartnerAsync(request, ct);

    public Task<Result<SymbioticPartnerServiceSymbiosisSession>> InitiateSymbiosisAsync(string partnerId, string playerId, SymbioticPartnerServiceSymbiosisRequest request, CancellationToken ct = default)
    {
        if (!_partnerManager.Partners.TryGetValue(partnerId, out var partner))
        {
            return Task.FromResult(Result<SymbioticPartnerServiceSymbiosisSession>.Failure("Partner not found"));
        }
        if (partner.PlayerId != playerId)
        {
            return Task.FromResult(Result<SymbioticPartnerServiceSymbiosisSession>.Failure("Partner does not belong to this player"));
        }
        return _symbiosisManager.InitiateSymbiosisAsync(partner, playerId, request, ct);
    }

    public async Task<Result<SymbioticPartnerServicePartnerEvolution>> EvolvePartnerAsync(string partnerId, SymbioticPartnerServiceEvolutionTrigger trigger, CancellationToken ct = default)
    {
        if (!_partnerManager.Partners.TryGetValue(partnerId, out var partner))
        {
            return Result<SymbioticPartnerServicePartnerEvolution>.Failure("Partner not found");
        }

        var result = await _evolutionManager.ProcessEvolutionAsync(partner, trigger, ct);
        if (result.IsFailure)
        {
            return result;
        }

        var evolution = result.Value;
        if (evolution.Success)
        {
            partner.SymbioticPartnerServiceEvolutionStage = evolution.NewStage;
            partner.Level = evolution.NewLevel;
            partner.Abilities = evolution.NewAbilities;
            partner.Stats = evolution.NewStats;

            var history = new List<SymbioticPartnerServiceEvolutionEvent>(partner.EvolutionHistory)
            {
                new SymbioticPartnerServiceEvolutionEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Trigger = trigger,
                    OldStage = evolution.OldStage,
                    NewStage = evolution.NewStage,
                    Timestamp = evolution.EvolutionTimestamp
                }
            };
            partner.EvolutionHistory = history;
        }

        return Result.Success(evolution);
    }

    public async Task<Result<SymbioticPartnerServicePartnerAdaptation>> AdaptPartnerAsync(string partnerId, SymbioticPartnerServicePlayerBehavior behavior, CancellationToken ct = default)
    {
        if (!_partnerManager.Partners.TryGetValue(partnerId, out var partner))
        {
            return Result<SymbioticPartnerServicePartnerAdaptation>.Failure("Partner not found");
        }

        var result = await _adaptationManager.AdaptToBehaviorAsync(partner, behavior, ct);
        if (result.IsFailure)
        {
            return result;
        }

        var adaptation = result.Value;
        partner.Preferences.PreferredPlaystyle = adaptation.NewPreferredPlaystyle;
        partner.Abilities = adaptation.UpdatedAbilities;
        partner.BondStrength = Math.Min(partner.BondStrength + adaptation.BondIncrease, 1.0f);

        return Result.Success(adaptation);
    }

    public async Task<Result<SymbioticPartnerServiceCommunicationResponse>> CommunicateWithPartnerAsync(string partnerId, SymbioticPartnerServiceCommunicationRequest request, CancellationToken ct = default)
    {
        if (!_partnerManager.Partners.TryGetValue(partnerId, out var partner))
        {
            return Result<SymbioticPartnerServiceCommunicationResponse>.Failure("Partner not found");
        }

        var result = await _communicationManager.ProcessCommunicationAsync(partner, request, ct);
        if (result.IsFailure)
        {
            return result;
        }

        var response = result.Value;
        partner.TrustLevel = Math.Min(partner.TrustLevel + response.TrustChange, 1.0f);
        partner.LastInteraction = response.Timestamp;

        return Result.Success(response);
    }

    public async Task<Result<SymbioticPartnerServiceFusionAttack>> PerformFusionAttackAsync(string sessionId, SymbioticPartnerServiceFusionAttackRequest request, CancellationToken ct = default)
    {
        if (!_symbiosisManager.Sessions.TryGetValue(sessionId, out var session))
        {
            return Result<SymbioticPartnerServiceFusionAttack>.Failure("Symbiosis session not found");
        }

        if (!_partnerManager.Partners.TryGetValue(session.PartnerId, out var partner))
        {
            return Result<SymbioticPartnerServiceFusionAttack>.Failure("Partner not found");
        }

        return await _symbiosisManager.GenerateFusionAttackAsync(partner, session, request, ct);
    }

    public async Task<Result<SymbioticPartnerServicePartnerAnalytics>> GetPartnerAnalyticsAsync(string partnerId, TimeSpan period, CancellationToken ct = default)
    {
        if (!_partnerManager.Partners.TryGetValue(partnerId, out var partner))
        {
            return Result<SymbioticPartnerServicePartnerAnalytics>.Failure("Partner not found");
        }

        return await _analyticsManager.GetPartnerAnalyticsAsync(partner, period, ct);
    }

    public async Task<Result> EndSymbiosisAsync(string sessionId, CancellationToken ct = default)
    {
        var result = await _symbiosisManager.EndSymbiosisAsync(sessionId, (Dictionary<string, SymbioticPartnerServiceSymbioticPartner>)_partnerManager.Partners, ct);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error!);
        }

        var rewards = result.Value;

        if (_symbiosisManager.Sessions.TryGetValue(sessionId, out var session) &&
            _partnerManager.Partners.TryGetValue(session.PartnerId, out var partner))
        {
            await CheckEvolutionEligibilityAsync(partner, ct);
        }

        return Result.Success();
    }

    private async Task CheckEvolutionEligibilityAsync(SymbioticPartnerServiceSymbioticPartner partner, CancellationToken ct)
    {
        var eligibility = _evolutionManager.CheckEvolutionEligibility(partner);
        if (eligibility.IsSuccess)
        {
            var evolutionThreshold = (int)partner.SymbioticPartnerServiceEvolutionStage * 1000;
            await EvolvePartnerAsync(partner.PartnerId, new SymbioticPartnerServiceEvolutionTrigger
            {
                TriggerType = SymbioticPartnerServiceEvolutionTriggerType.ExperienceThreshold,
                TriggerData = new { RequiredXP = evolutionThreshold, CurrentXP = partner.Experience }
            }, ct);
        }
    }
}

/// <summary>
/// Symbiotic Partner Service interface.
/// </summary>
public interface SymbioticPartnerServiceISymbioticPartnerService
{
    Task<Result<SymbioticPartnerServiceSymbioticPartner>> CreatePartnerAsync(SymbioticPartnerServicePartnerCreationRequest request, CancellationToken ct = default);
    Task<Result<SymbioticPartnerServiceSymbiosisSession>> InitiateSymbiosisAsync(string partnerId, string playerId, SymbioticPartnerServiceSymbiosisRequest request, CancellationToken ct = default);
    Task<Result<SymbioticPartnerServicePartnerEvolution>> EvolvePartnerAsync(string partnerId, SymbioticPartnerServiceEvolutionTrigger trigger, CancellationToken ct = default);
    Task<Result<SymbioticPartnerServicePartnerAdaptation>> AdaptPartnerAsync(string partnerId, SymbioticPartnerServicePlayerBehavior behavior, CancellationToken ct = default);
    Task<Result<SymbioticPartnerServiceCommunicationResponse>> CommunicateWithPartnerAsync(string partnerId, SymbioticPartnerServiceCommunicationRequest request, CancellationToken ct = default);
    Task<Result<SymbioticPartnerServiceFusionAttack>> PerformFusionAttackAsync(string sessionId, SymbioticPartnerServiceFusionAttackRequest request, CancellationToken ct = default);
    Task<Result<SymbioticPartnerServicePartnerAnalytics>> GetPartnerAnalyticsAsync(string partnerId, TimeSpan period, CancellationToken ct = default);
    Task<Result> EndSymbiosisAsync(string sessionId, CancellationToken ct = default);
}

/// <summary>
/// Symbiotic partner data.
/// </summary>
public class SymbioticPartnerServiceSymbioticPartner
{
    public string PartnerId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public SymbioticPartnerServicePartnerType SymbioticPartnerServicePartnerType { get; set; } = default!;
    public SymbioticPartnerServicePartnerPersonality Personality { get; set; } = default!;
    public SymbioticPartnerServiceEvolutionStage SymbioticPartnerServiceEvolutionStage { get; set; } = default!;
    public int Experience { get; set; } = default!;
    public int Level { get; set; } = default!;
    public float TrustLevel { get; set; } = default!;
    public float BondStrength { get; set; } = default!;
    public IReadOnlyList<SymbioticPartnerServicePartnerAbility> Abilities { get; set; } = default!;
    public SymbioticPartnerServicePartnerStats Stats { get; set; } = default!;
    public SymbioticPartnerServicePartnerPreferences Preferences { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastInteraction { get; set; } = default!;
    public IReadOnlyList<SymbioticPartnerServiceEvolutionEvent> EvolutionHistory { get; set; } = default!;
    public SymbioticPartnerServicePartnerStatus Status { get; set; } = default!;
}

/// <summary>
/// Partner creation request.
/// </summary>
public class SymbioticPartnerServicePartnerCreationRequest
{
    public string PlayerId { get; set; } = default!;
    public string PartnerName { get; set; } = default!;
    public SymbioticPartnerServicePartnerType SymbioticPartnerServicePartnerType { get; set; } = default!;
    public SymbioticPartnerServicePartnerPersonality Personality { get; set; } = default!;
}

/// <summary>
/// Partner ability data.
/// </summary>
public record SymbioticPartnerServicePartnerAbility
{
    public string AbilityId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Power { get; set; } = default!;
    public TimeSpan Cooldown { get; set; } = default!;
}

/// <summary>
/// Partner stats data.
/// </summary>
public class SymbioticPartnerServicePartnerStats
{
    public int Attack { get; set; } = default!;
    public int Defense { get; set; } = default!;
    public int Speed { get; set; } = default!;
    public int Intelligence { get; set; } = default!;
}

/// <summary>
/// Partner preferences data.
/// </summary>
public class SymbioticPartnerServicePartnerPreferences
{
    public SymbioticPartnerServicePlaystyle PreferredPlaystyle { get; set; } = default!;
    public SymbioticPartnerServiceCommunicationStyle SymbioticPartnerServiceCommunicationStyle { get; set; } = default!;
    public float LearningRate { get; set; } = default!;
}

/// <summary>
/// Evolution event data.
/// </summary>
public class SymbioticPartnerServiceEvolutionEvent
{
    public string EventId { get; set; } = default!;
    public SymbioticPartnerServiceEvolutionTrigger Trigger { get; set; } = default!;
    public SymbioticPartnerServiceEvolutionStage OldStage { get; set; } = default!;
    public SymbioticPartnerServiceEvolutionStage NewStage { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Evolution trigger data.
/// </summary>
public class SymbioticPartnerServiceEvolutionTrigger
{
    public SymbioticPartnerServiceEvolutionTriggerType TriggerType { get; set; } = default!;
    public object TriggerData { get; set; } = default!;
}

/// <summary>
/// Partner evolution data.
/// </summary>
public class SymbioticPartnerServicePartnerEvolution
{
    public string PartnerId { get; set; } = default!;
    public bool Success { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public SymbioticPartnerServiceEvolutionStage OldStage { get; set; } = default!;
    public SymbioticPartnerServiceEvolutionStage NewStage { get; set; } = default!;
    public int NewLevel { get; set; } = default!;
    public IReadOnlyList<SymbioticPartnerServicePartnerAbility> NewAbilities { get; set; } = default!;
    public SymbioticPartnerServicePartnerStats NewStats { get; set; } = default!;
    public DateTime EvolutionTimestamp { get; set; } = default!;
}

/// <summary>
/// Symbiosis session data.
/// </summary>
public class SymbioticPartnerServiceSymbiosisSession
{
    public string SessionId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public string PartnerId { get; set; } = default!;
    public SymbioticPartnerServiceSymbiosisType SymbioticPartnerServiceSymbiosisType { get; set; } = default!;
    public float FusionLevel { get; set; } = default!;
    public IReadOnlyList<SymbioticPartnerServicePartnerSynergyEffect> SynergyEffects { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public SymbioticPartnerServiceSymbiosisStatus Status { get; set; } = default!;
    public SymbioticPartnerServiceSymbiosisMetrics PerformanceMetrics { get; set; } = default!;
}

/// <summary>
/// Symbiosis request.
/// </summary>
public class SymbioticPartnerServiceSymbiosisRequest
{
    public SymbioticPartnerServiceSymbiosisType SymbioticPartnerServiceSymbiosisType { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Synergy effect data.
/// </summary>
public class SymbioticPartnerServicePartnerSynergyEffect
{
    public string EffectType { get; set; } = default!;
    public float Magnitude { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Symbiosis metrics data.
/// </summary>
public class SymbioticPartnerServiceSymbiosisMetrics
{
    public float Harmony { get; set; } = default!;
    public float Efficiency { get; set; } = default!;
    public float Stability { get; set; } = default!;
    public float PowerOutput { get; set; } = default!;
}

/// <summary>
/// Partner adaptation data.
/// </summary>
public class SymbioticPartnerServicePartnerAdaptation
{
    public string PartnerId { get; set; } = default!;
    public SymbioticPartnerServicePlaystyle OldPreferredPlaystyle { get; set; } = default!;
    public SymbioticPartnerServicePlaystyle NewPreferredPlaystyle { get; set; } = default!;
    public IReadOnlyList<SymbioticPartnerServicePartnerAbility> UpdatedAbilities { get; set; } = default!;
    public float BondIncrease { get; set; } = default!;
    public DateTime AdaptationTimestamp { get; set; } = default!;
}

/// <summary>
/// Player behavior data.
/// </summary>
public class SymbioticPartnerServicePlayerBehavior
{
    public float Aggressiveness { get; set; } = default!;
    public float Defensiveness { get; set; } = default!;
    public float Technicality { get; set; } = default!;
    public SymbioticPartnerServicePlaystyle CurrentPlaystyle { get; set; } = default!;
    public IReadOnlyList<string> PreferredMoves { get; set; } = default!;
}

/// <summary>
/// Communication request.
/// </summary>
public class SymbioticPartnerServiceCommunicationRequest
{
    public SymbioticPartnerServiceMessageType SymbioticPartnerServiceMessageType { get; set; } = default!;
    public string Content { get; set; } = default!;
    public float Intensity { get; set; } = default!;
}

/// <summary>
/// Communication response data.
/// </summary>
public class SymbioticPartnerServiceCommunicationResponse
{
    public string PartnerId { get; set; } = default!;
    public SymbioticPartnerServiceResponseType SymbioticPartnerServiceResponseType { get; set; } = default!;
    public string Message { get; set; } = default!;
    public float EmotionalResponse { get; set; } = default!;
    public float TrustChange { get; set; } = default!;
    public float BondChange { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Fusion attack request.
/// </summary>
public class SymbioticPartnerServiceFusionAttackRequest
{
    public string BaseAttack { get; set; } = default!;
    public int BasePower { get; set; } = default!;
    public IReadOnlyList<string> PartnerAbilities { get; set; } = default!;
}

/// <summary>
/// Fusion attack data.
/// </summary>
public class SymbioticPartnerServiceFusionAttack
{
    public string AttackId { get; set; } = default!;
    public string PartnerId { get; set; } = default!;
    public string AttackName { get; set; } = default!;
    public int Power { get; set; } = default!;
    public IReadOnlyList<SymbioticPartnerServiceAttackEffect> Effects { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public TimeSpan Cooldown { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Attack effect data.
/// </summary>
public class SymbioticPartnerServiceAttackEffect
{
    public string EffectType { get; set; } = default!;
    public float Magnitude { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Partner analytics data.
/// </summary>
public class SymbioticPartnerServicePartnerAnalytics
{
    public string PartnerId { get; set; } = default!;
    public TimeSpan Period { get; set; } = default!;
    public float EvolutionProgress { get; set; } = default!;
    public SymbioticPartnerServiceSymbiosisStatistics SymbiosisStats { get; set; } = default!;
    public SymbioticPartnerServiceAdaptationStatistics AdaptationMetrics { get; set; } = default!;
    public SymbioticPartnerServiceCommunicationStatistics CommunicationStats { get; set; } = default!;
    public float BondStrengthTrend { get; set; } = default!;
    public SymbioticPartnerServicePartnerPerformanceMetrics PerformanceMetrics { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Symbiosis statistics data.
/// </summary>
public class SymbioticPartnerServiceSymbiosisStatistics
{
    public int TotalSessions { get; set; } = default!;
    public TimeSpan AverageDuration { get; set; } = default!;
    public float AverageFusionLevel { get; set; } = default!;
    public SymbioticPartnerServiceSymbiosisType MostUsedSymbiosisType { get; set; } = default!;
    public float SuccessRate { get; set; } = default!;
}

/// <summary>
/// Adaptation statistics data.
/// </summary>
public class SymbioticPartnerServiceAdaptationStatistics
{
    public int TotalAdaptations { get; set; } = default!;
    public float AverageBondIncrease { get; set; } = default!;
    public int PreferredPlaystyleChanges { get; set; } = default!;
    public int AbilityModifications { get; set; } = default!;
    public float AdaptationSuccessRate { get; set; } = default!;
}

/// <summary>
/// Communication statistics data.
/// </summary>
public class SymbioticPartnerServiceCommunicationStatistics
{
    public int TotalInteractions { get; set; } = default!;
    public TimeSpan AverageResponseTime { get; set; } = default!;
    public float TrustLevelIncrease { get; set; } = default!;
    public SymbioticPartnerServiceMessageType MostCommonMessageType { get; set; } = default!;
    public float CommunicationEffectiveness { get; set; } = default!;
}

/// <summary>
/// Partner performance metrics data.
/// </summary>
public class SymbioticPartnerServicePartnerPerformanceMetrics
{
    public float Effectiveness { get; set; } = default!;
    public float Reliability { get; set; } = default!;
    public float GrowthRate { get; set; } = default!;
    public float PlayerSatisfaction { get; set; } = default!;
}

/// <summary>
/// Session rewards data.
/// </summary>
public class SymbioticPartnerServiceSessionRewards
{
    public int ExperienceGained { get; set; } = default!;
    public float BondIncrease { get; set; } = default!;
    public int AbilitiesUnlocked { get; set; } = default!;
}

/// <summary>
/// Various enumeration types.
/// </summary>
public enum SymbioticPartnerServicePartnerType { Combat, Support, Stealth, Utility }
public enum SymbioticPartnerServicePartnerPersonality { Aggressive, Defensive, Technical, Loyal, Independent }
public enum SymbioticPartnerServiceEvolutionStage { Egg, Larva, Pupa, Adult, Ultimate }
public enum SymbioticPartnerServicePartnerStatus { Active, Resting, Evolving, Inactive }
public enum SymbioticPartnerServiceEvolutionTriggerType { ExperienceThreshold, BondStrength, CombatAchievement, TimeBased }
public enum SymbioticPartnerServiceSymbiosisType { CombatFusion, SupportLink, StealthBond, UtilitySync }
public enum SymbioticPartnerServiceSymbiosisStatus { Preparing, Active, Ending, Completed }
public enum SymbioticPartnerServicePlaystyle { Rushdown, Zoning, Mixup, Grappling, Balanced }
public enum SymbioticPartnerServiceCommunicationStyle { Direct, Subtle, Emotional, Analytical }
public enum SymbioticPartnerServiceMessageType { Encouragement, Praise, Criticism, Request, Question }
public enum SymbioticPartnerServiceResponseType { Positive, Negative, Neutral, Constructive, Grateful }
