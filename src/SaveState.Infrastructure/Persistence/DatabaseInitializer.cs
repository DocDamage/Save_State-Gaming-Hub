using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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

            // Repair legacy SQLite databases that were previously created via EnsureCreated
            // (tables exist but migration history is missing/empty).
            await BackfillMigrationHistoryForLegacySqliteAsync(context, logger).ConfigureAwait(false);

            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync().ConfigureAwait(false);
            logger.LogInformation("Database migrations applied successfully");

            // Run seeders
            await SeedDataAsync(scope.ServiceProvider, context, logger).ConfigureAwait(false);

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

            await retroArchSeeder.SeedAsync().ConfigureAwait(false);

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            // Don't throw - seeding failures shouldn't prevent app startup
        }
    }

    private static async Task BackfillMigrationHistoryForLegacySqliteAsync(
        SaveStateDbContext context,
        ILogger logger)
    {
        if (!context.Database.IsSqlite())
        {
            return;
        }

        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        try
        {
            var userTableCount = await ExecuteScalarIntAsync(
                connection,
                """
                SELECT COUNT(1)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name NOT LIKE 'sqlite_%'
                  AND name NOT IN ('__EFMigrationsHistory', '__EFMigrationsLock');
                """
            ).ConfigureAwait(false);

            if (userTableCount == 0)
            {
                return;
            }

            var historyTableExists = await ExecuteScalarIntAsync(
                connection,
                "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory';"
            ).ConfigureAwait(false) > 0;

            if (!historyTableExists)
            {
                await ExecuteNonQueryAsync(
                    connection,
                    """
                    CREATE TABLE "__EFMigrationsHistory" (
                        "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                        "ProductVersion" TEXT NOT NULL
                    );
                    """
                ).ConfigureAwait(false);
            }

            var historyCount = await ExecuteScalarIntAsync(
                connection,
                "SELECT COUNT(1) FROM \"__EFMigrationsHistory\";"
            ).ConfigureAwait(false);

            if (historyCount > 0)
            {
                return;
            }

            var migrations = context.Database.GetMigrations().ToList();
            if (migrations.Count == 0)
            {
                return;
            }

            var productVersion = context.Model.GetProductVersion();
            foreach (var migrationId in migrations)
            {
                await InsertMigrationHistoryRowAsync(connection, migrationId, productVersion).ConfigureAwait(false);
            }

            logger.LogWarning(
                "Detected SQLite schema with existing tables but empty migration history. Backfilled {Count} migration records.",
                migrations.Count);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task<int> ExecuteScalarIntAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var scalar = await command.ExecuteScalarAsync().ConfigureAwait(false);
        return Convert.ToInt32(scalar);
    }

    private static async Task ExecuteNonQueryAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task InsertMigrationHistoryRowAsync(
        DbConnection connection,
        string migrationId,
        string productVersion)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@migrationId, @productVersion);";

        var migrationIdParameter = command.CreateParameter();
        migrationIdParameter.ParameterName = "@migrationId";
        migrationIdParameter.Value = migrationId;
        command.Parameters.Add(migrationIdParameter);

        var productVersionParameter = command.CreateParameter();
        productVersionParameter.ParameterName = "@productVersion";
        productVersionParameter.Value = productVersion;
        command.Parameters.Add(productVersionParameter);

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
