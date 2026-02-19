namespace SaveState.Application.Mugen.Services.Educational.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.Educational;
using SaveState.Core.Common.Services;

public class ContentEngine
{
    private readonly ILogger<ContentEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, Tutorial> _tutorials;
    private readonly Dictionary<string, StrategyGuide> _strategyGuides;
    private readonly Dictionary<string, MechanicsGuide> _mechanicsGuides;

    public ContentEngine(
        ILogger<ContentEngine> logger,
        ITimeProvider timeProvider,
        Dictionary<string, Tutorial> tutorials,
        Dictionary<string, StrategyGuide> strategyGuides,
        Dictionary<string, MechanicsGuide> mechanicsGuides)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _tutorials = tutorials;
        _strategyGuides = strategyGuides;
        _mechanicsGuides = mechanicsGuides;
    }

    /// <summary>
    /// Queries tutorials based on filter criteria.
    /// </summary>
    public Task<IReadOnlyList<Tutorial>> QueryTutorialsAsync(TutorialQuery query, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying tutorials with filters");

        var results = _tutorials.Values.AsEnumerable();

        if (query.Difficulty.HasValue)
        {
            results = results.Where(t => t.Difficulty == query.Difficulty.Value);
        }

        if (!string.IsNullOrEmpty(query.Category))
        {
            results = results.Where(t => t.Category.Equals(query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Tags?.Count > 0)
        {
            results = results.Where(t => query.Tags.Any(tag => t.Tags.Contains(tag)));
        }

        results = query.SortBy switch
        {
            TutorialSort.Popularity => results.OrderByDescending(t => t.ViewCount),
            TutorialSort.Rating => results.OrderByDescending(t => t.AverageRating),
            TutorialSort.Difficulty => results.OrderBy(t => t.Difficulty),
            TutorialSort.Recent => results.OrderByDescending(t => t.CreatedAt),
            _ => results.OrderBy(t => t.Title)
        };

        var pagedResults = results
            .Skip(query.Offset)
            .Take(query.Limit > 0 ? query.Limit : 20)
            .ToList();

        return Task.FromResult<IReadOnlyList<Tutorial>>(pagedResults);
    }

    /// <summary>
    /// Gets a specific tutorial by ID.
    /// </summary>
    public async Task<Tutorial?> GetTutorialAsync(string tutorialId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting tutorial {TutorialId}", tutorialId);

        await Task.CompletedTask;
        _tutorials.TryGetValue(tutorialId, out var tutorial);
        return tutorial;
    }

    /// <summary>
    /// Checks if a tutorial exists.
    /// </summary>
    public bool TutorialExists(string tutorialId)
    {
        return _tutorials.ContainsKey(tutorialId);
    }

    /// <summary>
    /// Gets the step count for a tutorial.
    /// </summary>
    public int GetTutorialStepCount(string tutorialId)
    {
        return _tutorials.TryGetValue(tutorialId, out var tutorial) ? tutorial.Steps.Count : 0;
    }

    /// <summary>
    /// Gets a specific tutorial step.
    /// </summary>
    public TutorialStep? GetTutorialStep(string tutorialId, int stepNumber)
    {
        if (_tutorials.TryGetValue(tutorialId, out var tutorial))
        {
            return tutorial.Steps.ElementAtOrDefault(stepNumber - 1);
        }
        return null;
    }

    /// <summary>
    /// Creates a new tutorial.
    /// </summary>
    public Tutorial CreateTutorial(TutorialCreationRequest request)
    {
        var now = _timeProvider.UtcNow;

        var tutorial = new Tutorial
        {
            TutorialId = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Difficulty = request.Difficulty,
            EstimatedDuration = request.EstimatedDuration,
            Tags = request.Tags,
            Prerequisites = request.Prerequisites,
            Steps = request.Steps,
            AuthorId = request.AuthorId,
            CreatedAt = now,
            UpdatedAt = now,
            ViewCount = 0,
            CompletionCount = 0,
            AverageRating = 0,
            TotalRatings = 0
        };
        _tutorials[tutorial.TutorialId] = tutorial;
        return tutorial;
    }

    /// <summary>
    /// Gets the total count of tutorials.
    /// </summary>
    public int TutorialCount() => _tutorials.Count;

    /// <summary>
    /// Gets the count of strategy guides.
    /// </summary>
    public int StrategyGuideCount() => _strategyGuides.Count;

    /// <summary>
    /// Gets the count of mechanics guides.
    /// </summary>
    public int MechanicsGuideCount() => _mechanicsGuides.Count;

    /// <summary>
    /// Gets popular categories.
    /// </summary>
    public IReadOnlyList<string> GetPopularCategories()
    {
        return _tutorials.Values.Select(t => t.Category).Distinct().ToList();
    }

    /// <summary>
    /// Gets completion rates.
    /// </summary>
    public Dictionary<string, float> GetCompletionRates()
    {
        return _tutorials.Values.ToDictionary(
            t => t.TutorialId,
            t => t.ViewCount > 0 ? (float)t.CompletionCount / t.ViewCount : 0f);
    }

    /// <summary>
    /// Increments tutorial completion count.
    /// </summary>
    public void IncrementTutorialCompletion(string tutorialId)
    {
        if (_tutorials.TryGetValue(tutorialId, out var tutorial))
        {
            tutorial.CompletionCount++;
        }
    }

    /// <summary>
    /// Queries strategy guides based on filter criteria.
    /// </summary>
    public Task<IReadOnlyList<StrategyGuide>> QueryStrategyGuides(StrategyGuideQuery query, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying strategy guides with filters");

        var results = _strategyGuides.Values.AsEnumerable();

        if (query.GameMode.HasValue)
        {
            results = results.Where(g => g.GameMode == query.GameMode.Value);
        }

        if (query.Character.HasValue)
        {
            results = results.Where(g => g.CharacterSpecific == query.Character.Value);
        }

        if (query.SkillLevel.HasValue)
        {
            results = results.Where(g => g.SkillLevel == query.SkillLevel.Value);
        }

        var pagedResults = results
            .Skip(query.Offset)
            .Take(query.Limit > 0 ? query.Limit : 20)
            .ToList();

        return Task.FromResult<IReadOnlyList<StrategyGuide>>(pagedResults);
    }

    /// <summary>
    /// Gets a strategy guide by ID.
    /// </summary>
    public Task<StrategyGuide?> GetStrategyGuide(string guideId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting strategy guide {GuideId}", guideId);
        _strategyGuides.TryGetValue(guideId, out var guide);
        return Task.FromResult(guide);
    }

    /// <summary>
    /// Gets a mechanics guide by topic.
    /// </summary>
    public Task<MechanicsGuide?> GetMechanicsGuide(string topic, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting mechanics guide for topic {Topic}", topic);
        _mechanicsGuides.TryGetValue(topic, out var guide);
        return Task.FromResult(guide);
    }
}
