namespace SaveState.Presentation;

using System.Diagnostics;
using Avalonia;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure;
using SaveState.Application.Common.DependencyInjection;
using SaveState.Infrastructure.Persistence;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.Views;
using SaveState.Presentation.Services;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen.Repositories;
using SaveState.Core.Mugen.Repositories;
using SaveState.Core.Input.Services;
using SaveState.Infrastructure.Logging;

/// <summary>
/// Entry point for the SaveState Avalonia application.
/// Configures dependency injection, initializes the database, and starts the UI.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application entry point. Configures services, initializes database, and starts the Avalonia UI.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the application execution.</returns>
    [STAThread]
    public static async Task Main(string[] args)
    {
        // Initialize bootstrap logger for early logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting SaveStateReborn v{Version}", "2.5.1");

            var builder = Host.CreateApplicationBuilder(args);

            // Configure Serilog
            builder.Logging.ClearProviders();
            var serilogConfig = SerilogConfiguration.CreateConfiguration(builder.Configuration)
                .AddSeq(builder.Configuration["Seq:ServerUrl"]);
            Log.Logger = serilogConfig.CreateLogger();
            builder.Logging.AddSerilog(Log.Logger);

            // Add layers
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplicationServices();

            // Add localization
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.AddTransient<SaveState.Presentation.Resources.Resources>();

            // Add ViewModels
            builder.Services.AddTransient<GameLibraryViewModel>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            // Phase 4: Immersive Launch Experience ViewModels
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.BigPicture.LaunchExperienceViewModel>();

            // Add theme service
            builder.Services.AddSingleton<IThemeService, ThemeService>();

            // Add shell services
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IShortcutService, ShortcutService>();
            builder.Services.AddSingleton<IOverlayService, OverlayService>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();
            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<IClipboardService, ClipboardService>();
            builder.Services.AddSingleton<IUiGameContextService, UiGameContextService>();
            builder.Services.AddSingleton<ICommandPaletteService, CommandPaletteService>();

            // Add terminal services
            builder.Services.AddSingleton<SaveState.Presentation.Services.Terminal.ICommandExecutor, SaveState.Presentation.Services.Terminal.CommandExecutor>();

            // Add shell ViewModels
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.MainShellViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.TitleBarViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.HeaderBarViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.StatusBarViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.OverlayContainerViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.CommandPaletteViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.QuickSearchViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.AiAssistantViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.PerformanceHudViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.VoiceIndicatorViewModel>();

            // Add dashboard widgets
            builder.Services.AddTransient<SaveState.Presentation.Services.Dashboard.Widgets.QuickActionsWidget>();
            builder.Services.AddTransient<SaveState.Presentation.Services.Dashboard.Widgets.TodaysStatsWidget>();
            builder.Services.AddTransient<SaveState.Presentation.Services.Dashboard.Widgets.ActivityFeedWidget>();
            builder.Services.AddTransient<SaveState.Presentation.Services.Dashboard.Widgets.RecentlyAddedWidget>();
            builder.Services.AddTransient<SaveState.Presentation.Services.Dashboard.Widgets.GoalsProgressWidget>();
            builder.Services.AddTransient<SaveState.Presentation.Services.Dashboard.Widgets.EmulatorStatusWidget>();


            // Add tab ViewModels
            // Add tab ViewModels
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.DashboardViewModel>();

            // Add Library UI components
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Library.LibrarySidebarViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Library.LibraryToolbarViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Library.GameGridViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Library.GameListViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Library.LibraryViewModel>();
            // builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.LibraryViewModel>(); // Remove if unused or duplicate

            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.MugenHubViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.MugenViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.AnalyticsViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.SocialViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.CloudSyncViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.MacroRecorderViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.TaskSchedulerViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Automation.AutomationDashboardViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.AutomationViewModel>();
            builder.Services.AddSingleton<SaveState.Presentation.ViewModels.Shell.VoiceControlViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.ToolsViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.TerminalViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.GameMemoryViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.SmartLauncher.SmartLauncherViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.SmartLauncher.SmartLauncherStatisticsViewModel>();

            // Register MUGEN ViewModels
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.Mugen.MoveCreationViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.Mugen.MachineLearningViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Automation.MacroMarketplaceViewModel>();

            // Register Optional Feature ViewModels (Phase 6)
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.VoiceCommandViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Analytics.AdvancedAnalyticsViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Settings.AccessibilityViewModel>();
            builder.Services.AddTransient<SaveState.Presentation.ViewModels.Settings.AudioOptimizationViewModel>();

            // Register MUGEN Move Creation and Machine Learning services
            builder.Services.AddTransient<IMoveCreationService, SaveState.Infrastructure.Mugen.MoveCreationService>();
            builder.Services.AddTransient<IMugenTemplateRepository, SaveState.Infrastructure.Mugen.MugenTemplateRepository>();
            builder.Services.AddTransient<IMugenValidationService, SaveState.Infrastructure.Mugen.MugenValidationService>();
            builder.Services.AddTransient<IMugenBalancingService, SaveState.Infrastructure.Mugen.MugenBalancingService>();
            builder.Services.AddTransient<IMugenExportService, SaveState.Infrastructure.Mugen.MugenExportService>();
            builder.Services.AddTransient<IMugenPreviewService, SaveState.Infrastructure.Mugen.MugenPreviewService>();
            builder.Services.AddTransient<IMugenTestService, SaveState.Infrastructure.Mugen.MugenTestService>();
            builder.Services.AddTransient<IMachineLearningService, SaveState.Infrastructure.Mugen.MachineLearningService>();
            builder.Services.AddTransient<IMatchDataRepository, MugenMatchDataRepository>();
            builder.Services.AddTransient<ICharacterDataRepository, MugenCharacterDataRepository>();
            builder.Services.AddTransient<IPlayerDataRepository, MugenPlayerDataRepository>();

            var host = builder.Build();

            // Initialize database and seed test game
            using (var scope = host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();

                // Ensure connection string is set (fallback for standalone runs)
                if (string.IsNullOrEmpty(dbContext.Database.GetConnectionString()))
                {
                    dbContext.Database.SetConnectionString("Data Source=savestate.db");
                }

                // Ensure database is created
                await dbContext.Database.EnsureCreatedAsync();

                // Seed "Test Game" if library is empty
                if (!await dbContext.Games.AnyAsync())
                {
                    var platform = await dbContext.Platforms.FirstOrDefaultAsync(p => p.Name == "PC");
                    if (platform == null)
                    {
                        platform = new Platform(
                            SaveState.Core.GameLibrary.ValueObjects.PlatformName.From("PC"),
                            SaveState.Core.GameLibrary.ValueObjects.PlatformShortName.From("PC"),
                            SaveState.Core.GameLibrary.Enums.PlatformType.Computer);
                        dbContext.Platforms.Add(platform);
                        await dbContext.SaveChangesAsync();
                    }

                    var testGame = Game.Create("Test Game", platform.Id);
                    dbContext.Games.Add(testGame);
                    await dbContext.SaveChangesAsync();
                }

                // Seed MUGEN characters if empty
                if (!await dbContext.MugenCharacters.AnyAsync())
                {
                    var kfm = MugenCharacter.Create("Kung Fu Man", "chars/kfm/kfm.def", "chars/kfm");
                    var ryu = MugenCharacter.Create("Ryu", "chars/ryu/ryu.def", "chars/ryu");
                    dbContext.MugenCharacters.AddRange(kfm, ryu);
                    await dbContext.SaveChangesAsync();
                }

                // Enable WAL Mode for performance
                dbContext.EnableWalMode();

                // Run database initialization and seeding
                await SaveState.Infrastructure.Persistence.DatabaseInitializer.InitializeAsync(host.Services);
            }

            Log.Information("Database initialization complete");

            // Run startup content scanning
            await RunStartupContentScanAsync(host.Services);

            // Setup the service locator for Avalonia
            Locator.Current.SetServices(host.Services);

            Log.Information("Starting background services...");
            await host.StartAsync();

            Log.Information("Locator configured, starting Avalonia...");

            // Run the Avalonia application
            try
            {
                Log.Information("Building AppBuilder...");
                var appBuilder = AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .LogToTrace();

                Log.Information("Starting with ClassicDesktopLifetime...");
                appBuilder.StartWithClassicDesktopLifetime(args);

                Log.Information("Stopping background services...");
                await host.StopAsync();

                Log.Information("Application exited normally");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Avalonia startup failed");
                throw;
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Scans for MUGEN characters and ROM files on startup.
    /// </summary>
    private static async Task RunStartupContentScanAsync(IServiceProvider services)
    {
        Log.Information("Starting content discovery scan...");

        try
        {
            var mediator = services.GetRequiredService<IMediator>();
            var configuration = services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

            // Get MUGEN character directories from configuration
            var mugenSection = configuration.GetSection("Mugen");
            var characterDirs = mugenSection.GetSection("CharacterDirectories").Get<string[]>()
                ?? new[] { "data/characters" };

            var scannedCount = 0;
            foreach (var dir in characterDirs)
            {
                var fullPath = Path.IsPathRooted(dir) ? dir : Path.Combine(AppContext.BaseDirectory, dir);
                if (Directory.Exists(fullPath))
                {
                    Log.Information("Scanning MUGEN characters in: {Path}", fullPath);
                    try
                    {
                        await mediator.Send(new SaveState.Application.Mugen.Commands.ScanMugenCharactersCommand(
                            fullPath,
                            IncludeSubdirectories: true,
                            OverwriteExisting: false));
                        scannedCount++;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to scan {Path}", fullPath);
                    }
                }
                else
                {
                    Log.Debug("Directory not found: {Path}", fullPath);
                }
            }

            Log.Information("Content scan complete. Scanned {Count} directories.", scannedCount);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Content scan failed");
            // Don't throw - app should still start even if scan fails
        }
    }
}
