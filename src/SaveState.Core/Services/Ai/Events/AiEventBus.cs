using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Events
{
    /// <summary>
    /// Event-driven AI activation.
    /// - Subscribe to world events
    /// - Async reactions
    /// </summary>
    public class AiEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Data { get; set; } = new();
        public int Priority { get; set; } = 0;
    }

    public class AiReaction
    {
        public string EventId { get; set; } = string.Empty;
        public string ReactionType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool ShouldDisplay { get; set; } = true;
        public TimeSpan? Delay { get; set; }
    }

    public delegate Task<AiReaction?> EventHandler(AiEvent evt);

    public interface IAiEventBus
    {
        void Subscribe(string eventType, EventHandler handler);
        void Unsubscribe(string eventType, EventHandler handler);
        Task<List<AiReaction>> PublishAsync(AiEvent evt);
        void QueueEvent(AiEvent evt);
        Task ProcessQueueAsync();
    }

    public class AiEventBus : IAiEventBus
    {
        private readonly Dictionary<string, List<EventHandler>> _handlers = new();
        private readonly Queue<AiEvent> _eventQueue = new();
        private readonly List<AiEvent> _eventHistory = new();
        private readonly int _maxHistory = 100;

        public void Subscribe(string eventType, EventHandler handler)
        {
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<EventHandler>();
            }
            _handlers[eventType].Add(handler);
        }

        public void Unsubscribe(string eventType, EventHandler handler)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        public async Task<List<AiReaction>> PublishAsync(AiEvent evt)
        {
            var reactions = new List<AiReaction>();

            // Record in history
            _eventHistory.Add(evt);
            while (_eventHistory.Count > _maxHistory)
            {
                _eventHistory.RemoveAt(0);
            }

            // Handle specific event type
            if (_handlers.TryGetValue(evt.EventType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        var reaction = await handler(evt);
                        if (reaction != null)
                        {
                            reaction.EventId = evt.Id;
                            reactions.Add(reaction);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Event handler error: {ex.Message}");
                    }
                }
            }

            // Handle wildcard subscribers
            if (_handlers.TryGetValue("*", out var wildcardHandlers))
            {
                foreach (var handler in wildcardHandlers)
                {
                    try
                    {
                        var reaction = await handler(evt);
                        if (reaction != null)
                        {
                            reaction.EventId = evt.Id;
                            reactions.Add(reaction);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Event handler failed: {ex.Message}");
                    }
                }
            }

            return reactions;
        }

        public void QueueEvent(AiEvent evt)
        {
            _eventQueue.Enqueue(evt);
        }

        public async Task ProcessQueueAsync()
        {
            while (_eventQueue.Count > 0)
            {
                var evt = _eventQueue.Dequeue();
                await PublishAsync(evt);
            }
        }

        /// <summary>
        /// Register common game event handlers
        /// </summary>
        public void RegisterDefaultHandlers()
        {
            Subscribe("combat_start", async evt =>
            {
                return await Task.FromResult(new AiReaction
                {
                    ReactionType = "announcement",
                    Content = "Battle begins!",
                    ShouldDisplay = true
                });
            });

            Subscribe("quest_complete", async evt =>
            {
                var questName = evt.Data.TryGetValue("quest_name", out var name) ? name.ToString() : "the quest";
                return await Task.FromResult(new AiReaction
                {
                    ReactionType = "celebration",
                    Content = $"Congratulations! {questName} has been completed!"
                });
            });

            Subscribe("discovery", async evt =>
            {
                return await Task.FromResult(new AiReaction
                {
                    ReactionType = "narration",
                    Content = "You've uncovered something new..."
                });
            });
        }
    }
}
