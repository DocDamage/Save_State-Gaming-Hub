using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SaveState.Core.Services.GameState
{
    using SaveState.Core.Models;

    /// <summary>
    /// Injects world state into every LLM prompt.
    /// - Auto-formats current flags
    /// - Filters relevant state per context
    /// - Prevents contradictions
    /// </summary>
    public class InjectionContext
    {
        public string? CurrentLocation { get; set; }
        public string? CurrentQuest { get; set; }
        public List<string>? RelevantCharacters { get; set; }
        public List<string>? RelevantFactions { get; set; }
        public bool IncludeAllFlags { get; set; } = false;
        public int MaxFlags { get; set; } = 20;
    }

    public interface IStateInjector
    {
        string Inject(string prompt, InjectionContext? context = null);
        string BuildStateSection(InjectionContext? context = null);
        List<string> GetRelevantFlags(InjectionContext? context = null);
        WorldStateSnapshot GetSnapshot(InjectionContext? context = null);
    }

    public class StateInjector : IStateInjector
    {
        private readonly IWorldStateService _worldState;
        private readonly Dictionary<string, List<string>> _flagCategories = new();

        public StateInjector(IWorldStateService worldState)
        {
            _worldState = worldState;
            InitializeCategories();
        }

        private void InitializeCategories()
        {
            _flagCategories["quest"] = new() { "QUEST_", "OBJECTIVE_", "MISSION_" };
            _flagCategories["character"] = new() { "NPC_", "CHAR_", "COMPANION_" };
            _flagCategories["location"] = new() { "AREA_", "LOCATION_", "REGION_", "UNLOCKED_" };
            _flagCategories["combat"] = new() { "ENEMY_", "BOSS_", "BATTLE_" };
            _flagCategories["story"] = new() { "STORY_", "CHAPTER_", "ACT_", "ENDING_" };
            _flagCategories["item"] = new() { "ITEM_", "WEAPON_", "HAS_", "COLLECTED_" };
        }

        public string Inject(string prompt, InjectionContext? context = null)
        {
            var stateSection = BuildStateSection(context);
            
            if (string.IsNullOrEmpty(stateSection))
                return prompt;

            var sb = new StringBuilder();
            sb.AppendLine(stateSection);
            sb.AppendLine();
            sb.AppendLine("=== USER REQUEST ===");
            sb.AppendLine(prompt);

            return sb.ToString();
        }

        public WorldStateSnapshot GetSnapshot(InjectionContext? context = null)
        {
            var state = _worldState.CurrentState;
            var snapshot = new WorldStateSnapshot
            {
                SceneId = context?.CurrentLocation ?? state.CurrentLocation,
                RegionId = state.CurrentLocation, // Simplified mapping
            };

            // Flags
            var relevantFlags = GetRelevantFlags(context);
            foreach (var flag in relevantFlags.Take(context?.MaxFlags ?? 50))
            {
                snapshot.QuestFlags[flag] = _worldState.GetFlag(flag);
            }

            // Counters -> PlayerStats
            var importantCounters = GetImportantCounters();
            foreach (var (key, value) in importantCounters)
            {
                snapshot.PlayerStats[key] = value;
            }

            // Relations -> NpcStates
            if (context?.RelevantCharacters != null)
            {
                var relations = GetCharacterRelations(context.RelevantCharacters);
                foreach (var (character, relation) in relations)
                {
                    snapshot.NpcStates[character] = relation;
                }
            }

            // Timelines
            snapshot.ActiveTimelines = state.Timelines
                .Where(t => t.IsActive)
                .Select(t => t.Name)
                .ToList();

            return snapshot;
        }

        public string BuildStateSection(InjectionContext? context = null)
        {
            var state = _worldState.CurrentState;
            var sb = new StringBuilder();
            
            sb.AppendLine("=== CURRENT WORLD STATE (GROUND TRUTH) ===");
            sb.AppendLine("The following facts are TRUE and must not be contradicted:");
            sb.AppendLine();

            // Location context
            var location = context?.CurrentLocation ?? state.CurrentLocation;
            if (!string.IsNullOrEmpty(location))
            {
                sb.AppendLine($"📍 Current Location: {location}");
            }

            // Active quest
            var quest = context?.CurrentQuest ?? state.CurrentQuest;
            if (!string.IsNullOrEmpty(quest))
            {
                sb.AppendLine($"📋 Active Quest: {quest}");
            }

            // Key flags
            var relevantFlags = GetRelevantFlags(context);
            if (relevantFlags.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("KEY FLAGS:");
                foreach (var flag in relevantFlags.Take(context?.MaxFlags ?? 20))
                {
                    var value = _worldState.GetFlag(flag);
                    sb.AppendLine($"  • {flag}: {(value ? "TRUE" : "FALSE")}");
                }
            }

            // Important counters
            var importantCounters = GetImportantCounters();
            if (importantCounters.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("COUNTERS:");
                foreach (var (key, value) in importantCounters)
                {
                    sb.AppendLine($"  • {key}: {value}");
                }
            }

            // Character relations
            if (context?.RelevantCharacters != null && context.RelevantCharacters.Count > 0)
            {
                var relations = GetCharacterRelations(context.RelevantCharacters);
                if (relations.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("RELATIONSHIPS:");
                    foreach (var (character, relation) in relations)
                    {
                        sb.AppendLine($"  • {character}: {relation}");
                    }
                }
            }

            // Faction standings
            if (context?.RelevantFactions != null && context.RelevantFactions.Count > 0)
            {
                var factions = GetFactionStandings(context.RelevantFactions);
                if (factions.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("FACTION STANDINGS:");
                    foreach (var (faction, standing) in factions)
                    {
                        sb.AppendLine($"  • {faction}: {standing}");
                    }
                }
            }

            // Recent changes
            var recentChanges = _worldState.GetRecentChanges(5).ToList();
            if (recentChanges.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("RECENT WORLD CHANGES:");
                foreach (var change in recentChanges)
                {
                    sb.AppendLine($"  • {change.Key}: {change.OldValue} → {change.NewValue}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== END WORLD STATE ===");
            sb.AppendLine("IMPORTANT: Your response MUST be consistent with the above state.");

            return sb.ToString();
        }

        public List<string> GetRelevantFlags(InjectionContext? context = null)
        {
            var state = _worldState.CurrentState;
            var flags = new List<string>();

            if (context?.IncludeAllFlags == true)
            {
                return state.Flags.Keys.ToList();
            }

            // Filter by location
            if (!string.IsNullOrEmpty(context?.CurrentLocation))
            {
                var locationFlags = state.Flags.Keys
                    .Where(k => k.Contains(context.CurrentLocation, StringComparison.OrdinalIgnoreCase));
                flags.AddRange(locationFlags);
            }

            // Filter by quest
            if (!string.IsNullOrEmpty(context?.CurrentQuest))
            {
                var questFlags = state.Flags.Keys
                    .Where(k => k.Contains(context.CurrentQuest, StringComparison.OrdinalIgnoreCase) ||
                               k.StartsWith("QUEST_", StringComparison.OrdinalIgnoreCase));
                flags.AddRange(questFlags);
            }

            // Filter by relevant characters
            if (context?.RelevantCharacters != null)
            {
                foreach (var character in context.RelevantCharacters)
                {
                    var charFlags = state.Flags.Keys
                        .Where(k => k.Contains(character, StringComparison.OrdinalIgnoreCase));
                    flags.AddRange(charFlags);
                }
            }

            // Add important global flags
            var globalFlags = state.Flags.Keys
                .Where(k => k.StartsWith("STORY_") || k.StartsWith("ACT_") || k.StartsWith("CHAPTER_"));
            flags.AddRange(globalFlags);

            return flags.Distinct().ToList();
        }

        private List<(string Key, int Value)> GetImportantCounters()
        {
            var state = _worldState.CurrentState;
            var importantPrefixes = new[] { "GOLD", "HEALTH", "MANA", "LEVEL", "XP", "REP_", "SCORE" };
            
            return state.Counters
                .Where(kvp => importantPrefixes.Any(p => kvp.Key.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        private List<(string Character, string Relation)> GetCharacterRelations(List<string> characters)
        {
            var state = _worldState.CurrentState;
            var relations = new List<(string, string)>();

            foreach (var character in characters)
            {
                var key = $"REL_{character.ToUpperInvariant()}";
                if (state.Relations.TryGetValue(key, out var relation))
                {
                    relations.Add((character, relation));
                }
            }

            return relations;
        }

        private List<(string Faction, string Standing)> GetFactionStandings(List<string> factions)
        {
            var state = _worldState.CurrentState;
            var standings = new List<(string, string)>();

            foreach (var faction in factions)
            {
                var key = $"FACTION_{faction.ToUpperInvariant()}";
                if (state.Relations.TryGetValue(key, out var standing))
                {
                    standings.Add((faction, standing));
                }
            }

            return standings;
        }
    }
}
