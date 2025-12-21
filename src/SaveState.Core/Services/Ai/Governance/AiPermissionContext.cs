using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Governance
{
    /// <summary>
    /// Permission context for AI capability checks.
    /// Contains all relevant context for determining what an AI can do.
    /// </summary>
    public class AiPermissionContext
    {
        /// <summary>
        /// Unique identifier for the user making the request
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User's subscription tier (Free, Premium, Pro, Developer)
        /// </summary>
        public UserTier Tier { get; set; } = UserTier.Free;

        /// <summary>
        /// Current game being played (null if no game context)
        /// </summary>
        public string? GameId { get; set; }

        /// <summary>
        /// Current game mode (Story, Arcade, Versus, Practice, etc.)
        /// </summary>
        public GameMode Mode { get; set; } = GameMode.Default;

        /// <summary>
        /// The AI service requesting the capability
        /// </summary>
        public AiServiceType RequestingService { get; set; } = AiServiceType.Unknown;

        /// <summary>
        /// The specific module within the service
        /// </summary>
        public string? ModuleName { get; set; }

        /// <summary>
        /// Session identifier for tracking
        /// </summary>
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp of the request
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata for specialized checks
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Whether this is a test/simulation context
        /// </summary>
        public bool IsTestMode { get; set; } = false;

        /// <summary>
        /// Parent context for nested capability checks
        /// </summary>
        public AiPermissionContext? ParentContext { get; set; }
    }

    /// <summary>
    /// User subscription tiers
    /// </summary>
    public enum UserTier
    {
        Free = 0,
        Premium = 1,
        Pro = 2,
        Developer = 3,
        Admin = 99
    }

    /// <summary>
    /// Game modes that affect AI behavior
    /// </summary>
    public enum GameMode
    {
        Default = 0,
        Story = 1,
        Arcade = 2,
        Versus = 3,
        Practice = 4,
        Training = 5,
        Speedrun = 6,
        Creative = 7,
        Challenge = 8,
        Multiplayer = 9,
        Sandbox = 10
    }

    /// <summary>
    /// Types of AI services that can request capabilities
    /// </summary>
    public enum AiServiceType
    {
        Unknown = 0,
        
        // Core AI Services
        Chat = 1,
        Recommendation = 2,
        Analysis = 3,
        
        // Game AI
        Npc = 10,
        Dialogue = 11,
        Quest = 12,
        Combat = 13,
        Narrator = 14,
        
        // Content Generation
        StableDiffusion = 20,
        TextGeneration = 21,
        MusicGeneration = 22,
        
        // System AI
        Orchestrator = 30,
        Memory = 31,
        Validation = 32,
        
        // Feature Services
        LiveCommentary = 40,
        DreamSequence = 41,
        Bmad = 42,
        
        // Development/Testing
        TestHarness = 90,
        Developer = 91
    }

    /// <summary>
    /// AI capabilities that can be gated
    /// </summary>
    public enum AiCapability
    {
        // Basic capabilities
        BasicChat = 0,
        GameAnalysis = 1,
        Recommendations = 2,
        
        // Content generation
        TextGeneration = 10,
        ImageGeneration = 11,
        DialogueGeneration = 12,
        NarrativeGeneration = 13,
        
        // Game modification
        ModifyGameState = 20,
        ModifyEconomy = 21,
        ModifyProgression = 22,
        ModifyCanon = 23,
        
        // NPC behaviors
        NpcDialogue = 30,
        NpcDecisionMaking = 31,
        NpcEmotions = 32,
        NpcMemory = 33,
        
        // Advanced features
        WorldSimulation = 40,
        TimelineManipulation = 41,
        PersonaSwapping = 42,
        TrustModeling = 43,
        
        // Tool usage
        ToolExecution = 50,
        ExternalApiCalls = 51,
        FileSystemAccess = 52,
        DatabaseAccess = 53,
        
        // Administrative
        ConfigurationChange = 60,
        ModelSwapping = 61,
        FeatureFlagOverride = 62,
        
        // Testing
        TestSimulation = 70,
        StressTest = 71,
        EdgeCaseTesting = 72
    }

    /// <summary>
    /// Result of a capability check
    /// </summary>
    public class CapabilityCheckResult
    {
        public bool IsAllowed { get; set; }
        public AiCapability Capability { get; set; }
        public string? DenialReason { get; set; }
        public string? AlternativeAction { get; set; }
        public Dictionary<string, object> Constraints { get; set; } = new();
        
        public static CapabilityCheckResult Allowed(AiCapability capability) => new()
        {
            IsAllowed = true,
            Capability = capability
        };
        
        public static CapabilityCheckResult Denied(AiCapability capability, string reason, string? alternative = null) => new()
        {
            IsAllowed = false,
            Capability = capability,
            DenialReason = reason,
            AlternativeAction = alternative
        };
    }

    /// <summary>
    /// Permission rule definition
    /// </summary>
    public class PermissionRule
    {
        public string RuleId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public AiCapability Capability { get; set; }
        public UserTier MinimumTier { get; set; } = UserTier.Free;
        public List<GameMode> AllowedModes { get; set; } = new();
        public List<AiServiceType> AllowedServices { get; set; } = new();
        
        public bool RequiresExplicitGrant { get; set; } = false;
        public bool IsDisabledGlobally { get; set; } = false;
        
        public int? RateLimitPerMinute { get; set; }
        public int? RateLimitPerHour { get; set; }
        public int? RateLimitPerDay { get; set; }
        
        public Dictionary<string, object> CustomConstraints { get; set; } = new();
    }
}
