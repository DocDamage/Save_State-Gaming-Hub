using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Events
{
    /// <summary>
    /// Enhanced event bus with priority queues, agent subscriptions, 
    /// background simulation support, and batching.
    /// </summary>
    public interface IEnhancedEventBus : IAiEventBus
    {
        /// <summary>
        /// Subscribe an AI agent to specific event categories
        /// </summary>
        void SubscribeAgent(AiAgent agent);

        /// <summary>
        /// Unsubscribe an AI agent
        /// </summary>
        void UnsubscribeAgent(string agentId);

        /// <summary>
        /// Publish a game event with priority handling
        /// </summary>
        Task<List<AiReaction>> PublishGameEventAsync(GameEvent evt);

        /// <summary>
        /// Start background event processing loop
        /// </summary>
        Task StartBackgroundProcessingAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Get event statistics
        /// </summary>
        EventBusStatistics GetStatistics();

        /// <summary>
        /// Enable/disable event batching
        /// </summary>
        void SetBatchingEnabled(bool enabled);

        /// <summary>
        /// Flush all pending batched events
        /// </summary>
        Task FlushBatchAsync();
    }

    /// <summary>
    /// An AI agent that subscribes to events
    /// </summary>
    public class AiAgent
    {
        public string AgentId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public List<string> SubscribedEventTypes { get; set; } = new();
        public List<EventCategory> SubscribedCategories { get; set; } = new();
        public EventPriority MinimumPriority { get; set; } = EventPriority.Low;
        public Func<GameEvent, Task<AiReaction?>>? Handler { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int MaxConcurrentHandlers { get; set; } = 3;
    }

    /// <summary>
    /// Statistics about event bus usage
    /// </summary>
    public class EventBusStatistics
    {
        public long TotalEventsPublished { get; set; }
        public long TotalReactionsGenerated { get; set; }
        public long EventsInQueue { get; set; }
        public long FailedHandlers { get; set; }
        public Dictionary<string, long> EventTypeCounts { get; set; } = new();
        public Dictionary<string, long> AgentReactionCounts { get; set; } = new();
        public TimeSpan AverageProcessingTime { get; set; }
    }

    /// <summary>
    /// Enhanced event bus implementation
    /// </summary>
    public class EnhancedEventBus : AiEventBus, IEnhancedEventBus
    {
        private readonly ConcurrentDictionary<string, AiAgent> _agents = new();
        private readonly ConcurrentDictionary<EventPriority, ConcurrentQueue<GameEvent>> _priorityQueues = new();
        private readonly ConcurrentBag<GameEvent> _batchedEvents = new();
        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
        
        private bool _batchingEnabled = false;
        private int _batchSize = 10;
        private TimeSpan _batchTimeout = TimeSpan.FromMilliseconds(100);
        
        // Statistics
        private long _totalEventsPublished = 0;
        private long _totalReactionsGenerated = 0;
        private long _failedHandlers = 0;
        private readonly ConcurrentDictionary<string, long> _eventTypeCounts = new();
        private readonly ConcurrentDictionary<string, long> _agentReactionCounts = new();
        private readonly List<TimeSpan> _processingTimes = new();

        public EnhancedEventBus()
        {
            // Initialize priority queues
            foreach (EventPriority priority in Enum.GetValues<EventPriority>())
            {
                _priorityQueues[priority] = new ConcurrentQueue<GameEvent>();
            }
        }

        public void SubscribeAgent(AiAgent agent)
        {
            _agents[agent.AgentId] = agent;

            // Also subscribe to the base event types
            if (agent.Handler != null)
            {
                foreach (var eventType in agent.SubscribedEventTypes)
                {
                    Subscribe(eventType, async evt =>
                    {
                        if (evt is GameEvent gameEvent && agent.IsEnabled)
                        {
                            return await agent.Handler(gameEvent);
                        }
                        return null;
                    });
                }
            }
        }

        public void UnsubscribeAgent(string agentId)
        {
            _agents.TryRemove(agentId, out _);
        }

        public async Task<List<AiReaction>> PublishGameEventAsync(GameEvent evt)
        {
            var startTime = DateTime.UtcNow;
            Interlocked.Increment(ref _totalEventsPublished);
            _eventTypeCounts.AddOrUpdate(evt.EventType, 1, (_, count) => count + 1);

            // Handle batching
            if (_batchingEnabled && evt.CanBeBatched && 
                evt.EventPriority < EventPriority.High)
            {
                _batchedEvents.Add(evt);
                
                if (_batchedEvents.Count >= _batchSize)
                {
                    return await FlushBatchInternalAsync();
                }
                
                return new List<AiReaction>();
            }

            // Direct publish for high priority
            if (evt.RequiresImmediateResponse || evt.EventPriority >= EventPriority.Critical)
            {
                return await ProcessEventAsync(evt);
            }

            // Queue for background processing
            _priorityQueues[evt.EventPriority].Enqueue(evt);
            
            var duration = DateTime.UtcNow - startTime;
            lock (_processingTimes)
            {
                _processingTimes.Add(duration);
                if (_processingTimes.Count > 1000) _processingTimes.RemoveAt(0);
            }

            return new List<AiReaction>();
        }

        public async Task StartBackgroundProcessingAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Process queues in priority order
                    foreach (EventPriority priority in Enum.GetValues<EventPriority>().Reverse())
                    {
                        if (_priorityQueues.TryGetValue(priority, out var queue))
                        {
                            while (queue.TryDequeue(out var evt))
                            {
                                if (cancellationToken.IsCancellationRequested) break;
                                
                                await ProcessEventAsync(evt);
                            }
                        }
                    }

                    // Check batch timeout
                    if (_batchingEnabled && _batchedEvents.Any())
                    {
                        await FlushBatchInternalAsync();
                    }

                    await Task.Delay(16, cancellationToken); // ~60fps tick rate
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Background processing error: {ex.Message}");
                }
            }
        }

        public EventBusStatistics GetStatistics()
        {
            var queueCount = _priorityQueues.Values.Sum(q => q.Count);

            TimeSpan avgTime = TimeSpan.Zero;
            lock (_processingTimes)
            {
                if (_processingTimes.Any())
                {
                    avgTime = TimeSpan.FromTicks(
                        (long)_processingTimes.Average(t => t.Ticks));
                }
            }

            return new EventBusStatistics
            {
                TotalEventsPublished = _totalEventsPublished,
                TotalReactionsGenerated = _totalReactionsGenerated,
                EventsInQueue = queueCount,
                FailedHandlers = _failedHandlers,
                EventTypeCounts = new Dictionary<string, long>(_eventTypeCounts),
                AgentReactionCounts = new Dictionary<string, long>(_agentReactionCounts),
                AverageProcessingTime = avgTime
            };
        }

        public void SetBatchingEnabled(bool enabled)
        {
            _batchingEnabled = enabled;
        }

        public async Task FlushBatchAsync()
        {
            await FlushBatchInternalAsync();
        }

        private async Task<List<AiReaction>> ProcessEventAsync(GameEvent evt)
        {
            var reactions = new List<AiReaction>();

            // Find matching agents
            var matchingAgents = _agents.Values.Where(a =>
                a.IsEnabled &&
                (int)evt.EventPriority >= (int)a.MinimumPriority &&
                (a.SubscribedEventTypes.Contains(evt.EventType) ||
                 a.SubscribedCategories.Contains(evt.Category) ||
                 a.SubscribedEventTypes.Contains("*")));

            var tasks = new List<Task<AiReaction?>>();

            foreach (var agent in matchingAgents)
            {
                if (agent.Handler != null)
                {
                    tasks.Add(SafeInvokeHandlerAsync(agent, evt));
                }
            }

            // Also call base handlers
            var baseReactions = await base.PublishAsync(evt);
            reactions.AddRange(baseReactions);

            // Wait for agent handlers
            var agentReactions = await Task.WhenAll(tasks);
            foreach (var reaction in agentReactions.Where(r => r != null))
            {
                reactions.Add(reaction!);
                Interlocked.Increment(ref _totalReactionsGenerated);
            }

            return reactions;
        }

        private async Task<AiReaction?> SafeInvokeHandlerAsync(AiAgent agent, GameEvent evt)
        {
            try
            {
                if (agent.Handler != null)
                {
                    var reaction = await agent.Handler(evt);
                    if (reaction != null)
                    {
                        _agentReactionCounts.AddOrUpdate(
                            agent.AgentId, 1, (_, count) => count + 1);
                    }
                    return reaction;
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedHandlers);
                Console.WriteLine($"Agent {agent.Name} handler error: {ex.Message}");
            }
            return null;
        }

        private async Task<List<AiReaction>> FlushBatchInternalAsync()
        {
            var reactions = new List<AiReaction>();
            var eventsToProcess = new List<GameEvent>();

            while (_batchedEvents.TryTake(out var evt))
            {
                eventsToProcess.Add(evt);
            }

            foreach (var evt in eventsToProcess)
            {
                var eventReactions = await ProcessEventAsync(evt);
                reactions.AddRange(eventReactions);
            }

            return reactions;
        }

        /// <summary>
        /// Register common game event handlers with enhanced logic
        /// </summary>
        public void RegisterEnhancedHandlers()
        {
            // Register base handlers
            RegisterDefaultHandlers();

            // Enhanced NPC death handler
            SubscribeAgent(new AiAgent
            {
                Name = "NpcDeathNarrator",
                SubscribedEventTypes = new List<string> { GameEvents.NpcDied },
                Handler = evt =>
                {
                    var npcName = evt.Data.TryGetValue("npc_name", out var name) 
                        ? name.ToString() : "An enemy";
                    var wasImportant = evt.Data.TryGetValue("is_important", out var important) 
                        && important is bool b && b;

                    if (wasImportant)
                    {
                        return Task.FromResult<AiReaction?>(new AiReaction
                        {
                            EventId = evt.Id,
                            ReactionType = "dramatic_narration",
                            Content = $"A significant figure falls. {npcName}'s fate has been sealed...",
                            ShouldDisplay = true,
                            Delay = TimeSpan.FromMilliseconds(500)
                        });
                    }
                    return Task.FromResult<AiReaction?>(null);
                }
            });

            // Player level up celebration
            SubscribeAgent(new AiAgent
            {
                Name = "LevelUpCelebrator",
                SubscribedEventTypes = new List<string> { GameEvents.PlayerLevelUp },
                Handler = evt =>
                {
                    var level = evt.Data.TryGetValue("new_level", out var lvl) 
                        ? lvl.ToString() : "?";
                    return Task.FromResult<AiReaction?>(new AiReaction
                    {
                        EventId = evt.Id,
                        ReactionType = "celebration",
                        Content = $"Congratulations! You've reached level {level}!",
                        ShouldDisplay = true
                    });
                }
            });

            // World state observer for background simulation
            SubscribeAgent(new AiAgent
            {
                Name = "WorldSimulator",
                SubscribedCategories = new List<EventCategory> { EventCategory.World },
                MinimumPriority = EventPriority.Background,
                Handler = evt =>
                {
                    // Background world simulation - doesn't display
                    return Task.FromResult<AiReaction?>(new AiReaction
                    {
                        EventId = evt.Id,
                        ReactionType = "simulation_update",
                        ShouldDisplay = false
                    });
                }
            });
        }
    }
}
