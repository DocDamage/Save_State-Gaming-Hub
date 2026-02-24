using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Tests.Persistence;

public sealed class DatabaseInitializerAndMugenPersistenceTests
{
    [Fact]
    public async Task InitializeAsync_WithLegacyEnsureCreatedSqlite_BackfillsMigrationHistory()
    {
        var dbPath = GetUniqueSqlitePath();
        var options = CreateSqliteOptions(dbPath);

        try
        {
            await using (var legacyContext = new SaveStateDbContext(options))
            {
                await legacyContext.Database.EnsureCreatedAsync();
            }

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped(_ => new SaveStateDbContext(options));

            await using (var provider = services.BuildServiceProvider())
            {
                await DatabaseInitializer.InitializeAsync(provider);
            }

            var migrationCount = await ExecuteScalarIntAsync(
                dbPath,
                "SELECT COUNT(1) FROM \"__EFMigrationsHistory\";");

            migrationCount.Should().BeGreaterThan(0);
        }
        finally
        {
            DeleteSqliteArtifacts(dbPath);
        }
    }

    [Fact]
    public async Task MugenCharacterCreate_WithOwnedDefaults_PersistsPaletteInfo()
    {
        var dbPath = GetUniqueSqlitePath();
        var options = CreateSqliteOptions(dbPath);

        try
        {
            await using (var context = new SaveStateDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();

                var character = MugenCharacter.Create(
                    "Kung Fu Man",
                    "chars/kfm/kfm.def",
                    "chars/kfm");

                context.MugenCharacters.Add(character);
                await context.SaveChangesAsync();
            }

            var paletteCount = await ExecuteScalarIntAsync(
                dbPath,
                "SELECT PaletteInfo_PaletteCount FROM MugenCharacters LIMIT 1;");

            paletteCount.Should().Be(1);
        }
        finally
        {
            DeleteSqliteArtifacts(dbPath);
        }
    }

    private static DbContextOptions<SaveStateDbContext> CreateSqliteOptions(string dbPath)
    {
        return new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
    }

    private static string GetUniqueSqlitePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"savestate-infra-test-{Guid.NewGuid():N}.db");
    }

    private static async Task<int> ExecuteScalarIntAsync(string dbPath, string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var scalar = await command.ExecuteScalarAsync();
        return Convert.ToInt32(scalar);
    }

    private static void DeleteSqliteArtifacts(string dbPath)
    {
        DeleteIfExists(dbPath);
        DeleteIfExists($"{dbPath}-wal");
        DeleteIfExists($"{dbPath}-shm");
    }

    private static void DeleteIfExists(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // SQLite file handles can remain briefly open due pooling; best-effort cleanup is enough for tests.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }
}
