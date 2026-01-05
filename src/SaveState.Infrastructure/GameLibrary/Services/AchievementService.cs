using Microsoft.Extensions.Logging;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

public class AchievementService : IAchievementService
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly ILogger<AchievementService> _logger;

    public AchievementService(IAchievementRepository achievementRepository, ILogger<AchievementService> logger)
    {
        _achievementRepository = achievementRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Achievement>> CheckForUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        // TODO: Implement achievement unlocking logic based on rules/criteria
        return new List<Achievement>();
    }

    public async Task<IReadOnlyList<UserAchievement>> UpdateProgressAsync(Guid userId, AchievementType achievementType, int progressIncrement, string? metadata = null, CancellationToken ct = default)
    {
        var achievements = await _achievementRepository.GetAchievementsByTypeAsync(achievementType, ct);
        var updatedAchievements = new List<UserAchievement>();

        foreach (var achievement in achievements)
        {
            var userAchievement = await _achievementRepository.GetUserAchievementAsync(userId, achievement.Id, ct);

            if (userAchievement != null && userAchievement.IsUnlocked) continue;

            if (userAchievement == null)
            {
                userAchievement = new UserAchievement(userId, achievement.Id, achievement.TargetValue);
            }

            userAchievement.AddProgress(progressIncrement, metadata);
            await _achievementRepository.AddOrUpdateUserAchievementAsync(userAchievement, ct);
            updatedAchievements.Add(userAchievement);
        }

        return updatedAchievements;
    }

    public async Task<bool> AwardAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default)
    {
        var achievement = await _achievementRepository.GetAchievementByIdAsync(achievementId, ct);
        if (achievement == null) return false;

        var userAchievement = await _achievementRepository.GetUserAchievementAsync(userId, achievementId, ct);
        if (userAchievement != null && userAchievement.IsUnlocked) return false;

        if (userAchievement == null)
        {
            userAchievement = new UserAchievement(userId, achievementId, achievement.TargetValue);
        }

        userAchievement.Unlock();
        await _achievementRepository.AddOrUpdateUserAchievementAsync(userAchievement, ct);
        return true;
    }

    public async Task<IReadOnlyList<Achievement>> GetUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
        return userAchievements.Where(ua => ua.IsUnlocked && ua.Achievement != null).Select(ua => ua.Achievement!).ToList();
    }

    public async Task<IReadOnlyList<UserAchievement>> GetUserProgressAsync(Guid userId, CancellationToken ct = default)
    {
        return await _achievementRepository.GetUserAchievementsAsync(userId, ct);
    }

    public async Task ResetUserProgressAsync(Guid userId, CancellationToken ct = default)
    {
        // Not implemented in repository yet (Reset/Delete), so skipping for now or would loop delete
        _logger.LogWarning("ResetUserProgressAsync not implemented");
        await Task.CompletedTask;
    }
}
