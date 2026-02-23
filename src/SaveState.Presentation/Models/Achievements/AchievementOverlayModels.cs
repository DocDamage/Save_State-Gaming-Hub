using System;
using System.Collections.Generic;

namespace SaveState.Presentation.Models.Achievements;

public enum AchievementRarity
{
    Common,     // > 50% unlocked
    Uncommon,   // 25-50%
    Rare,       // 10-25%
    Epic,       // 5-10%
    Legendary   // < 5%
}

public record AchievementNotification
{
    public int AchievementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? BadgeUrl { get; set; }
    public int Points { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string? GameIcon { get; set; }
    public DateTime UnlockedAt { get; set; }
    public bool IsHardcore { get; set; }
    public AchievementRarity Rarity { get; set; }
}

public record AchievementProgress
{
    public int TotalAchievements { get; set; }
    public int UnlockedAchievements { get; set; }
    public int TotalPoints { get; set; }
    public int CurrentPoints { get; set; }
    public double CompletionPercentage => TotalAchievements > 0 ? (double)UnlockedAchievements / TotalAchievements * 100 : 0;
    public List<string> RecentUnlocks { get; set; } = new();
}

public record AchievementMilestone
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ReachedAt { get; set; }
}
