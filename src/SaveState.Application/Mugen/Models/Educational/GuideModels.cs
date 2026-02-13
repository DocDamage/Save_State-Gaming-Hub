using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Application.Mugen.Models.Educational;

/// <summary>
/// Strategy guide data model.
/// </summary>
public class StrategyGuide
{
    public string GuideId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public GameMode GameMode { get; set; } = default!;
    public bool CharacterSpecific { get; set; } = default!;
    public SkillLevel SkillLevel { get; set; } = default!;
    public IReadOnlyList<GuideSection> Sections { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = default!;
    public int ViewCount { get; set; } = default!;
    public int HelpfulVotes { get; set; } = default!;
}

/// <summary>
/// Strategy guide query parameters.
/// </summary>
public class StrategyGuideQuery
{
    public GameMode? GameMode { get; set; } = default!;
    public bool? Character { get; set; } = default!;
    public SkillLevel? SkillLevel { get; set; } = default!;
    public int Offset { get; set; } = default!;
    public int Limit { get; set; } = default!;
}

/// <summary>
/// Guide section data.
/// </summary>
public class GuideSection
{
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public IReadOnlyList<string> Examples { get; set; } = default!;
    public IReadOnlyList<string> Tips { get; set; } = default!;
}

/// <summary>
/// Mechanics guide data model.
/// </summary>
public class MechanicsGuide
{
    public string Topic { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public GuideContent Content { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; } = default!;
    public IReadOnlyList<string> RelatedTopics { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = default!;
    public int ViewCount { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
}

/// <summary>
/// Guide content data.
/// </summary>
public class GuideContent
{
    public string Overview { get; set; } = default!;
    public string DetailedExplanation { get; set; } = default!;
    public IReadOnlyList<string> VisualAids { get; set; } = default!;
    public IReadOnlyList<string> Examples { get; set; } = default!;
    public IReadOnlyList<string> PracticeExercises { get; set; } = default!;
}
