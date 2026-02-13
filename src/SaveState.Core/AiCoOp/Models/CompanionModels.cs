using SaveState.Core.Common;

namespace SaveState.Core.AiCoOp.Models;

/// <summary>
/// Represents the personality type of an AI Co-Op companion.
/// </summary>
public enum CompanionPersonalityType
{
    Supportive,
    Aggressive,
    Tactical,
    Humorous,
    Silent,
    Roleplay
}

/// <summary>
/// Represents the current state of game context for AI companion.
/// </summary>
public record GameContextSnapshot
{
    public string GameId { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public string CurrentScene { get; init; } = string.Empty;
    public PlayerStatus PlayerStatus { get; init; } = new();
    public GameObjective? CurrentObjective { get; init; }
    public IReadOnlyList<GameEntity> NearbyEntities { get; init; } = Array.Empty<GameEntity>();
    public IReadOnlyList<string> RecentEvents { get; init; } = Array.Empty<string>();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents player status information.
/// </summary>
public record PlayerStatus
{
    public int Health { get; init; }
    public int MaxHealth { get; init; }
    public int Mana { get; init; }
    public int MaxMana { get; init; }
    public string CurrentWeapon { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public IReadOnlyList<string> StatusEffects { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents a game objective.
/// </summary>
public record GameObjective
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ObjectivePriority Priority { get; init; }
    public float CompletionPercentage { get; init; }
}

/// <summary>
/// Priority levels for objectives.
/// </summary>
public enum ObjectivePriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Represents an entity in the game world.
/// </summary>
public record GameEntity
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public EntityType Type { get; init; }
    public float Distance { get; init; }
    public bool IsHostile { get; init; }
    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Types of game entities.
/// </summary>
public enum EntityType
{
    NPC,
    Enemy,
    Item,
    Object,
    Location
}

/// <summary>
/// Configuration for AI companion personality.
/// </summary>
public record CompanionPersonality
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "Companion";
    public CompanionPersonalityType Type { get; init; } = CompanionPersonalityType.Supportive;
    public string VoiceProfile { get; init; } = "default";
    public IReadOnlyList<string> Catchphrases { get; init; } = Array.Empty<string>();
    public float AggressivenessLevel { get; init; } = 0.5f;
    public float HelpfulnessLevel { get; init; } = 0.8f;
    public float VerbosityLevel { get; init; } = 0.6f;
    public IReadOnlyDictionary<string, string> CustomTraits { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents a player behavior pattern learned by the AI.
/// </summary>
public record PlayerBehaviorPattern
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string PatternType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public int OccurrenceCount { get; init; }
    public DateTime FirstObserved { get; init; }
    public DateTime LastObserved { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents a companion action to be executed.
/// </summary>
public record CompanionAction
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public ActionType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public int Priority { get; init; } = 5;
    public TimeSpan? Delay { get; init; }
}

/// <summary>
/// Types of companion actions.
/// </summary>
public enum ActionType
{
    Speak,
    Emote,
    Suggest,
    Warn,
    Assist,
    Celebrate,
    Console
}

/// <summary>
/// Represents the result of a companion action execution.
/// </summary>
public record ActionExecutionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string ExecutedActionId { get; init; } = string.Empty;
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a player behavior profile for learning.
/// </summary>
public record PlayerBehaviorProfile
{
    public string PlayerId { get; init; } = string.Empty;
    public IReadOnlyList<PlayerBehaviorPattern> Patterns { get; init; } = Array.Empty<PlayerBehaviorPattern>();
    public IReadOnlyList<string> PreferredStrategies { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> CommonMistakes { get; init; } = Array.Empty<string>();
    public float SkillLevelEstimate { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a suggestion from the AI companion.
/// </summary>
public record CompanionSuggestion
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Message { get; init; } = string.Empty;
    public SuggestionType Type { get; init; }
    public float Confidence { get; init; }
    public IReadOnlyList<string> RelatedGameElements { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Types of companion suggestions.
/// </summary>
public enum SuggestionType
{
    Strategy,
    Warning,
    Tip,
    Lore,
    Shortcut,
    Challenge
}
