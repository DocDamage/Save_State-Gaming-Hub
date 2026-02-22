using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Behavior modulator for opponent AI adaptation.
/// </summary>
public class DdaBehaviorModulator
{
    private readonly ILogger<DdaBehaviorModulator> _logger;
    private readonly ITimeProvider _timeProvider;

    public DdaBehaviorModulator(ILogger<DdaBehaviorModulator> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<DdaOpponentBehavior> GenerateBehaviorAsync(DdaDifficultyProfile profile, DdaDifficultyAdjustment adjustment, DdaMatchState matchState, CancellationToken ct = default)
    {
        var baseAggression = profile.BehaviorParameters.AggressionBase;
        var adaptationModifier = adjustment.AdjustmentType switch
        {
            DdaDifficultyAdjustmentType.Increase => 0.15,
            DdaDifficultyAdjustmentType.Decrease => -0.15,
            _ => 0.0
        };

        return Task.FromResult(new DdaOpponentBehavior
        {
            AggressionLevel = Math.Clamp(baseAggression + adaptationModifier, 0.1, 0.9),
            DefensePriority = profile.BehaviorParameters.DefensePriority,
            RiskTolerance = profile.BehaviorParameters.RiskTolerance,
            PatternComplexity = CalculatePatternComplexity(adjustment),
            ReactionTime = CalculateReactionTime(adjustment),
            ResourceUsage = CalculateResourceUsage(adjustment),
            ComboFrequency = CalculateComboFrequency(adjustment),
            ProjectileUsage = CalculateProjectileUsage(matchState),
            AntiAirFrequency = CalculateAntiAirFrequency(matchState),
            ThrowAttempts = CalculateThrowAttempts(matchState),
            MeterManagement = CalculateMeterManagement(adjustment),
            ActiveUntil = _timeProvider.UtcNow.AddMinutes(5)
        });
    }

    private double CalculatePatternComplexity(DdaDifficultyAdjustment adjustment)
    {
        return adjustment.AdjustmentType switch
        {
            DdaDifficultyAdjustmentType.Increase => 0.8,
            DdaDifficultyAdjustmentType.Decrease => 0.4,
            _ => 0.6
        };
    }

    private TimeSpan CalculateReactionTime(DdaDifficultyAdjustment adjustment)
    {
        var baseMs = 150;
        var modifier = adjustment.AdjustmentType switch
        {
            DdaDifficultyAdjustmentType.Increase => -30,
            DdaDifficultyAdjustmentType.Decrease => 50,
            _ => 0
        };
        return TimeSpan.FromMilliseconds(Math.Max(50, baseMs + modifier));
    }

    private double CalculateResourceUsage(DdaDifficultyAdjustment adjustment)
    {
        return adjustment.AdjustmentType switch
        {
            DdaDifficultyAdjustmentType.Increase => 0.9,
            DdaDifficultyAdjustmentType.Decrease => 0.6,
            _ => 0.75
        };
    }

    private double CalculateComboFrequency(DdaDifficultyAdjustment adjustment)
    {
        return adjustment.AdjustmentType switch
        {
            DdaDifficultyAdjustmentType.Increase => 0.7,
            DdaDifficultyAdjustmentType.Decrease => 0.4,
            _ => 0.55
        };
    }

    private double CalculateProjectileUsage(DdaMatchState matchState) => 0.6;
    private double CalculateAntiAirFrequency(DdaMatchState matchState) => 0.65;
    private double CalculateThrowAttempts(DdaMatchState matchState) => 0.45;

    private double CalculateMeterManagement(DdaDifficultyAdjustment adjustment)
    {
        return adjustment.AdjustmentType switch
        {
            DdaDifficultyAdjustmentType.Increase => 0.8,
            DdaDifficultyAdjustmentType.Decrease => 0.5,
            _ => 0.65
        };
    }
}
