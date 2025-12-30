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

        // PERFORMANCE OPTIMIZATION: Enhanced database configuration with connection pooling
        builder.Services.AddDbContext<SaveStateDbContext>((sp, options) =>
        {
            options.UseSqlite("Data Source=savestate.db", sqliteOptions =>
            {
                // Enable connection pooling for better performance
                sqliteOptions.MaxBatchSize(100);
                sqliteOptions.CommandTimeout(30);
                sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // PERFORMANCE OPTIMIZATION: Configure EF Core for better performance
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);

#if DEBUG
            // Keep detailed logging in debug mode for development
            options.EnableSensitiveDataLogging(true);
            options.EnableDetailedErrors(true);
#endif
        });

        // Add localization
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddTransient<SaveState.Presentation.Resources.Resources>();

        // Add ViewModels
        builder.Services.AddTransient<GameLibraryViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Add theme service
        builder.Services.AddSingleton<IThemeService, ThemeService>();

        var host = builder.Build();

        // Initialize database and seed test game
        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            // Seed "Test Game" if not exists
            if (!await dbContext.Games.AnyAsync())
            {
                var testGame = Game.Create("Test Game");
                dbContext.Games.Add(testGame);
                await dbContext.SaveChangesAsync();
            }
        }

        // Setup the service locator for Avalonia
        Locator.Current.SetServices(host.Services);

        // Run the Avalonia application
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}
