namespace SaveState.Application.GameLibrary.DTOs;

using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Data transfer object for user achievement progress.
/// </summary>
public record UserAchievementDto(
    Guid Id,
    Guid UserId,
    Guid AchievementId,
    string AchievementName,
    string AchievementDescription,
    string AchievementIconPath,
    int AchievementPoints,
    AchievementType AchievementType,
    int CurrentProgress,
    int TargetProgress,
    bool IsUnlocked,
    DateTime? UnlockedAt,
    DateTime LastUpdatedAt
);
