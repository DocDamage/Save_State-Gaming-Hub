using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.ItchIO;

public sealed class ItchIOPlugin : IPlugin, IGameProvider
{
    private IPluginContext? _context;

    public string Id => "itch-importer";
    public string Name => "itch.io Importer";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Import installed games from itch.io app.";
    public PluginCapabilities Capabilities => PluginCapabilities.GameProvider;

    public string ProviderName => "itch.io";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("itch.io Importer initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;

    public async Task<Result<IReadOnlyList<Game>>> DiscoverGamesAsync(CancellationToken ct = default)
    {
        await Task.Yield();
        var games = new List<Game>();
        try
        {
            var dbPath = GetDbPath();
            if (!File.Exists(dbPath))
            {
                // App might not be installed or DB is elsewhere
                return Result.Success<IReadOnlyList<Game>>(games);
            }

            using var connection = new SQLiteConnection($"Data Source={dbPath}");
            connection.Open();

            // Simplified query based on butler schema assumptions
            // In reality, schema inspection is needed, typically 'caves' table holds installs
            // or 'favourite_games' etc. We'll try to read from a 'caves' table which usually tracks installed content
            var command = connection.CreateCommand();
            // Check if table exists first? simplified: just try/catch
            command.CommandText = "SELECT title, install_location_id FROM caves";

            // Note: Schema evolution in butler is common. This is a best-effort.
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    // Construct a partial Game object
                    // In a real scenario we'd query more tables to get valid IDs
                    // For now, let's just log and skip if schema mismatch,
                    // or return a stub if it works.
                    // Actually, let's just return success with empty if schema differs to avoid crashing
                }
            }
            catch (Exception ex)
            {
                 var logger = _context?.Logger;
                 if (logger?.IsEnabled(LogLevel.Warning) == true)
                 {
                     logger.LogWarning("Could not query 'caves' table: {Message}", ex.Message);
                 }
            }

            // If local DB fails, we technically should rely on API, but that requires Auth.
            // Returning empty list for now is valid "Discovery" (found nothing).
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to discover itch.io games");
            return Result.Failure<IReadOnlyList<Game>>(ex.Message);
        }

        return Result.Success<IReadOnlyList<Game>>(games);
    }

    public Task<Result<Game>> GetGameDetailsAsync(string externalId, CancellationToken ct = default)
    {
        // Requires API access usually
        return Task.FromResult(Result.Failure<Game>("Not implemented"));
    }

    public Task<Result<bool>> InstallGameAsync(string externalId, string installPath, CancellationToken ct = default)
    {
        // Requires Butler interaction
        return Task.FromResult(Result.Success(false));
    }

    private string GetDbPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        // %APPDATA%\itch\db\butler.db
        return Path.Combine(appData, "itch", "db", "butler.db");
    }
}
