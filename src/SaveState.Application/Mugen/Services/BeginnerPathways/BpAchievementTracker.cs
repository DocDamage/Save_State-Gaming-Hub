using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Achievement tracker for learning milestones.
/// </summary>
public class BpAchievementTracker
{
    private readonly ILogger<BpAchievementTracker> _logger;
    private readonly ITimeProvider _timeProvider;

    public BpAchievementTracker(ILogger<BpAchievementTracker> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public List<BpAchievementData> CheckAchievements(BpUserPathProgress progress)
    {
        var achievements = new List<BpAchievementData>();
        if (progress.CompletedLessons.Count >= 1)
            achievements.Add(new BpAchievementData { Name = "First Steps", Description = "Complete your first lesson", UnlockedAt = _timeProvider.UtcNow });
        if (progress.CurrentStreak >= 7)
            achievements.Add(new BpAchievementData { Name = "Week Warrior", Description = "Maintain a 7-day streak", UnlockedAt = _timeProvider.UtcNow });
        return achievements;
    }
}
