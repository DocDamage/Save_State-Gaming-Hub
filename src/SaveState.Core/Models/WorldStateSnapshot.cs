using System;
using System.Collections.Generic;

namespace SaveState.Core.Models
{
    /// <summary>
    /// Represents a minimal, deterministic snapshot of the world state 
    /// injected into the AI context for reality discipline.
    /// </summary>
    public class WorldStateSnapshot
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? SceneId { get; set; }
        public string? RegionId { get; set; }
        
        /// <summary>
        /// Flags relevant to the current context (Quests, Unlocks).
        /// </summary>
        public Dictionary<string, bool> QuestFlags { get; set; } = new();
        
        /// <summary>
        /// Status of relevant NPCs (Alive/Dead, Disposition).
        /// </summary>
        public Dictionary<string, string> NpcStates { get; set; } = new();
        
        /// <summary>
        /// Player reputation, stats, resources.
        /// </summary>
        public Dictionary<string, int> PlayerStats { get; set; } = new();
        
        /// <summary>
        /// Summary of relevant inventory items.
        /// </summary>
        public Dictionary<string, string> InventorySummary { get; set; } = new();
        
        /// <summary>
        /// Active global effects or timelines.
        /// </summary>
        public List<string> ActiveTimelines { get; set; } = new();
    }
}
