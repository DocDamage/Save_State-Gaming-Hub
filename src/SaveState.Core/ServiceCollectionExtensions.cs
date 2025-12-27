using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Interfaces;
using SaveState.Core.Services.Accessibility;
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
            services.AddSingleton<LlmService>();
            services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<LlmService>());
            services.AddSingleton<OllamaManager>();
            services.AddSingleton<ModelManager>();
            services.AddSingleton<StableDiffusionService>();
            services.AddSingleton<RagService>(sp => 
                new RagService(sp.GetRequiredService<ILlmService>()));
            
            // AI orchestration - typically singleton for shared state
            services.AddSingleton<ProductionAiService>();
            services.AddSingleton<ResilientAiService>();
            services.AddSingleton<UltimateAiOrchestrator>();
            services.AddSingleton<EdgeCaseHandler>();
            
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
            services.AddSingleton<AuthService>(sp => AuthService.Instance);
            services.AddSingleton<CloudSyncService>(sp => CloudSyncService.Instance);
            services.AddSingleton<BackupService>(sp => BackupService.Instance);
            
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
            services.AddSingleton<AchievementService>(sp => AchievementService.Instance);
            services.AddSingleton<ChallengeService>(sp => ChallengeService.Instance);
            
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
            services.AddSingleton<CheatService>(sp => CheatService.Instance);
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
