using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Adaptive difficulty system for personalized learning.
/// </summary>
public class BpAdaptiveDifficulty
{
    private readonly ILogger<BpAdaptiveDifficulty> _logger;
    private readonly ITimeProvider _timeProvider;

    public BpAdaptiveDifficulty(ILogger<BpAdaptiveDifficulty> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<BpAdaptiveAdjustment> CalculateAdjustmentAsync(BpUserPathProgress progress, CancellationToken ct)
    {
        return Task.FromResult(new BpAdaptiveAdjustment
        {
            AdjustmentType = progress.CurrentStreak > 3 ? BpBeginnerAdjustmentType.Increase : BpBeginnerAdjustmentType.Maintain,
            DifficultyMultiplier = progress.AverageScore > 80 ? 1.2 : 1.0,
            Reasoning = "Based on recent performance and learning streak",
            SuggestedActions = new[] { "Try more challenging exercises", "Review weak areas" },
            NextReviewDate = _timeProvider.UtcNow.AddDays(7)
        });
    }
}
