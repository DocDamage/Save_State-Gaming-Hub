using System;

namespace SaveState.Core.Services.Ai.Events
{
    /// <summary>
    /// Standard game event types for AI subscription.
    /// AI agents subscribe to these events instead of polling state.
    /// </summary>
    public static class GameEvents
    {
        // === Player Events ===
        public const string PlayerEnteredRegion = "PLAYER_ENTERED_REGION";
        public const string PlayerExitedRegion = "PLAYER_EXITED_REGION";
        public const string PlayerLevelUp = "PLAYER_LEVEL_UP";
        public const string PlayerDied = "PLAYER_DIED";
        public const string PlayerRespawned = "PLAYER_RESPAWNED";
        public const string PlayerAchievement = "PLAYER_ACHIEVEMENT";
        public const string PlayerDecision = "PLAYER_DECISION";
        public const string PlayerInteraction = "PLAYER_INTERACTION";

        // === NPC Events ===
        public const string NpcSpawned = "NPC_SPAWNED";
        public const string NpcDied = "NPC_DIED";
        public const string NpcDialogueStart = "NPC_DIALOGUE_START";
        public const string NpcDialogueEnd = "NPC_DIALOGUE_END";
        public const string NpcMoodChanged = "NPC_MOOD_CHANGED";
        public const string NpcRelationshipChanged = "NPC_RELATIONSHIP_CHANGED";
        public const string NpcAiTakeover = "NPC_AI_TAKEOVER";

        // === Combat Events ===
        public const string CombatStart = "COMBAT_START";
        public const string CombatEnd = "COMBAT_END";
        public const string CombatTurn = "COMBAT_TURN";
        public const string CriticalHit = "CRITICAL_HIT";
        public const string NearDeath = "NEAR_DEATH";
        public const string EnemyDefeated = "ENEMY_DEFEATED";
        public const string BossEncounter = "BOSS_ENCOUNTER";

        // === Quest Events ===
        public const string QuestStarted = "QUEST_STARTED";
        public const string QuestUpdated = "QUEST_UPDATED";
        public const string QuestCompleted = "QUEST_COMPLETED";
        public const string QuestFailed = "QUEST_FAILED";
        public const string QuestAbandoned = "QUEST_ABANDONED";
        public const string ObjectiveCompleted = "OBJECTIVE_COMPLETED";

        // === World Events ===
        public const string WorldStateChanged = "WORLD_STATE_CHANGED";
        public const string TimeOfDayChanged = "TIME_OF_DAY_CHANGED";
        public const string WeatherChanged = "WEATHER_CHANGED";
        public const string RegionDiscovered = "REGION_DISCOVERED";
        public const string SecretFound = "SECRET_FOUND";
        public const string EnvironmentTriggered = "ENVIRONMENT_TRIGGERED";

        // === Faction Events ===
        public const string FactionReputationChanged = "FACTION_REPUTATION_CHANGED";
        public const string FactionWar = "FACTION_WAR";
        public const string FactionAlliance = "FACTION_ALLIANCE";

        // === Economy Events ===
        public const string ItemAcquired = "ITEM_ACQUIRED";
        public const string ItemLost = "ITEM_LOST";
        public const string ItemCrafted = "ITEM_CRAFTED";
        public const string TransactionCompleted = "TRANSACTION_COMPLETED";
        public const string ItemDuped = "ITEM_DUPED"; // For exploit detection

        // === Narrative Events ===
        public const string CutsceneStart = "CUTSCENE_START";
        public const string CutsceneEnd = "CUTSCENE_END";
        public const string StoryMilestone = "STORY_MILESTONE";
        public const string LoreDiscovered = "LORE_DISCOVERED";
        public const string BranchingChoice = "BRANCHING_CHOICE";

        // === System Events ===
        public const string GameSaved = "GAME_SAVED";
        public const string GameLoaded = "GAME_LOADED";
        public const string SessionStarted = "SESSION_STARTED";
        public const string SessionEnded = "SESSION_ENDED";
        public const string AiModelChanged = "AI_MODEL_CHANGED";
        public const string ErrorOccurred = "ERROR_OCCURRED";
    }

    /// <summary>
    /// Priority levels for events
    /// </summary>
    public enum EventPriority
    {
        Background = 0,    // Process when idle
        Low = 1,           // Non-urgent updates
        Normal = 2,        // Standard priority
        High = 3,          // Important, process soon
        Critical = 4,      // Process immediately
        System = 5         // System-level, highest priority
    }

    /// <summary>
    /// Event categories for filtering
    /// </summary>
    public enum EventCategory
    {
        Player,
        Npc,
        Combat,
        Quest,
        World,
        Faction,
        Economy,
        Narrative,
        System
    }

    /// <summary>
    /// Extended event with additional metadata
    /// </summary>
    public class GameEvent : AiEvent
    {
        public EventPriority EventPriority { get; set; } = EventPriority.Normal;
        public EventCategory Category { get; set; } = EventCategory.System;
        public string? SourceEntityId { get; set; }
        public string? TargetEntityId { get; set; }
        public string? LocationId { get; set; }
        public bool RequiresImmediateResponse { get; set; } = false;
        public bool CanBeBatched { get; set; } = true;
        public TimeSpan? ExpiresIn { get; set; }

        public static GameEvent Create(
            string eventType,
            EventCategory category,
            EventPriority priority = EventPriority.Normal,
            string? source = null,
            string? target = null,
            Dictionary<string, object>? data = null)
        {
            return new GameEvent
            {
                EventType = eventType,
                Category = category,
                EventPriority = priority,
                SourceEntityId = source,
                TargetEntityId = target,
                Data = data ?? new(),
                Priority = (int)priority
            };
        }
    }
}
