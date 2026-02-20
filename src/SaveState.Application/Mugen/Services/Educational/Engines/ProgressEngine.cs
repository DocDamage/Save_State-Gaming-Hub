namespace SaveState.Application.Mugen.Services.Educational.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.Educational;

public class ProgressEngine
{
    private readonly ILogger<ProgressEngine> _logger;
    private readonly Dictionary<string, UserProgress> _userProgress;

    public ProgressEngine(ILogger<ProgressEngine> logger)
    {
        _logger = logger;
        _userProgress = new Dictionary<string, UserProgress>();
    }

    /// <summary>
    /// Gets the learning progress data for a specific user.
    /// </summary>
    public Task<LearningProgress> GetUserProgressAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting user progress for {UserId}", userId);

        if (!_userProgress.TryGetValue(userId, out var userProgress))
        {
            userProgress = new UserProgress
            {
                UserId = userId,
                TutorialsCompleted = 0,
                TutorialsInProgress = 0,
                TotalTimeSpent = TimeSpan.Zero,
                SkillsMastered = new List<string>(),
                CurrentStreak = 0,
                LongestStreak = 0,
                AverageScore = 0,
                WeakAreas = new List<string>(),
                RecommendedNext = string.Empty,
                CategoryProgress = new List<CategoryProgress>()
            };
            _userProgress[userId] = userProgress;
        }

        // Map UserProgress to LearningProgress
        var learningProgress = new LearningProgress
        {
            UserId = userProgress.UserId,
            TutorialsCompleted = userProgress.TutorialsCompleted,
            TutorialsInProgress = userProgress.TutorialsInProgress,
            TotalTimeSpent = userProgress.TotalTimeSpent,
            SkillsMastered = userProgress.SkillsMastered,
            CurrentStreak = userProgress.CurrentStreak,
            LongestStreak = userProgress.LongestStreak,
            AverageScore = userProgress.AverageScore,
            WeakAreas = userProgress.WeakAreas,
            RecommendedNext = userProgress.RecommendedNext
        };

        return Task.FromResult(learningProgress);
    }

    /// <summary>
    /// Calculates progress percentage for a specific category.
    /// </summary>
    public Task<decimal> CalculateCategoryProgressAsync(string userId, string category)
    {
        _logger.LogDebug("Calculating category progress for user {UserId}, category {Category}", userId, category);

        var progress = _userProgress.TryGetValue(userId, out var userProgress)
            ? userProgress
            : new UserProgress
            {
                UserId = userId,
                TutorialsCompleted = 0,
                TutorialsInProgress = 0,
                TotalTimeSpent = TimeSpan.Zero,
                SkillsMastered = new List<string>(),
                CurrentStreak = 0,
                LongestStreak = 0,
                AverageScore = 0,
                WeakAreas = new List<string>(),
                RecommendedNext = string.Empty,
                CategoryProgress = new List<CategoryProgress>()
            };

        var categoryProgress = progress.CategoryProgress?.FirstOrDefault(cp =>
            cp.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        // Return completion percentage as decimal (0-100)
        var completionPercentage = categoryProgress?.CompletionPercentage ?? 0;
        return Task.FromResult((decimal)completionPercentage);
    }

    /// <summary>
    /// Updates the progress for a user.
    /// </summary>
    public Task UpdateProgressAsync(string userId, UserProgress progress, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating progress for user {UserId}", userId);
        _userProgress[userId] = progress;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Records tutorial completion for a user.
    /// </summary>
    public async Task RecordTutorialCompletionAsync(string userId, string tutorialId, CancellationToken ct = default)
    {
        _logger.LogInformation("Recording tutorial completion for user {UserId}, tutorial {TutorialId}", userId, tutorialId);

        // Get or create UserProgress directly
        if (!_userProgress.TryGetValue(userId, out var userProgress))
        {
            userProgress = new UserProgress
            {
                UserId = userId,
                TutorialsCompleted = 0,
                TutorialsInProgress = 0,
                TotalTimeSpent = TimeSpan.Zero,
                SkillsMastered = new List<string>(),
                CurrentStreak = 0,
                LongestStreak = 0,
                AverageScore = 0,
                WeakAreas = new List<string>(),
                RecommendedNext = string.Empty,
                CategoryProgress = new List<CategoryProgress>()
            };
        }

        userProgress.TutorialsCompleted++;
        _userProgress[userId] = userProgress;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the total number of users with progress tracked.
    /// </summary>
    public int GetTrackedUserCount()
    {
        return _userProgress.Count;
    }
}
