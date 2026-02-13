using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;

namespace SaveState.Core.OpenMK.Services;

/// <summary>
/// Service for managing OpenMK story mode and arcade progression.
/// </summary>
public interface IOpenMKStoryService
{
    /// <summary>
    /// Starts a new story mode campaign.
    /// </summary>
    Task<Result<OpenMKStoryCampaign>> StartCampaignAsync(Guid userId, Guid selectedCharacterId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current story campaign for a user.
    /// </summary>
    Task<Result<OpenMKStoryCampaign>> GetCurrentCampaignAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Advances to the next story chapter.
    /// </summary>
    Task<Result<OpenMKStoryChapter>> AdvanceToNextChapterAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets available story chapters.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKStoryChapter>>> GetAvailableChaptersAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Starts a story chapter match.
    /// </summary>
    Task<Result<OpenMKStoryMatch>> StartChapterMatchAsync(Guid userId, Guid chapterId, CancellationToken ct = default);

    /// <summary>
    /// Completes a story chapter with the match result.
    /// </summary>
    Task<Result<OpenMKChapterCompletion>> CompleteChapterAsync(
        Guid userId,
        Guid chapterId,
        OpenMKMatchResult matchResult,
        CancellationToken ct = default);

    /// <summary>
    /// Makes a story choice that affects the campaign.
    /// </summary>
    Task<Result<OpenMKStoryChoiceResult>> MakeStoryChoiceAsync(
        Guid userId,
        Guid choiceId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the character ending based on campaign choices.
    /// </summary>
    Task<Result<OpenMKCharacterEnding>> GetCharacterEndingAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Resets the story campaign progress.
    /// </summary>
    Task<Result> ResetCampaignAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// A story mode campaign.
/// </summary>
public record OpenMKStoryCampaign(
    Guid Id,
    Guid UserId,
    Guid SelectedCharacterId,
    string CharacterName,
    OpenMKCampaignDifficulty Difficulty,
    DateTime StartedAt,
    int CurrentChapter,
    int TotalChapters,
    decimal ProgressPercentage,
    IReadOnlyList<OpenMKStoryChoice> ChoicesMade,
    IReadOnlyList<OpenMKChapterCompletion> CompletedChapters);

/// <summary>
/// Campaign difficulty levels.
/// </summary>
public enum OpenMKCampaignDifficulty
{
    /// <summary>
    /// Easy difficulty.
    /// </summary>
    Easy,

    /// <summary>
    /// Medium difficulty.
    /// </summary>
    Medium,

    /// <summary>
    /// Hard difficulty.
    /// </summary>
    Hard,

    /// <summary>
    /// Very hard difficulty.
    /// </summary>
    VeryHard,

    /// <summary>
    /// Mortal Kombat difficulty.
    /// </summary>
    Mortal
}

/// <summary>
/// A story chapter.
/// </summary>
public record OpenMKStoryChapter(
    Guid Id,
    string Name,
    string Description,
    int ChapterNumber,
    OpenMKChapterType Type,
    string OpponentCharacter,
    string Location,
    IReadOnlyList<OpenMKStoryChoice> AvailableChoices,
    IReadOnlyList<string> Objectives);

/// <summary>
/// Types of story chapters.
/// </summary>
public enum OpenMKChapterType
{
    /// <summary>
    /// Standard fight chapter.
    /// </summary>
    Fight,

    /// <summary>
    /// Boss fight chapter.
    /// </summary>
    Boss,

    /// <summary>
    /// Multiple opponent chapter.
    /// </summary>
    MultiFight,

    /// <summary>
    /// Choice-based chapter.
    /// </summary>
    Choice,

    /// <summary>
    /// Final chapter.
    /// </summary>
    Final
}

/// <summary>
/// A choice in the story.
/// </summary>
public record OpenMKStoryChoice(
    Guid Id,
    string Description,
    string Consequence,
    OpenMKChoiceAlignment Alignment,
    IReadOnlyList<OpenMKChoiceEffect> Effects);

/// <summary>
/// Alignment of a story choice.
/// </summary>
public enum OpenMKChoiceAlignment
{
    /// <summary>
    /// Heroic choice.
    /// </summary>
    Heroic,

    /// <summary>
    /// Neutral choice.
    /// </summary>
    Neutral,

    /// <summary>
    /// Villainous choice.
    /// </summary>
    Villainous
}

/// <summary>
/// Effects of making a story choice.
/// </summary>
public record OpenMKChoiceEffect(
    OpenMKChoiceEffectType Type,
    string Target,
    int Value);

/// <summary>
/// Types of choice effects.
/// </summary>
public enum OpenMKChoiceEffectType
{
    /// <summary>
    /// Modify relationship with character.
    /// </summary>
    RelationshipChange,

    /// <summary>
    /// Unlock new content.
    /// </summary>
    ContentUnlock,

    /// <summary>
    /// Change campaign difficulty.
    /// </summary>
    DifficultyModifier,

    /// <summary>
    /// Award bonus rewards.
    /// </summary>
    BonusReward,

    /// <summary>
    /// Affect story branching.
    /// </summary>
    StoryBranch
}

/// <summary>
/// A match within a story chapter.
/// </summary>
public record OpenMKStoryMatch(
    Guid Id,
    Guid ChapterId,
    string OpponentCharacter,
    string Location,
    OpenMKMatchType MatchType,
    IReadOnlyList<string> SpecialRules);

/// <summary>
/// Result of completing a chapter.
/// </summary>
public record OpenMKChapterCompletion(
    Guid ChapterId,
    int ChapterNumber,
    bool Success,
    TimeSpan CompletionTime,
    int Score,
    IReadOnlyList<OpenMKChapterReward> Rewards);

/// <summary>
/// Rewards from completing a chapter.
/// </summary>
public record OpenMKChapterReward(
    OpenMKChapterRewardType Type,
    string Description,
    int? Value,
    Guid? UnlockedContentId);

/// <summary>
/// Types of chapter rewards.
/// </summary>
public enum OpenMKChapterRewardType
{
    /// <summary>
    /// Experience points.
    /// </summary>
    Experience,

    /// <summary>
    /// Koins currency.
    /// </summary>
    Koins,

    /// <summary>
    /// New character unlock.
    /// </summary>
    CharacterUnlock,

    /// <summary>
    /// New move unlock.
    /// </summary>
    MoveUnlock,

    /// <summary>
    /// Story progression.
    /// </summary>
    StoryProgress
}

/// <summary>
/// Result of making a story choice.
/// </summary>
public record OpenMKStoryChoiceResult(
    Guid ChoiceId,
    IReadOnlyList<OpenMKChoiceEffect> Effects,
    string NarrativeResult,
    IReadOnlyList<OpenMKStoryChapter> UnlockedChapters);

/// <summary>
/// Character ending based on campaign choices.
/// </summary>
public record OpenMKCharacterEnding(
    string Title,
    string Description,
    OpenMKEndingType Type,
    IReadOnlyList<string> KeyChoices,
    string FullNarrative);

/// <summary>
/// Types of character endings.
/// </summary>
public enum OpenMKEndingType
{
    /// <summary>
    /// Heroic ending.
    /// </summary>
    Heroic,

    /// <summary>
    /// Neutral ending.
    /// </summary>
    Neutral,

    /// <summary>
    /// Villainous ending.
    /// </summary>
    Villainous,

    /// <summary>
    /// Secret ending.
    /// </summary>
    Secret,

    /// <summary>
    /// Bad ending.
    /// </summary>
    Bad
}