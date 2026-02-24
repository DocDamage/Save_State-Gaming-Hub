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
    /// <summary>
    /// Adds AI/ML services.
    /// </summary>
    private static void AddAiServices(IServiceCollection services, IConfiguration configuration)
    {
        // Phase 2: Intelligence & Personalization Services
        // Smart Recommendations 2.0 - Hybrid Recommendation Engine
        services.AddScoped<
            SaveState.Core.GameLibrary.Services.IRecommendationEngineV2,
            SaveState.Infrastructure.GameLibrary.Services.RecommendationEngineV2>();

        // Gamer DNA Service
        services.AddScoped<IGamerDnaService, GamerDnaService>();
        services.AddScoped<INaturalLanguageGameSearch, SaveState.Infrastructure.Ai.Services.NaturalLanguageGameSearch>();

        // AI Content Generation (use ContentGeneration namespace to avoid ambiguity)
        services.AddScoped<
            SaveState.Core.ContentGeneration.Services.IThumbnailGeneratorService, 
            SaveState.Infrastructure.ContentGeneration.Services.ThumbnailGeneratorService>();

        services.AddScoped<
            SaveState.Core.ContentGeneration.Services.INaturalLanguageSaveSearch,
            SaveState.Infrastructure.ContentGeneration.Services.NaturalLanguageSaveSearch>();

        services.AddScoped<
            SaveState.Core.ContentGeneration.Services.IGameSummaryService,
            SaveState.Infrastructure.ContentGeneration.Services.GameSummaryService>();

        // Universal Search (use Search.Services namespace to avoid ambiguity)
        services.AddScoped<
            SaveState.Core.Search.Services.IUniversalSearchService,
            SaveState.Infrastructure.Search.Services.UniversalSearchService>();
        services.AddScoped<ISearchProvider, GameSearchProvider>();
        services.AddScoped<ISearchProvider, SettingsSearchProvider>();
        services.AddScoped<ISearchProvider, ActionSearchProvider>();
        services.AddScoped<ISearchProvider, CommandSearchProvider>();
        services.AddScoped<ISearchProvider, SaveStateSearchProvider>();

        // OpenAI Clients
        services.AddScoped<IOpenAiImageClient, OpenAiImageClient>();
        services.AddScoped<IOpenAiEmbeddingClient, OpenAiEmbeddingClient>();

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
        services.AddSingleton<SaveState.Core.Ai.Assistant.IDifficultyAnalyzer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Ai.ML.DifficultyAnalyzer>>();
            var timeProvider = sp.GetRequiredService<ITimeProvider>();
            var modelPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SaveStateReborn",
                "MLModels",
                "difficulty_model.zip");
            return new Ai.ML.DifficultyAnalyzer(logger, timeProvider, modelPath);
        });

        // Game Session Monitor (Background Service)
        services.AddSingleton<SaveState.Core.Ai.Assistant.IGameSessionMonitor, Ai.Assistant.GameSessionMonitor>();
        services.AddHostedService<Ai.Assistant.GameSessionMonitor>(sp =>
            (Ai.Assistant.GameSessionMonitor)sp.GetRequiredService<SaveState.Core.Ai.Assistant.IGameSessionMonitor>());

        // User Preference Learning Service
        services.AddSingleton<SaveState.Core.Ai.Assistant.IUserPreferenceLearningService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Ai.Assistant.UserPreferenceLearningService>>();
            var timeProvider = sp.GetRequiredService<ITimeProvider>();
            var prefsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SaveStateReborn",
                "UserPreferences.json");
            return new Ai.Assistant.UserPreferenceLearningService(logger, timeProvider, prefsPath);
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
    }
}
