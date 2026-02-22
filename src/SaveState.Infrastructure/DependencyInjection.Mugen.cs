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
    /// Adds MUGEN and OpenMK services.
    /// </summary>
    private static void AddMugenServices(IServiceCollection services)
    {
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
    }
}
