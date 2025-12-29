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

        // Override database connection for development
        builder.Services.AddDbContext<SaveStateDbContext>((sp, options) =>
            options.UseSqlite("Data Source=savestate.db"));

        // Add ViewModels
        builder.Services.AddTransient<GameLibraryViewModel>();
        builder.Services.AddTransient<MainViewModel>();

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
