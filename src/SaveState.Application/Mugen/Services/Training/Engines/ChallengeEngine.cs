namespace SaveState.Application.Mugen.Services.Training.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Manages training challenges and achievements.
/// </summary>
public class ChallengeEngine
{
    private readonly ILogger<ChallengeEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, Challenge> _challenges = new();
    private readonly Dictionary<string, ChallengeAttempt> _attempts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChallengeEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public ChallengeEngine(ILogger<ChallengeEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        InitializeDefaultChallenges();
    }

    /// <summary>
    /// Gets all available challenges.
    /// </summary>
    /// <returns>List of all challenges.</returns>
    public IReadOnlyList<Challenge> GetAllChallenges()
    {
        return _challenges.Values.ToList();
    }

    /// <summary>
    /// Gets challenges by difficulty level.
    /// </summary>
    /// <param name="difficulty">The difficulty level.</param>
    /// <returns>List of challenges at the specified difficulty.</returns>
    public IReadOnlyList<Challenge> GetChallengesByDifficulty(ChallengeDifficulty difficulty)
    {
        return _challenges.Values.Where(c => c.Difficulty == difficulty).ToList();
    }

    /// <summary>
    /// Gets a challenge by ID.
    /// </summary>
    /// <param name="challengeId">The challenge ID.</param>
    /// <returns>The challenge if found, null otherwise.</returns>
    public Challenge? GetChallenge(string challengeId)
    {
        return _challenges.TryGetValue(challengeId, out var challenge) ? challenge : null;
    }

    /// <summary>
    /// Starts a new challenge attempt.
    /// </summary>
    /// <param name="challengeId">The challenge ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The new challenge attempt.</returns>
    public ChallengeAttempt StartAttempt(string challengeId, string userId)
    {
        var attempt = new ChallengeAttempt
        {
            AttemptId = Guid.NewGuid().ToString(),
            ChallengeId = challengeId,
            UserId = userId,
            StartedAt = _timeProvider.UtcNow,
            Status = ChallengeAttemptStatus.InProgress,
            Events = new List<AttemptEvent>(),
            CurrentScore = 0,
            ProgressPercentage = 0
        };

        _attempts[attempt.AttemptId] = attempt;
        _logger.LogInformation("Started challenge attempt {AttemptId} for user {UserId}", attempt.AttemptId, userId);

        return attempt;
    }

    /// <summary>
    /// Gets an attempt by ID.
    /// </summary>
    /// <param name="attemptId">The attempt ID.</param>
    /// <returns>The attempt if found, null otherwise.</returns>
    public ChallengeAttempt? GetAttempt(string attemptId)
    {
        return _attempts.TryGetValue(attemptId, out var attempt) ? attempt : null;
    }

    /// <summary>
    /// Updates an attempt with progress.
    /// </summary>
    /// <param name="attemptId">The attempt ID.</param>
    /// <param name="scoreDelta">The score change.</param>
    /// <param name="progressPercentage">The progress percentage.</param>
    public void UpdateAttempt(string attemptId, int scoreDelta, int progressPercentage)
    {
        if (_attempts.TryGetValue(attemptId, out var attempt))
        {
            attempt.CurrentScore += scoreDelta;
            attempt.ProgressPercentage = Math.Min(100, Math.Max(0, progressPercentage));
            attempt.ElapsedTime = _timeProvider.UtcNow - attempt.StartedAt;

            attempt.Events = attempt.Events.ToList();
            ((List<AttemptEvent>)attempt.Events).Add(new AttemptEvent
            {
                Timestamp = _timeProvider.UtcNow,
                EventType = "Progress",
                Description = $"Score changed by {scoreDelta}",
                ScoreDelta = scoreDelta,
                Metadata = new Dictionary<string, object> { ["progress"] = progressPercentage }
            });
        }
    }

    /// <summary>
    /// Completes a challenge attempt.
    /// </summary>
    /// <param name="attemptId">The attempt ID.</param>
    /// <param name="success">Whether the attempt was successful.</param>
    /// <returns>The completed attempt.</returns>
    public ChallengeAttempt? CompleteAttempt(string attemptId, bool success)
    {
        if (_attempts.TryGetValue(attemptId, out var attempt))
        {
            attempt.Status = success ? ChallengeAttemptStatus.Completed : ChallengeAttemptStatus.Failed;
            attempt.CompletedAt = _timeProvider.UtcNow;
            attempt.ElapsedTime = attempt.CompletedAt.Value - attempt.StartedAt;

            _logger.LogInformation("Completed challenge attempt {AttemptId} with status {Status}",
                attemptId, attempt.Status);

            return attempt;
        }

        return null;
    }

    /// <summary>
    /// Abandons a challenge attempt.
    /// </summary>
    /// <param name="attemptId">The attempt ID.</param>
    /// <returns>True if the attempt was abandoned.</returns>
    public bool AbandonAttempt(string attemptId)
    {
        if (_attempts.TryGetValue(attemptId, out var attempt))
        {
            attempt.Status = ChallengeAttemptStatus.Abandoned;
            attempt.CompletedAt = _timeProvider.UtcNow;
            attempt.ElapsedTime = attempt.CompletedAt.Value - attempt.StartedAt;

            _logger.LogInformation("Abandoned challenge attempt {AttemptId}", attemptId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all attempts for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of user attempts.</returns>
    public IReadOnlyList<ChallengeAttempt> GetUserAttempts(string userId)
    {
        return _attempts.Values.Where(a => a.UserId == userId).ToList();
    }

    /// <summary>
    /// Validates if an objective has been met.
    /// </summary>
    /// <param name="objective">The objective.</param>
    /// <param name="currentValue">The current value.</param>
    /// <returns>True if the objective is met.</returns>
    public bool ValidateObjective(ChallengeObjective objective, int currentValue)
    {
        return currentValue >= objective.TargetValue;
    }

    /// <summary>
    /// Generates a random daily challenge.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A daily challenge.</returns>
    public Challenge GenerateDailyChallenge(string userId)
    {
        var random = new Random();
        var types = new[] { TrainingType.Reflex, TrainingType.PatternRecognition, TrainingType.ComboLab };
        var difficulties = new[] { ChallengeDifficulty.Easy, ChallengeDifficulty.Normal, ChallengeDifficulty.Hard };

        var type = types[random.Next(types.Length)];
        var difficulty = difficulties[random.Next(difficulties.Length)];
        var targetValue = random.Next(5, 50);

        return new Challenge
        {
            ChallengeId = $"daily_{_timeProvider.UtcNow:yyyyMMdd}_{userId}",
            Name = $"Daily {type} Challenge",
            Description = $"Complete {targetValue} successful {type} training exercises",
            Difficulty = difficulty,
            TrainingType = type,
            Objective = new ChallengeObjective
            {
                Type = "count",
                Description = $"Complete {targetValue} exercises",
                TargetValue = targetValue
            },
            Rewards = new ChallengeRewards
            {
                ExperiencePoints = (int)difficulty * 100,
                Unlockables = new List<string> { $"daily_badge_{_timeProvider.UtcNow:yyyyMMdd}" }
            },
            CreatedAt = _timeProvider.UtcNow,
            TimeLimit = TimeSpan.FromHours(24),
            MaxAttempts = 3
        };
    }

    private void InitializeDefaultChallenges()
    {
        var challenges = new[]
        {
            new Challenge
            {
                ChallengeId = "reflex_master_1",
                Name = "Reflex Master I",
                Description = "Achieve 90% accuracy in reflex training",
                Difficulty = ChallengeDifficulty.Normal,
                TrainingType = TrainingType.Reflex,
                Objective = new ChallengeObjective
                {
                    Type = "accuracy",
                    Description = "Achieve 90% accuracy",
                    TargetValue = 90
                },
                Rewards = new ChallengeRewards
                {
                    ExperiencePoints = 200,
                    Unlockables = new List<string> { "reflex_badge_bronze" }
                },
                CreatedAt = _timeProvider.UtcNow,
                MaxAttempts = 5
            },
            new Challenge
            {
                ChallengeId = "pattern_pro_1",
                Name = "Pattern Pro I",
                Description = "Complete 10 pattern sequences without mistakes",
                Difficulty = ChallengeDifficulty.Normal,
                TrainingType = TrainingType.PatternRecognition,
                Objective = new ChallengeObjective
                {
                    Type = "streak",
                    Description = "Complete 10 sequences",
                    TargetValue = 10
                },
                Rewards = new ChallengeRewards
                {
                    ExperiencePoints = 250,
                    Unlockables = new List<string> { "pattern_badge_bronze" }
                },
                CreatedAt = _timeProvider.UtcNow,
                MaxAttempts = 5
            },
            new Challenge
            {
                ChallengeId = "combo_king_1",
                Name = "Combo King I",
                Description = "Complete a 10-hit combo",
                Difficulty = ChallengeDifficulty.Hard,
                TrainingType = TrainingType.ComboLab,
                Objective = new ChallengeObjective
                {
                    Type = "combo_hits",
                    Description = "Complete 10-hit combo",
                    TargetValue = 10
                },
                Rewards = new ChallengeRewards
                {
                    ExperiencePoints = 500,
                    Unlockables = new List<string> { "combo_badge_bronze" },
                    AchievementId = "combo_king_1"
                },
                CreatedAt = _timeProvider.UtcNow,
                MaxAttempts = 10
            }
        };

        foreach (var challenge in challenges)
        {
            _challenges[challenge.ChallengeId] = challenge;
        }
    }
}
