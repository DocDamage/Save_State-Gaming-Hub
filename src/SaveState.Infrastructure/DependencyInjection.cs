using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using SaveState.Core.Common;
using SaveState.Core.Common.Configuration;
using SaveState.Core.Common.Services;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Services;
using SaveState.Infrastructure.UserManagement;
using SaveState.Infrastructure.Ai.Providers;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.RomManagement;
using SaveState.Core.Mugen.ComboDatabase.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Core.OpenMK.Services;
using SaveState.Infrastructure.Repositories;
using SaveState.Infrastructure.Mugen;
using SaveState.Infrastructure.Mugen.ComboDatabase;
using SaveState.Infrastructure.Mugen.IkemenGo;
using SaveState.Infrastructure.Mugen.ComboDatabase.Managers;
using SaveState.Infrastructure.Mugen.IkemenGo.Managers;
using SaveState.Infrastructure.Mugen.SpriteAnimation.Managers;
using SaveState.Application.Mugen.Services.PredictiveAnalytics.Managers;
using SaveState.Application.Mugen.Services.Blockchain.Managers;
using SaveState.Application.Mugen.Services.Graphics.Managers;
using SaveState.Application.Mugen.Services.SoundDesign;
using SaveState.Infrastructure.Mugen.StoryMode;
using SaveState.Infrastructure.Mugen.StoryMode.Managers;
using SaveState.Infrastructure.Mugen.PerformanceProfiler;
using SaveState.Infrastructure.Mugen.PerformanceProfiler.Managers;
using SaveState.Application.Mugen.Services.SymbioticPartner;
using SaveState.Infrastructure.Mugen.ReplayAnalysis.Managers;
using SaveState.Infrastructure.OpenMK;
using SaveState.Infrastructure.OpenMK.Services.OpenMK;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Memory;
using SaveState.Core.Ai.Learning;
using SaveState.Core.Ai.Context;
using SaveState.Core.Ai.Voice;
using SaveState.Core.Sync;
using SaveState.Infrastructure.Ai;
using SaveState.Infrastructure.Ai.Resilience;
using SaveState.Infrastructure.Ai.Knowledge;
using SaveState.Infrastructure.Ai.Memory;
using SaveState.Infrastructure.Ai.Learning;
using SaveState.Infrastructure.Ai.Context;
using SaveState.Infrastructure.Ai.Voice;
using SaveState.Infrastructure.Assistant;
using SaveState.Infrastructure.Sync;
using SaveState.Infrastructure.External;
using SaveState.Infrastructure.GameLibrary.Services;
using SaveState.Infrastructure.Persistence;
using SaveState.Infrastructure.RomManagement.Services;
using SaveState.Infrastructure.Common;
using SaveState.Infrastructure.Health;
using SaveState.Infrastructure.CrossPlatform;
using SaveState.Infrastructure.Resilience;
using SaveState.Infrastructure.Performance;
using SaveState.Infrastructure.DataPortability;
using SaveState.Core.DataPortability;
using SaveState.Core.RetroArch.Services;
using SaveState.Infrastructure.RetroArch;
using SaveState.Infrastructure.RetroArch.RetroArchCloudSync;
using SaveState.Infrastructure.Subscriptions;
using SaveState.Infrastructure.GameDeals;
using SaveState.Infrastructure.SmartLauncher;
using SaveState.Core.SmartLauncher;

namespace SaveState.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ISaveStateDbContext, SaveStateDbContext>(options =>
        {
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"), sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });
        })
        .AddDbContextFactory<SaveStateDbContext>(options =>
        {
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"), sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });
        });

        // Repositories
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IPlatformRepository, PlatformRepository>();
        services.AddScoped<IRomFileRepository, RomFileRepository>();
        services.AddScoped<IEmulatorRepository, EmulatorRepository>
        ();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IPlatformExtensionRegistry, PlatformExtensionRegistry>();

        // ROM Management Services
        services.AddScoped<SaveState.Core.RomManagement.Services.IRomVerificationService, SaveState.Core.RomManagement.Services.RomVerificationService>();
        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IBacklogRepository, BacklogRepository>();
        services.AddScoped<SaveState.Core.Analytics.IGamingGoalRepository, Repositories.GamingGoalRepository>();
        services.AddScoped<SaveState.Core.GameLibrary.IVirtualCollectionRepository, Repositories.VirtualCollectionRepository>();
        services.AddScoped<SaveState.Core.SaveStates.ISaveStateRepository, Repositories.SaveStateRepository>();
        services.AddScoped<ISaveStateBranchRepository, SaveStateBranchRepository>();
        services.AddScoped<SaveState.Core.Input.IControllerProfileRepository, Repositories.ControllerProfileRepository>();
        services.AddScoped<IGameNoteRepository, GameNoteRepository>();
        services.AddScoped<IGameModRepository, GameModRepository>();
        services.AddScoped<IGameMediaRepository, GameMediaRepository>();

        // Session Tracking Services
        services.AddScoped<ISessionTrackingService, SessionTrackingService>();

        // Game Detection Services
        services.AddSingleton<GameLibrary.Detection.SteamLibraryScanner>();
        services.AddSingleton<GameLibrary.Detection.EpicLibraryScanner>();
        services.AddSingleton<GameLibrary.Detection.GogLibraryScanner>();
        services.AddSingleton<GameLibrary.Detection.EmulatorRomScanner>();
        services.AddSingleton<IGameDetectorService, GameLibrary.Detection.GameDetectorService>();

        // Social Services
        services.AddSingleton<SaveState.Core.Social.IDiscordPresenceService, Social.DiscordPresenceService>();
        services.AddScoped<SaveState.Core.Social.IGameReviewRepository, Repositories.GameReviewRepository>();
        services.AddScoped<SaveState.Core.Social.Services.IGameReviewService, Social.GameReviewService>();
        services.AddScoped<SaveState.Core.Social.ISharedCollectionRepository, Repositories.SharedCollectionRepository>();
        services.AddScoped<SaveState.Core.Social.Services.ISharedCollectionService, Social.SharedCollectionService>();
        services.AddScoped<SaveState.Core.Social.IFriendRepository, Repositories.FriendRepository>();
        services.AddScoped<SaveState.Core.Social.Services.IFriendActivityService, Social.FriendActivityService>();

        services.AddScoped<IAchievementService, AchievementService>();
        services.AddScoped<IModManagementService, ModManagementService>();
        services.AddScoped<IGameMediaService, GameMediaService>();

        // Plugin system
        services.AddSingleton<SaveState.Core.Plugins.Services.IPluginManager, Plugins.PluginManager>();
        services.AddHostedService<Plugins.PluginLoaderBackgroundService>();

        // User Services
        services.AddSingleton<SaveState.Core.Common.Services.IUserPreferencesService, SaveState.Infrastructure.Services.UserPreferencesService>();
        services.AddSingleton<ITimeProvider>(_ => SystemTimeProvider.Instance);
        services.AddSingleton<ITaskRunner, TaskRunner>();

        // Culture and Localization Services
        services.AddSingleton<SaveState.Core.Common.Services.ICultureManager, CultureManager>();

        // Accessibility Services
        services.AddSingleton<SaveState.Core.Common.Services.IAccessibilityService, AccessibilityService>();

        // MUGEN Fusion, Move, and Creative Services
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenFusionService, Mugen.MugenFusionService>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMoveCreationService, SaveState.Infrastructure.Mugen.MoveCreationService>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenGraphicsEngine, SaveState.Infrastructure.Mugen.MugenGraphicsEngine>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenSoundDesignStudio, SaveState.Infrastructure.Mugen.MugenSoundDesignStudio>();

        // Sprite Animation Service - Manager Pattern
        services.AddScoped<SpriteManager>();
        services.AddScoped<AnimationManager>();
        services.AddScoped<PaletteManager>();
        services.AddScoped<PreviewManager>();
        services.AddScoped<BatchOperationManager>();
        services.AddScoped<ProjectManager>();
        services.AddScoped<ISpriteAnimationService, SaveState.Infrastructure.Mugen.SpriteAnimation.SpriteAnimationService>();

        // Predictive Analytics Engine - Manager Pattern
        services.AddScoped<MatchPredictionManager>();
        services.AddScoped<PlayerSkillManager>();
        services.AddScoped<MachineLearningManager>();
        services.AddScoped<PerformanceForecastingManager>();
        services.AddScoped<AnalyticsReportingManager>();
        services.AddScoped<SaveState.Application.Mugen.Services.PredictiveAnalytics.IPredictiveAnalyticsEngine, SaveState.Application.Mugen.Services.PredictiveAnalytics.PredictiveAnalyticsEngine>();

        // Combo Database - Manager Pattern
        services.AddScoped<ComboCrudManager>();
        services.AddScoped<ComboSearchManager>();
        services.AddScoped<ComboRatingManager>();
        services.AddScoped<ComboPracticeManager>();
        services.AddScoped<ComboSubmissionManager>();
        services.AddScoped<ComboCollectionManager>();
        services.AddScoped<ComboImportExportManager>();
        services.AddScoped<ComboAnalysisManager>();
        services.AddScoped<IComboDatabaseService, SaveState.Infrastructure.Mugen.ComboDatabase.ComboDatabaseService>();

        // Blockchain Service - Manager Pattern
        services.AddScoped<NftManager>();
        services.AddScoped<WalletManager>();
        services.AddScoped<MarketplaceManager>();
        services.AddScoped<StorageManager>();
        services.AddScoped<SaveState.Application.Mugen.Services.Blockchain.IBlockchainService, SaveState.Application.Mugen.Services.Blockchain.BlockchainService>();

        // Advanced Graphics Engine - Manager Pattern
        services.AddScoped<ShaderManager>();
        services.AddScoped<LightingManager>();
        services.AddScoped<PostProcessingManager>();
        services.AddScoped<ParticleManager>();
        services.AddScoped<SceneManager>();
        services.AddScoped<SaveState.Application.Mugen.Services.Graphics.IAdvancedGraphicsEngine, SaveState.Application.Mugen.Services.Graphics.AdvancedGraphicsEngine>();

        // Sound Design Studio - Manager Pattern
        services.AddScoped<SoundProjectManager>();
        services.AddScoped<SoundTrackManager>();
        services.AddScoped<SoundEffectManager>();
        services.AddScoped<SoundAnalysisManager>();
        services.AddScoped<SoundMixingManager>();
        services.AddScoped<SoundSpatialManager>();
        services.AddScoped<SoundRenderManager>();

        // Story Mode - Manager Pattern
        services.AddScoped<StoryProjectManager>();
        services.AddScoped<StoryChapterManager>();
        services.AddScoped<StorySceneManager>();
        services.AddScoped<StoryCastingManager>();
        services.AddScoped<StoryContentManager>();
        services.AddScoped<StoryBattleManager>();
        services.AddScoped<StoryTestingManager>();
        services.AddScoped<StoryAssetManager>();
        services.AddScoped<IStoryModeService, StoryModeService>();

        // Performance Profiler - Manager Pattern
        services.AddScoped<ProfilingSessionManager>();
        services.AddScoped<MetricsCollectionManager>();
        services.AddScoped<CharacterProfilerManager>();
        services.AddScoped<BattleProfilerManager>();
        services.AddScoped<BottleneckAnalyzerManager>();
        services.AddScoped<OptimizationManager>();
        services.AddScoped<IPerformanceProfilerService, PerformanceProfilerService>();

        // Symbiotic Partner - Manager Pattern
        services.AddScoped<PartnerManager>();
        services.AddScoped<SymbiosisManager>();
        services.AddScoped<EvolutionManager>();
        services.AddScoped<AdaptationManager>();
        services.AddScoped<CommunicationManager>();
        services.AddScoped<PartnerAnalyticsManager>();

        // Replay Analysis - Manager Pattern
        services.AddScoped<ReplayParsingManager>();
        services.AddScoped<HighlightReelManager>();
        services.AddScoped<QueryManager>();
        services.AddScoped<ComparisonManager>();
        // Note: ComboDetectionManager and StatisticsManager are static classes - no DI registration needed

        // IKEMEN GO Services - Manager Pattern
        services.AddScoped<IkemenGoInstallationManager>();
        services.AddScoped<IkemenGoConfigurationManager>();
        services.AddScoped<IkemenGoLaunchManager>();
        services.AddHttpClient<IkemenGoNetworkManager>().AddResiliencePolicies("IkemenGo");
        services.AddScoped<IkemenGoModuleManager>();
        services.AddScoped<IkemenGoReplayManager>();
        services.AddScoped<IkemenGoAnalyticsManager>();
        services.AddScoped<IkemenGoMigrationManager>();
        services.AddScoped<IIkemenGoService, IkemenGoService>();

        // OpenMK Services
        services.AddScoped<IOpenMKCharacterRepository, OpenMKCharacterRepository>();
        services.AddScoped<IOpenMKProgressRepository, OpenMKProgressRepository>();
        services.AddScoped<IOpenMKMatchStateRepository, OpenMKMatchStateRepository>();
        services.AddScoped<IOpenMKService, OpenMKService>();
        services.AddScoped<IOpenMKMatchService, OpenMKMatchService>();
        services.AddScoped<IOpenMKStoryService, OpenMKStoryService>();
        services.AddScoped<IOpenMKProgressionService, OpenMKProgressionService>();

        // Machine Learning Services
        services.AddScoped<SaveState.Core.Mugen.Services.IMachineLearningService, SaveState.Infrastructure.Mugen.MachineLearningService>();
        services.AddScoped<SaveState.Core.Mugen.Repositories.IPlayerDataRepository, SaveState.Infrastructure.Mugen.PlayerDataRepository>();
        // Game Providers
        services.AddScoped<IGameProvider, SteamProvider>();
        services.AddScoped<IGameProvider, GogProvider>();
        services.AddScoped<IGameProvider, EpicProvider>();

        // External API Clients (with resilience policies)
        services.AddHttpClient<ISteamApiClient, SteamApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.steampowered.com/");
        })
        .AddResiliencePolicies("Steam");

        services.AddHttpClient<IGogApiClient, GogApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.gog.com/");
        })
        .AddResiliencePolicies("GOG");

        services.AddHttpClient<IEpicApiClient, EpicApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.epicgames.dev/");
        })
        .AddResiliencePolicies("Epic");

        services.AddHttpClient<OneDriveStorageProvider>(client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/");
        })
        .AddResiliencePolicies("OneDrive");

        services.AddHttpClient<GoogleDriveStorageProvider>()
            .AddResiliencePolicies("GoogleDrive");

        services.AddHttpClient<ICloudAuthenticationService, CloudAuthenticationService>()
            .AddResiliencePolicies("CloudAuth");

        services.AddHttpClient("ModUpdates")
            .AddResiliencePolicies("ModUpdates");

        // RetroAchievements.org API Client
        services.AddHttpClient<SaveState.Core.Achievements.IRetroAchievementsClient, RetroAchievementsClient>(client =>
        {
            client.BaseAddress = new Uri("https://retroachievements.org/API/");
        })
        .AddResiliencePolicies("RetroAchievements");

        // HowLongToBeat API (game completion time data)
        services.AddHttpClient<IHowLongToBeatService, HowLongToBeatService>(client =>
        {
            client.BaseAddress = new Uri("https://howlongtobeat.com/");
        })
        .AddResiliencePolicies("HowLongToBeat");

        // IsThereAnyDeal API (game price tracking)
        services.AddHttpClient<IGamePriceService, GamePriceService>(client =>
        {
            client.BaseAddress = new Uri("https://api.isthereanydeal.com/");
        })
        .AddResiliencePolicies("GamePrices");

        // Cloud Sync Services
        services.AddSingleton<SaveState.Core.Sync.ISyncService, Sync.SyncService>();

        // Caching
        services.AddDistributedMemoryCache();
        services.AddMemoryCache();
        services.AddScoped<ICacheService, MemoryCacheService>();

        // Rate Limiting
        services.AddScoped<SaveState.Core.Common.Services.IRateLimiter, RateLimiter>();

        // Authentication Services
        services.AddScoped<SaveState.Core.UserManagement.Services.IJwtTokenService, JwtTokenService>();
        services.AddScoped<SaveState.Core.UserManagement.Services.IPasswordHasher, PasswordHasher>();
        services.AddScoped<SaveState.Core.UserManagement.Services.IUserContextService, UserManagement.UserContextService>();

        // Repositories (User Management)
        services.AddScoped<SaveState.Core.UserManagement.Repositories.IUserRepository, UserManagement.UserRepository>();
        services.AddScoped<SaveState.Core.UserManagement.Repositories.IRoleRepository, UserManagement.RoleRepository>();
        services.AddScoped<SaveState.Core.UserManagement.Repositories.IApiKeyRepository, UserManagement.ApiKeyRepository>();

        // Metadata Services
        services.AddScoped<IMetadataService, IgdbMetadataService>();
        services.Decorate<IMetadataService, ResilientMetadataService>();

        // Cover Art Services
        services.AddScoped<ICoverArtService, GameLibrary.CoverArtService>();
        services.AddScoped<IImageResizer, ImageResizer>();

        // External API Services
        services.AddHttpClient<IHowLongToBeatService, HowLongToBeatService>();

        // Analytics Services
        services.AddScoped<SaveState.Core.Analytics.Services.IAnalyticsService, Analytics.AnalyticsService>();
        services.AddSingleton<SaveState.Core.Analytics.Services.IStreakCalculator, Analytics.StreakCalculator>();
        services.AddScoped<SaveState.Core.Analytics.Services.ICompletionPredictionService, Analytics.CompletionPredictionService>();
        services.AddScoped<SaveState.Core.Analytics.Services.IGoalService, Analytics.GoalService>();
        services.AddSingleton<SaveState.Core.Analytics.Services.IRealTimeNotificationService, Analytics.RealTimeNotificationService>();
        services.AddScoped<SaveState.Core.Analytics.Services.IAnalyticsExportService, Analytics.AnalyticsExportService>();

        // Backlog Services
        services.AddScoped<SaveState.Core.GameLibrary.Services.IBacklogService, GameLibrary.Services.BacklogService>();

        // Virtual Collection Services
        services.AddScoped<SaveState.Core.GameLibrary.Services.IVirtualCollectionService, GameLibrary.VirtualCollectionService>();

        // Smart Categorization Services
        services.AddScoped<SaveState.Core.GameLibrary.Services.ISmartCategorizationService, GameLibrary.SmartCategorizationService>();

        // Recommendation Services
        services.AddScoped<SaveState.Core.Recommendations.Services.IRecommendationService, Recommendations.RecommendationService>();
        services.AddScoped<SaveState.Core.Recommendations.Services.IGameRecommendationService, Recommendations.GameRecommendationService>();

        // Plugin Services
        services.AddSingleton<SaveState.Core.Plugins.Services.IPluginDependencyResolver, SaveState.Core.Plugins.Services.PluginDependencyResolver>();
        services.AddSingleton<SaveState.Core.Plugins.Services.IPluginSettingsService, SaveState.Core.Plugins.Services.PluginSettingsService>();

        // Assistant Services
        services.AddScoped<SaveState.Core.Assistant.Services.IGameAssistantService, Assistant.GameAssistantService>();
        services.AddEyeTrackingMonitor();

        // Phase 1.2: AI-Powered Game Assistant Services
        // ML-based Difficulty Analysis
        services.AddSingleton<SaveState.Core.AI.Assistant.IDifficultyAnalyzer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AI.ML.DifficultyAnalyzer>>();
            var timeProvider = sp.GetRequiredService<ITimeProvider>();
            var modelPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SaveStateReborn",
                "MLModels",
                "difficulty_model.zip");
            return new AI.ML.DifficultyAnalyzer(logger, timeProvider, modelPath);
        });

        // Game Session Monitor (Background Service)
        services.AddSingleton<SaveState.Core.AI.Assistant.IGameSessionMonitor, AI.Assistant.GameSessionMonitor>();
        services.AddHostedService<AI.Assistant.GameSessionMonitor>(sp =>
            (AI.Assistant.GameSessionMonitor)sp.GetRequiredService<SaveState.Core.AI.Assistant.IGameSessionMonitor>());

        // User Preference Learning Service
        services.AddSingleton<SaveState.Core.AI.Assistant.IUserPreferenceLearningService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AI.Assistant.UserPreferenceLearningService>>();
            var timeProvider = sp.GetRequiredService<ITimeProvider>();
            var prefsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SaveStateReborn",
                "UserPreferences.json");
            return new AI.Assistant.UserPreferenceLearningService(logger, timeProvider, prefsPath);
        });

        // Save State Services
        services.AddScoped<SaveState.Core.SaveStates.Services.ISaveStateManager, SaveStates.SaveStateManager>();
        services.AddScoped<SaveState.Core.SaveStates.Services.ISaveStateBranchingService, SaveStates.SaveStateBranchingService>();
        services.AddScoped<SaveState.Core.SaveStates.Services.IAutoSaveManager, SaveStates.AutoSaveManager>();
        services.AddScoped<SaveState.Core.SaveStates.Services.ICloudSaveEncryptionService, SaveStates.CloudSaveEncryptionService>();
        services.AddScoped<SaveState.Core.SaveStates.Services.ISaveStateCloudService, SaveStates.SaveStateCloudService>();
        services.AddSingleton<SaveStates.SaveStateCloudSyncMonitor>();
        services.AddSingleton<SaveState.Core.SaveStates.Services.ISaveStateCloudSyncMonitor>(
            serviceProvider => serviceProvider.GetRequiredService<SaveStates.SaveStateCloudSyncMonitor>());
        services.AddSingleton<SaveStates.SaveStateCloudSyncDaemonProcessor>();
        services.AddHostedService<SaveStates.SaveStateCloudSyncBackgroundService>();

        // Input Services
        services.AddScoped<SaveState.Core.Input.Services.IInputService, Input.InputService>();
        services.AddScoped<SaveState.Core.Input.Services.IControllerProfileService, Input.ControllerProfileService>();
        services.AddScoped<SaveState.Core.Input.Services.ISteamDeckManager, Input.SteamDeckManager>();
        services.AddScoped<SaveState.Core.Input.Services.ITouchController, Input.TouchController>();

        // Performance Services
        services.AddSingleton<SaveState.Core.Performance.Services.IPerformanceMonitor, Performance.PerformanceMonitor>();
        services.AddSingleton<SaveState.Core.Performance.Services.IBatteryOptimizer, Performance.BatteryOptimizer>();

        // Phase 3: Gaming Environment Optimization Services
        services.AddSingleton<SaveState.Core.Performance.Services.ISystemResourceManager, Performance.SystemResourceManager>();
        services.AddSingleton<SaveState.Core.Performance.Services.IDisplayCalibrator, Performance.DisplayCalibrator>();

        // Audio Optimizer Service (platform-specific factory - Windows uses CoreAudio, others use no-op)
        services.AddAudioOptimizer();

        // Phase 4: Immersive Launch Experience Services
        services.AddScoped<SaveState.Core.GameLibrary.Services.ILaunchExperienceManager, GameLibrary.Services.LaunchExperienceManager>();
        services.AddScoped<SaveState.Core.GameLibrary.Services.IGameBriefingService, GameLibrary.Services.GameBriefingService>();

        // Phase 5: Cloud Gaming & Network Quality Services
        services.AddSingleton<SaveState.Core.Sync.Services.ICloudCatalogService, Sync.CloudCatalogService>();
        services.AddScoped<SaveState.Core.Sync.Services.ICloudGamingManager, Sync.CloudGamingManager>();
        services.AddScoped<SaveState.Core.Sync.INetworkQualityHistoryRepository, Repositories.NetworkQualityHistoryRepository>();
        services.AddSingleton<SaveState.Core.Sync.Services.INetworkQualityMonitor, Sync.NetworkQualityMonitor>();

        // Network Optimizer Service (platform-specific factory)
        services.AddNetworkOptimizer();

        // Cloud Catalog HTTP Client
        services.AddHttpClient("CloudCatalog")
            .AddResiliencePolicies("CloudCatalog");

        // Phase 6: Voice Command Integration Services
        services.AddScoped<SaveState.Core.Input.Services.IVoiceCommandService, Input.VoiceCommandService>();
        services.AddScoped<SaveState.Core.Ai.Services.ISpeechRecognitionService, Ai.SpeechRecognitionService>();

        // Phase 7: Performance Optimization Services - REQUIRED
        services.AddSingleton<Performance.QueryOptimizer>();
        services.AddSingleton<Performance.MemoryProfiler>();

        // Phase 7: Cloud Service Integration - REQUIRED (with resilience policies)
        services.AddHttpClient("AzureSpeech")
            .AddTypedClient<Cloud.AzureSpeechService>((client, sp) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var apiKey = config["CloudServices:Azure:SpeechKey"] ?? "";
                return new Cloud.AzureSpeechService(client, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Cloud.AzureSpeechService>>(), apiKey);
            })
            .AddResiliencePolicies("AzureSpeech");

        services.AddHttpClient("GoogleCloud")
            .AddTypedClient<Cloud.GoogleCloudService>((client, sp) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var apiKey = config["CloudServices:Google:ApiKey"] ?? "";
                var projectId = config["CloudServices:Google:ProjectId"] ?? "";
                var rateLimiter = sp.GetRequiredService<SaveState.Core.Common.Services.IRateLimiter>();
                var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                return new Cloud.GoogleCloudService(
                    client,
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Cloud.GoogleCloudService>>(),
                    rateLimiter,
                    cache,
                    apiKey,
                    projectId);
            })
            .AddResiliencePolicies("GoogleCloud");

        services.AddHttpClient("OpenAI")
            .AddTypedClient<Cloud.OpenAiService>((client, sp) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var apiKey = config["CloudServices:OpenAi:ApiKey"] ?? "";
                return new Cloud.OpenAiService(client, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Cloud.OpenAiService>>(), apiKey);
            })
            .AddResiliencePolicies("OpenAI");

        // Phase 7 Session 2: Distributed Caching - REQUIRED
        services.AddSingleton<Caching.DistributedCacheService>();

        // Phase 7 Session 2: Cross-Platform Audio - REQUIRED
        services.AddSingleton<CrossPlatform.MacOS.MacOSAudioService>();
        services.AddSingleton<CrossPlatform.Linux.LinuxAudioService>();

        // Register audio services based on platform
        services.AddPlatformAudioServices();

        // Phase 7 Session 3: Machine Learning - REQUIRED
        services.AddSingleton<MachineLearning.TensorFlowMLService>();

        // Phase 7 Session 3: UI Theming - REQUIRED
        services.AddSingleton<Theming.ThemeService>();

        // Phase 7 Session 3: Advanced Analytics Reporting - REQUIRED
        services.AddSingleton<Analytics.AdvancedReportingService>();

        // Phase 7 Session 4: Multiplayer - REQUIRED
        services.AddSingleton<Multiplayer.MultiplayerService>();

        // Phase 7 Session 5: Streaming - REQUIRED
        services.AddSingleton<Streaming.StreamingService>();

        // Phase 7 Session 5: Tournaments - REQUIRED
        services.AddSingleton<Tournaments.TournamentService>();

        // Phase 7 Session 5: Social Features - REQUIRED
        services.AddSingleton<Social.SocialFeaturesService>();

        // Phase 7 Session 5: Community Marketplace - REQUIRED
        services.AddSingleton<Community.CommunityMarketplaceService>();

        // Phase 7 Session 5: Plugins - REQUIRED
        services.AddSingleton<Plugins.PluginService>();

        // Phase 7 Session 6: Cloud Services (Azure, Google ML Engine, ML.NET) - REQUIRED
        services.AddSingleton<Cloud.AzureBlobStorageService>();
        services.AddSingleton<Cloud.GoogleCloudMLEngineService>();
        services.AddSingleton<Cloud.MLNetAdvancedModelsService>();

        // Phase 7 Session 6: Analytics - REQUIRED
        services.AddSingleton<Analytics.RealTimeAnalyticsDashboardService>();

        services.AddSingleton<Analytics.AnalyticsQueryBuilderService>();
        services.AddScoped<IDataExportService, DataExportService>();

        // Phase 7 Session 6: UI Enhancements - REQUIRED
        services.AddSingleton<UIEnhancements.AnimationEngineService>();
        services.AddSingleton<UIEnhancements.ResponsiveDesignService>();
        services.AddSingleton<UIEnhancements.AdvancedAccessibilityService>();

        // Phase 7: Automation Services
        services.AddScoped<SaveState.Core.Automation.Services.IMacroRecorder, Automation.MacroRecorder>();
        services.AddScoped<SaveState.Core.Automation.Services.IMacroPlayer, Automation.MacroPlayer>();

        // Phase 8: Game Memory Intelligence Services
        services.AddSingleton<SaveState.Core.GameLibrary.Services.IGameMemoryReader, GameLibrary.Services.GameMemoryReader>();
        services.AddSingleton<SaveState.Core.GameLibrary.Services.IPerformanceProfiler, GameLibrary.Services.PerformanceProfiler>();
        services.AddScoped<SaveState.Core.GameLibrary.Services.IAiCoachService, GameLibrary.Services.AiCoachService>();
        services.AddScoped<SaveState.Core.Social.Services.ISocialService, Social.Services.SocialService>();
        services.AddSingleton<SaveState.Core.GameLibrary.Services.IMemoryPatternDatabase, GameLibrary.Services.MemoryPatternDatabase>();
        services.AddScoped<SaveState.Core.Automation.Services.IMacroManager, Automation.MacroManager>();
        services.AddScoped<SaveState.Core.Automation.Services.IMacroService, Automation.MacroService>();
        services.AddScoped<SaveState.Core.Automation.Services.IBackupScheduler, Automation.BackupScheduler>();
        services.AddHostedService<Automation.AutomationWorker>();
        services.AddScoped<SaveState.Core.Automation.Services.IWorkflowAutomationService, Automation.WorkflowAutomationService>();

        // Phase 9: MUGEN Tournament Features
        services.AddScoped<SaveState.Core.Mugen.IMugenCharacterRepository, Repositories.MugenCharacterRepository>();

        // MUGEN Repositories (Phase 3 Implementation)
        services.AddScoped<SaveState.Core.Mugen.IMugenTournamentRepository, Repositories.MugenTournamentRepository>();
        services.AddScoped<SaveState.Core.Mugen.IMugenMatchHistoryRepository, Repositories.MugenMatchHistoryRepository>();
        services.AddScoped<SaveState.Core.Mugen.IMugenCollectionRepository, Repositories.MugenCollectionRepository>();
        services.AddScoped<SaveState.Core.Mugen.IMugenTrainingRepository, Repositories.MugenTrainingRepository>();

        services.AddScoped<SaveState.Core.Mugen.Services.IDeathMatchSimulator, Mugen.DeathMatchSimulator>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMatchPredictionEngine, Mugen.MatchPredictionEngine>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenTournamentService, Mugen.MugenTournamentService>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenStatsService, Mugen.MugenStatsService>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenCoachService, Mugen.MugenCoachService>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenCollectionService, Mugen.MugenCollectionService>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenTrainingService, Mugen.MugenTrainingService>();
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenFusionService, Mugen.MugenFusionService>();

        // Named HttpClient for Twitch authentication (used by IGDB)
        services.AddHttpClient("TwitchAuth")
            .AddResiliencePolicies("TwitchAuth");

        services.AddHttpClient<IIgdbApiClient, IgdbApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.igdb.com/v4/");
        })
        .AddResiliencePolicies("IGDB");

        // SteamGridDB API Client
        services.AddHttpClient<ISteamGridDbApiClient, SteamGridDbApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SteamGridDbOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("SaveState", "1.0"));
        })
        .AddResiliencePolicies("SteamGridDB");

        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<SaveStateDbContext>("Database", tags: new[] { "database", "infrastructure" })
            .AddCheck<DatabaseHealthCheck>("Database Detailed", tags: new[] { "database", "infrastructure" })
            .AddCheck<MetricsHealthCheck>("Application Metrics", tags: new[] { "metrics", "performance" })
            .AddCheck<ExternalApiHealthCheck>("External APIs", tags: new[] { "external", "apis", "dependencies" })
            .AddCheck<ResourceHealthCheck>("System Resources", tags: new[] { "system", "resources", "infrastructure" })
            .AddCheck<DependencyHealthCheck>("Dependencies", tags: new[] { "dependencies", "infrastructure" })
            .AddCheck<PerformanceHealthCheck>("Performance", tags: new[] { "performance", "monitoring" });

        // Application Metrics
        services.AddSingleton<SaveState.Core.Monitoring.IApplicationMetrics, Monitoring.ApplicationMetricsService>();

        // Performance Monitoring
        services.AddSingleton<Monitoring.PerformanceMonitorService>();
        services.AddHostedService<Monitoring.PerformanceMonitorBackgroundService>();

        // Database Monitoring
        services.AddScoped<Monitoring.DatabaseConnectionMonitor>();

        // Cache Monitoring
        services.AddSingleton<ICachePerformanceMonitor, Monitoring.CachePerformanceMonitor>();

        // Error Tracking
        services.AddSingleton<Monitoring.ErrorTrackingService>();

        // AI Providers (with HttpClient and resilience policies)
        services.AddHttpClient<ILlmProvider, OpenAiProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        })
        .AddResiliencePolicies("OpenAI-LLM");

        services.AddHttpClient<ILlmProvider, GroqProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<GroqOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        })
        .AddResiliencePolicies("Groq");

        services.AddSingleton<ILlmProvider, LocalEmbeddedProvider>();

        // Configuration with validation
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection("OpenAi"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.BaseUrl) &&
                       !string.IsNullOrEmpty(options.ApiKey) &&
                       !string.IsNullOrEmpty(options.DefaultModel) &&
                       Uri.IsWellFormedUriString(options.BaseUrl, UriKind.Absolute);
            }, "Invalid OpenAI configuration")
            .ValidateOnStart();

        services.AddOptions<GroqOptions>()
            .Bind(configuration.GetSection("Groq"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.BaseUrl) &&
                       !string.IsNullOrEmpty(options.ApiKey) &&
                       !string.IsNullOrEmpty(options.DefaultModel) &&
                       Uri.IsWellFormedUriString(options.BaseUrl, UriKind.Absolute);
            }, "Invalid Groq configuration")
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.Common.Configuration.AiOptions>()
            .Bind(configuration.GetSection("Ai"))
            .ValidateDataAnnotations()
            .Validate(options => options != null, "AI options cannot be null")
            .ValidateOnStart();

        services.AddOptions<SteamOptions>()
            .Bind(configuration.GetSection("Steam"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.ApiKey) &&
                       !string.IsNullOrEmpty(options.SteamId) &&
                       long.TryParse(options.SteamId, out _);
            }, "Invalid Steam configuration")
            .ValidateOnStart();

        services.AddOptions<GogOptions>()
            .Bind(configuration.GetSection("Gog"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.Username) &&
                       !string.IsNullOrEmpty(options.Password);
            }, "Invalid GOG configuration")
            .ValidateOnStart();

        services.AddOptions<EpicOptions>()
            .Bind(configuration.GetSection("Epic"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.AccountId) &&
                       !string.IsNullOrEmpty(options.AuthToken);
            }, "Invalid Epic configuration")
            .ValidateOnStart();

        services.AddOptions<IgdbOptions>()
            .Bind(configuration.GetSection("Igdb"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.ClientId) &&
                       !string.IsNullOrEmpty(options.ClientSecret);
            }, "Invalid IGDB configuration")
            .ValidateOnStart();

        services.AddOptions<SteamGridDbOptions>()
            .Bind(configuration.GetSection("SteamGridDB"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.ApiKey) &&
                       Uri.IsWellFormedUriString(options.BaseUrl, UriKind.Absolute) &&
                       options.MaxConcurrentRequests > 0 &&
                       options.CacheDurationHours > 0;
            }, "Invalid SteamGridDB configuration")
            .ValidateOnStart();

        services.AddOptions<ResilienceConfig>()
            .Bind(configuration.GetSection("Resilience"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return options.CircuitBreakerThreshold > 0 &&
                       options.CircuitBreakerDurationMs > 0 &&
                       options.MaxRetries >= 0 &&
                       options.InitialRetryDelayMs > 0 &&
                       options.RetryBackoffMultiplier >= 1.0 &&
                       options.DefaultTimeoutMs > 0;
            }, "Invalid resilience configuration")
            .ValidateOnStart();

        // Additional Configuration Validation
        services.AddOptions<SaveState.Core.Common.Configuration.MemoryOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Common.Configuration.MemoryOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.Common.Configuration.ApplicationOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Common.Configuration.ApplicationOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.Common.Configuration.DatabaseOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Common.Configuration.DatabaseOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Application.Common.Options.LaunchOptions>()
            .Bind(configuration.GetSection("Launch"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Application.AiGaming.Options.CheatDetectionOptions>()
            .Bind(configuration.GetSection("CheatDetection"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.Common.Configuration.RateLimitingOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Common.Configuration.RateLimitingOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Authentication Configuration
        services.AddOptions<SaveState.Core.UserManagement.Configuration.JwtOptions>()
            .Bind(configuration.GetSection(SaveState.Core.UserManagement.Configuration.JwtOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.UserManagement.Configuration.AuthenticationOptions>()
            .Bind(configuration.GetSection(SaveState.Core.UserManagement.Configuration.AuthenticationOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Localization Configuration
        services.AddOptions<SaveState.Core.Configuration.LocalizationOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Configuration.LocalizationOptions.Section))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return options.SupportedCultures.Contains(options.DefaultCulture) &&
                       options.CacheDurationDays > 0;
            }, "Invalid localization configuration")
            .ValidateOnStart();

        services.AddOptions<MugenOptions>()
            .Bind(configuration.GetSection(MugenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CloudSyncOptions>()
            .Bind(configuration.GetSection(CloudSyncOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // AI Services
        services.AddScoped<IKnowledgeStore, SqliteVectorStore>();
        services.AddScoped<SemanticKnowledgeClient>();
        services.AddScoped<IShortTermMemory, EnhancedShortTermMemory>();
        services.AddScoped<IAiOrchestrator, AiOrchestrator>();
        services.AddScoped<IKnowledgeBaseService, MarkdownKnowledgeBaseService>();
        services.AddSingleton<IConversationContextService, InMemoryConversationContextService>();
        services.AddHttpClient<IWebSearchService, SaveState.Infrastructure.Ai.Services.WebSearchService>()
            .AddResiliencePolicies("WebSearch");
        services.AddScoped<IVoiceProcessor, WhisperVoiceProcessor>();

        // PHASE 1: Image Analysis Service for Screenshot Scanning
        services.AddScoped<IImageAnalysisService, ImageAnalysisService>();

        // Register cloud sync services
        services.AddSingleton<ICloudAuthenticationService, CloudAuthenticationService>();
        services.AddSingleton<ISyncService, SyncService>();

        services.AddSingleton<LocalFileStorageProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<LocalFileStorageProvider>>();
            var tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SaveState", "SyncServerMock");
            return new LocalFileStorageProvider(tempDir, logger);
        });

        services.AddSingleton<ICloudStorageProvider>(sp => sp.GetRequiredService<LocalFileStorageProvider>());
        services.AddSingleton<ICloudStorageProvider, OneDriveStorageProvider>();
        services.AddSingleton<ICloudStorageProvider, GoogleDriveStorageProvider>();
        services.AddScoped<IAiResiliencePolicy, AiResiliencePolicy>();
        services.AddScoped<IFeedbackLoop, LocalLearningService>();
        services.AddScoped<IChaosTester, ChaosTester>();

        // RetroArch Services
        services.AddRetroArchServices();

        // Subscription Management Services
        services.AddSubscriptionServices(configuration);

        // Game Deals Services
        services.AddGameDealsServices(configuration);

        // Smart Launcher Services
        services.AddSmartLauncherServices();

        return services;
    }

    /// <summary>
    /// Adds Smart Launcher services for system optimization and game launching.
    /// </summary>
    private static IServiceCollection AddSmartLauncherServices(this IServiceCollection services)
    {
        // Core services
        services.AddScoped<ISmartLauncherService, SaveState.Application.SmartLauncher.SmartLauncherService>();
        services.AddScoped<ISystemOptimizerService, SmartLauncher.SystemOptimizerService>();
        services.AddScoped<ILaunchProfileRepository, SmartLauncher.LaunchProfileRepository>();
        services.AddScoped<ILaunchSessionRepository, SmartLauncher.LaunchSessionRepository>();
        services.AddSingleton<IGameProcessMonitor, SmartLauncher.GameProcessMonitor>();

        // Background service for session monitoring
        services.AddHostedService<SmartLauncher.SmartLauncherBackgroundService>();

        // Statistics and analytics
        services.AddScoped<ISmartLauncherStatisticsService, SmartLauncher.SmartLauncherStatisticsService>();

        // Profile import/export
        services.AddScoped<ILaunchProfileImportExportService, SmartLauncher.LaunchProfileImportExportService>();

        // Hotkey service
        services.AddSingleton<ISmartLauncherHotkeyService, SmartLauncher.SmartLauncherHotkeyService>();
        services.AddOptions<SmartLauncherHotkeyConfig>()
            .BindConfiguration("SmartLauncher:Hotkeys");

        return services;
    }

    /// <summary>
    /// Adds RetroArch services and cloud sync engines.
    /// </summary>
    private static IServiceCollection AddRetroArchServices(this IServiceCollection services)
    {
        services.AddScoped<IRetroArchService, RetroArchService>();

        // Register RetroArch cloud sync engines
        services.AddScoped<AzureBlobSyncEngine>();
        services.AddScoped<AwsS3SyncEngine>();
        services.AddScoped<GoogleCloudSyncEngine>();

        // Register ISyncEngine based on configuration
        services.AddScoped<ISyncEngine>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SaveState.Core.RetroArch.RetroArchOptions>>().Value;
            var provider = options.CloudSyncProvider?.ToLowerInvariant() ?? "azureblob";

            return provider switch
            {
                "azureblob" => sp.GetRequiredService<AzureBlobSyncEngine>(),
                "awss3" => sp.GetRequiredService<AwsS3SyncEngine>(),
                "googlecloud" => sp.GetRequiredService<GoogleCloudSyncEngine>(),
                _ => sp.GetRequiredService<AzureBlobSyncEngine>() // Default to Azure
            };
        });

        return services;
    }

}
