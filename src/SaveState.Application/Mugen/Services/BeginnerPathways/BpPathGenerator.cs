using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ValueObjects;
using Microsoft.Extensions.Logging;
using SkillLevel = SaveState.Application.Mugen.Models.Educational.SkillLevel;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Path generator for creating personalized learning paths.
/// </summary>
public class BpPathGenerator
{
    private readonly ILogger<BpPathGenerator> _logger;
    private readonly ITimeProvider _timeProvider;

    public BpPathGenerator(ILogger<BpPathGenerator> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<BpPathwayLearningPath> GeneratePersonalizedPathAsync(string userId, BpUserAssessment assessment, CancellationToken ct)
    {
        return Task.FromResult(new BpPathwayLearningPath
        {
            PathId = $"personalized-{userId}",
            Title = "Your Personal Learning Journey",
            Description = $"Customized path based on your {assessment.SkillLevel} skill level",
            Difficulty = assessment.SkillLevel switch
            {
                SkillLevel.Beginner => DifficultyLevel.Beginner,
                SkillLevel.Intermediate => DifficultyLevel.Intermediate,
                _ => DifficultyLevel.Advanced
            },
            EstimatedDuration = TimeSpan.FromHours(assessment.TimeCommitment == BpTimeCommitment.Heavy ? 20 : 10),
            TargetSkills = assessment.Weaknesses.Count > 0 ? assessment.Weaknesses : new[] { "General Improvement" },
            Prerequisites = Array.Empty<string>(),
            Modules = GeneratePersonalizedModules(assessment),
            CreatedAt = _timeProvider.UtcNow,
            TotalEnrollments = 1,
            AverageRating = 0.0,
            SuccessRate = 0.0
        });
    }

    private List<BpPathwayLearningModule> GeneratePersonalizedModules(BpUserAssessment assessment)
    {
        var modules = new List<BpPathwayLearningModule>();
        if (assessment.SkillLevel == SkillLevel.Beginner)
        {
            modules.Add(new BpPathwayLearningModule
            {
                ModuleId = "personal-1",
                Title = "Your First Steps",
                Description = "Start your MUGEN journey",
                Order = 1,
                Lessons = new List<BpLearningLesson>(),
                EstimatedDuration = TimeSpan.FromMinutes(30),
                SkillsCovered = new[] { "Interface", "Basic Movement" },
                Prerequisites = Array.Empty<string>()
            });
        }
        return modules;
    }
}
