using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Core
{
    /// <summary>
    /// Enforces the boundary between deterministic game systems and probabilistic AI.
    /// AI can COLOR reality, never DEFINE it.
    /// 
    /// Deterministic (Protected):
    /// - Game rules
    /// - Economy values
    /// - Progression flags
    /// - Canon state
    /// 
    /// Probabilistic (AI-Creative):
    /// - Dialogue variations
    /// - Flavor text
    /// - Side content
    /// - Emergent moments
    /// </summary>
    public interface IDeterministicBoundary
    {
        /// <summary>
        /// Check if a state modification is allowed by AI
        /// </summary>
        BoundaryCheckResult CheckModification(StateModification modification);

        /// <summary>
        /// Register a protected state path
        /// </summary>
        void ProtectPath(string path, ProtectionLevel level, string reason);

        /// <summary>
        /// Register a zone where AI has creative freedom
        /// </summary>
        void RegisterProbabilisticZone(string zone, ProbabilisticZoneConfig config);

        /// <summary>
        /// Get all protected paths
        /// </summary>
        IEnumerable<ProtectedPath> GetProtectedPaths();

        /// <summary>
        /// Check if a path is protected
        /// </summary>
        bool IsProtected(string path);

        /// <summary>
        /// Get the protection level for a path
        /// </summary>
        ProtectionLevel GetProtectionLevel(string path);
    }

    /// <summary>
    /// A modification request to game state
    /// </summary>
    public class StateModification
    {
        public string Path { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public ModificationSource Source { get; set; }
        public string? Reason { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Source of a modification request
    /// </summary>
    public enum ModificationSource
    {
        GameSystem,
        PlayerAction,
        AiGenerated,
        AdminOverride,
        Migration
    }

    /// <summary>
    /// Result of a boundary check
    /// </summary>
    public class BoundaryCheckResult
    {
        public bool IsAllowed { get; set; }
        public string? DenialReason { get; set; }
        public ProtectionLevel? ViolatedLevel { get; set; }
        public string? AllowedAlternative { get; set; }

        public static BoundaryCheckResult Allowed() => new() { IsAllowed = true };
        
        public static BoundaryCheckResult Denied(string reason, ProtectionLevel level, string? alternative = null)
            => new() 
            { 
                IsAllowed = false, 
                DenialReason = reason, 
                ViolatedLevel = level,
                AllowedAlternative = alternative
            };
    }

    /// <summary>
    /// Levels of protection for state
    /// </summary>
    public enum ProtectionLevel
    {
        /// <summary>No protection, AI can modify freely</summary>
        None = 0,
        
        /// <summary>AI can suggest but not apply changes directly</summary>
        SuggestOnly = 1,
        
        /// <summary>AI can modify within defined constraints</summary>
        Constrained = 2,
        
        /// <summary>Only game systems can modify</summary>
        GameSystemOnly = 3,
        
        /// <summary>Immutable - nothing can modify after initialization</summary>
        Immutable = 4
    }

    /// <summary>
    /// A protected state path
    /// </summary>
    public class ProtectedPath
    {
        public string Path { get; set; } = string.Empty;
        public ProtectionLevel Level { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public bool IsPrefix { get; set; } = false;
    }

    /// <summary>
    /// Configuration for a probabilistic zone
    /// </summary>
    public class ProbabilisticZoneConfig
    {
        public string ZoneName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Allowed creativity level (0-1, higher = more freedom)
        /// </summary>
        public double CreativityLevel { get; set; } = 0.5;
        
        /// <summary>
        /// Must maintain consistency with established facts
        /// </summary>
        public bool RequireConsistency { get; set; } = true;
        
        /// <summary>
        /// Tags that content in this zone can have
        /// </summary>
        public List<string> AllowedTags { get; set; } = new();
        
        /// <summary>
        /// Maximum deviation from established patterns
        /// </summary>
        public double MaxDeviation { get; set; } = 0.3;
    }

    /// <summary>
    /// Default implementation of deterministic boundary
    /// </summary>
    public class DeterministicBoundary : IDeterministicBoundary
    {
        private readonly ConcurrentDictionary<string, ProtectedPath> _protectedPaths = new();
        private readonly ConcurrentDictionary<string, ProbabilisticZoneConfig> _probabilisticZones = new();

        public DeterministicBoundary()
        {
            RegisterDefaultProtections();
            RegisterDefaultZones();
        }

        public BoundaryCheckResult CheckModification(StateModification modification)
        {
            // Find matching protection
            var protection = FindProtection(modification.Path);

            if (protection == null)
            {
                // No protection = allowed
                return BoundaryCheckResult.Allowed();
            }

            switch (protection.Level)
            {
                case ProtectionLevel.None:
                    return BoundaryCheckResult.Allowed();

                case ProtectionLevel.SuggestOnly:
                    if (modification.Source == ModificationSource.AiGenerated)
                    {
                        return BoundaryCheckResult.Denied(
                            $"AI can only suggest modifications to '{modification.Path}'",
                            protection.Level,
                            "Queue this as a suggestion for player/system review"
                        );
                    }
                    return BoundaryCheckResult.Allowed();

                case ProtectionLevel.Constrained:
                    if (modification.Source == ModificationSource.AiGenerated)
                    {
                        // Check constraints (simplified - would have more logic in production)
                        var zone = FindProbabilisticZone(modification.Path);
                        if (zone == null)
                        {
                            return BoundaryCheckResult.Denied(
                                $"No probabilistic zone configured for constrained path '{modification.Path}'",
                                protection.Level
                            );
                        }
                        // Allow within zone constraints
                        return BoundaryCheckResult.Allowed();
                    }
                    return BoundaryCheckResult.Allowed();

                case ProtectionLevel.GameSystemOnly:
                    if (modification.Source == ModificationSource.AiGenerated ||
                        modification.Source == ModificationSource.PlayerAction)
                    {
                        return BoundaryCheckResult.Denied(
                            $"Path '{modification.Path}' can only be modified by game systems: {protection.Reason}",
                            protection.Level
                        );
                    }
                    return BoundaryCheckResult.Allowed();

                case ProtectionLevel.Immutable:
                    if (modification.Source != ModificationSource.Migration)
                    {
                        return BoundaryCheckResult.Denied(
                            $"Path '{modification.Path}' is immutable: {protection.Reason}",
                            protection.Level
                        );
                    }
                    return BoundaryCheckResult.Allowed();

                default:
                    return BoundaryCheckResult.Allowed();
            }
        }

        public void ProtectPath(string path, ProtectionLevel level, string reason)
        {
            _protectedPaths[path] = new ProtectedPath
            {
                Path = path,
                Level = level,
                Reason = reason,
                IsPrefix = path.EndsWith("*")
            };
        }

        public void RegisterProbabilisticZone(string zone, ProbabilisticZoneConfig config)
        {
            config.ZoneName = zone;
            _probabilisticZones[zone] = config;
        }

        public IEnumerable<ProtectedPath> GetProtectedPaths() => _protectedPaths.Values;

        public bool IsProtected(string path)
        {
            var protection = FindProtection(path);
            return protection != null && protection.Level > ProtectionLevel.None;
        }

        public ProtectionLevel GetProtectionLevel(string path)
        {
            var protection = FindProtection(path);
            return protection?.Level ?? ProtectionLevel.None;
        }

        private ProtectedPath? FindProtection(string path)
        {
            // Exact match first
            if (_protectedPaths.TryGetValue(path, out var exact))
            {
                return exact;
            }

            // Then prefix matches
            foreach (var protection in _protectedPaths.Values.Where(p => p.IsPrefix))
            {
                var prefix = protection.Path.TrimEnd('*');
                if (path.StartsWith(prefix))
                {
                    return protection;
                }
            }

            return null;
        }

        private ProbabilisticZoneConfig? FindProbabilisticZone(string path)
        {
            foreach (var zone in _probabilisticZones)
            {
                if (path.StartsWith(zone.Key))
                {
                    return zone.Value;
                }
            }
            return null;
        }

        private void RegisterDefaultProtections()
        {
            // === IMMUTABLE - Canon and Core ===
            ProtectPath("canon.*", ProtectionLevel.Immutable, "Canonical lore cannot be changed");
            ProtectPath("world.history.*", ProtectionLevel.Immutable, "Historical events are immutable");
            ProtectPath("characters.deceased.*", ProtectionLevel.Immutable, "Dead characters cannot be resurrected");

            // === GAME SYSTEM ONLY - Rules and Economy ===
            ProtectPath("rules.*", ProtectionLevel.GameSystemOnly, "Game rules are system-controlled");
            ProtectPath("economy.currency.*", ProtectionLevel.GameSystemOnly, "Currency values are system-controlled");
            ProtectPath("economy.prices.*", ProtectionLevel.GameSystemOnly, "Prices are system-controlled");
            ProtectPath("progression.xp", ProtectionLevel.GameSystemOnly, "XP is earned through gameplay");
            ProtectPath("progression.level", ProtectionLevel.GameSystemOnly, "Level is system-calculated");
            ProtectPath("progression.achievements.*", ProtectionLevel.GameSystemOnly, "Achievements are system-granted");
            ProtectPath("inventory.quantities.*", ProtectionLevel.GameSystemOnly, "Item quantities are system-tracked");
            ProtectPath("combat.stats.*", ProtectionLevel.GameSystemOnly, "Combat stats are system-managed");

            // === CONSTRAINED - AI can modify within limits ===
            ProtectPath("dialogue.*", ProtectionLevel.Constrained, "Dialogue can vary but must be consistent");
            ProtectPath("npc.mood.*", ProtectionLevel.Constrained, "NPC moods can shift within bounds");
            ProtectPath("world.weather", ProtectionLevel.Constrained, "Weather can be influenced");
            ProtectPath("npc.opinions.*", ProtectionLevel.Constrained, "NPC opinions can evolve");

            // === SUGGEST ONLY - AI proposes, player/system approves ===
            ProtectPath("quests.active.*", ProtectionLevel.SuggestOnly, "Quest state changes require confirmation");
            ProtectPath("relationships.*", ProtectionLevel.SuggestOnly, "Relationship changes need player action");
            ProtectPath("factions.standing.*", ProtectionLevel.SuggestOnly, "Faction standing requires events");
        }

        private void RegisterDefaultZones()
        {
            // === HIGH CREATIVITY ZONES ===
            RegisterProbabilisticZone("dialogue.flavor", new ProbabilisticZoneConfig
            {
                Description = "Casual NPC banter and flavor text",
                CreativityLevel = 0.9,
                RequireConsistency = false,
                MaxDeviation = 0.8
            });

            RegisterProbabilisticZone("narration.ambient", new ProbabilisticZoneConfig
            {
                Description = "Environmental descriptions and atmosphere",
                CreativityLevel = 0.85,
                RequireConsistency = true,
                MaxDeviation = 0.6
            });

            RegisterProbabilisticZone("combat.flavor", new ProbabilisticZoneConfig
            {
                Description = "Combat descriptions and effects",
                CreativityLevel = 0.8,
                RequireConsistency = true,
                MaxDeviation = 0.5
            });

            // === MODERATE CREATIVITY ZONES ===
            RegisterProbabilisticZone("dialogue.main", new ProbabilisticZoneConfig
            {
                Description = "Main story dialogue variations",
                CreativityLevel = 0.5,
                RequireConsistency = true,
                MaxDeviation = 0.3
            });

            RegisterProbabilisticZone("quest.descriptions", new ProbabilisticZoneConfig
            {
                Description = "How quests are described to player",
                CreativityLevel = 0.4,
                RequireConsistency = true,
                MaxDeviation = 0.2
            });

            // === LOW CREATIVITY ZONES ===
            RegisterProbabilisticZone("lore.interpretations", new ProbabilisticZoneConfig
            {
                Description = "How established lore is presented",
                CreativityLevel = 0.2,
                RequireConsistency = true,
                MaxDeviation = 0.1
            });
        }
    }
}
