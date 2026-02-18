namespace SaveState.Application.Mugen.Services.Educational.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.Educational;

public class LearningPathEngine
{
    private readonly ILogger<LearningPathEngine> _logger;
    private readonly Dictionary<string, LearningPath> _learningPaths;

    public LearningPathEngine(
        ILogger<LearningPathEngine> logger,
        Dictionary<string, LearningPath> learningPaths)
    {
        _logger = logger;
        _learningPaths = learningPaths;
    }

    /// <summary>
    /// Gets a learning path by ID.
    /// </summary>
    public LearningPath? GetLearningPath(string pathId)
    {
        _logger.LogDebug("Getting learning path {PathId}", pathId);
        _learningPaths.TryGetValue(pathId, out var path);
        return path;
    }

    /// <summary>
    /// Gets the total count of learning paths.
    /// </summary>
    public int LearningPathCount()
    {
        return _learningPaths.Count;
    }

    /// <summary>
    /// Initializes default learning paths.
    /// </summary>
    public void InitializeDefaultLearningPaths()
    {
        _logger.LogInformation("Initializing default learning paths");

        var beginnerPath = new LearningPath
        {
            PathId = "beginner-fundamentals",
            Title = "Beginner Fundamentals",
            Description = "Learn the basics of fighting game mechanics",
            Difficulty = DifficultyLevel.Beginner,
            EstimatedDuration = TimeSpan.FromHours(5),
            Modules = new List<LearningModule>
            {
                new()
                {
                    ModuleId = "module-1",
                    Title = "Basic Controls",
                    Description = "Learn movement and basic attacks",
                    Order = 1,
                    ContentItems = new List<string> { "tutorial-1", "tutorial-2" },
                    EstimatedDuration = TimeSpan.FromHours(1),
                    SkillsTaught = new List<string> { "Movement", "Basic Attacks" }
                },
                new()
                {
                    ModuleId = "module-2",
                    Title = "Defense Basics",
                    Description = "Learn blocking and defense",
                    Order = 2,
                    ContentItems = new List<string> { "tutorial-3", "tutorial-4" },
                    EstimatedDuration = TimeSpan.FromHours(1.5),
                    SkillsTaught = new List<string> { "Blocking", "Defense" }
                }
            }.AsReadOnly(),
            Prerequisites = new List<string>().AsReadOnly(),
            SkillsCovered = new List<string> { "Movement", "Basic Attacks", "Blocking", "Defense" }.AsReadOnly(),
            AuthorId = "system",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EnrollmentCount = 0,
            CompletionRate = 0
        };

        var intermediatePath = new LearningPath
        {
            PathId = "intermediate-combos",
            Title = "Intermediate Combos",
            Description = "Master combo execution and timing",
            Difficulty = DifficultyLevel.Intermediate,
            EstimatedDuration = TimeSpan.FromHours(10),
            Modules = new List<LearningModule>
            {
                new()
                {
                    ModuleId = "module-1",
                    Title = "Basic Combos",
                    Description = "Learn simple combo sequences",
                    Order = 1,
                    ContentItems = new List<string> { "tutorial-5", "tutorial-6" },
                    EstimatedDuration = TimeSpan.FromHours(2),
                    SkillsTaught = new List<string> { "Combo Execution", "Timing" }
                },
                new()
                {
                    ModuleId = "module-2",
                    Title = "Advanced Combos",
                    Description = "Master advanced combo techniques",
                    Order = 2,
                    ContentItems = new List<string> { "tutorial-7", "tutorial-8" },
                    EstimatedDuration = TimeSpan.FromHours(3),
                    SkillsTaught = new List<string> { "Advanced Combos", "Cancel Techniques" }
                }
            }.AsReadOnly(),
            Prerequisites = new List<string> { "beginner-fundamentals" }.AsReadOnly(),
            SkillsCovered = new List<string> { "Combo Execution", "Timing", "Cancel Techniques" }.AsReadOnly(),
            AuthorId = "system",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EnrollmentCount = 0,
            CompletionRate = 0
        };

        var advancedPath = new LearningPath
        {
            PathId = "advanced-strategy",
            Title = "Advanced Strategy",
            Description = "Master high-level fighting game strategy",
            Difficulty = DifficultyLevel.Advanced,
            EstimatedDuration = TimeSpan.FromHours(20),
            Modules = new List<LearningModule>
            {
                new()
                {
                    ModuleId = "module-1",
                    Title = "Matchup Knowledge",
                    Description = "Learn character matchups",
                    Order = 1,
                    ContentItems = new List<string> { "guide-1", "guide-2" },
                    EstimatedDuration = TimeSpan.FromHours(5),
                    SkillsTaught = new List<string> { "Matchup Knowledge", "Character Analysis" }
                },
                new()
                {
                    ModuleId = "module-2",
                    Title = "Mind Games",
                    Description = "Master psychological aspects",
                    Order = 2,
                    ContentItems = new List<string> { "guide-3", "guide-4" },
                    EstimatedDuration = TimeSpan.FromHours(5),
                    SkillsTaught = new List<string> { "Mind Games", "Frame Traps" }
                }
            }.AsReadOnly(),
            Prerequisites = new List<string> { "intermediate-combos" }.AsReadOnly(),
            SkillsCovered = new List<string> { "Matchup Knowledge", "Mind Games", "Frame Traps" }.AsReadOnly(),
            AuthorId = "system",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EnrollmentCount = 0,
            CompletionRate = 0
        };

        _learningPaths[beginnerPath.PathId] = beginnerPath;
        _learningPaths[intermediatePath.PathId] = intermediatePath;
        _learningPaths[advancedPath.PathId] = advancedPath;

        _logger.LogInformation("Initialized {Count} default learning paths", _learningPaths.Count);
    }

    /// <summary>
    /// Gets the most popular categories.
    /// </summary>
    public IReadOnlyList<string> GetPopularCategories(int count)
    {
        _logger.LogDebug("Getting top {Count} popular categories", count);

        var categories = _learningPaths.Values
            .SelectMany(p => p.SkillsCovered)
            .GroupBy(s => s)
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => g.Key)
            .ToList();

        return categories;
    }

    /// <summary>
    /// Gets completion rates for learning paths.
    /// </summary>
    public IReadOnlyList<float> GetCompletionRates(int count)
    {
        _logger.LogDebug("Getting completion rates for top {Count} paths", count);

        var rates = _learningPaths.Values
            .OrderByDescending(p => p.EnrollmentCount)
            .Take(count)
            .Select(p => (float)p.CompletionRate)
            .ToList();

        return rates;
    }
}
