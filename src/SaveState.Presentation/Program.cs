namespace SaveState.Presentation;

using Avalonia;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

public static class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configuration is already set up by Host.CreateApplicationBuilder

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
        builder.Services.AddTransient<SaveState.Presentation.ViewModels.Mugen.MugenViewModel>();

        // Phase 4: Immersive Launch Experience ViewModels
        builder.Services.AddTransient<SaveState.Presentation.ViewModels.BigPicture.LaunchExperienceViewModel>();

        // Add theme service
        builder.Services.AddSingleton<IThemeService, ThemeService>();

        // Add shell services
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IShortcutService, ShortcutService>();
        builder.Services.AddSingleton<IOverlayService, OverlayService>();

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
        builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.DashboardViewModel>();
        builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.LibraryViewModel>();
        builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.MugenViewModel>();
        builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.AnalyticsViewModel>();
        builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.SocialViewModel>();
        builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.ToolsViewModel>();
        builder.Services.AddTransient<SaveState.Presentation.ViewModels.Shell.TerminalViewModel>();

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
        }

        Console.WriteLine("[DEBUG] Database initialization complete");

        // Setup the service locator for Avalonia
        Locator.Current.SetServices(host.Services);

        Console.WriteLine("[DEBUG] Locator configured, starting Avalonia...");

        // Run the Avalonia application
        try
        {
            Console.WriteLine("[DEBUG] Building AppBuilder...");
            var appBuilder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

            Console.WriteLine("[DEBUG] Starting with ClassicDesktopLifetime...");
            appBuilder.StartWithClassicDesktopLifetime(args);

            Console.WriteLine("[DEBUG] Application exited normally");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Avalonia startup failed: {ex}");
            throw;
        }
    }
}
