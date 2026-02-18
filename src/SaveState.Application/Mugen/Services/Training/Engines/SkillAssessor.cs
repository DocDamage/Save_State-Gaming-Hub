namespace SaveState.Application.Mugen.Services.Training.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ValueObjects;
using Priority = SaveState.Application.Mugen.Services.Training.Priority;

/// <summary>
/// Assesses player skills and generates recommendations.
/// </summary>
public class SkillAssessor
{
    private readonly ILogger<SkillAssessor> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillAssessor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public SkillAssessor(ILogger<SkillAssessor> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Calculates training statistics for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="period">The time period.</param>
    /// <param name="sessions">The user's training sessions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The training statistics.</returns>
    public Task<TrainingStatistics> CalculateStatisticsAsync(
        string userId,
        TimeSpan period,
        IReadOnlyList<TrainingSession> sessions,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Calculating statistics for user {UserId} over period {Period}", userId, period);

        var cutoffDate = _timeProvider.UtcNow - period;
        var relevantSessions = sessions.Where(s => s.StartedAt >= cutoffDate).ToList();

        var reflexSessions = relevantSessions.Where(s => s.TrainingType == TrainingType.Reflex).ToList();
        var patternSessions = relevantSessions.Where(s => s.TrainingType == TrainingType.PatternRecognition).ToList();
        var comboSessions = relevantSessions.Where(s => s.TrainingType == TrainingType.ComboLab).ToList();

        var totalTrainingTime = CalculateTotalTrainingTime(relevantSessions);
        var averageSessionLength = relevantSessions.Count > 0
            ? TimeSpan.FromMilliseconds(totalTrainingTime.TotalMilliseconds / relevantSessions.Count)
            : TimeSpan.Zero;

        var overallAccuracy = CalculateOverallAccuracy(relevantSessions);
        var (bestReactionTime, averageReactionTime) = CalculateReactionTimes(reflexSessions);
        var skillsImproved = IdentifySkillsImproved(relevantSessions);
        var consistency = CalculateConsistency(relevantSessions, period);

        var stats = new TrainingStatistics
        {
            UserId = userId,
            Period = period,
            TotalSessions = relevantSessions.Count,
            TotalTrainingTime = totalTrainingTime,
            AverageSessionLength = averageSessionLength,
            ReflexTrainingSessions = reflexSessions.Count,
            PatternTrainingSessions = patternSessions.Count,
            ComboLabSessions = comboSessions.Count,
            OverallAccuracy = overallAccuracy,
            BestReactionTime = bestReactionTime,
            AverageReactionTime = averageReactionTime,
            SkillsImproved = skillsImproved,
            TrainingConsistency = consistency,
            GeneratedAt = _timeProvider.UtcNow
        };

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Generates training recommendations for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="statistics">The user's training statistics.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The training recommendations.</returns>
    public Task<TrainingRecommendations> GenerateRecommendationsAsync(
        string userId,
        TrainingStatistics? statistics,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating recommendations for user {UserId}", userId);

        var recommendations = new List<RecommendedTraining>();
        var skillGaps = new List<string>();
        var nextMilestones = new List<string>();
        var personalizedTips = new List<string>();

        if (statistics == null || statistics.TotalSessions == 0)
        {
            recommendations.Add(CreateRecommendation(
                TrainingType.Reflex,
                "Start with reflex training to build fundamental reaction skills",
                DifficultyLevel.Easy,
                Priority.High));

            skillGaps.Add("Reaction time");
            skillGaps.Add("Pattern recognition");
            nextMilestones.Add("Complete first reflex training session");
            personalizedTips.Add("Start with easy difficulty and gradually increase");
        }
        else
        {
            AnalyzeForRecommendations(statistics, recommendations, skillGaps, nextMilestones, personalizedTips);
        }

        var result = new TrainingRecommendations
        {
            UserId = userId,
            RecommendedTrainings = recommendations,
            SkillGaps = skillGaps,
            NextMilestones = nextMilestones,
            PersonalizedTips = personalizedTips,
            GeneratedAt = _timeProvider.UtcNow
        };

        return Task.FromResult(result);
    }

    private static void AnalyzeForRecommendations(
        TrainingStatistics stats,
        List<RecommendedTraining> recommendations,
        List<string> skillGaps,
        List<string> nextMilestones,
        List<string> personalizedTips)
    {
        if (stats.ReflexTrainingSessions < 5)
        {
            recommendations.Add(CreateRecommendation(
                TrainingType.Reflex,
                "Improve reaction time fundamentals",
                DifficultyLevel.Medium,
                Priority.High));
            skillGaps.Add("Reaction time");
        }

        if (stats.PatternTrainingSessions < 5)
        {
            recommendations.Add(CreateRecommendation(
                TrainingType.PatternRecognition,
                "Practice pattern recognition for better execution",
                DifficultyLevel.Medium,
                Priority.High));
            skillGaps.Add("Pattern recognition");
        }

        if (stats.ComboLabSessions < 5)
        {
            recommendations.Add(CreateRecommendation(
                TrainingType.ComboLab,
                "Build combo execution skills",
                DifficultyLevel.Medium,
                Priority.Medium));
            skillGaps.Add("Combo execution");
        }

        if (stats.OverallAccuracy < 70)
        {
            recommendations.Add(CreateRecommendation(
                TrainingType.PatternRecognition,
                "Focus on accuracy over speed",
                DifficultyLevel.Easy,
                Priority.Critical));
            skillGaps.Add("Input accuracy");
            personalizedTips.Add("Slow down and focus on correct inputs");
        }

        if (stats.AverageReactionTime > TimeSpan.FromMilliseconds(300))
        {
            skillGaps.Add("Reaction speed");
            personalizedTips.Add("Practice daily to improve reaction time");
        }

        if (stats.TrainingConsistency < 0.5)
        {
            skillGaps.Add("Training consistency");
            personalizedTips.Add("Try to train at least 3 times per week");
        }

        if (stats.BestReactionTime < TimeSpan.FromMilliseconds(150))
        {
            nextMilestones.Add("Achieve sub-150ms average reaction time");
        }

        if (stats.OverallAccuracy > 90)
        {
            nextMilestones.Add("Move to higher difficulty level");
            personalizedTips.Add("Great accuracy! Try increasing difficulty");
        }

        if (nextMilestones.Count == 0)
        {
            nextMilestones.Add("Complete 10 sessions of each training type");
        }

        if (personalizedTips.Count == 0)
        {
            personalizedTips.Add("Maintain your current training routine");
            personalizedTips.Add("Try challenging yourself with higher difficulties");
        }
    }

    private static RecommendedTraining CreateRecommendation(
        TrainingType type,
        string reason,
        DifficultyLevel difficulty,
        Priority priority)
    {
        return new RecommendedTraining
        {
            TrainingType = type,
            Reason = reason,
            Difficulty = difficulty,
            EstimatedBenefit = CalculateEstimatedBenefit(priority),
            Priority = priority
        };
    }

    private static double CalculateEstimatedBenefit(Priority priority)
    {
        return priority switch
        {
            Priority.Critical => 0.95,
            Priority.High => 0.80,
            Priority.Medium => 0.60,
            Priority.Low => 0.40,
            _ => 0.50
        };
    }

    private static TimeSpan CalculateTotalTrainingTime(List<TrainingSession> sessions)
    {
        var totalTicks = sessions.Sum(s =>
        {
            if (s.CompletedAt.HasValue)
            {
                return (s.CompletedAt.Value - s.StartedAt).Ticks;
            }
            return s.Duration?.Ticks ?? TimeSpan.FromMinutes(15).Ticks;
        });
        return TimeSpan.FromTicks(totalTicks);
    }

    private static double CalculateOverallAccuracy(List<TrainingSession> sessions)
    {
        if (sessions.Count == 0) return 0;

        var accuracies = new List<double>();

        foreach (var session in sessions)
        {
            var accuracy = session.TrainingType switch
            {
                TrainingType.Reflex => session.ReflexData?.ResponseAccuracy ?? 0,
                TrainingType.PatternRecognition => session.PatternData?.AccuracyRate ?? 0,
                TrainingType.ComboLab => session.ComboData != null
                    ? (session.ComboData.TargetCombo.Count > 0
                        ? (double)session.ComboData.ComboProgress / session.ComboData.TargetCombo.Count * 100
                        : 0)
                    : 0,
                _ => 0
            };
            accuracies.Add(accuracy);
        }

        return accuracies.Count > 0 ? accuracies.Average() : 0;
    }

    private static (TimeSpan best, TimeSpan average) CalculateReactionTimes(List<TrainingSession> sessions)
    {
        var reactionTimes = sessions
            .Where(s => s.ReflexData != null)
            .Select(s => s.ReflexData!)
            .ToList();

        if (reactionTimes.Count == 0)
        {
            return (TimeSpan.MaxValue, TimeSpan.Zero);
        }

        var best = reactionTimes.Min(r => r.FastestReaction);
        var averageTicks = reactionTimes.Average(r => r.AverageReactionTime.Ticks);

        return (best, TimeSpan.FromTicks((long)averageTicks));
    }

    private static List<string> IdentifySkillsImproved(List<TrainingSession> sessions)
    {
        var skills = new List<string>();

        var reflexSessions = sessions.Where(s => s.TrainingType == TrainingType.Reflex).ToList();
        var patternSessions = sessions.Where(s => s.TrainingType == TrainingType.PatternRecognition).ToList();
        var comboSessions = sessions.Where(s => s.TrainingType == TrainingType.ComboLab).ToList();

        if (reflexSessions.Count >= 3 && reflexSessions.Any(s => s.ReflexData?.ResponseAccuracy > 70))
        {
            skills.Add("Reaction Time");
        }

        if (patternSessions.Count >= 3 && patternSessions.Any(s => s.PatternData?.AccuracyRate > 70))
        {
            skills.Add("Pattern Recognition");
        }

        if (comboSessions.Count >= 3 && comboSessions.Any(s => s.ComboData?.Mistakes < 5))
        {
            skills.Add("Combo Execution");
        }

        if (sessions.Count >= 10)
        {
            skills.Add("Training Consistency");
        }

        return skills;
    }

    private static double CalculateConsistency(List<TrainingSession> sessions, TimeSpan period)
    {
        if (sessions.Count == 0) return 0;

        var daysInPeriod = period.TotalDays;
        var uniqueTrainingDays = sessions.Select(s => s.StartedAt.Date).Distinct().Count();

        return Math.Min(1.0, uniqueTrainingDays / daysInPeriod);
    }
}
