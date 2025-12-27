using System;
using System.Collections.Generic;

namespace SaveState.Core.Services.Rules
{
    /// <summary>
    /// Represents a proposed action from the AI or Player.
    /// </summary>
    public class ActionProposal
    {
        public string ActionId { get; set; } = Guid.NewGuid().ToString();
        public string ActionType { get; set; } = string.Empty; // e.g. "ATTACK", "BUY", "QUEST_ACCEPT"
        public string Source { get; set; } = "AI"; // "AI", "PLAYER", "SYSTEM"
        
        public string ActorId { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents the deterministic outcome of an action.
    /// </summary>
    public class ResolutionResult
    {
        public string ActionId { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }
        public bool Success { get; set; }
        
        public string OutcomeDescription { get; set; } = string.Empty; // Narrative result
        public string? FailureReason { get; set; }
        
        /// <summary>
        /// State changes to be applied (Delta).
        /// </summary>
        public StateDiff StateDiff { get; set; } = new();
        
        public List<string> EventsEmitted { get; set; } = new();
    }

    /// <summary>
    /// Represents the difference to apply to the world state.
    /// </summary>
    public class StateDiff
    {
        public Dictionary<string, bool> FlagUpdates { get; set; } = new();
        public Dictionary<string, int> CounterUpdates { get; set; } = new();
        public Dictionary<string, string> RelationUpdates { get; set; } = new();
        public Dictionary<string, string> InventoryUpdates { get; set; } = new(); // ItemId -> "ADD" or "REMOVE" or Quantity
    }

    /// <summary>
    /// Logic for resolving specific actions.
    /// </summary>
    public class ActionResolver
    {
        public string ActionType { get; set; } = string.Empty;
        public Func<GameContext, ResolutionResult> Resolve { get; set; } = _ => new ResolutionResult { Success = false, FailureReason = "No resolver defined" };
    }
}
