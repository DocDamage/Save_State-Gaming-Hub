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
            await ApplyMigrationsWithRetryAsync(context, logger).ConfigureAwait(false);
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

    /// <summary>
    /// Applies migrations with retry logic for schema drift scenarios.
    /// Handles cases where legacy databases have tables but missing columns.
    /// </summary>
    private static async Task ApplyMigrationsWithRetryAsync(SaveStateDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // SQLite Error 1: Generic error - often indicates schema mismatch
            // (e.g., missing columns from owned entity types in legacy databases)
            logger.LogWarning(ex, "Initial migration failed due to schema mismatch. Attempting recovery...");

            if (context.Database.IsSqlite())
            {
                await HandleSchemaMismatchAsync(context, logger).ConfigureAwait(false);
            }
            else
            {
                throw;
            }
        }
        catch (Exception ex) when (ex.Message.Contains("NOT NULL constraint failed") && context.Database.IsSqlite())
        {
            // Handle NOT NULL constraint failures (e.g., missing owned entity values)
            logger.LogWarning(ex, "Migration failed due to NOT NULL constraint. Attempting recovery...");
            await HandleSchemaMismatchAsync(context, logger).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Attempts to recover from schema mismatches by adding missing columns.
    /// This handles legacy databases created before owned entity types were added.
    /// </summary>
    private static async Task HandleSchemaMismatchAsync(SaveStateDbContext context, ILogger logger)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        try
        {
            // Check for missing PaletteInfo columns on MugenCharacters
            await AddColumnIfMissingAsync(connection, "MugenCharacters", "PaletteInfo_PaletteCount", "INTEGER NOT NULL DEFAULT 1", logger);
            await AddColumnIfMissingAsync(connection, "MugenCharacters", "PaletteInfo_PaletteFile", "TEXT", logger);

            // Check for missing ArcadeInfo columns on MugenCharacters
            await AddColumnIfMissingAsync(connection, "MugenCharacters", "ArcadeInfo_IntroStoryboard", "INTEGER NOT NULL DEFAULT 0", logger);
            await AddColumnIfMissingAsync(connection, "MugenCharacters", "ArcadeInfo_EndingStoryboard", "INTEGER NOT NULL DEFAULT 0", logger);

            // Check for missing CharacterDirectories columns on MugenCharacters
            await AddColumnIfMissingAsync(connection, "MugenCharacters", "Directories_SpriteDirectory", "TEXT", logger);
            await AddColumnIfMissingAsync(connection, "MugenCharacters", "Directories_SoundDirectory", "TEXT", logger);
            await AddColumnIfMissingAsync(connection, "MugenCharacters", "Directories_PaletteDirectory", "TEXT", logger);

            logger.LogInformation("Schema mismatch recovery completed. Retrying migrations...");

            // Retry migrations after fixing schema
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Adds a column to a table if it doesn't already exist.
    /// </summary>
    private static async Task AddColumnIfMissingAsync(DbConnection connection, string tableName, string columnName, string columnType, ILogger logger)
    {
        var checkColumnSql = $@"
            SELECT COUNT(1)
            FROM pragma_table_info('{tableName}')
            WHERE name = '{columnName}';";

        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = checkColumnSql;
        var result = await checkCommand.ExecuteScalarAsync().ConfigureAwait(false);
        var columnExists = Convert.ToInt32(result) > 0;

        if (!columnExists)
        {
            var alterSql = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnType};";
            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = alterSql;
            await alterCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            logger.LogInformation("Added missing column {ColumnName} to {TableName}", columnName, tableName);
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
