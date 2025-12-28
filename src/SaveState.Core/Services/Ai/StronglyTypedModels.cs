using System;
using System.Collections.Generic;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Strongly-typed models to replace Dictionary&lt;string, object&gt; primitive obsession.
    /// Phase 4: Technical Debt Elimination
    /// </summary>

    // ========================
    // World State Models
    // ========================

    /// <summary>
    /// Strongly-typed world state representation.
    /// Replaces Dictionary&lt;string, object&gt; WorldState
    /// </summary>
    public class WorldStateData
    {
        public string? CurrentScene { get; set; }
        public string? CurrentLocation { get; set; }
        public string? TimeOfDay { get; set; }
        public string? Weather { get; set; }
        public int? GameDay { get; set; }
        public bool IsPaused { get; set; }
        public Dictionary<string, bool> Flags { get; set; } = new();
        public Dictionary<string, int> Variables { get; set; } = new();
        public List<string> ActiveQuests { get; set; } = new();
        public List<string> CompletedQuests { get; set; } = new();
    }

    // ========================
    // Player State Models
    // ========================

    /// <summary>
    /// Strongly-typed player state representation.
    /// Replaces Dictionary&lt;string, object&gt; PlayerState
    /// </summary>
    public class PlayerStateData
    {
        public int? Level { get; set; }
        public int? Experience { get; set; }
        public int? Health { get; set; }
        public int? MaxHealth { get; set; }
        public int? Mana { get; set; }
        public int? MaxMana { get; set; }
        public int? Gold { get; set; }
        public string? CurrentClass { get; set; }
        public List<string> Inventory { get; set; } = new();
        public List<string> EquippedItems { get; set; } = new();
        public Dictionary<string, int> Stats { get; set; } = new();
        public Dictionary<string, int> Skills { get; set; } = new();
    }

    // ========================
    // Request Metadata Models
    // ========================

    /// <summary>
    /// Strongly-typed request metadata.
    /// Replaces Dictionary&lt;string, object&gt; Metadata in requests
    /// </summary>
    public class AiRequestMetadata
    {
        public string? Source { get; set; }
        public string? Feature { get; set; }
        public int? RetryCount { get; set; }
        public bool IsUserInitiated { get; set; } = true;
        public string? CorrelationId { get; set; }
        public DateTime? RequestedAt { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
        public Dictionary<string, int> Metrics { get; set; } = new();
    }

    // ========================
    // Response Metadata Models
    // ========================

    /// <summary>
    /// Strongly-typed additional response metadata.
    /// Replaces Dictionary&lt;string, object&gt; Additional in ProductionAiResponseMetadata
    /// </summary>
    public class AdditionalResponseMetadata
    {
        public string? CacheSource { get; set; }
        public bool WasRegenerated { get; set; }
        public int? SimilarityScore { get; set; }
        public List<string> AppliedFilters { get; set; } = new();
        public string? ValidationDetails { get; set; }
        public Dictionary<string, double> ConfidenceBreakdown { get; set; } = new();
        public Dictionary<string, string> CustomTags { get; set; } = new();
    }

    // ========================
    // Stage Metadata Models
    // ========================

    /// <summary>
    /// Strongly-typed pipeline stage metadata.
    /// Replaces Dictionary&lt;string, object&gt; Metadata in PipelineStageResult
    /// </summary>
    public class StageMetadata
    {
        public string? ProcessorType { get; set; }
        public int? ItemsProcessed { get; set; }
        public long? MemoryUsedBytes { get; set; }
        public bool WasCached { get; set; }
        public string? CacheKey { get; set; }
        public Dictionary<string, double> Timings { get; set; } = new();
        public Dictionary<string, int> Counters { get; set; } = new();
    }

    // ========================
    // Pipeline Context Data Models
    // ========================

    /// <summary>
    /// Strongly-typed pipeline context data.
    /// Replaces Dictionary&lt;string, object&gt; Data in PipelineContext
    /// </summary>
    public class PipelineContextData
    {
        public string? SessionId { get; set; }
        public string? UserId { get; set; }
        public string? AgentId { get; set; }
        public string? Intent { get; set; }
        public float? QualityScore { get; set; }
        public string? ExperimentVariant { get; set; }
        public WorldStateData? WorldState { get; set; }
        public PlayerStateData? PlayerState { get; set; }
        public List<string> AppliedTransformations { get; set; } = new();
        public Dictionary<string, object> CustomData { get; set; } = new();
    }

    // ========================
    // Experiment Variant Config Models
    // ========================

    /// <summary>
    /// Strongly-typed experiment variant configuration.
    /// Replaces Dictionary&lt;string, object&gt; Config in ExperimentVariant
    /// </summary>
    public class VariantConfiguration
    {
        public float? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public string? Model { get; set; }
        public string? SystemPromptOverride { get; set; }
        public bool EnableCaching { get; set; } = true;
        public bool EnableValidation { get; set; } = true;
        public Dictionary<string, float> ParameterOverrides { get; set; } = new();
        public Dictionary<string, bool> FeatureFlags { get; set; } = new();
    }

    // ========================
    // Observability Event Data Models
    // ========================

    /// <summary>
    /// Strongly-typed observability event data.
    /// Replaces Dictionary&lt;string, object&gt; Data in ObservabilityData
    /// </summary>
    public class EventData
    {
        public int? InputLength { get; set; }
        public int? OutputLength { get; set; }
        public double? LatencyMs { get; set; }
        public string? Status { get; set; }
        public string? ExperimentVariant { get; set; }
        public string? ModelUsed { get; set; }
        public int? TokensConsumed { get; set; }
        public float? ConfidenceScore { get; set; }
        public bool WasCached { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    // ========================
    // Tool/Agent Context Models
    // ========================

    /// <summary>
    /// Strongly-typed tool execution parameters.
    /// Replaces Dictionary&lt;string, object&gt; Parameters in tool calls
    /// </summary>
    public class ToolParameters
    {
        public string? Query { get; set; }
        public string? Target { get; set; }
        public int? Count { get; set; }
        public int? Limit { get; set; }
        public bool? StrictMode { get; set; }
        public Dictionary<string, string> StringParams { get; set; } = new();
        public Dictionary<string, int> NumericParams { get; set; } = new();
        public Dictionary<string, bool> BooleanParams { get; set; } = new();
    }

    /// <summary>
    /// Strongly-typed agent routing context.
    /// Replaces Dictionary&lt;string, object&gt; Constraints/Context in agent routing
    /// </summary>
    public class AgentRoutingContext
    {
        public string? PreferredAgent { get; set; }
        public List<string> ExcludedAgents { get; set; } = new();
        public int? MaxConcurrentAgents { get; set; }
        public TimeSpan? Timeout { get; set; }
        public float? MinConfidenceThreshold { get; set; }
        public bool RequireSpecialization { get; set; }
        public Dictionary<string, object> RoutingHints { get; set; } = new();
    }

    // ========================
    // Hallucination Detection Models
    // ========================

    /// <summary>
    /// Strongly-typed hallucination check context.
    /// Replaces Dictionary&lt;string, object&gt; WorldState in hallucination detection
    /// </summary>
    public class KnownFactsContext
    {
        public List<string> CanonicalFacts { get; set; } = new();
        public List<string> VerifiedEntities { get; set; } = new();
        public Dictionary<string, string> EntityAttributes { get; set; } = new();
        public List<string> EstablishedRelationships { get; set; } = new();
        public HashSet<string> ValidatedLocations { get; set; } = new();
        public Dictionary<string, object> DomainKnowledge { get; set; } = new();
    }
}
