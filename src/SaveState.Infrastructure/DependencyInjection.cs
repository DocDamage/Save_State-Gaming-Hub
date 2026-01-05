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
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Repositories;
using SaveState.Infrastructure.Mugen;
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
using SaveState.Infrastructure.Sync;
using SaveState.Infrastructure.External;
using SaveState.Infrastructure.GameLibrary.Services;
using SaveState.Infrastructure.Persistence;
using SaveState.Infrastructure.RomManagement.Services;
using SaveState.Infrastructure.Common;
using SaveState.Infrastructure.Health;

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
        services.AddScoped<IEmulatorRepository, EmulatorRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IPlatformExtensionRegistry, PlatformExtensionRegistry>();
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

        // Plugin system
        services.AddSingleton<SaveState.Core.Plugins.Services.IPluginManager, Plugins.PluginManager>();
        services.AddHostedService<Plugins.PluginLoaderBackgroundService>();

        // User Services
        services.AddSingleton<SaveState.Core.Common.Services.IUserPreferencesService, SaveState.Infrastructure.Services.UserPreferencesService>();
        services.AddSingleton<ITaskRunner, TaskRunner>();

        // Culture and Localization Services
        services.AddSingleton<SaveState.Core.Common.Services.ICultureManager, CultureManager>();

        // Accessibility Services
        services.AddSingleton<SaveState.Core.Common.Services.IAccessibilityService, AccessibilityService>();

        // MUGEN Services
        services.AddScoped<SaveState.Core.Mugen.Services.IMugenCharacterParser, SaveState.Core.Mugen.Services.MugenCharacterParser>();
        services.AddScoped<IMugenCharacterLoader, MugenCharacterLoader>();
        services.AddScoped<IMugenLauncher, MugenLauncher>();

        // Game Providers
        services.AddScoped<IGameProvider, SteamProvider>();
        services.AddScoped<IGameProvider, GogProvider>();
        services.AddScoped<IGameProvider, EpicProvider>();

        // External API Clients
        services.AddHttpClient<ISteamApiClient, SteamApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.steampowered.com/");
        });

        services.AddHttpClient<IGogApiClient, GogApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.gog.com/");
        });

        services.AddHttpClient<IEpicApiClient, EpicApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.epicgames.dev/");
        });

        // RetroAchievements.org API Client
        services.AddHttpClient<SaveState.Core.Achievements.IRetroAchievementsClient, RetroAchievementsClient>(client =>
        {
            client.BaseAddress = new Uri("https://retroachievements.org/API/");
        });

        // Cloud Sync Services
        services.AddSingleton<SaveState.Core.Sync.ISyncService, Sync.SyncService>();


        // Caching
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

        // Analytics Services
        services.AddScoped<SaveState.Core.Analytics.Services.IAnalyticsService, Analytics.AnalyticsService>();
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

        // Assistant Services
        services.AddScoped<SaveState.Core.Assistant.Services.IGameAssistantService, Assistant.GameAssistantService>();

        // Save State Services
        services.AddScoped<SaveState.Core.SaveStates.Services.ISaveStateManager, SaveStates.SaveStateManager>();
        services.AddScoped<SaveState.Core.SaveStates.Services.ISaveStateBranchingService, SaveStates.SaveStateBranchingService>();
        services.AddScoped<SaveState.Core.SaveStates.Services.IAutoSaveManager, SaveStates.AutoSaveManager>();

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
        services.AddSingleton<SaveState.Core.Performance.Services.IAudioOptimizer, Performance.AudioOptimizer>();

        // Phase 4: Immersive Launch Experience Services
        services.AddScoped<SaveState.Core.GameLibrary.Services.ILaunchExperienceManager, GameLibrary.Services.LaunchExperienceManager>();
        services.AddScoped<SaveState.Core.GameLibrary.Services.IGameBriefingService, GameLibrary.Services.GameBriefingService>();

        // Phase 5: Cloud Gaming & Network Quality Services
        services.AddScoped<SaveState.Core.Sync.Services.ICloudGamingManager, Sync.CloudGamingManager>();
        services.AddSingleton<SaveState.Core.Sync.Services.INetworkQualityMonitor, Sync.NetworkQualityMonitor>();

        // Phase 6: Voice Command Integration Services
        services.AddScoped<SaveState.Core.Input.Services.IVoiceCommandService, Input.VoiceCommandService>();
        services.AddScoped<SaveState.Core.Ai.Services.ISpeechRecognitionService, Ai.SpeechRecognitionService>();

        // Phase 7: Automation Services
        services.AddScoped<SaveState.Core.Automation.Services.IMacroRecorder, Automation.MacroRecorder>();
        services.AddScoped<SaveState.Core.Automation.Services.IMacroPlayer, Automation.MacroPlayer>();

        // Phase 8: Game Memory Intelligence Services
        services.AddSingleton<SaveState.Core.GameLibrary.Services.IGameMemoryReader, GameLibrary.Services.GameMemoryReader>();
        services.AddSingleton<SaveState.Core.GameLibrary.Services.IPerformanceProfiler, GameLibrary.Services.PerformanceProfiler>();
        services.AddScoped<SaveState.Core.GameLibrary.Services.IAiCoachService, GameLibrary.Services.AiCoachService>();
        services.AddScoped<SaveState.Core.Social.Services.ISocialService, Social.Services.SocialService>();
        services.AddSingleton<GameLibrary.Services.MemoryPatternDatabase>();
        services.AddScoped<SaveState.Core.Automation.Services.IMacroManager, Automation.MacroManager>();
        services.AddScoped<SaveState.Core.Automation.Services.IMacroService, Automation.MacroService>();
        services.AddScoped<SaveState.Core.Automation.Services.IBackupScheduler, Automation.BackupScheduler>();
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

        // Named HttpClient for Twitch authentication (used by IGDB)
        services.AddHttpClient("TwitchAuth");

        services.AddHttpClient<IIgdbApiClient, IgdbApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.igdb.com/v4/");
        });

        // SteamGridDB API Client
        services.AddHttpClient<ISteamGridDbApiClient, SteamGridDbApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SteamGridDbOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("SaveState", "1.0"));
        });

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

        // AI Providers (with HttpClient)
        services.AddHttpClient<ILlmProvider, OpenAiProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddHttpClient<ILlmProvider, GroqProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<GroqOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

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

        // AI Services
        services.AddScoped<IKnowledgeStore, SqliteVectorStore>();
        services.AddScoped<SemanticKnowledgeClient>();
        services.AddScoped<IShortTermMemory, EnhancedShortTermMemory>();
        services.AddScoped<IAiOrchestrator, AiOrchestrator>();
        services.AddScoped<IKnowledgeBaseService, MarkdownKnowledgeBaseService>();
        services.AddSingleton<IConversationContextService, InMemoryConversationContextService>();
        services.AddHttpClient<IWebSearchService, SaveState.Infrastructure.Ai.Services.WebSearchService>();
        services.AddScoped<IVoiceProcessor, WhisperVoiceProcessor>();

        // Register cloud sync services
        services.AddSingleton<ICloudStorageProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<LocalFileStorageProvider>>();
            // Use a temporary directory for local testing - in production this would be configured
            var tempDir = Path.Combine(Path.GetTempPath(), "savestate-sync");
            return new LocalFileStorageProvider(tempDir, logger);
        });
        services.AddScoped<IAiResiliencePolicy, AiResiliencePolicy>();
        services.AddScoped<IFeedbackLoop, LocalLearningService>();
        services.AddScoped<IChaosTester, ChaosTester>();


        return services;
    }
}
