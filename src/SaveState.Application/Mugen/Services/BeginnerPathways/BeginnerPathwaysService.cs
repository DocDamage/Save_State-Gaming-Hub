using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ValueObjects;
using Microsoft.Extensions.Logging;
using SkillLevel = SaveState.Application.Mugen.Models.Educational.SkillLevel;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Beginner pathways service providing structured learning progression,
/// personalized learning paths, and adaptive difficulty scaling for new players.
/// </summary>
public class BeginnerPathwaysService : IBeginnerPathwaysService
{
    private readonly ILogger<BeginnerPathwaysService> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly BpPathGenerator _pathGenerator;
    private readonly BpProgressEvaluator _progressEvaluator;
    private readonly BpAdaptiveDifficulty _adaptiveDifficulty;
    private readonly BpAchievementTracker _achievementTracker;
    private readonly Dictionary<string, BpPathwayLearningPath> _learningPaths = new();
    private readonly Dictionary<string, BpUserPathProgress> _userProgress = new();

    public BeginnerPathwaysService(
        ILogger<BeginnerPathwaysService> logger,
        ICacheService cache,
        ITimeProvider timeProvider,
        BpPathGenerator pathGenerator,
        BpProgressEvaluator progressEvaluator,
        BpAdaptiveDifficulty adaptiveDifficulty,
        BpAchievementTracker achievementTracker)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _pathGenerator = pathGenerator;
        _progressEvaluator = progressEvaluator;
        _adaptiveDifficulty = adaptiveDifficulty;
        _achievementTracker = achievementTracker;
    }

    public async Task<Result<BpPathwayLearningPath>> CreatePersonalizedPathAsync(string userId, BpUserAssessment assessment, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating personalized learning path for user {UserId}", userId);
            var path = await _pathGenerator.GeneratePersonalizedPathAsync(userId, assessment, ct);
            _learningPaths[path.PathId] = path;
            var progress = new BpUserPathProgress
            {
                UserId = userId,
                PathId = path.PathId,
                StartedAt = _timeProvider.UtcNow,
                CurrentModule = 0,
                CurrentLesson = 0,
                CompletedLessons = new List<string>(),
                CompletedModules = new List<string>(),
                TotalTimeSpent = TimeSpan.Zero,
                AverageScore = 0.0,
                CurrentStreak = 0,
                LongestStreak = 0,
                SkillProgression = new Dictionary<string, double>(),
                AdaptiveDifficulty = 1.0,
                LastActivity = _timeProvider.UtcNow,
                Status = BpPathStatus.Active
            };
            _userProgress[$"{userId}_{path.PathId}"] = progress;
            _logger.LogInformation("Personalized learning path created: {PathId}", path.PathId);
            return Result.Success(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating personalized path for {UserId}", userId);
            return Result.Failure<BpPathwayLearningPath>($"Failed to create path: {ex.Message}");
        }
    }

    public async Task<Result<BpPathwayLearningPath>> GetPathAsync(string pathId, CancellationToken ct = default)
    {
        try
        {
            if (_learningPaths.TryGetValue(pathId, out var path))
                return Result.Success(path);
            return Result.Failure<BpPathwayLearningPath>("Path not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving path {PathId}", pathId);
            return Result.Failure<BpPathwayLearningPath>($"Failed to retrieve path: {ex.Message}");
        }
    }

    public async Task<Result<BpUserPathProgress>> GetUserProgressAsync(string userId, string pathId, CancellationToken ct = default)
    {
        try
        {
            var key = $"{userId}_{pathId}";
            if (_userProgress.TryGetValue(key, out var progress))
                return Result.Success(progress);
            return Result.Failure<BpUserPathProgress>("Progress not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving progress for {UserId}", userId);
            return Result.Failure<BpUserPathProgress>($"Failed to retrieve progress: {ex.Message}");
        }
    }

    public async Task<Result<BpUserPathProgress>> UpdateProgressAsync(string userId, string pathId, BpBeginnerLessonProgressUpdate update, CancellationToken ct = default)
    {
        try
        {
            var key = $"{userId}_{pathId}";
            if (!_userProgress.TryGetValue(key, out var progress))
                return Result.Failure<BpUserPathProgress>("Progress not found");
            progress.CompletedLessons.Add(update.LessonId);
            progress.AverageScore = (progress.AverageScore * (progress.CompletedLessons.Count - 1) + update.Score) / progress.CompletedLessons.Count;
            progress.LastActivity = _timeProvider.UtcNow;
            if (update.Score >= 70) progress.CurrentStreak++;
            else progress.CurrentStreak = 0;
            if (progress.CurrentStreak > progress.LongestStreak) progress.LongestStreak = progress.CurrentStreak;
            _logger.LogInformation("Progress updated for {UserId}: Lesson {LessonId}, Score {Score}", userId, update.LessonId, update.Score);
            return Result.Success(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating progress for {UserId}", userId);
            return Result.Failure<BpUserPathProgress>($"Failed to update progress: {ex.Message}");
        }
    }

    public async Task<Result<BpAdaptiveAdjustment>> GetAdaptiveAdjustmentAsync(string userId, string pathId, CancellationToken ct = default)
    {
        try
        {
            var key = $"{userId}_{pathId}";
            if (!_userProgress.TryGetValue(key, out var progress))
                return Result.Failure<BpAdaptiveAdjustment>("Progress not found");
            var adjustment = await _adaptiveDifficulty.CalculateAdjustmentAsync(progress, ct);
            return Result.Success(adjustment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating adjustment for {UserId}", userId);
            return Result.Failure<BpAdaptiveAdjustment>($"Failed to calculate adjustment: {ex.Message}");
        }
    }

    public async Task<Result<List<BpAchievementData>>> GetAchievementsAsync(string userId, string pathId, CancellationToken ct = default)
    {
        try
        {
            var key = $"{userId}_{pathId}";
            if (!_userProgress.TryGetValue(key, out var progress))
                return Result.Failure<List<BpAchievementData>>("Progress not found");
            var achievements = _achievementTracker.CheckAchievements(progress);
            return Result.Success(achievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving achievements for {UserId}", userId);
            return Result.Failure<List<BpAchievementData>>($"Failed to retrieve achievements: {ex.Message}");
        }
    }

    public async Task<Result<double>> GetProgressPercentageAsync(string userId, string pathId, CancellationToken ct = default)
    {
        try
        {
            var key = $"{userId}_{pathId}";
            if (!_userProgress.TryGetValue(key, out var progress))
                return Result.Failure<double>("Progress not found");
            var percentage = _progressEvaluator.EvaluateProgress(progress);
            return Result.Success(percentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating progress percentage for {UserId}", userId);
            return Result.Failure<double>($"Failed to calculate progress: {ex.Message}");
        }
    }
}

/// <summary>
/// Beginner Pathways Service interface.
/// </summary>
public interface IBeginnerPathwaysService
{
    Task<Result<BpPathwayLearningPath>> CreatePersonalizedPathAsync(string userId, BpUserAssessment assessment, CancellationToken ct = default);
    Task<Result<BpPathwayLearningPath>> GetPathAsync(string pathId, CancellationToken ct = default);
    Task<Result<BpUserPathProgress>> GetUserProgressAsync(string userId, string pathId, CancellationToken ct = default);
    Task<Result<BpUserPathProgress>> UpdateProgressAsync(string userId, string pathId, BpBeginnerLessonProgressUpdate update, CancellationToken ct = default);
    Task<Result<BpAdaptiveAdjustment>> GetAdaptiveAdjustmentAsync(string userId, string pathId, CancellationToken ct = default);
    Task<Result<List<BpAchievementData>>> GetAchievementsAsync(string userId, string pathId, CancellationToken ct = default);
    Task<Result<double>> GetProgressPercentageAsync(string userId, string pathId, CancellationToken ct = default);
}

// Types

public class BpPathwayLearningPath
{
    public string PathId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public IReadOnlyList<string> TargetSkills { get; set; } = default!;
    public IReadOnlyList<string> Prerequisites { get; set; } = default!;
    public List<BpPathwayLearningModule> Modules { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public int TotalEnrollments { get; set; }
    public double AverageRating { get; set; }
    public double SuccessRate { get; set; }
}

public class BpPathwayLearningModule
{
    public string ModuleId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int Order { get; set; }
    public List<BpLearningLesson> Lessons { get; set; } = default!;
    public TimeSpan EstimatedDuration { get; set; }
    public IReadOnlyList<string> SkillsCovered { get; set; } = default!;
    public IReadOnlyList<string> Prerequisites { get; set; } = default!;
}

public class BpLearningLesson
{
    public string LessonId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int Order { get; set; }
    public List<BpLessonStep> Steps { get; set; } = default!;
    public TimeSpan EstimatedDuration { get; set; }
    public bool IsInteractive { get; set; }
    public BpInteractiveElement? InteractiveElement { get; set; }
}

public class BpLessonStep
{
    public string StepId { get; set; } = default!;
    public string Content { get; set; } = default!;
    public int Order { get; set; }
    public BpLessonAction? RequiredAction { get; set; }
}

public class BpInteractiveElement
{
    public string ElementId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Content { get; set; } = default!;
    public Dictionary<string, object> Properties { get; set; } = default!;
}

public class BpUserAssessment
{
    public string UserId { get; set; } = default!;
    public SkillLevel SkillLevel { get; set; }
    public IReadOnlyList<string> Strengths { get; set; } = default!;
    public IReadOnlyList<string> Weaknesses { get; set; } = default!;
    public BpTimeCommitment TimeCommitment { get; set; }
    public IReadOnlyList<string> LearningGoals { get; set; } = default!;
    public IReadOnlyList<string> PreferredCharacters { get; set; } = default!;
}

public class BpUserPathProgress
{
    public string UserId { get; set; } = default!;
    public string PathId { get; set; } = default!;
    public DateTime StartedAt { get; set; }
    public int CurrentModule { get; set; }
    public int CurrentLesson { get; set; }
    public List<string> CompletedLessons { get; set; } = default!;
    public List<string> CompletedModules { get; set; } = default!;
    public TimeSpan TotalTimeSpent { get; set; }
    public double AverageScore { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public Dictionary<string, double> SkillProgression { get; set; } = default!;
    public double AdaptiveDifficulty { get; set; }
    public DateTime LastActivity { get; set; }
    public BpPathStatus Status { get; set; }
    public int TotalLessons => 10;
}

public class BpLessonProgress
{
    public string LessonId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public bool IsCompleted { get; set; }
    public double Score { get; set; }
    public TimeSpan TimeSpent { get; set; }
    public List<BpLessonResponse> Responses { get; set; } = default!;
    public DateTime CompletedAt { get; set; }
}

public class BpLessonAction
{
    public string ActionId { get; set; } = default!;
    public string ActionType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Dictionary<string, object> Parameters { get; set; } = default!;
}

public class BpLessonResponse
{
    public string ResponseId { get; set; } = default!;
    public string StepId { get; set; } = default!;
    public string Response { get; set; } = default!;
    public bool IsCorrect { get; set; }
    public DateTime RespondedAt { get; set; }
}

public class BpBeginnerLessonProgressUpdate
{
    public string LessonId { get; set; } = default!;
    public double Score { get; set; }
    public TimeSpan TimeSpent { get; set; }
    public bool IsCompleted { get; set; }
}

public class BpAchievementData
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime UnlockedAt { get; set; }
}

public class BpMilestoneCheck
{
    public string MilestoneId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsAchieved { get; set; }
    public double Progress { get; set; }
    public DateTime? AchievedAt { get; set; }
    public List<BpMilestoneReward> Rewards { get; set; } = default!;
}

public class BpPathMilestone
{
    public string MilestoneId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int RequiredLessons { get; set; }
    public List<BpMilestoneReward> Rewards { get; set; } = default!;
}

public class BpMilestoneReward
{
    public string RewardType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int Value { get; set; }
}

public class BpAdaptiveAdjustment
{
    public BpBeginnerAdjustmentType AdjustmentType { get; set; }
    public double DifficultyMultiplier { get; set; }
    public string Reasoning { get; set; } = default!;
    public IReadOnlyList<string> SuggestedActions { get; set; } = default!;
    public DateTime NextReviewDate { get; set; }
}

public class BpPathAnalytics
{
    public string PathId { get; set; } = default!;
    public int TotalEnrollments { get; set; }
    public int ActiveUsers { get; set; }
    public int CompletedUsers { get; set; }
    public double AverageCompletionTime { get; set; }
    public double AverageRating { get; set; }
    public double DropoutRate { get; set; }
}

public enum BpPathStatus { Active, Paused, Completed, Abandoned }
public enum BpTimeCommitment { Light, Moderate, Heavy }
public enum BpBeginnerAdjustmentType { Increase, Decrease, Maintain }
