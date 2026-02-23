using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Input.Services;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Repositories;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen;
using SaveState.Infrastructure.Mugen.Repositories;
using SaveState.Presentation.Services;
using SaveState.Presentation.Services.Dashboard.Widgets;
using SaveState.Presentation.Services.Keyboard;
using SaveState.Presentation.Services.QuickActions;
using SaveState.Presentation.Services.Terminal;
using SaveState.Presentation.Services.Voice;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Analytics;
using SaveState.Presentation.ViewModels.Automation;
using SaveState.Presentation.ViewModels.BigPicture;
using SaveState.Presentation.ViewModels.CloudGaming;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.Esports;
using SaveState.Presentation.ViewModels.Library;
using SaveState.Presentation.ViewModels.MobileCompanion;
using SaveState.Presentation.ViewModels.Overlays;
using SaveState.Presentation.ViewModels.PluginStore;
using SaveState.Presentation.ViewModels.Replay;
using SaveState.Presentation.ViewModels.RetroArch;
using SaveState.Presentation.ViewModels.RgbSync;
using SaveState.Presentation.ViewModels.Search;
using SaveState.Presentation.ViewModels.Settings;
using SaveState.Presentation.ViewModels.Shell;
using SaveState.Presentation.ViewModels.Shell.Mugen;
using SaveState.Presentation.ViewModels.SmartLauncher;
using SaveState.Presentation.ViewModels.Theme;
using SaveState.Presentation.ViewModels.WebBrowser;

namespace SaveState.EndToEndTests.Infrastructure;

/// <summary>
/// Extension methods for registering all presentation layer services in E2E tests.
/// Mirrors the registration in Program.cs but tailored for test scenarios.
/// </summary>
public static class PresentationServiceExtensions
{
    /// <summary>
    /// Adds all presentation layer services required for E2E testing.
    /// </summary>
    public static IServiceCollection AddPresentationServicesForE2E(this IServiceCollection services)
    {
        // Localization
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddTransient<SaveState.Presentation.Resources.Resources>();

        // Core ViewModels
        services.AddTransient<GameLibraryViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

        // RetroArch ViewModels
        services.AddTransient<RetroArchTabViewModel>();
        services.AddTransient<RetroArchCoreManagerViewModel>();
        services.AddTransient<RetroArchPlaylistViewModel>();
        services.AddTransient<RetroArchNetplayViewModel>();

        // System Health & Accounts ViewModels
        services.AddTransient<SystemHealthViewModel>();
        services.AddTransient<ConnectedAccountsViewModel>();
        services.AddTransient<DataManagementViewModel>();

        // Intelligence & Personalization ViewModels
        services.AddTransient<RecommendationsViewModel>();
        services.AddTransient<GamerDnaViewModel>();
        services.AddTransient<UniversalSearchViewModel>();

        // Performance Dashboard & Data Management ViewModels
        services.AddTransient<PerformanceDashboardViewModel>();
        services.AddTransient<GamePerformanceDetailViewModel>();
        services.AddTransient<ImportPreviewDialogViewModel>();

        // Immersive Launch Experience ViewModels
        services.AddTransient<LaunchExperienceViewModel>();
        services.AddTransient<LaunchExperienceConfigDialogViewModel>();

        // Dialog ViewModels
        services.AddTransient<ErrorLogViewerDialogViewModel>();
        services.AddTransient<AccountConnectionWizardViewModel>();

        // Security & Auth UI
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<ApiKeyManagerViewModel>();
        services.AddTransient<RoleManagementViewModel>();

        // AI Administration
        services.AddTransient<AiAdministrationViewModel>();
        services.AddTransient<ProviderConfigDialogViewModel>();
        services.AddTransient<MemoryDetailsDialogViewModel>();

        // Tournament Management (Esports)
        services.AddTransient<TournamentListViewModel>();
        services.AddTransient<TournamentDetailViewModel>();
        services.AddTransient<MatchDetailViewModel>();
        services.AddTransient<LiveTournamentTrackerViewModel>();
        services.AddTransient<TournamentStandingsViewModel>();
        services.AddTransient<CreateTournamentDialogViewModel>();
        services.AddTransient<MatchResultDialogViewModel>();

        // Mobile Companion
        services.AddTransient<MobileLandingViewModel>();
        services.AddTransient<MobileDashboardViewModel>();
        services.AddTransient<MobileRemoteControlViewModel>();
        services.AddTransient<MobileSaveStatesViewModel>();
        services.AddTransient<MobileScreenshotsViewModel>();
        services.AddTransient<MobileNotificationsViewModel>();
        services.AddTransient<PairingDialogViewModel>();

        // Mobile Companion Services
        services.AddSingleton<IQRCodeService, QRCodeService>();
        services.AddSingleton<IMobileConnectionManager, MobileConnectionManager>();

        // RGB Sync
        services.AddTransient<RgbControlPanelViewModel>();
        services.AddTransient<RgbColorPickerViewModel>();
        services.AddTransient<RgbDeviceEditorViewModel>();
        services.AddTransient<RgbProfileManagerViewModel>();
        services.AddTransient<RgbSyncGroupEditorViewModel>();
        services.AddTransient<RgbGameStateConfigViewModel>();

        // Theme Builder
        services.AddTransient<ThemeBuilderViewModel>();

        // Accessibility
        services.AddTransient<ShortcutEditorViewModel>();
        services.AddTransient<AccessibilitySettingsViewModel>();

        // Cloud Gaming
        services.AddTransient<CloudGamingDashboardViewModel>();
        services.AddTransient<CloudGameDetailViewModel>();
        services.AddTransient<StreamLauncherViewModel>();
        services.AddTransient<ConnectionTestViewModel>();
        services.AddTransient<ProviderLoginDialogViewModel>();
        services.AddTransient<StreamOverlayViewModel>();

        // Replay Theater
        services.AddTransient<ReplayTheaterViewModel>();

        // Advanced Search
        services.AddTransient<AdvancedSearchViewModel>();

        // Plugin Store
        services.AddTransient<PluginStoreViewModel>();
        services.AddTransient<PluginInstallDialogViewModel>();
        services.AddTransient<PluginReviewViewModel>();

        // CefSharp Web Browser
        services.AddTransient<BrowserShellViewModel>();
        services.AddTransient<BookmarksManagerViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<GameGuideViewModel>();
        services.AddTransient<DocumentationBrowserViewModel>();
        services.AddTransient<CommunityBrowserViewModel>();
        services.AddTransient<StreamingBrowserOverlayViewModel>();
        services.AddTransient<DownloadManagerDialogViewModel>();
        services.AddTransient<CertificateViewerDialogViewModel>();
        services.AddTransient<BrowserSettingsViewModel>();

        // Shell Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IShortcutService, ShortcutService>();
        services.AddSingleton<IOverlayService, OverlayService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IUiGameContextService, UiGameContextService>();
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();

        // Terminal Services
        services.AddSingleton<ICommandExecutor, CommandExecutor>();

        // Shell ViewModels
        services.AddTransient<MainShellViewModel>();
        services.AddTransient<TitleBarViewModel>();
        services.AddTransient<HeaderBarViewModel>();
        services.AddTransient<StatusBarViewModel>();
        services.AddTransient<OverlayContainerViewModel>();
        services.AddTransient<CommandPaletteViewModel>();
        services.AddTransient<QuickSearchViewModel>();
        services.AddTransient<AiAssistantViewModel>();
        services.AddTransient<PerformanceHudViewModel>();
        services.AddTransient<VoiceIndicatorViewModel>();

        // Voice Visualizer Services
        services.AddSingleton<IVoiceVisualizerService, VoiceVisualizerService>();
        services.AddTransient<VoiceVisualizerViewModel>();

        // Dashboard Widgets
        services.AddTransient<QuickActionsWidget>();
        services.AddTransient<TodaysStatsWidget>();
        services.AddTransient<ActivityFeedWidget>();
        services.AddTransient<RecentlyAddedWidget>();
        services.AddTransient<GoalsProgressWidget>();
        services.AddTransient<EmulatorStatusWidget>();

        // Tab ViewModels
        services.AddTransient<DashboardViewModel>();

        // Library UI Components
        services.AddTransient<LibrarySidebarViewModel>();
        services.AddTransient<LibraryToolbarViewModel>();
        services.AddTransient<GameGridViewModel>();
        services.AddTransient<GameListViewModel>();
        services.AddTransient<LibraryViewModel>();

        // Shell ViewModels
        services.AddTransient<MugenHubViewModel>();
        services.AddTransient<MugenViewModel>();
        services.AddTransient<AnalyticsViewModel>();
        services.AddTransient<SocialViewModel>();
        services.AddTransient<CloudSyncViewModel>();
        services.AddTransient<MacroRecorderViewModel>();
        services.AddTransient<TaskSchedulerViewModel>();
        services.AddTransient<AutomationDashboardViewModel>();
        services.AddTransient<AutomationViewModel>();
        services.AddSingleton<VoiceControlViewModel>();
        services.AddTransient<ToolsViewModel>();
        services.AddTransient<TerminalViewModel>();
        services.AddTransient<GameMemoryViewModel>();
        services.AddTransient<SmartLauncherViewModel>();
        services.AddTransient<SmartLauncherStatisticsViewModel>();

        // MUGEN ViewModels
        services.AddTransient<MoveCreationViewModel>();
        services.AddTransient<MachineLearningViewModel>();
        services.AddTransient<MacroMarketplaceViewModel>();

        // Optional Feature ViewModels (Phase 6)
        services.AddTransient<VoiceCommandViewModel>();
        services.AddTransient<AdvancedAnalyticsViewModel>();
        services.AddTransient<AccessibilityViewModel>();
        services.AddTransient<AudioOptimizationViewModel>();

        // MUGEN Move Creation and Machine Learning services
        services.AddTransient<IMoveCreationService, MoveCreationService>();
        services.AddTransient<IMugenTemplateRepository, MugenTemplateRepository>();
        services.AddTransient<IMugenValidationService, MugenValidationService>();
        services.AddTransient<IMugenBalancingService, MugenBalancingService>();
        services.AddTransient<IMugenExportService, MugenExportService>();
        services.AddTransient<IMugenPreviewService, MugenPreviewService>();
        services.AddTransient<IMugenTestService, MugenTestService>();
        services.AddTransient<IMachineLearningService, MachineLearningService>();
        services.AddTransient<IMatchDataRepository, MugenMatchDataRepository>();
        services.AddTransient<ICharacterDataRepository, MugenCharacterDataRepository>();
        services.AddTransient<IPlayerDataRepository, MugenPlayerDataRepository>();

        // Additional services required by ViewModels
        services.AddSingleton<IKeyboardNavigationService, KeyboardNavigationService>();
        services.AddSingleton<IQuickActionService, QuickActionService>();

        return services;
    }
}
