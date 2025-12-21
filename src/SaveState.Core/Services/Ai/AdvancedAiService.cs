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

    public class AdvancedAiService : IAdvancedAiService
    {
        // Core services
        private readonly ILlmService _llmService;
        private readonly RagService _ragService;
        
        // Memory layer
        private readonly IMemoryOrchestrator _memoryOrchestrator;
        private readonly IShortTermMemory _shortTermMemory;
        private readonly IEpisodicMemory _episodicMemory;
        private readonly ICanonicalMemory _canonicalMemory;
        
        // Orchestration
        private readonly IIntentClassifier _intentClassifier;
        private readonly IAgentRouter _agentRouter;
        private readonly IResponseAggregator _responseAggregator;
        
        // World state
        private readonly IWorldStateService _worldStateService;
        private readonly IStateInjector _stateInjector;
        
        // Player modeling
        private readonly IPlayerModelService _playerModelService;
        private readonly IBehaviorTracker _behaviorTracker;
        
        // Validation
        private readonly IOutputCritiquer _outputCritiquer;
        private readonly IConfidenceScorer _confidenceScorer;
        private readonly IUncertaintyWrapper _uncertaintyWrapper;
        
        // Rules & Timeline
        private readonly IRuleEngine _ruleEngine;
        private readonly IActionValidator _actionValidator;
        private readonly ITimelineService _timelineService;
        private readonly IRewindService _rewindService;
        
        // Prompts
        private readonly IPromptMutator _promptMutator;
        private readonly IPromptTemplateService _templateService;
        
        // Events & Emotion
        private readonly IAiEventBus _eventBus;
        private readonly IEmotionTagger _emotionTagger;
        
        // Configuration
        private AdvancedAiConfig _config = new();
        private bool _initialized = false;
        
        public bool IsInitialized => _initialized;

        public AdvancedAiService(ILlmService? llmService = null)
        {
            _llmService = llmService ?? new LlmService();
            _ragService = new RagService(_llmService);
            
            // Initialize memory layer
            _shortTermMemory = new ShortTermMemory();
            _episodicMemory = new EpisodicMemory();
            _canonicalMemory = new CanonicalMemory();
            _memoryOrchestrator = new MemoryOrchestrator(_shortTermMemory, _episodicMemory, _canonicalMemory);
            
            // Initialize orchestration
            _intentClassifier = new IntentClassifier();
            _agentRouter = new AgentRouter(_intentClassifier);
            _responseAggregator = new ResponseAggregator(_llmService);
            
            // Initialize world state
            _worldStateService = new WorldStateService();
            _stateInjector = new StateInjector(_worldStateService);
            
            // Initialize player modeling
            _playerModelService = new PlayerModelService();
            _behaviorTracker = new BehaviorTracker();
            
            // Initialize validation
            _outputCritiquer = new OutputCritiquer();
            _confidenceScorer = new ConfidenceScorer();
            _uncertaintyWrapper = new UncertaintyWrapper();
            
            // Initialize rules & timeline
            _ruleEngine = new RuleEngine();
            _actionValidator = new ActionValidator(_ruleEngine);
            _timelineService = new TimelineService();
            _rewindService = new RewindService();
            
            // Initialize prompts
            _promptMutator = new PromptMutator();
            _templateService = new PromptTemplateService();
            
            // Initialize events & emotion
            _eventBus = new AiEventBus();
            _emotionTagger = new EmotionTagger();
            
            // Wire up LlmService with advanced features
            if (_llmService is LlmService concreteService)
            {
                concreteService.ConfigureAdvancedAi(_stateInjector, _memoryOrchestrator, true);
            }
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
            
            // Update LlmService configuration
            if (_llmService is LlmService concreteService)
            {
                concreteService.ConfigureAdvancedAi(
                    config.EnableStateInjection ? _stateInjector : null,
                    config.EnableMemoryOrchestration ? _memoryOrchestrator : null,
                    config.EnableStateInjection
                );
            }
        }

        public async Task<AiResponse> ProcessAsync(string input, AiRequestContext? context = null)
        {
            if (!_initialized) await InitializeAsync();
            
            context ??= new AiRequestContext();
            var response = new AiResponse();
            
            // 1. Classify intent
            var intentContext = new Dictionary<string, object>();
            if (context.CurrentScene != null) intentContext["scene"] = context.CurrentScene;
            if (context.CurrentQuest != null) intentContext["quest"] = context.CurrentQuest;
            
            var intent = await _intentClassifier.ClassifyAsync(input, intentContext);
            response.Intent = intent.PrimaryIntent;
            
            // 2. Get player profile for prompt mutations
            PlayerProfile? playerProfile = null;
            if (_config.EnablePlayerModeling)
            {
                playerProfile = await _playerModelService.GetProfile(_config.DefaultPlayerId);
            }
            
            // 3. Build context from memory
            var memoryContext = await _memoryOrchestrator.BuildContext(input, context.RelevantCharacters);
            
            // 5. Build routing context for agent selection
            var routingContext = new Dictionary<string, object>()
            {
                ["session_id"] = context.SessionId ?? Guid.NewGuid().ToString(),
                ["scene"] = context.CurrentScene ?? "",
                ["lore"] = memoryContext.CanonicalContext ?? ""
            };

            // Add world state to routing context
            if (_config.EnableStateInjection)
            {
                foreach (var flag in _worldStateService.CurrentState.Flags)
                    routingContext[$"flag_{flag.Key}"] = flag.Value;
                foreach (var counter in _worldStateService.CurrentState.Counters)
                    routingContext[$"counter_{counter.Key}"] = counter.Value;
            }

            // 6. Route to select the best agent
            var routeDecision = await _agentRouter.RouteAsync(input, routingContext);

            // 7. Generate response using the selected agent's system prompt
            var agentSystemPrompt = routeDecision.SelectedAgent.SystemPrompt;
            var llmResponse = await _llmService.CompleteAsync(input, agentSystemPrompt);
            response.Content = llmResponse;
            response.Agent = routeDecision.SelectedAgent.AgentId;
            
            // 6. Validate response if required
            if (_config.EnableValidation && context.RequireValidation)
            {
                var critiqueContext = new CritiqueContext
                {
                    ExpectedTone = context.RequestType,
                    ActiveFlags = _worldStateService.CurrentState.Flags,
                    MinConfidence = _config.MinConfidenceThreshold
                };
                
                var critique = await _outputCritiquer.CritiqueAsync(response.Content, critiqueContext);
                response.WasValidated = critique.IsApproved;
                
                if (!critique.IsApproved && !string.IsNullOrEmpty(critique.RevisionRequired))
                {
                    response.Metadata["revision_note"] = critique.RevisionRequired;
                }
            }
            
            // 7. Score confidence and wrap if needed
            if (_config.EnableConfidenceScoring)
            {
                var confidenceContext = new ConfidenceContext
                {
                    OriginalQuery = input,
                    KnowledgeBaseHits = new List<string>()
                };
                
                var confidence = _confidenceScorer.Score(response.Content, confidenceContext);
                response.Confidence = confidence.OverallConfidence;
                
                if (confidence.ConfidenceLevel != "high")
                {
                    var wrapped = _uncertaintyWrapper.Wrap(response.Content, confidence);
                    if (wrapped.WasWrapped)
                    {
                        response.Content = wrapped.FinalOutput;
                        response.Metadata["was_hedged"] = true;
                    }
                }
            }
            
            // 8. Record interaction in memory
            if (_config.EnableMemoryOrchestration)
            {
                await _memoryOrchestrator.RecordInteraction(input, response.Content, context.RequestType);
            }
            
            // 9. Tag emotion and publish event
            var emotion = _emotionTagger.Tag(response.Content);
            response.Metadata["emotion"] = emotion.PrimaryEmotion;
            
            await _eventBus.PublishAsync(new AiEvent
            {
                EventType = "ai_response",
                Source = response.Agent ?? "unknown",
                Data = new Dictionary<string, object>
                {
                    ["intent"] = response.Intent.ToString(),
                    ["emotion"] = emotion.PrimaryEmotion,
                    ["confidence"] = response.Confidence
                }
            });
            
            return response;
        }

        public async Task<string> GenerateNarrativeAsync(string prompt, NarrativeContext? context = null)
        {
            context ??= new NarrativeContext();
            
            var variables = new Dictionary<string, object>
            {
                ["location"] = context.Location ?? "the scene",
                ["player_action"] = prompt,
                ["mood"] = context.Mood ?? "neutral",
                ["time_of_day"] = context.TimeOfDay ?? "day"
            };
            
            var narrativePrompt = _templateService.Render("narrative_scene", variables);
            
            var response = await ProcessAsync(narrativePrompt, new AiRequestContext
            {
                RequestType = "narrative",
                CurrentScene = context.Location
            });
            
            return response.Content;
        }

        public async Task<string> GenerateCommentaryAsync(string gameEvent, CommentaryContext? context = null)
        {
            context ??= new CommentaryContext();
            
            var playerProfile = await _playerModelService.GetProfile(_config.DefaultPlayerId);
            
            var prompt = $"Generate exciting live commentary for this gaming moment:\n" +
                        $"Event: {gameEvent}\n" +
                        $"Game: {context.GameTitle ?? "the game"}\n" +
                        $"Player action: {context.PlayerAction ?? "playing"}\n" +
                        (context.Score.HasValue ? $"Score: {context.Score}\n" : "") +
                        (context.Combo.HasValue ? $"Combo: {context.Combo}x\n" : "");
            
            // Mutate based on player preferences
            var mutatedPrompt = _promptMutator.Mutate(prompt, playerProfile);
            
            var response = await ProcessAsync(mutatedPrompt, new AiRequestContext
            {
                RequestType = "commentary",
                RequireValidation = false // Commentary doesn't need strict validation
            });
            
            return response.Content;
        }

        public async Task RecordInteractionAsync(string input, string output, string? context = null)
        {
            await _memoryOrchestrator.RecordInteraction(input, output, context);
        }

        public async Task<string> GetContextualMemoryAsync(string query)
        {
            var memories = await _memoryOrchestrator.Query(query);
            return string.Join("\n\n", memories);
        }

        public void UpdateWorldState(string key, object value, string? source = null)
        {
            if (value is bool boolVal)
                _worldStateService.SetFlag(key, boolVal, source);
            else if (value is int intVal)
                _worldStateService.SetCounter(key, intVal, source);
            else
                _worldStateService.SetRelation(key, value.ToString() ?? "", source);
                
            // Record state change in timeline
            if (_config.EnableTimeline)
            {
                var delta = new StateDelta
                {
                    TriggerEvent = source,
                    Changes = new Dictionary<string, DeltaChange>
                    {
                        [key] = new DeltaChange
                        {
                            Key = key,
                            Type = value is bool ? "flag" : value is int ? "counter" : "relation",
                            NewValue = value
                        }
                    }
                };
            }
        }

        public WorldState GetCurrentWorldState() => _worldStateService.CurrentState;

        public async Task UpdatePlayerModelAsync(PlayerAction action)
        {
            _behaviorTracker.TrackAction(action);
            await _playerModelService.UpdateFromAction(_config.DefaultPlayerId, action);
        }

        public async Task<PlayerProfile> GetPlayerProfileAsync(string playerId)
        {
            return await _playerModelService.GetProfile(playerId);
        }

        public void CreateSavePoint(string name, string? description = null)
        {
            _rewindService.CreateRewindPoint(name, description);
        }

        public async Task<WhatIfResult> SimulateWhatIfAsync(string scenario)
        {
            return await _timelineService.SimulateWhatIf(scenario, new List<StateDelta>());
        }

        public Task<ActionValidationResult> ValidateActionAsync(ProposedAction action)
        {
            var gameContext = new GameContext
            {
                Flags = _worldStateService.CurrentState.Flags,
                Counters = _worldStateService.CurrentState.Counters,
                CurrentAction = action.ActionType,
                Actor = action.Actor,
                Target = action.Target
            };
            
            var result = _actionValidator.Validate(action, gameContext);
            return Task.FromResult(result);
        }

        public void SubscribeToEvent(string eventType, Events.EventHandler handler)
        {
            _eventBus.Subscribe(eventType, handler);
        }

        public async Task PublishEventAsync(AiEvent evt)
        {
            await _eventBus.PublishAsync(evt);
        }

        // Expose internal services for advanced usage
        public IMemoryOrchestrator Memory => _memoryOrchestrator;
        public IWorldStateService WorldState => _worldStateService;
        public IPlayerModelService PlayerModel => _playerModelService;
        public IRuleEngine Rules => _ruleEngine;
        public ITimelineService Timeline => _timelineService;
        public IAiEventBus Events => _eventBus;
    }
}
