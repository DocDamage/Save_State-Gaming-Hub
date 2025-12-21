using System;
using SaveState.Core.Services.Ai;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.Ai.Orchestration;
using SaveState.Core.Services.Ai.Prompts;
using SaveState.Core.Services.Ai.Validation;
using SaveState.Core.Services.Ai.Uncertainty;
using SaveState.Core.Services.Ai.Models;
using SaveState.Core.Services.Ai.Events;
using SaveState.Core.Services.Ai.Emotion;
using SaveState.Core.Services.Ai.Governance;
using SaveState.Core.Services.Ai.Core;
using SaveState.Core.Services.Ai.Latency;
using SaveState.Core.Services.Ai.Persona;
using SaveState.Core.Services.Ai.Trust;
using SaveState.Core.Services.Ai.Tools;
using SaveState.Core.Services.Ai.Resilience;
using SaveState.Core.Services.Ai.Telemetry;
using SaveState.Core.Services.Ai.Testing;
using SaveState.Core.Services.GameState;
using SaveState.Core.Services.Player;
using SaveState.Core.Services.Rules;
using SaveState.Core.Services.Timeline;
using SaveState.Core.Services.EmulatorEnhancements;


namespace SaveState.Core.Services
{
    /// <summary>
    /// Centralized service locator for Advanced AI Architecture components.
    /// Provides lazy initialization and wiring for all AI subsystems.
    /// 
    /// Usage:
    ///   var services = AiServiceProvider.Instance;
    ///   var response = await services.AdvancedAi.ProcessAsync("Tell me about the quest");
    /// </summary>
    public class AiServiceProvider
    {
        private static readonly Lazy<AiServiceProvider> _instance = 
            new Lazy<AiServiceProvider>(() => new AiServiceProvider());
        
        public static AiServiceProvider Instance => _instance.Value;

        // Core Services
        private ILlmService? _llmService;
        private RagService? _ragService;
        private IAdvancedAiService? _advancedAi;

        // Memory Layer
        private IShortTermMemory? _shortTermMemory;
        private IEpisodicMemory? _episodicMemory;
        private ICanonicalMemory? _canonicalMemory;
        private IMemoryOrchestrator? _memoryOrchestrator;

        // Orchestration
        private IIntentClassifier? _intentClassifier;
        private IAgentRouter? _agentRouter;
        private IResponseAggregator? _responseAggregator;

        // World State
        private IWorldStateService? _worldStateService;
        private IStateInjector? _stateInjector;

        // Player Modeling
        private IPlayerModelService? _playerModelService;
        private IBehaviorTracker? _behaviorTracker;

        // Validation
        private IOutputCritiquer? _outputCritiquer;
        private IConfidenceScorer? _confidenceScorer;
        private IUncertaintyWrapper? _uncertaintyWrapper;

        // Rules & Timeline
        private IRuleEngine? _ruleEngine;
        private IActionValidator? _actionValidator;
        private ITimelineService? _timelineService;
        private IRewindService? _rewindService;

        // Prompts
        private IPromptMutator? _promptMutator;
        private IPromptTemplateService? _templateService;

        // Events & Emotion
        private IAiEventBus? _eventBus;
        private IEmotionTagger? _emotionTagger;

        // Model Management
        private IModelRegistry? _modelRegistry;

        // Feature Services (enhanced with AI)
        private LiveCommentaryService? _liveCommentary;
        private DreamSequenceService? _dreamSequence;
        private BmadService? _bmadService;

        // === NEW AI INFRASTRUCTURE LAYERS ===
        
        // Governance Layer
        private IAiGovernanceService? _governanceService;
        private ICapabilityGate? _capabilityGate;
        private IFeatureFlagService? _featureFlagService;
        private ISafetyRails? _safetyRails;
        
        // Deterministic Core
        private IDeterministicBoundary? _deterministicBoundary;
        private ICanonEnforcer? _canonEnforcer;
        private IStateIntegrity? _stateIntegrity;
        
        // Event Bus & Latency
        private IEnhancedEventBus? _enhancedEventBus;
        private ILatencyManager? _latencyManager;
        private IStreamingHandler? _streamingHandler;
        private IResponseWarmer? _responseWarmer;
        
        // Memory Enhancement
        private ILoreLocker? _loreLocker;
        private INarrativeCompressor? _narrativeCompressor;
        
        // Persona & Trust
        private IPersonaHotSwapper? _personaHotSwapper;
        private IPlayerTrustModel? _playerTrustModel;
        
        // Tools & Resilience
        private IToolAwareAi? _toolAwareAi;
        private IFailureAsContent? _failureAsContent;
        
        // Telemetry & Testing
        private IAiTelemetry? _aiTelemetry;
        private IAiTestHarness? _aiTestHarness;
        private IFakePlayerSimulator? _fakePlayerSimulator;
        private IHallucinationDetector? _hallucinationDetector;

        private AiServiceProvider() { }

        // === Core Services ===
        public ILlmService LlmService => _llmService ??= new LlmService();
        
        public RagService RagService => _ragService ??= new RagService(LlmService);
        
        public IAdvancedAiService AdvancedAi => _advancedAi ??= CreateAdvancedAiService();

        private AdvancedAiService CreateAdvancedAiService()
        {
            var service = new AdvancedAiService(LlmService);
            service.Configure(new AdvancedAiConfig
            {
                EnableStateInjection = true,
                EnableMemoryOrchestration = true,
                EnablePlayerModeling = true,
                EnableValidation = true,
                EnableConfidenceScoring = true,
                EnableTimeline = true
            });
            return service;
        }

        // === Memory Layer ===
        public IShortTermMemory ShortTermMemory => _shortTermMemory ??= new ShortTermMemory();
        public IEpisodicMemory EpisodicMemory => _episodicMemory ??= new EpisodicMemory();
        public ICanonicalMemory CanonicalMemory => _canonicalMemory ??= new CanonicalMemory();
        public IMemoryOrchestrator MemoryOrchestrator => 
            _memoryOrchestrator ??= new MemoryOrchestrator(ShortTermMemory, EpisodicMemory, CanonicalMemory);

        // === Orchestration ===
        public IIntentClassifier IntentClassifier => _intentClassifier ??= new IntentClassifier();
        public IAgentRouter AgentRouter => _agentRouter ??= new AgentRouter(IntentClassifier);
        public IResponseAggregator ResponseAggregator => _responseAggregator ??= new ResponseAggregator(LlmService);

        // === World State ===
        public IWorldStateService WorldStateService => _worldStateService ??= new WorldStateService();
        public IStateInjector StateInjector => _stateInjector ??= new StateInjector(WorldStateService);

        // === Player Modeling ===
        public IPlayerModelService PlayerModelService => _playerModelService ??= new PlayerModelService();
        public IBehaviorTracker BehaviorTracker => _behaviorTracker ??= new BehaviorTracker();

        // === Validation ===
        public IOutputCritiquer OutputCritiquer => _outputCritiquer ??= new OutputCritiquer();
        public IConfidenceScorer ConfidenceScorer => _confidenceScorer ??= new ConfidenceScorer();
        public IUncertaintyWrapper UncertaintyWrapper => _uncertaintyWrapper ??= new UncertaintyWrapper();

        // === Rules & Timeline ===
        public IRuleEngine RuleEngine => _ruleEngine ??= new RuleEngine();
        public IActionValidator ActionValidator => _actionValidator ??= new ActionValidator(RuleEngine);
        public ITimelineService TimelineService => _timelineService ??= new TimelineService();
        public IRewindService RewindService => _rewindService ??= new RewindService();

        // === Prompts ===
        public IPromptMutator PromptMutator => _promptMutator ??= new PromptMutator();
        public IPromptTemplateService TemplateService => _templateService ??= new PromptTemplateService();

        // === Events & Emotion ===
        public IAiEventBus EventBus => _eventBus ??= new AiEventBus();
        public IEmotionTagger EmotionTagger => _emotionTagger ??= new EmotionTagger();

        // === Model Management ===
        public IModelRegistry ModelRegistry => _modelRegistry ??= new ModelRegistry();

        // === Enhanced Feature Services ===
        public LiveCommentaryService LiveCommentary => 
            _liveCommentary ??= new LiveCommentaryService(LlmService, AdvancedAi);
        
        public DreamSequenceService DreamSequence => 
            _dreamSequence ??= new DreamSequenceService(LlmService);
        
        public BmadService BmadService => 
            _bmadService ??= new BmadService(LlmService);

        // === NEW AI INFRASTRUCTURE LAYER ACCESSORS ===

        // --- Governance Layer ---
        public IAiGovernanceService GovernanceService => 
            _governanceService ??= new AiGovernanceService(CapabilityGate, FeatureFlagService, SafetyRails);
        
        public ICapabilityGate CapabilityGate => 
            _capabilityGate ??= new CapabilityGate();
        
        public IFeatureFlagService FeatureFlagService => 
            _featureFlagService ??= new FeatureFlagService();
        
        public ISafetyRails SafetyRails => 
            _safetyRails ??= new SafetyRails();

        // --- Deterministic Core ---
        public IDeterministicBoundary DeterministicBoundary => 
            _deterministicBoundary ??= new DeterministicBoundary();
        
        public ICanonEnforcer CanonEnforcer => 
            _canonEnforcer ??= new CanonEnforcer();
        
        public IStateIntegrity StateIntegrity => 
            _stateIntegrity ??= new StateIntegrity();

        // --- Event Bus & Latency ---
        public IEnhancedEventBus EnhancedEventBus => 
            _enhancedEventBus ??= new EnhancedEventBus();
        
        public ILatencyManager LatencyManager => 
            _latencyManager ??= new LatencyManager();
        
        public IStreamingHandler StreamingHandler => 
            _streamingHandler ??= new StreamingHandler();
        
        public IResponseWarmer ResponseWarmer => 
            _responseWarmer ??= new ResponseWarmer();

        // --- Memory Enhancement ---
        public ILoreLocker LoreLocker => 
            _loreLocker ??= new LoreLocker();
        
        public INarrativeCompressor NarrativeCompressor => 
            _narrativeCompressor ??= new NarrativeCompressor();

        // --- Persona & Trust ---
        public IPersonaHotSwapper PersonaHotSwapper => 
            _personaHotSwapper ??= new PersonaHotSwapper();
        
        public IPlayerTrustModel PlayerTrustModel => 
            _playerTrustModel ??= new PlayerTrustModel();

        // --- Tools & Resilience ---
        public IToolAwareAi ToolAwareAi => 
            _toolAwareAi ??= new ToolAwareAi();
        
        public IFailureAsContent FailureAsContent => 
            _failureAsContent ??= new FailureAsContent();

        // --- Telemetry & Testing ---
        public IAiTelemetry AiTelemetry => 
            _aiTelemetry ??= new AiTelemetry();
        
        public IAiTestHarness AiTestHarness => 
            _aiTestHarness ??= new AiTestHarness();

        public IFakePlayerSimulator FakePlayerSimulator => 
            _fakePlayerSimulator ??= new FakePlayerSimulator();

        public IHallucinationDetector HallucinationDetector => 
            _hallucinationDetector ??= new HallucinationDetector();

        /// <summary>
        /// Initialize all services (call at application startup)
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync()
        {
            await LlmService.InitializeAsync();
            await AdvancedAi.InitializeAsync();
        }

        /// <summary>
        /// Register a custom LLM service (for dependency injection scenarios)
        /// </summary>
        public void SetLlmService(ILlmService llmService)
        {
            _llmService = llmService;
            // Reset dependent services so they get recreated with new LLM
            _ragService = null;
            _advancedAi = null;
            _agentRouter = null;
            _responseAggregator = null;
            _liveCommentary = null;
            _dreamSequence = null;
            _bmadService = null;
        }

        /// <summary>
        /// Get a summary of all available providers
        /// </summary>
        public string GetProviderStatus()
        {
            var llm = LlmService;
            return $"LLM Provider: {llm.CurrentProvider}, Available: {llm.IsAvailable}";
        }
    }

    /// <summary>
    /// Extension methods for easy access to AI services
    /// </summary>
    public static class AiServiceExtensions
    {
        /// <summary>
        /// Quick access to process an AI request with all advanced features
        /// </summary>
        public static async System.Threading.Tasks.Task<string> AskAiAsync(this string query, string? context = null)
        {
            var response = await AiServiceProvider.Instance.AdvancedAi.ProcessAsync(query, new AiRequestContext
            {
                RequestType = context,
                RequireValidation = true,
                InjectWorldState = true
            });
            return response.Content;
        }

        /// <summary>
        /// Record a player action for modeling
        /// </summary>
        public static void TrackPlayerAction(this IBehaviorTracker tracker, string actionType, ActionCategory category, 
            string? target = null, Dictionary<string, object>? metadata = null)
        {
            tracker.TrackAction(new PlayerAction
            {
                ActionType = actionType,
                Category = category,
                Target = target,
                Metadata = metadata ?? new Dictionary<string, object>()
            });
        }

        /// <summary>
        /// Update world state with automatic source tracking
        /// </summary>
        public static void SetGameFlag(this IWorldStateService worldState, string key, bool value, string source = "game")
        {
            worldState.SetFlag(key, value, source);
        }

        /// <summary>
        /// Add a canonical lore fact
        /// </summary>
        public static async System.Threading.Tasks.Task AddLoreFactAsync(this ICanonicalMemory memory, 
            string statement, string source, FactCategory category = FactCategory.WorldLore)
        {
            await memory.AddFact(statement, category, source);
        }
    }
}
