namespace SaveState.Core.AiCoOp.Models;

/// <summary>
/// Represents the personality type of an AI Co-Op companion.
/// </summary>
public enum CompanionPersonality
{
    /// <summary>Encouraging and helpful personality.</summary>
    Supportive,
    /// <summary>Challenges the player and competes.</summary>
    Competitive,
    /// <summary>Makes jokes and keeps things lighthearted.</summary>
    Humorous,
    /// <summary>Strategic and analytical approach.</summary>
    Tactical,
    /// <summary>Minimal chatter, focused on gameplay.</summary>
    Silent
}

/// <summary>
/// Represents the skill level of the AI companion.
/// </summary>
public enum SkillLevel
{
    /// <summary>Learns alongside the player.</summary>
    Beginner,
    /// <summary>Matches the player's skill level.</summary>
    Equal,
    /// <summary>Slightly better, teaches the player.</summary>
    Mentor,
    /// <summary>High skill, can carry when needed.</summary>
    Professional
}

/// <summary>
/// Represents the voice profile for the AI companion.
/// </summary>
public enum VoiceProfile
{
    /// <summary>Neutral voice.</summary>
    Neutral,
    /// <summary>Energetic and enthusiastic voice.</summary>
    Energetic,
    /// <summary>Calm and soothing voice.</summary>
    Calm,
    /// <summary>Robotic/mechanical voice.</summary>
    Robotic,
    /// <summary>Custom user-defined voice.</summary>
    Custom
}

/// <summary>
/// Configuration for the AI Co-Op companion.
/// </summary>
public record CompanionConfiguration
{
    /// <summary>The display name of the companion.</summary>
    public required string Name { get; init; }

    /// <summary>The personality type of the companion.</summary>
    public required CompanionPersonality Personality { get; init; }

    /// <summary>The skill level of the companion.</summary>
    public required SkillLevel SkillLevel { get; init; }

    /// <summary>The voice profile for the companion.</summary>
    public required VoiceProfile Voice { get; init; }

    /// <summary>Whether the companion should proactively offer suggestions.</summary>
    public required bool ProactiveSuggestions { get; init; }

    /// <summary>Whether voice output is enabled.</summary>
    public required bool VoiceEnabled { get; init; }

    /// <summary>Whether the companion is allowed to take control in critical situations.</summary>
    public required bool TakeControlAllowed { get; init; }
}

/// <summary>
/// Represents a snapshot of the current game state for the companion to analyze.
/// </summary>
public record GameStateSnapshot
{
    /// <summary>The unique identifier of the game.</summary>
    public required string GameId { get; init; }

    /// <summary>The current level or area in the game.</summary>
    public required string CurrentLevel { get; init; }

    /// <summary>The player's current position in the game world.</summary>
    public required string PlayerPosition { get; init; }

    /// <summary>The player's current health (0.0 to 1.0).</summary>
    public required float PlayerHealth { get; init; }

    /// <summary>The number of enemies currently nearby.</summary>
    public required int EnemyCount { get; init; }

    /// <summary>The current objective or mission.</summary>
    public required string CurrentObjective { get; init; }

    /// <summary>List of nearby items or collectibles.</summary>
    public required IReadOnlyList<string> NearbyItems { get; init; }

    /// <summary>Duration of the current gaming session.</summary>
    public required TimeSpan SessionDuration { get; init; }
}

/// <summary>
/// Represents an action the AI companion wants to take.
/// </summary>
public record CompanionAction
{
    /// <summary>The type of action (e.g., "Suggest", "Warn", "Assist").</summary>
    public required string ActionType { get; init; }

    /// <summary>Human-readable description of the action.</summary>
    public required string Description { get; init; }

    /// <summary>Confidence score (0.0 to 1.0) of the action.</summary>
    public required float Confidence { get; init; }

    /// <summary>Voice line to speak, if any.</summary>
    public required string? VoiceLine { get; init; }

    /// <summary>Additional parameters for the action.</summary>
    public required Dictionary<string, object> Parameters { get; init; }
}

/// <summary>
/// Represents a sample of player behavior for learning purposes.
/// </summary>
public record PlayerBehaviorSample
{
    /// <summary>The game identifier.</summary>
    public required string GameId { get; init; }

    /// <summary>The action the player took.</summary>
    public required string Action { get; init; }

    /// <summary>The context in which the action was taken.</summary>
    public required string Context { get; init; }

    /// <summary>Whether the action was successful.</summary>
    public required bool WasSuccessful { get; init; }

    /// <summary>Time taken to react and execute the action.</summary>
    public required TimeSpan ReactionTime { get; init; }

    /// <summary>When the action occurred.</summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Represents a chat message between the player and companion.
/// </summary>
public record CompanionChatMessage
{
    /// <summary>Unique identifier for the message.</summary>
    public required string Id { get; init; }

    /// <summary>The sender ("Player" or "Companion").</summary>
    public required string Sender { get; init; }

    /// <summary>The message content.</summary>
    public required string Message { get; init; }

    /// <summary>When the message was sent.</summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>Whether this message was sent via voice.</summary>
    public required bool IsVoice { get; init; }
}
