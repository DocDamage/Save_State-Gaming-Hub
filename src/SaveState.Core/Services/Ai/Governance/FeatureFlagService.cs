using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Governance
{
    /// <summary>
    /// Feature flag service for granular AI feature control.
    /// Supports per-user, per-tier, per-game, per-mode, per-service, and per-module flags.
    /// </summary>
    public interface IFeatureFlagService
    {
        /// <summary>
        /// Check if a feature is enabled for the given context
        /// </summary>
        Task<bool> IsEnabledAsync(string featureKey, AiPermissionContext context);

        /// <summary>
        /// Get feature configuration value
        /// </summary>
        Task<T?> GetValueAsync<T>(string featureKey, AiPermissionContext context);

        /// <summary>
        /// Set a feature flag value (requires admin permission)
        /// </summary>
        void SetFlag(FeatureFlag flag);

        /// <summary>
        /// Remove a feature flag
        /// </summary>
        void RemoveFlag(string featureKey);

        /// <summary>
        /// Get all feature flags
        /// </summary>
        IEnumerable<FeatureFlag> GetAllFlags();

        /// <summary>
        /// Get flags applicable to a context
        /// </summary>
        IEnumerable<FeatureFlag> GetFlagsForContext(AiPermissionContext context);
    }

    /// <summary>
    /// Feature flag definition
    /// </summary>
    public class FeatureFlag
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Default enabled state when no specific rule matches
        /// </summary>
        public bool DefaultEnabled { get; set; } = true;

        /// <summary>
        /// The typed value of this flag (for non-boolean flags)
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// User IDs this flag applies to (empty = all users)
        /// </summary>
        public HashSet<string> UserIds { get; set; } = new();

        /// <summary>
        /// Tiers this flag applies to (empty = all tiers)
        /// </summary>
        public HashSet<UserTier> Tiers { get; set; } = new();

        /// <summary>
        /// Game IDs this flag applies to (empty = all games)
        /// </summary>
        public HashSet<string> GameIds { get; set; } = new();

        /// <summary>
        /// Game modes this flag applies to (empty = all modes)
        /// </summary>
        public HashSet<GameMode> Modes { get; set; } = new();

        /// <summary>
        /// Service types this flag applies to (empty = all services)
        /// </summary>
        public HashSet<AiServiceType> Services { get; set; } = new();

        /// <summary>
        /// Module names this flag applies to (empty = all modules)
        /// </summary>
        public HashSet<string> Modules { get; set; } = new();

        /// <summary>
        /// Percentage of users to enable for (0-100, null = not a rollout)
        /// </summary>
        public int? RolloutPercentage { get; set; }

        /// <summary>
        /// When this flag starts being active
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// When this flag stops being active
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Flag category for organization
        /// </summary>
        public FeatureFlagCategory Category { get; set; } = FeatureFlagCategory.General;

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Categories for organizing feature flags
    /// </summary>
    public enum FeatureFlagCategory
    {
        General,
        Experimental,
        Beta,
        Deprecated,
        Security,
        Performance,
        Ai,
        Game,
        Ui,
        Integration
    }

    /// <summary>
    /// Default implementation of feature flag service
    /// </summary>
    public class FeatureFlagService : IFeatureFlagService
    {
        private readonly ConcurrentDictionary<string, FeatureFlag> _flags = new();
        private readonly Random _random = new();

        public FeatureFlagService()
        {
            RegisterDefaultFlags();
        }

        public Task<bool> IsEnabledAsync(string featureKey, AiPermissionContext context)
        {
            if (!_flags.TryGetValue(featureKey, out var flag))
            {
                // Unknown flag = assume enabled
                return Task.FromResult(true);
            }

            // Check time constraints
            var now = DateTime.UtcNow;
            if (flag.StartTime.HasValue && now < flag.StartTime.Value)
            {
                return Task.FromResult(false);
            }
            if (flag.EndTime.HasValue && now > flag.EndTime.Value)
            {
                return Task.FromResult(false);
            }

            // Check specific user override
            if (flag.UserIds.Any() && !flag.UserIds.Contains(context.UserId))
            {
                return Task.FromResult(!flag.DefaultEnabled);
            }

            // Check tier constraint
            if (flag.Tiers.Any() && !flag.Tiers.Contains(context.Tier))
            {
                return Task.FromResult(false);
            }

            // Check game constraint
            if (flag.GameIds.Any() && context.GameId != null && !flag.GameIds.Contains(context.GameId))
            {
                return Task.FromResult(false);
            }

            // Check mode constraint
            if (flag.Modes.Any() && !flag.Modes.Contains(context.Mode))
            {
                return Task.FromResult(false);
            }

            // Check service constraint
            if (flag.Services.Any() && !flag.Services.Contains(context.RequestingService))
            {
                return Task.FromResult(false);
            }

            // Check module constraint
            if (flag.Modules.Any() && context.ModuleName != null && !flag.Modules.Contains(context.ModuleName))
            {
                return Task.FromResult(false);
            }

            // Check rollout percentage
            if (flag.RolloutPercentage.HasValue)
            {
                // Use deterministic hash based on user ID for consistent rollout
                var hash = Math.Abs(context.UserId.GetHashCode()) % 100;
                if (hash >= flag.RolloutPercentage.Value)
                {
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(flag.DefaultEnabled);
        }

        public Task<T?> GetValueAsync<T>(string featureKey, AiPermissionContext context)
        {
            if (!_flags.TryGetValue(featureKey, out var flag))
            {
                return Task.FromResult(default(T));
            }

            if (flag.Value is T typedValue)
            {
                return Task.FromResult<T?>(typedValue);
            }

            return Task.FromResult(default(T));
        }

        public void SetFlag(FeatureFlag flag)
        {
            _flags[flag.Key] = flag;
        }

        public void RemoveFlag(string featureKey)
        {
            _flags.TryRemove(featureKey, out _);
        }

        public IEnumerable<FeatureFlag> GetAllFlags() => _flags.Values;

        public IEnumerable<FeatureFlag> GetFlagsForContext(AiPermissionContext context)
        {
            return _flags.Values.Where(f =>
                (!f.UserIds.Any() || f.UserIds.Contains(context.UserId)) &&
                (!f.Tiers.Any() || f.Tiers.Contains(context.Tier)) &&
                (!f.Modes.Any() || f.Modes.Contains(context.Mode)) &&
                (!f.Services.Any() || f.Services.Contains(context.RequestingService))
            );
        }

        private void RegisterDefaultFlags()
        {
            // === AI Feature Flags ===
            SetFlag(new FeatureFlag
            {
                Key = "ai.advanced_memory",
                Name = "Advanced Memory System",
                Description = "Enable episodic and canonical memory layers",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Ai
            });

            SetFlag(new FeatureFlag
            {
                Key = "ai.persona_swapping",
                Name = "Dynamic Persona Swapping",
                Description = "Allow NPCs to change personality states dynamically",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Ai,
                Tiers = new HashSet<UserTier> { UserTier.Premium, UserTier.Pro, UserTier.Developer, UserTier.Admin }
            });

            SetFlag(new FeatureFlag
            {
                Key = "ai.trust_modeling",
                Name = "Player Trust Modeling",
                Description = "Track player behavior and adjust NPC interactions",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Ai
            });

            SetFlag(new FeatureFlag
            {
                Key = "ai.streaming_responses",
                Name = "Streaming AI Responses",
                Description = "Stream AI responses as they're generated",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Performance
            });

            // === Game Feature Flags ===
            SetFlag(new FeatureFlag
            {
                Key = "game.live_commentary",
                Name = "Live AI Commentary",
                Description = "Enable real-time AI commentary during gameplay",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Game,
                Modes = new HashSet<GameMode> 
                { 
                    GameMode.Story, GameMode.Arcade, GameMode.Versus, GameMode.Practice 
                }
            });

            SetFlag(new FeatureFlag
            {
                Key = "game.dream_sequences",
                Name = "Dream Sequence Generation",
                Description = "Generate AI-powered dream sequences",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Game,
                Modes = new HashSet<GameMode> { GameMode.Story }
            });

            SetFlag(new FeatureFlag
            {
                Key = "game.dynamic_difficulty",
                Name = "AI-Driven Dynamic Difficulty",
                Description = "Adjust difficulty based on player performance",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Game
            });

            // === Experimental Flags ===
            SetFlag(new FeatureFlag
            {
                Key = "experimental.new_intent_classifier",
                Name = "Enhanced Intent Classifier",
                Description = "Use the new intent classification model",
                DefaultEnabled = false,
                Category = FeatureFlagCategory.Experimental,
                RolloutPercentage = 10
            });

            SetFlag(new FeatureFlag
            {
                Key = "experimental.world_simulation",
                Name = "Background World Simulation",
                Description = "Simulate world events when player is not present",
                DefaultEnabled = false,
                Category = FeatureFlagCategory.Experimental,
                Tiers = new HashSet<UserTier> { UserTier.Developer, UserTier.Admin }
            });

            // === Security Flags ===
            SetFlag(new FeatureFlag
            {
                Key = "security.strict_content_filter",
                Name = "Strict Content Filtering",
                Description = "Apply stricter content filtering to AI outputs",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Security
            });

            SetFlag(new FeatureFlag
            {
                Key = "security.audit_all_requests",
                Name = "Full Request Auditing",
                Description = "Log all AI requests for security review",
                DefaultEnabled = false,
                Category = FeatureFlagCategory.Security,
                Tiers = new HashSet<UserTier> { UserTier.Admin }
            });

            // === Service-Specific Flags ===
            SetFlag(new FeatureFlag
            {
                Key = "service.npc.enhanced_emotions",
                Name = "Enhanced NPC Emotions",
                Description = "Enable advanced emotional modeling for NPCs",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Ai,
                Services = new HashSet<AiServiceType> { AiServiceType.Npc, AiServiceType.Dialogue }
            });

            SetFlag(new FeatureFlag
            {
                Key = "service.orchestrator.parallel_agents",
                Name = "Parallel Agent Execution",
                Description = "Allow orchestrator to run multiple agents in parallel",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Performance,
                Services = new HashSet<AiServiceType> { AiServiceType.Orchestrator }
            });

            // === Integration Flags ===
            SetFlag(new FeatureFlag
            {
                Key = "integration.ollama",
                Name = "Ollama Integration",
                Description = "Enable local Ollama model support",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Integration
            });

            SetFlag(new FeatureFlag
            {
                Key = "integration.stable_diffusion",
                Name = "Stable Diffusion Integration",
                Description = "Enable image generation via Stable Diffusion",
                DefaultEnabled = true,
                Category = FeatureFlagCategory.Integration,
                Tiers = new HashSet<UserTier> { UserTier.Premium, UserTier.Pro, UserTier.Developer, UserTier.Admin }
            });
        }
    }
}
