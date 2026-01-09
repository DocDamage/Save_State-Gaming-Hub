using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Social.Entities;
using SaveState.Core.Social.Repositories;
using SaveState.Core.Social.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Social;

/// <summary>
/// Implementation of challenge progress tracking service.
/// </summary>
public class ChallengeProgressService : IChallengeProgressService
{
    private readonly SaveStateDbContext _context;
    private readonly ICommunityRepository _communityRepository;
    private readonly ILogger<ChallengeProgressService> _logger;

    public ChallengeProgressService(
        SaveStateDbContext context,
        ICommunityRepository communityRepository,
        ILogger<ChallengeProgressService> logger)
    {
        _context = context;
        _communityRepository = communityRepository;
        _logger = logger;
    }

    public async Task<Result> UpdateProgressOnGameSessionAsync(
        Guid userId,
        Guid gameId,
        TimeSpan sessionDuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeChallenges = await GetUserActiveChallengesAsync(userId, cancellationToken);

            foreach (var challenge in activeChallenges)
            {
                // Check if this challenge tracks playtime
                if (challenge.Type == ChallengeType.Playtime)
                {
                    var participant = challenge.Participants.FirstOrDefault(p => p.UserId == userId);
                    var requirement = challenge.Requirements.FirstOrDefault();
                    if (participant != null && requirement != null)
                    {
                        // Update progress (convert TimeSpan to hours)
                        participant.CurrentProgress += sessionDuration.TotalHours;
                        participant.LastUpdatedAt = DateTime.UtcNow;

                        // Check if challenge is completed
                        if (participant.CurrentProgress >= requirement.TargetValue && !participant.IsCompleted)
                        {
                            participant.IsCompleted = true;
                            participant.CompletedAt = DateTime.UtcNow;
                            _logger.LogInformation("User {UserId} completed challenge {ChallengeId}", userId, challenge.Id);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update challenge progress for game session");
            return Result.Failure($"Failed to update progress: {ex.Message}");
        }
    }

    public async Task<Result> UpdateProgressOnAchievementAsync(
        Guid userId,
        Guid gameId,
        string achievementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeChallenges = await GetUserActiveChallengesAsync(userId, cancellationToken);

            foreach (var challenge in activeChallenges)
            {
                // Check if this challenge tracks achievements
                if (challenge.Type == ChallengeType.Achievement)
                {
                    var participant = challenge.Participants.FirstOrDefault(p => p.UserId == userId);
                    var requirement = challenge.Requirements.FirstOrDefault();
                    if (participant != null && requirement != null)
                    {
                        // Increment achievement count
                        participant.CurrentProgress += 1;
                        participant.LastUpdatedAt = DateTime.UtcNow;

                        // Check if challenge is completed
                        if (participant.CurrentProgress >= requirement.TargetValue && !participant.IsCompleted)
                        {
                            participant.IsCompleted = true;
                            participant.CompletedAt = DateTime.UtcNow;
                            _logger.LogInformation("User {UserId} completed achievement challenge {ChallengeId}", userId, challenge.Id);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update challenge progress for achievement");
            return Result.Failure($"Failed to update progress: {ex.Message}");
        }
    }

    public async Task<Result> UpdateProgressOnStatsChangeAsync(
        Guid userId,
        Guid gameId,
        Dictionary<string, object> stats,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeChallenges = await GetUserActiveChallengesAsync(userId, cancellationToken);

            foreach (var challenge in activeChallenges)
            {
                // Check if this challenge tracks specific stats
                if (challenge.Type == ChallengeType.HighScore || challenge.Type == ChallengeType.Speedrun)
                {
                    var participant = challenge.Participants.FirstOrDefault(p => p.UserId == userId);
                    var requirement = challenge.Requirements.FirstOrDefault();
                    if (participant != null && requirement != null && !string.IsNullOrEmpty(requirement.TargetMetric))
                    {
                        // Try to get the relevant stat
                        if (stats.TryGetValue(requirement.TargetMetric, out var statValue))
                        {
                            var numericValue = Convert.ToDouble(statValue);

                            // For high score challenges, take the maximum
                            if (challenge.Type == ChallengeType.HighScore)
                            {
                                if (numericValue > participant.CurrentProgress)
                                {
                                    participant.CurrentProgress = numericValue;
                                    participant.LastUpdatedAt = DateTime.UtcNow;
                                }
                            }
                            // For speedrun challenges, take the minimum (faster time)
                            else if (challenge.Type == ChallengeType.Speedrun)
                            {
                                if (participant.CurrentProgress == 0 || numericValue < participant.CurrentProgress)
                                {
                                    participant.CurrentProgress = numericValue;
                                    participant.LastUpdatedAt = DateTime.UtcNow;
                                }
                            }

                            // Check if challenge is completed
                            if (participant.CurrentProgress >= requirement.TargetValue && !participant.IsCompleted)
                            {
                                participant.IsCompleted = true;
                                participant.CompletedAt = DateTime.UtcNow;
                                _logger.LogInformation("User {UserId} completed stats challenge {ChallengeId}", userId, challenge.Id);
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update challenge progress for stats change");
            return Result.Failure($"Failed to update progress: {ex.Message}");
        }
    }

    public async Task<Result<List<ChallengeProgress>>> GetUserChallengeProgressAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeChallenges = await GetUserActiveChallengesAsync(userId, cancellationToken);

            var progressList = activeChallenges
                .SelectMany(c => c.Participants
                    .Where(p => p.UserId == userId)
                    .Select(p => new ChallengeProgress
                    {
                        ChallengeId = c.Id,
                        ChallengeName = c.Name,
                        UserId = userId,
                        CurrentProgress = p.CurrentProgress,
                        TargetProgress = c.Requirements.FirstOrDefault()?.TargetValue ?? 0,
                        IsCompleted = p.IsCompleted,
                        CompletedAt = p.CompletedAt,
                        LastUpdatedAt = p.LastUpdatedAt
                    }))
                .ToList();

            return Result.Success(progressList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user challenge progress");
            return Result.Failure<List<ChallengeProgress>>($"Failed to get progress: {ex.Message}");
        }
    }

    public async Task<Result> RecalculateUserProgressAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting challenge progress recalculation for user {UserId}", userId);

            // Step 1: Query GameSessions for playtime/session count
            // Note: GameSession doesn't have UserId directly, so we need to join through Game
            var allSessions = await _context.GameSessions
                .Where(s => s.EndedAt != null)
                .ToListAsync(cancellationToken);

            var sessionsByGame = allSessions
                .GroupBy(s => s.GameId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        TotalPlaytime = TimeSpan.FromTicks(g.Sum(s => (s.EndedAt!.Value - s.StartedAt).Ticks)),
                        SessionCount = g.Count()
                    });

            _logger.LogInformation("Aggregated {SessionCount} game sessions", allSessions.Count);

            // Step 2: Query UserAchievements for unlock counts
            var userAchievements = await _context.UserAchievements
                .Where(ua => ua.UserId == userId && ua.IsUnlocked)
                .ToListAsync(cancellationToken);

            var totalAchievementsUnlocked = userAchievements.Count;
            var totalAchievementPoints = userAchievements.Sum(ua => ua.CurrentProgress);

            _logger.LogInformation("Aggregated {AchievementCount} unlocked achievements for user {UserId}", totalAchievementsUnlocked, userId);

            // Step 3: Calculate aggregate statistics from sessions
            var totalPlaytimeHours = sessionsByGame.Values.Sum(s => s.TotalPlaytime.TotalHours);
            var totalSessions = sessionsByGame.Values.Sum(s => s.SessionCount);

            _logger.LogInformation("Total playtime: {Hours} hours across {Sessions} sessions", totalPlaytimeHours, totalSessions);

            // Step 4: For each active challenge, recalculate progress from aggregated data
            var activeChallenges = await GetUserActiveChallengesAsync(userId, cancellationToken);
            var recalculatedCount = 0;
            var newlyCompletedCount = 0;

            foreach (var challenge in activeChallenges)
            {
                var updateResult = await RecalculateChallengeProgressAsync(
                    challenge,
                    userId,
                    totalPlaytimeHours,
                    totalAchievementsUnlocked,
                    totalSessions,
                    allSessions,
                    cancellationToken);

                if (updateResult.WasUpdated)
                {
                    recalculatedCount++;
                }

                if (updateResult.NewlyCompleted)
                {
                    newlyCompletedCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Completed challenge progress recalculation for user {UserId}. Updated {RecalculatedCount} challenges, {NewlyCompletedCount} newly completed",
                userId, recalculatedCount, newlyCompletedCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recalculate user progress for user {UserId}", userId);
            return Result.Failure($"Failed to recalculate: {ex.Message}");
        }
    }

    private async Task<(bool WasUpdated, bool NewlyCompleted)> RecalculateChallengeProgressAsync(
        Challenge challenge,
        Guid userId,
        double totalPlaytimeHours,
        int totalAchievementsUnlocked,
        int totalSessions,
        List<GameSession> allSessions,
        CancellationToken cancellationToken)
    {
        var participant = challenge.Participants.FirstOrDefault(p => p.UserId == userId);
        var requirement = challenge.Requirements.FirstOrDefault();

        if (participant == null || requirement == null)
            return (false, false);

        var previousProgress = participant.CurrentProgress;
        var wasCompleted = participant.IsCompleted;

        // Step 5: Update ChallengeProgress entities with corrected values
        participant.CurrentProgress = challenge.Type switch
        {
            ChallengeType.Playtime => totalPlaytimeHours,
            ChallengeType.Achievement => totalAchievementsUnlocked,
            ChallengeType.Daily or ChallengeType.Weekly or ChallengeType.Monthly =>
                allSessions.Count(s => s.StartedAt >= challenge.StartDate && s.StartedAt <= challenge.EndDate),
            _ => participant.CurrentProgress // Keep existing for unsupported types
        };

        // Step 6: Detect and mark newly completed challenges
        var newlyCompleted = false;
        if (participant.CurrentProgress >= requirement.TargetValue && !participant.IsCompleted)
        {
            participant.IsCompleted = true;
            participant.CompletedAt = DateTime.UtcNow;
            newlyCompleted = true;
            _logger.LogInformation(
                "User {UserId} completed challenge {ChallengeId} '{ChallengeName}' during recalculation",
                userId, challenge.Id, challenge.Name);
        }

        participant.LastUpdatedAt = DateTime.UtcNow;

        var wasUpdated = Math.Abs(previousProgress - participant.CurrentProgress) > 0.001 || wasCompleted != participant.IsCompleted;

        if (wasUpdated)
        {
            _logger.LogDebug(
                "Recalculated challenge {ChallengeId}: Progress {OldProgress} -> {NewProgress}, Completed: {WasCompleted} -> {IsCompleted}",
                challenge.Id, previousProgress, participant.CurrentProgress, wasCompleted, participant.IsCompleted);
        }

        return (wasUpdated, newlyCompleted);
    }

    private async Task<List<Challenge>> GetUserActiveChallengesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return await _context.Challenges
            .Include(c => c.Participants)
            .Include(c => c.Requirements)
            .Where(c => !c.IsDeleted
                && c.StartDate <= now
                && c.EndDate >= now
                && c.Participants.Any(p => p.UserId == userId))
            .ToListAsync(cancellationToken);
    }
}

