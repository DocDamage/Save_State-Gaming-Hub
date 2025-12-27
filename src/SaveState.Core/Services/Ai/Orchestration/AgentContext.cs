using System;
using System.Collections.Generic;
using SaveState.Core.Models;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.Memory;

namespace SaveState.Core.Services.Ai.Orchestration
{
    public class AgentContext
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        
        // The deterministic snapshot of reality
        public WorldStateSnapshot? WorldState { get; set; }
        
        // Relevant memories retrieved for this request
        public List<Episode> RelevantMemories { get; set; } = new();
        
        // Relevant lore facts validated and retrieved
        public List<LockedLore> RelevantLore { get; set; } = new();
        
        // The classified intent
        public IntentClassification? Intent { get; set; }
        
        // Raw intent classification for advanced usage
        public EnhancedIntentClassification? EnhancedIntent { get; set; }
        
        // Memory profile for the current game (cheat/memory specialist use)
        public GameMemoryProfile? GameMemoryProfile { get; set; }
        
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
