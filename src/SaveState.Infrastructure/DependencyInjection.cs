using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using SaveState.Core.Configuration;
using SaveState.Core.Common;
using SaveState.Core.Common.Configuration;
using SaveState.Core.Common.Services;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Logging;
using SaveState.Infrastructure.Services;
using SaveState.Infrastructure.UserManagement;
using SaveState.Infrastructure.Ai.Providers;
using SaveState.Infrastructure.AiCoOp.Services;
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
using SaveState.Core.GameLibrary.Services;
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
using SaveState.Core.Intelligence.Recommendations.Services;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Intelligence.AiContent.Services;
using SaveState.Core.ContentGeneration.Services;
using SaveState.Core.Intelligence.Search.Services;
using SaveState.Core.Search.Services;
using SaveState.Infrastructure.Intelligence.Recommendations;
using SaveState.Infrastructure.Analytics.Services;
using SaveState.Infrastructure.Intelligence.AiContent;
using SaveState.Infrastructure.ContentGeneration.Services;
using SaveState.Infrastructure.Intelligence.Search;
using SaveState.Infrastructure.Search.Services;
using SaveState.Infrastructure.Search.Providers;
using SaveState.Infrastructure.GameDeals;
using SaveState.Infrastructure.SmartLauncher;
using SaveState.Core.SmartLauncher;
using SaveState.Infrastructure.OpenApi;

namespace SaveState.Infrastructure;

public static partial class DependencyInjection
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

        // Structured Logging Services
        services.AddStructuredLogging();

        // Culture and Localization Services
        services.AddSingleton<SaveState.Core.Common.Services.ICultureManager, CultureManager>();

        // Accessibility Services
        services.AddSingleton<SaveState.Core.Common.Services.IAccessibilityService, AccessibilityService>();

        // MUGEN services (extracted to partial class)
        AddMugenServices(services);

        // External API services (extracted to partial class)
        AddExternalServices(services);

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

        // AI/ML services (extracted to partial class)
        AddAiServices(services, configuration);

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

        // Diagnostics services (extracted to partial class)
        AddDiagnosticsServices(services);

        // AI Providers (with HttpClient and resilience policies) (extracted to partial class)
        AddAiProviders(services);

        // Configuration with validation (extracted to partial class)
        AddConfigurationOptions(services, configuration);

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

        // Register new Metrics Service
        services.AddMetrics();

        // Phase 5.1: AI Co-Op Companion Service
        services.AddScoped<SaveState.Core.AiCoOp.Services.IAiCoOpCompanionService, SaveState.Infrastructure.AiCoOp.Services.AiCoOpCompanionService>();

        // Register OpenAPI Documentation
        services.AddOpenApiDocumentation();

        return services;
    }

    /// <summary>
    /// Adds OpenAPI/Swagger documentation services.
    /// </summary>
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        OpenApiConfiguration.ConfigureOpenApiDocument(services);
        services.AddSingleton<OpenApiDocumentationService>();
        return services;
    }

    /// <summary>
    /// Adds metrics services for Prometheus/Grafana integration.
    /// </summary>
    private static IServiceCollection AddMetrics(this IServiceCollection services)
    {
        // Register MetricsService as both interface implementations
        services.AddSingleton<Metrics.MetricsService>();
        services.AddSingleton<Core.Metrics.IMetricsService>(sp => 
            sp.GetRequiredService<Metrics.MetricsService>());
        services.AddSingleton<Core.Metrics.IMetricsReporter>(sp => 
            sp.GetRequiredService<Metrics.MetricsService>());

        // Register Prometheus Exporter
        services.AddSingleton<Metrics.PrometheusExporter>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure logging services including correlation ID provider and structured logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureLogging(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddStructuredLogging();
        
        // Register correlation ID provider as scoped for web scenarios
        services.AddScoped<ICorrelationIdProvider>(sp => 
        {
            var provider = new CorrelationIdProvider();
            // Could read from HTTP header in web context
            return provider;
        });
        
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
