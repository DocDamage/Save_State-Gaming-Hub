using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Events;

namespace SaveState.Core.Services.Ai.Memory
{
    public interface IMemoryWriterService
    {
        void RegisterSubscription();
    }

    /// <summary>
    /// Subscribes to the Event Bus and writes significant events to Episodic Memory.
    /// Implements the "Write Pipeline": Capture -> Summarize -> Store
    /// </summary>
    public class MemoryWriterService : IMemoryWriterService
    {
        private readonly IEnhancedEventBus _eventBus;
        private readonly IEpisodicMemory _episodicMemory;
        
        // We might want an LLM service here for summarization, 
        // but for V1 we can use heuristic extraction to save tokens/latency.
        // private readonly ILlmService _llm; 

        public MemoryWriterService(IEnhancedEventBus eventBus, IEpisodicMemory episodicMemory)
        {
            _eventBus = eventBus;
            _episodicMemory = episodicMemory;
        }

        public void RegisterSubscription()
        {
            _eventBus.SubscribeAgent(new AiAgent
            {
                Name = "EpisodicMemoryWriter",
                // Subscribe to all categories, but filter in handler
                SubscribedEventTypes = new List<string> { "*" }, 
                MinimumPriority = EventPriority.Normal, // Ignore low priority background noise
                Handler = HandleEventAsync
            });
        }

        private async Task<AiReaction?> HandleEventAsync(GameEvent evt)
        {
            // 1. Filter: specific events we definitely capture
            if (!IsMemorizable(evt))
            {
                return null;
            }

            // 2. Extract/Summarize
            var (description, context, outcome) = ExtractDetails(evt);
            var emotion = DetermineEmotion(evt);
            var significance = DetermineSignificance(evt);
            var tags = ExtractTags(evt);

            // 3. Store
            await _episodicMemory.RecordEpisode(
                eventDesc: description,
                context: context,
                outcome: outcome,
                emotion: emotion,
                significance: significance,
                tags: tags,
                worldState: evt.Data // Capture raw data as world state snapshot
            );

            // No reaction needed, this is a passive observer
            return null;
        }

        private bool IsMemorizable(GameEvent evt)
        {
            // Filter list
            var significantEvents = new HashSet<string>
            {
                GameEvents.QuestStarted,
                GameEvents.QuestCompleted,
                GameEvents.QuestFailed,
                GameEvents.BossEncounter,
                GameEvents.EnemyDefeated,
                GameEvents.ItemAcquired,
                GameEvents.RegionDiscovered,
                GameEvents.PlayerAchievement,
                GameEvents.PlayerDied,
                "DIALOGUE_IMPORTANT"
            };

            return significantEvents.Contains(evt.EventType) || evt.EventPriority >= EventPriority.High;
        }

        private (string Desc, string Context, string Outcome) ExtractDetails(GameEvent evt)
        {
            // In a full implementation, this calls an LLM to summarize the JSON payload.
            // For V1, we stringify core fields.
            string desc = evt.EventType;
            string context = $"Location: {GetDetail(evt, "location")}, Timestamp: {evt.Timestamp}";
            string outcome = GetDetail(evt, "result") ?? GetDetail(evt, "outcome") ?? "Event occurred";

            if (evt.Data.ContainsKey("summary"))
            {
                desc = evt.Data["summary"].ToString() ?? desc;
            }
            else if (evt.Data.ContainsKey("message"))
            {
                desc = evt.Data["message"].ToString() ?? desc;
            }

            return (desc, context, outcome);
        }

        private string? GetDetail(GameEvent evt, string key)
        {
            return evt.Data.TryGetValue(key, out var val) ? val?.ToString() : null;
        }

        private EmotionalTone DetermineEmotion(GameEvent evt)
        {
            // Heuristic mapping
            return evt.EventType switch
            {
                var t when t.Contains("DIED") || t.Contains("FAILED") => EmotionalTone.Defeat,
                var t when t.Contains("COMPLETED") || t.Contains("WON") || t.Contains("ACHIEVEMENT") => EmotionalTone.Victory,
                var t when t.Contains("DANGER") || t.Contains("ATTACK") => EmotionalTone.Tense,
                var t when t.Contains("DISCOVERED") => EmotionalTone.Curiosity,
                _ => EmotionalTone.Neutral
            };
        }

        private float DetermineSignificance(GameEvent evt)
        {
            if (evt.EventPriority == EventPriority.Critical) return 1.0f;
            if (evt.EventPriority == EventPriority.High) return 0.8f;
            
            // Event type modifiers
            if (evt.EventType.Contains("BOSS")) return 0.9f;
            if (evt.EventType == "QUEST_COMPLETED") return 0.7f;
            
            return 0.5f;
        }

        private List<string> ExtractTags(GameEvent evt)
        {
            var tags = new List<string> { evt.EventType, evt.Category.ToString() };
            if (evt.SourceEntityId != null) tags.Add(evt.SourceEntityId);
            return tags;
        }
    }
}
