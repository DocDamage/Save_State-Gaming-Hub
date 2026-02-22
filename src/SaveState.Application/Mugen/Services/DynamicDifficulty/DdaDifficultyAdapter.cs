using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Difficulty adapter for calculating adjustments.
/// </summary>
public class DdaDifficultyAdapter
{
    private readonly ILogger<DdaDifficultyAdapter> _logger;
    private readonly ITimeProvider _timeProvider;

    public DdaDifficultyAdapter(ILogger<DdaDifficultyAdapter> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<DdaDifficultyAdjustment> CalculateAdjustmentAsync(DdaDifficultyProfile profile, DdaCurrentPerformanceData performance, CancellationToken ct = default)
    {
        var adjustment = DdaDifficultyAdjustmentType.Maintain;
        var magnitude = 0.0;

        if (performance.WinRate < profile.PerformanceThresholds.WinRateDecreaseThreshold + 0.5)
        {
            adjustment = DdaDifficultyAdjustmentType.Decrease;
            magnitude = Math.Abs(performance.WinRate - 0.5) * 2;
        }
        else if (performance.WinRate > profile.PerformanceThresholds.WinRateIncreaseThreshold + 0.5)
        {
            adjustment = DdaDifficultyAdjustmentType.Increase;
            magnitude = (performance.WinRate - 0.5) * 2;
        }

        if (performance.ComboSuccess < profile.PerformanceThresholds.ComboSuccessThreshold)
        {
            magnitude += 0.1;
        }

        return Task.FromResult(new DdaDifficultyAdjustment
        {
            AdjustmentType = adjustment,
            Magnitude = Math.Clamp(magnitude, 0, 1),
            Reasoning = GenerateAdjustmentReasoning(adjustment, performance),
            Confidence = CalculateAdjustmentConfidence(performance),
            SuggestedDuration = TimeSpan.FromMinutes(5),
            GeneratedAt = _timeProvider.UtcNow
        });
    }

    private string GenerateAdjustmentReasoning(DdaDifficultyAdjustmentType adjustment, DdaCurrentPerformanceData performance)
    {
        return adjustment switch
        {
            DdaDifficultyAdjustmentType.Increase => $"Player performing well (Win Rate: {performance.WinRate:P1}) - increasing challenge",
            DdaDifficultyAdjustmentType.Decrease => $"Player struggling (Win Rate: {performance.WinRate:P1}) - reducing difficulty",
            _ => "Maintaining current difficulty level"
        };
    }

    private double CalculateAdjustmentConfidence(DdaCurrentPerformanceData performance)
    {
        var metrics = new[] { performance.WinRate, performance.ComboSuccess, performance.DamageEfficiency };
        var average = (float)metrics.Average();
        var variance = metrics.Sum(m => Math.Pow(m - average, 2)) / metrics.Length;
        return Math.Clamp(1.0 - variance * 2, 0.1, 0.95);
    }
}
