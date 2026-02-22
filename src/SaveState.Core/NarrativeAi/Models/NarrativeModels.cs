using SaveState.Core.Common;

namespace SaveState.Core.NarrativeAi.Models;

/// <summary>
/// Represents a generated quest with narrative context.
/// </summary>
public record GeneratedQuest
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public QuestType Type { get; init; }
    public QuestDifficulty Difficulty { get; init; }
    public IReadOnlyList<QuestObjective> Objectives { get; init; } = Array.Empty<QuestObjective>();
    public IReadOnlyList<QuestReward> Rewards { get; init; } = Array.Empty<QuestReward>();
    public string? NarrativeContext { get; init; }
    public IReadOnlyList<string> Prerequisites { get; init; } = Array.Empty<string>();
    public TimeSpan? TimeLimit { get; init; }
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Types of quests that can be generated.
/// </summary>
public enum QuestType
{
    MainStory,
    SideQuest,
    Bounty,
    Exploration,
    Puzzle,
    Escort,
    Delivery,
    Investigation,
    Survival,
    Challenge
}

/// <summary>
/// Difficulty levels for quests.
/// </summary>
public enum QuestDifficulty
{
    VeryEasy,
    Easy,
    Normal,
    Hard,
    VeryHard,
    Legendary
}

/// <summary>
/// Represents a quest objective.
/// </summary>
public record QuestObjective
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Description { get; init; } = string.Empty;
    public ObjectiveType Type { get; init; }
    public int TargetAmount { get; init; } = 1;
    public int CurrentAmount { get; init; } = 0;
    public bool IsOptional { get; init; } = false;
    public string? LocationHint { get; init; }
}

/// <summary>
/// Types of quest objectives.
/// </summary>
public enum ObjectiveType
{
    Kill,
    Collect,
    Deliver,
    Talk,
    Explore,
    Survive,
    Protect,
    Craft,
    Solve,
    Find
}

/// <summary>
/// Represents a quest reward.
/// </summary>
public record QuestReward
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public RewardType Type { get; init; }
    public string ItemId { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public int Quantity { get; init; } = 1;
    public int ExperiencePoints { get; init; }
}

/// <summary>
/// Types of quest rewards.
/// </summary>
public enum RewardType
{
    Item,
    Currency,
    Experience,
    Reputation,
    Ability,
    Title,
    Cosmetic
}

/// <summary>
/// Represents a dialogue node in a conversation tree.
/// </summary>
public record DialogueNode
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string SpeakerId { get; init; } = string.Empty;
    public string SpeakerName { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DialogueEmotion Emotion { get; init; } = DialogueEmotion.Neutral;
    public IReadOnlyList<DialogueOption> Options { get; init; } = Array.Empty<DialogueOption>();
    public string? AudioClipId { get; init; }
    public float DisplayDuration { get; init; } = 3.0f;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents a player dialogue option.
/// </summary>
public record DialogueOption
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Text { get; init; } = string.Empty;
    public string? NextNodeId { get; init; }
    public IReadOnlyList<DialogueCondition> Conditions { get; init; } = Array.Empty<DialogueCondition>();
    public IReadOnlyList<DialogueEffect> Effects { get; init; } = Array.Empty<DialogueEffect>();
    public DialogueOptionStyle Style { get; init; } = DialogueOptionStyle.Normal;
}

/// <summary>
/// Styles for dialogue options.
/// </summary>
public enum DialogueOptionStyle
{
    Normal,
    Aggressive,
    Friendly,
    Sarcastic,
    Persuasive,
    Intimidating,
    Diplomatic,
    Honest,
    Deceptive
}

/// <summary>
/// Emotions that can be expressed in dialogue.
/// </summary>
public enum DialogueEmotion
{
    Neutral,
    Happy,
    Sad,
    Angry,
    Surprised,
    Fearful,
    Disgusted,
    Excited,
    Worried,
    Confident
}

/// <summary>
/// Condition that must be met for a dialogue option to be available.
/// </summary>
public record DialogueCondition
{
    public ConditionType Type { get; init; }
    public string Parameter { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public ComparisonOperator Operator { get; init; } = ComparisonOperator.Equals;
}

/// <summary>
/// Effect that occurs when a dialogue option is selected.
/// </summary>
public record DialogueEffect
{
    public EffectType Type { get; init; }
    public string Target { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

/// <summary>
/// Types of dialogue conditions.
/// </summary>
public enum ConditionType
{
    HasItem,
    HasReputation,
    CompletedQuest,
    HasSkill,
    KnowsInformation,
    TimeOfDay,
    RelationshipLevel
}

/// <summary>
/// Types of dialogue effects.
/// </summary>
public enum EffectType
{
    AddItem,
    RemoveItem,
    ChangeReputation,
    StartQuest,
    CompleteQuest,
    SetFlag,
    ChangeRelationship,
    Teleport
}

/// <summary>
/// Comparison operators for conditions.
/// </summary>
public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains
}

/// <summary>
/// Represents a branch in the story narrative.
/// </summary>
public record StoryBranch
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int BranchLevel { get; init; }
    public string? ParentBranchId { get; init; }
    public IReadOnlyList<string> ChildBranchIds { get; init; } = Array.Empty<string>();
    public BranchStatus Status { get; init; } = BranchStatus.Available;
    public IReadOnlyList<string> RequiredFlags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> UnlockConditions { get; init; } = Array.Empty<string>();
    public DateTime? UnlockedAt { get; init; }
}

/// <summary>
/// Status of a story branch.
/// </summary>
public enum BranchStatus
{
    Locked,
    Available,
    InProgress,
    Completed,
    Abandoned
}

/// <summary>
/// Represents the overall narrative state.
/// </summary>
public record NarrativeState
{
    public string StoryId { get; init; } = string.Empty;
    public string CurrentBranchId { get; init; } = string.Empty;
    public IReadOnlyList<string> CompletedBranches { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ActiveFlags { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, int> CharacterRelationships { get; init; } = new Dictionary<string, int>();
    public int TotalPlayTimeMinutes { get; init; }
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Request for generating a quest.
/// </summary>
public record QuestGenerationRequest
{
    public string GameContext { get; init; } = string.Empty;
    public QuestType PreferredType { get; init; } = QuestType.SideQuest;
    public QuestDifficulty PreferredDifficulty { get; init; } = QuestDifficulty.Normal;
    public IReadOnlyList<string> PlayerPreferences { get; init; } = Array.Empty<string>();
    public string? CurrentLocation { get; init; }
    public int PlayerLevel { get; init; } = 1;
    public IReadOnlyList<string> CompletedQuestIds { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Request for generating dialogue.
/// </summary>
public record DialogueGenerationRequest
{
    public string SpeakerId { get; init; } = string.Empty;
    public string SpeakerPersonality { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public DialogueEmotion PreferredEmotion { get; init; } = DialogueEmotion.Neutral;
    public IReadOnlyList<string> Topics { get; init; } = Array.Empty<string>();
    public int MaxOptions { get; init; } = 4;
    public string? PreviousNodeId { get; init; }
}
