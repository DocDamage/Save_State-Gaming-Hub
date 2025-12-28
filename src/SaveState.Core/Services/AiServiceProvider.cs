using System;
using SaveState.Core.Infrastructure;
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
using SaveState.Core.Services.Ai.Optimization;
using SaveState.Core.Services.Ai.Safety;
using SaveState.Core.Services.GameState;
using SaveState.Core.Services.Player;
using SaveState.Core.Services.Rules;
using SaveState.Core.Services.Timeline;
using SaveState.Core.Services.EmulatorEnhancements;
using SaveState.Core.Services.Memory;


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
        private DriftTestManager? _driftTestManager;
        private IHallucinationDetector? _hallucinationDetector;
        private IUltimateAiOrchestrator? _ultimateAiOrchestrator;

        // Phase A Services
        private IGlobalKillSwitch? _killSwitch;
        private IPolicyGate? _policyGate;
        private IProvenanceLedger? _provenanceLedger;
        private IPregenService? _pregenService;
        private IIntentRouter? _intentRouter;
        // Specialists
        private NarrativeSpecialist? _narrativeSpecialist;
        private LoreSpecialist? _loreSpecialist;
        private SystemSpecialist? _systemSpecialist;

        // Monitoring
        private IGameSessionMonitor? _gameSessionMonitor;
        private IMemoryProfileService? _memoryProfileService;
        private ITrainerGeneratorService? _trainerGeneratorService;

        private AiServiceProvider() { }

        // === Core Services ===
        public ILlmService LlmService => _llmService ??= CreateLlmService();

        private ILlmService CreateLlmService()
        {
            var httpClientFactory = new SimpleHttpClientFactory();
            var ollamaManager = new OllamaManager(httpClientFactory);
            return new LlmService(AppConfiguration.Instance, httpClientFactory, ollamaManager);
        }

        private class SimpleHttpClientFactory : System.Net.Http.IHttpClientFactory
        {
            public System.Net.Http.HttpClient CreateClient(string name)
            {
                return new System.Net.Http.HttpClient();
            }
        }

        public IGameSessionMonitor GameSessionMonitor => _gameSessionMonitor ??= new GameSessionMonitor(
            UltimateAiOrchestrator,
            WorldStateService,
            MemoryProfileService,
            new SaveState.Core.Services.Memory.WindowsMemoryReader());

        public IMemoryProfileService MemoryProfileService => _memoryProfileService ??= new MemoryProfileService();

        public ITrainerGeneratorService TrainerGeneratorService => _trainerGeneratorService ??= new TrainerGeneratorService(
            new SaveState.Core.Services.Memory.WindowsMemoryReader(),
            MemoryProfileService);

        public RagService RagService => _ragService ??= new RagService(LlmService);

        public IAdvancedAiService AdvancedAi => _advancedAi ??= CreateAdvancedAiService();

        private AdvancedAiService CreateAdvancedAiService()
        {
            // Manually wire all components (legacy pattern - prefer DI)
            var config = new AdvancedAiConfig
            {
                EnableStateInjection = true,
                EnableMemoryOrchestration = true,
                EnablePlayerModeling = true,
                EnableValidation = true,
                EnableConfidenceScoring = true,
                EnableTimeline = true
            };

            // Memory layer
            var shortTermMemory = new ShortTermMemory();
            var episodicMemory = new EpisodicMemory();
            var canonicalMemory = new CanonicalMemory();
            var memoryOrchestrator = new MemoryOrchestrator(shortTermMemory, episodicMemory, canonicalMemory);
            var memoryCoordinator = new AiMemoryCoordinator(memoryOrchestrator);

            // World state
            var worldStateService = new WorldStateService();
            var playerModelService = new PlayerModelService();
            var behaviorTracker = new BehaviorTracker();
            var worldStateCoordinator = new AiWorldStateCoordinator(
                worldStateService, playerModelService, behaviorTracker, config);

            // Validation
            var outputCritiquer = new OutputCritiquer();
            var confidenceScorer = new ConfidenceScorer();
            var uncertaintyWrapper = new UncertaintyWrapper();
            var ruleEngine = new RuleEngine();
            var actionValidator = new ActionValidator(ruleEngine);
            var validationCoordinator = new AiValidationCoordinator(
                outputCritiquer, confidenceScorer, uncertaintyWrapper, actionValidator, worldStateService);

            // Timeline
            var timelineService = new TimelineService();
            var rewindService = new RewindService();
            var timelineCoordinator = new AiTimelineCoordinator(timelineService, rewindService);

            // Events
            var eventBus = new AiEventBus();
            var emotionTagger = new EmotionTagger();
            var eventCoordinator = new AiEventCoordinator(eventBus, emotionTagger);

            // Request processor
            var intentClassifier = new IntentClassifier();
            var agentRouter = new AgentRouter(intentClassifier);
            var requestProcessor = new AiRequestProcessor(
                LlmService,
                intentClassifier,
                agentRouter,
                memoryCoordinator,
                worldStateCoordinator,
                validationCoordinator,
                eventCoordinator,
                emotionTagger,
                config);

            // Narrative generator
            var promptMutator = new PromptMutator();
            var templateService = new PromptTemplateService();
            var narrativeGenerator = new AiNarrativeGenerator(
                requestProcessor,
                templateService,
                promptMutator,
                playerModelService,
                config);

            // Create facade
            var service = new AdvancedAiService(
                requestProcessor,
                memoryCoordinator,
                worldStateCoordinator,
                validationCoordinator,
                narrativeGenerator,
                timelineCoordinator,
                eventCoordinator,
                LlmService,
                worldStateService,
                playerModelService,
                episodicMemory,
                canonicalMemory,
                eventBus);

            service.Configure(config);
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

        public DriftTestManager DriftTestManager =>
            _driftTestManager ??= new DriftTestManager(AiTestHarness);

        public IHallucinationDetector HallucinationDetector =>
            _hallucinationDetector ??= new HallucinationDetector();

        public IUltimateAiOrchestrator UltimateAiOrchestrator =>
             _ultimateAiOrchestrator ??= CreateConfiguredOrchestrator();

        private IUltimateAiOrchestrator CreateConfiguredOrchestrator()
        {
            var pipeline = new PipelineOrchestrator();
            var metrics = new MetricsService();
            var cache = new CacheManager();
            var experiments = new ExperimentManager();
            var health = new HealthMonitor(metrics, cache);
            var builder = new AiPipelineBuilder(
                pipeline,
                cache,
                metrics,
                KillSwitch,
                IntentRouter,
                ProvenanceLedger);

            return new UltimateAiOrchestrator(
                pipeline,
                cache,
                experiments,
                metrics,
                health,
                builder);
        }

        // --- Phase A Services ---
        public IGlobalKillSwitch KillSwitch => _killSwitch ??= new GlobalKillSwitch();
        public IPolicyGate PolicyGate => _policyGate ??= new PolicyGate();
        public IProvenanceLedger ProvenanceLedger => _provenanceLedger ??= new ProvenanceLedger();
        public IPregenService PregenService => _pregenService ??= new PregenService(EnhancedEventBus, GetSpecialistAgents());

        // Router & Specialists
        public NarrativeSpecialist NarrativeSpecialist => _narrativeSpecialist ??= new NarrativeSpecialist(LlmService);
        public LoreSpecialist LoreSpecialist => _loreSpecialist ??= new LoreSpecialist(LlmService, LoreLocker);
        public SystemSpecialist SystemSpecialist => _systemSpecialist ??= new SystemSpecialist(LlmService, RuleEngine);

        public IEnumerable<ISpecialistAgent> GetSpecialistAgents()
        {
            yield return NarrativeSpecialist;
            yield return LoreSpecialist;
            yield return SystemSpecialist;
        }

        public IIntentRouter IntentRouter => _intentRouter ??= new IntentRouter(
            new EnhancedIntentClassifier(), // Or use existing IntentClassifier if they share interface? Enhanced is new.
            GetSpecialistAgents(),
            StateInjector,
            EpisodicMemory,
            LoreLocker,
            new MemoryWriterService(EnhancedEventBus, EpisodicMemory)
        );

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
        public static async System.Threading.Tasks.Task<string> AskAiAsync(this IAdvancedAiService aiService, string query, string? context = null)
        {
            var response = await aiService.ProcessAsync(query, new AiRequestContext
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
