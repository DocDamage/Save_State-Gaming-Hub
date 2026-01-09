using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Infrastructure.Persistence.Seeders;

namespace SaveState.Infrastructure.Persistence;

/// <summary>
/// Initializes the database and runs seeders.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Initializes the database, runs migrations, and seeds initial data.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SaveStateDbContext>>();

        try
        {
            logger.LogInformation("Initializing database...");

            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            // Run seeders
            await SeedDataAsync(scope.ServiceProvider, context, logger);

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    private static async Task SeedDataAsync(
        IServiceProvider serviceProvider,
        SaveStateDbContext context,
        ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting database seeding...");

            // Seed RetroArch configuration
            var retroArchSeeder = new RetroArchSeeder(
                context,
                serviceProvider.GetRequiredService<ILogger<RetroArchSeeder>>());

            await retroArchSeeder.SeedAsync();

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            // Don't throw - seeding failures shouldn't prevent app startup
        }
    }
}
