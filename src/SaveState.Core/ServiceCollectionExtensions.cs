using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Infrastructure;
using SaveState.Core.Interfaces;
using SaveState.Core.Services.Accessibility;
using SaveState.Core.Services.Audio;
using SaveState.Core.Services.Account;
using SaveState.Core.Services.Ai;
using SaveState.Core.Services.Cloud;
using SaveState.Core.Services.EmulatorEnhancements;
using SaveState.Core.Services.Gamification;
using SaveState.Core.Services.Input;
using SaveState.Core.Services.Media;
using SaveState.Core.Services.Mugen;
using SaveState.Core.Services.Netplay;
using SaveState.Core.Services.Player;
using SaveState.Core.Services.Rom;
using SaveState.Core.Services.Timeline;
using SaveState.Core.Services.Ai.Governance;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.Ai.Optimization;
using SaveState.Core.Services.Ai.Safety;
using SaveState.Core.Services.Ai.Events;
using SaveState.Core.Services.Ai.Orchestration;
using SaveState.Core.Services.Ai.EdgeCases;
using SaveState.Core.Services.Ai.Production;
using SaveState.Core.Services.Rules;
using System;

namespace SaveState.Core.Services
{
    /// <summary>
    /// Extension methods for registering SaveState.Core services with DI container.
    /// Use this in your startup configuration to enable dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all SaveState.Core services with the DI container.
        /// </summary>
        public static IServiceCollection AddSaveStateCoreServices(this IServiceCollection services)
        {
            // ============ Configuration ============
            services.AddSingleton<IAppConfiguration, AppConfiguration>();

            // ============ AI Services ============
            services.AddAiServices();

            // ============ Emulator Enhancement Services ============
            services.AddEmulatorEnhancementServices();

            // ============ Mugen/Fighting Game Services ============
            services.AddMugenServices();

            // ============ Media Services ============
            services.AddMediaServices();

            // ============ Input Services ============
            services.AddInputServices();

            // ============ Accessibility Services ============
            services.AddAccessibilityServices();

            // ============ Account & Cloud Services ============
            services.AddAccountServices();

            // ============ Gamification Services ============
            services.AddGamificationServices();

            // ============ Netplay Services ============
            services.AddNetplayServices();

            // ============ Rom Services ============
            services.AddRomServices();

            // ============ Timeline Services ============
            services.AddTimelineServices();

            // ============ Central Service Provider ============
            services.AddSingleton<AiServiceProvider>(sp => AiServiceProvider.Instance);

            return services;
        }

        /// <summary>
        /// Registers AI-related services.
        /// </summary>
        public static IServiceCollection AddAiServices(this IServiceCollection services)
        {
            // Core AI services as singletons (they maintain state/caches)
            // Register concrete type first, then forward interface registration
            services.AddHttpClient("LlmProvider", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            services.AddSingleton<LlmService>();
            services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<LlmService>());
            services.AddHttpClient("OllamaManager", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
            });
            services.AddSingleton<OllamaManager>();

            services.AddHttpClient("ModelManager", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(30);
            });
            services.AddSingleton<ModelManager>();
            services.AddHttpClient("StableDiffusionService", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            });
            services.AddSingleton<StableDiffusionService>(sp =>
            {
                var config = sp.GetRequiredService<IAppConfiguration>();
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new StableDiffusionService(config, factory.CreateClient("StableDiffusionService"));
            });
            services.AddSingleton<RagService>(sp =>
                new RagService(sp.GetRequiredService<ILlmService>()));

            // Base AI Infrastructure
            services.AddSingleton<IEdgeCaseHandler, EdgeCaseHandler>();
            services.AddSingleton<IProductionAiService, ProductionAiService>();
            services.AddSingleton<IResilientAiService, ResilientAiService>();
            services.AddSingleton<IUltimateAiOrchestrator, UltimateAiOrchestrator>();

            // Phase A: Core Runtime Upgrades
            services.AddSingleton<IGlobalKillSwitch, GlobalKillSwitch>();
            services.AddSingleton<IPolicyGate, PolicyGate>();
            services.AddSingleton<IRuleEngine, RuleEngine>();
            services.AddSingleton<ILoreLocker, LoreLocker>();
            services.AddSingleton<IEpisodicMemory, EpisodicMemory>();
            services.AddSingleton<IEnhancedEventBus, EnhancedEventBus>();
            services.AddSingleton<IEnhancedIntentClassifier, EnhancedIntentClassifier>();
            services.AddSingleton<IProvenanceLedger, ProvenanceLedger>();
            services.AddSingleton<IPregenService, PregenService>();
            services.AddSingleton<IMemoryWriterService, MemoryWriterService>();

            // Edge Case Components (Split God Object)
            services.AddSingleton<IInputSanitizer, InputSanitizer>();
            services.AddSingleton<ITextTruncator, TextTruncator>();
            services.AddSingleton<IInjectionDetector, InjectionDetector>();
            services.AddSingleton<IPatternDetector, PatternDetector>();
            services.AddSingleton<IResourceMonitor, ResourceMonitor>();
            services.AddSingleton<IRecoveryCoordinator, RecoveryCoordinator>();
            services.AddSingleton<IEdgeCaseStatisticsCollector, EdgeCaseStatisticsCollector>();
            services.AddSingleton<IOutputValidator, OutputValidator>();
            services.AddSingleton<EdgeCaseConfig>(new EdgeCaseConfig());

            // Production AI Components (Split God Object)
            services.AddSingleton<ProductionAiConfig>(new ProductionAiConfig());
            services.AddSingleton<IAiStatisticsCollector, AiStatisticsCollector>();
            services.AddSingleton<IAiResponseCache, AiResponseCache>();
            services.AddSingleton<IAiConversationManager, AiConversationManager>();
            services.AddSingleton<IAiPromptAssembler, AiPromptAssembler>();
            services.AddSingleton<IAiFallbackGenerator, AiFallbackGenerator>();
            services.AddSingleton<IAiRequestPipeline, AiRequestPipeline>();

            // Ultimate AI Orchestrator Components (Split God Object)
            services.AddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();
            services.AddSingleton<IAiCacheCoordinator, CacheManager>();
            services.AddSingleton<IAiExperimentCoordinator, ExperimentManager>();
            services.AddSingleton<IAiMetricsAggregator, MetricsService>();
            services.AddSingleton<IAiHealthCoordinator, HealthMonitor>();
            services.AddSingleton<IAiPipelineBuilder, AiPipelineBuilder>();

            // Advanced AI Service Components (Split God Object - Phase 5)
            services.AddSingleton<AdvancedAiConfig>(new AdvancedAiConfig());
            services.AddSingleton<IAiMemoryCoordinator, AiMemoryCoordinator>();
            services.AddSingleton<IAiWorldStateCoordinator, AiWorldStateCoordinator>();
            services.AddSingleton<IAiValidationCoordinator, AiValidationCoordinator>();
            services.AddSingleton<IAiTimelineCoordinator, AiTimelineCoordinator>();
            services.AddSingleton<IAiEventCoordinator, AiEventCoordinator>();
            services.AddSingleton<IAiRequestProcessor, AiRequestProcessor>();
            services.AddSingleton<IAiNarrativeGenerator, AiNarrativeGenerator>();
            services.AddSingleton<IAdvancedAiService, AdvancedAiService>();

            // Specialist Agents
            services.AddSingleton<ISpecialistAgent, NarrativeSpecialist>();
            services.AddSingleton<ISpecialistAgent, LoreSpecialist>();
            services.AddSingleton<ISpecialistAgent, SystemSpecialist>();

            // Router ("The Brain")
            services.AddSingleton<IIntentRouter, IntentRouter>();

            return services;
        }

        /// <summary>
        /// Registers emulator enhancement services.
        /// </summary>
        public static IServiceCollection AddEmulatorEnhancementServices(this IServiceCollection services)
        {
            services.AddSingleton<LiveCommentaryService>(sp =>
            {
                var llm = sp.GetRequiredService<ILlmService>();
                return new LiveCommentaryService(llm);
            });

            services.AddSingleton<DreamSequenceService>(sp =>
            {
                var llm = sp.GetRequiredService<ILlmService>();
                return new DreamSequenceService(llm);
            });

            services.AddSingleton<TimeCapsuleService>(sp =>
            {
                var llm = sp.GetRequiredService<ILlmService>();
                return new TimeCapsuleService(llm);
            });

            services.AddSingleton<MemoryEvolutionService>(sp =>
            {
                var llm = sp.GetRequiredService<ILlmService>();
                return new MemoryEvolutionService(llm);
            });

            services.AddSingleton<ShaderStudioService>();
            services.AddSingleton<RetroRewindService>();

            return services;
        }

        /// <summary>
        /// Registers MUGEN and fighting game services.
        /// </summary>
        public static IServiceCollection AddMugenServices(this IServiceCollection services)
        {
            services.AddSingleton<MugenService>();
            services.AddSingleton<MugenTournamentService>();

            services.AddSingleton<CharacterFusionService>(sp =>
            {
                var llm = sp.GetRequiredService<ILlmService>();
                return new CharacterFusionService(llm);
            });

            services.AddSingleton<CrossGameBattleService>(sp =>
            {
                var llm = sp.GetRequiredService<ILlmService>();
                return new CrossGameBattleService(llm);
            });

            return services;
        }

        /// <summary>
        /// Registers media recording and screenshot services.
        /// </summary>
        public static IServiceCollection AddMediaServices(this IServiceCollection services)
        {
            // Use existing singletons if available, or create new
            services.AddSingleton<RecordingService>(sp => RecordingService.Instance);
            services.AddSingleton<ScreenshotService>(sp => ScreenshotService.Instance);
            services.AddSingleton<MontageGenerator>(sp => MontageGenerator.Instance);
            services.AddSingleton<TtsService>(sp =>
            {
                var config = sp.GetRequiredService<IAppConfiguration>();
                var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                return new TtsService(config, httpClient);
            });


            return services;
        }

        /// <summary>
        /// Registers input-related services.
        /// </summary>
        public static IServiceCollection AddInputServices(this IServiceCollection services)
        {
            services.AddSingleton<HotkeyService>(sp => HotkeyService.Instance);
            services.AddSingleton<GamepadService>(sp => GamepadService.Instance);

            return services;
        }

        /// <summary>
        /// Registers accessibility services.
        /// </summary>
        public static IServiceCollection AddAccessibilityServices(this IServiceCollection services)
        {
            services.AddSingleton<ThemeService>(sp => ThemeService.Instance);
            services.AddSingleton<AccessibilityService>(sp => AccessibilityService.Instance);
            services.AddSingleton<NotificationService>(sp => NotificationService.Instance);

            return services;
        }

        /// <summary>
        /// Registers account and cloud services.
        /// </summary>
        public static IServiceCollection AddAccountServices(this IServiceCollection services)
        {
            services.AddHttpClient("AuthService", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddSingleton<AuthService>();
            services.AddHttpClient("CloudSyncService", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(10);
            });
            services.AddSingleton<CloudSyncService>();
            services.AddSingleton<BackupService>();

            // These may not have static instances yet
            services.AddSingleton<FriendsService>();
            services.AddSingleton<ProfileService>();
            services.AddSingleton<LeaderboardService>();

            return services;
        }

        /// <summary>
        /// Registers gamification services.
        /// </summary>
        public static IServiceCollection AddGamificationServices(this IServiceCollection services)
        {
            services.AddSingleton<AchievementService>();
            services.AddSingleton<ChallengeService>();

            return services;
        }

        /// <summary>
        /// Registers netplay services.
        /// </summary>
        public static IServiceCollection AddNetplayServices(this IServiceCollection services)
        {
            services.AddSingleton<NetplayService>(sp => NetplayService.Instance);
            services.AddSingleton<SpectatorService>(sp => SpectatorService.Instance);

            return services;
        }

        /// <summary>
        /// Registers ROM management services.
        /// </summary>
        public static IServiceCollection AddRomServices(this IServiceCollection services)
        {
            services.AddHttpClient("CheatService", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddSingleton<CheatService>();
            services.AddSingleton<PatchService>(sp => PatchService.Instance);

            return services;
        }

        /// <summary>
        /// Registers timeline and rewind services.
        /// </summary>
        public static IServiceCollection AddTimelineServices(this IServiceCollection services)
        {
            services.AddSingleton<ITimelineService, TimelineService>();
            services.AddSingleton<IRewindService, RewindService>();
            services.AddSingleton<StateDeltaService>();

            return services;
        }
    }
}
