using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.Ai.Orchestration;
using SaveState.Core.Services.Ai.Prompts;
using SaveState.Core.Services.Ai.Validation;
using SaveState.Core.Services.Ai.Uncertainty;
using SaveState.Core.Services.Ai.Models;
using SaveState.Core.Services.Ai.Events;
using SaveState.Core.Services.Ai.Emotion;
using SaveState.Core.Services.GameState;
using SaveState.Core.Services.Player;
using SaveState.Core.Services.Rules;
using SaveState.Core.Services.Timeline;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Unified AI Orchestration Service that wires together all AI architecture components.
    /// This is the main entry point for all AI-powered features in SaveState.
    /// Refactored as a Facade delegating to focused components.
    /// </summary>
    public interface IAdvancedAiService
    {
        // Core AI capabilities
        Task<AiResponse> ProcessAsync(string input, AiRequestContext? context = null);
        Task<string> GenerateNarrativeAsync(string prompt, NarrativeContext? context = null);
        Task<string> GenerateCommentaryAsync(string gameEvent, CommentaryContext? context = null);

        // Memory operations
        Task RecordInteractionAsync(string input, string output, string? context = null);
        Task<string> GetContextualMemoryAsync(string query);

        // State management
        void UpdateWorldState(string key, object value, string? source = null);
        WorldState GetCurrentWorldState();

        // Player modeling
        Task UpdatePlayerModelAsync(PlayerAction action);
        Task<PlayerProfile> GetPlayerProfileAsync(string playerId);

        // Timeline operations
        void CreateSavePoint(string name, string? description = null);
        Task<WhatIfResult> SimulateWhatIfAsync(string scenario);

        // Validation
        Task<ActionValidationResult> ValidateActionAsync(ProposedAction action);

        // Events
        void SubscribeToEvent(string eventType, Events.EventHandler handler);
        Task PublishEventAsync(AiEvent evt);

        // Configuration
        void Configure(AdvancedAiConfig config);
        bool IsInitialized { get; }
        Task InitializeAsync();
    }

    public class AiResponse
    {
        public string Content { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string? Agent { get; set; }
        public IntentCategory Intent { get; set; }
        public bool WasValidated { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class AiRequestContext
    {
        public string? SessionId { get; set; }
        public string? CurrentScene { get; set; }
        public string? CurrentQuest { get; set; }
        public List<string>? RelevantCharacters { get; set; }
        public string? RequestType { get; set; }
        public bool RequireValidation { get; set; } = true;
        public bool InjectWorldState { get; set; } = true;
    }

    public class NarrativeContext
    {
        public string? Location { get; set; }
        public string? Mood { get; set; }
        public string? TimeOfDay { get; set; }
        public string? ActiveQuest { get; set; }
        public List<string>? Characters { get; set; }
    }

    public class CommentaryContext
    {
        public string? GameTitle { get; set; }
        public string? PlayerAction { get; set; }
        public int? Score { get; set; }
        public int? Combo { get; set; }
        public TimeSpan? SessionDuration { get; set; }
    }

    public class AdvancedAiConfig
    {
        public bool EnableStateInjection { get; set; } = true;
        public bool EnableMemoryOrchestration { get; set; } = true;
        public bool EnablePlayerModeling { get; set; } = true;
        public bool EnableValidation { get; set; } = true;
        public bool EnableConfidenceScoring { get; set; } = true;
        public bool EnableTimeline { get; set; } = true;
        public string DefaultPlayerId { get; set; } = "default";
        public float MinConfidenceThreshold { get; set; } = 0.7f;
    }

    /// <summary>
    /// Refactored Advanced AI Service - now a lightweight facade delegating to focused components.
    /// Reduced from 506 lines to ~120 lines.
    /// </summary>
    public class AdvancedAiService : IAdvancedAiService
    {
        // Focused components
        private readonly IAiRequestProcessor _requestProcessor;
        private readonly IAiMemoryCoordinator _memoryCoordinator;
        private readonly IAiWorldStateCoordinator _worldStateCoordinator;
        private readonly IAiValidationCoordinator _validationCoordinator;
        private readonly IAiNarrativeGenerator _narrativeGenerator;
        private readonly IAiTimelineCoordinator _timelineCoordinator;
        private readonly IAiEventCoordinator _eventCoordinator;

        // Core services for initialization
        private readonly ILlmService _llmService;
        private readonly IWorldStateService _worldStateService;
        private readonly IPlayerModelService _playerModelService;
        private readonly IEpisodicMemory _episodicMemory;
        private readonly ICanonicalMemory _canonicalMemory;
        private readonly IAiEventBus _eventBus;

        // Configuration
        private AdvancedAiConfig _config = new();
        private bool _initialized = false;

        public bool IsInitialized => _initialized;

        public AdvancedAiService(
            IAiRequestProcessor requestProcessor,
            IAiMemoryCoordinator memoryCoordinator,
            IAiWorldStateCoordinator worldStateCoordinator,
            IAiValidationCoordinator validationCoordinator,
            IAiNarrativeGenerator narrativeGenerator,
            IAiTimelineCoordinator timelineCoordinator,
            IAiEventCoordinator eventCoordinator,
            ILlmService llmService,
            IWorldStateService worldStateService,
            IPlayerModelService playerModelService,
            IEpisodicMemory episodicMemory,
            ICanonicalMemory canonicalMemory,
            IAiEventBus eventBus)
        {
            _requestProcessor = requestProcessor;
            _memoryCoordinator = memoryCoordinator;
            _worldStateCoordinator = worldStateCoordinator;
            _validationCoordinator = validationCoordinator;
            _narrativeGenerator = narrativeGenerator;
            _timelineCoordinator = timelineCoordinator;
            _eventCoordinator = eventCoordinator;
            _llmService = llmService;
            _worldStateService = worldStateService;
            _playerModelService = playerModelService;
            _episodicMemory = episodicMemory;
            _canonicalMemory = canonicalMemory;
            _eventBus = eventBus;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;

            // Initialize LLM service
            await _llmService.InitializeAsync();

            // Load persisted data
            await _worldStateService.LoadAsync();
            await _playerModelService.LoadAsync();
            await _episodicMemory.LoadAsync();
            await _canonicalMemory.LoadAsync();

            // Register default event handlers
            ((AiEventBus)_eventBus).RegisterDefaultHandlers();

            _initialized = true;
        }

        public void Configure(AdvancedAiConfig config)
        {
            _config = config;
        }

        // Core AI capabilities (delegated)
        public async Task<AiResponse> ProcessAsync(string input, AiRequestContext? context = null)
        {
            if (!_initialized) await InitializeAsync();
            return await _requestProcessor.ProcessAsync(input, context);
        }

        public Task<string> GenerateNarrativeAsync(string prompt, NarrativeContext? context = null)
            => _narrativeGenerator.GenerateNarrativeAsync(prompt, context);

        public Task<string> GenerateCommentaryAsync(string gameEvent, CommentaryContext? context = null)
            => _narrativeGenerator.GenerateCommentaryAsync(gameEvent, context);

        // Memory operations (delegated)
        public Task RecordInteractionAsync(string input, string output, string? context = null)
            => _memoryCoordinator.RecordInteractionAsync(input, output, context);

        public Task<string> GetContextualMemoryAsync(string query)
            => _memoryCoordinator.GetContextualMemoryAsync(query);

        // State management (delegated)
        public void UpdateWorldState(string key, object value, string? source = null)
            => _worldStateCoordinator.UpdateWorldState(key, value, source);

        public WorldState GetCurrentWorldState()
            => _worldStateCoordinator.GetCurrentWorldState();

        public Task UpdatePlayerModelAsync(PlayerAction action)
            => _worldStateCoordinator.UpdatePlayerModelAsync(action);

        public Task<PlayerProfile> GetPlayerProfileAsync(string playerId)
            => _worldStateCoordinator.GetPlayerProfileAsync(playerId);

        // Timeline operations (delegated)
        public void CreateSavePoint(string name, string? description = null)
            => _timelineCoordinator.CreateSavePoint(name, description);

        public Task<WhatIfResult> SimulateWhatIfAsync(string scenario)
            => _timelineCoordinator.SimulateWhatIfAsync(scenario);

        // Validation (delegated)
        public Task<ActionValidationResult> ValidateActionAsync(ProposedAction action)
            => _validationCoordinator.ValidateActionAsync(action);

        // Events (delegated)
        public void SubscribeToEvent(string eventType, Events.EventHandler handler)
            => _eventCoordinator.SubscribeToEvent(eventType, handler);

        public Task PublishEventAsync(AiEvent evt)
            => _eventCoordinator.PublishEventAsync(evt);
    }
}
