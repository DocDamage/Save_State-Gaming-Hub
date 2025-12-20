using Microsoft.Win32;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System.Data.SQLite;
using System.Diagnostics;

namespace SaveState.Core.Providers;

public class GogProvider : IGameProvider
{
    public string Id => "gog";
    public string Name => "GOG Galaxy";

    private readonly ILogger _logger = Log.ForContext<GogProvider>();

    public async Task<IEnumerable<Game>> GetInstalledGamesAsync()
    {
        var games = new List<Game>();
        
        // Try GOG Galaxy database first
        var galaxyDbPath = GetGalaxyDbPath();
        if (!string.IsNullOrEmpty(galaxyDbPath) && File.Exists(galaxyDbPath))
        {
            games.AddRange(await GetGamesFromGalaxyDb(galaxyDbPath));
        }

        // Fallback to registry
        if (games.Count == 0)
        {
            games.AddRange(GetGamesFromRegistry());
        }

        _logger.Information("Found {Count} GOG games", games.Count);
        return games;
    }

    public Task<IEnumerable<Game>> GetOwnedGamesAsync()
    {
        return Task.FromResult(Enumerable.Empty<Game>());
    }

    public Task LaunchGameAsync(Game game)
    {
        if (!string.IsNullOrEmpty(game.SourceId))
        {
            var url = $"goggalaxy://openGameView/{game.SourceId}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            _logger.Information("Launching GOG game: {Title}", game.Title);
        }
        return Task.CompletedTask;
    }

    private string? GetGalaxyDbPath()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dbPath = Path.Combine(programData, "GOG.com", "Galaxy", "storage", "galaxy-2.0.db");
        return File.Exists(dbPath) ? dbPath : null;
    }

    private async Task<List<Game>> GetGamesFromGalaxyDb(string dbPath)
    {
        var games = new List<Game>();
        try
        {
            // Copy DB to temp to avoid lock issues
            var tempDb = Path.Combine(Path.GetTempPath(), "gog_temp.db");
            File.Copy(dbPath, tempDb, true);

            using var connection = new SQLiteConnection($"Data Source={tempDb};Version=3;Read Only=True;");
            await connection.OpenAsync();

            var query = @"
                SELECT productId, title, installationPath 
                FROM InstalledBaseProducts 
                WHERE isInstalled = 1";

            using var command = new SQLiteCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                games.Add(new Game
                {
                    Title = reader.GetString(1),
                    SortTitle = reader.GetString(1),
                    Source = "GOG",
                    SourceId = reader.GetInt64(0).ToString(),
                    IsInstalled = true,
                    InstallPath = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }

            File.Delete(tempDb);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to read GOG Galaxy database");
        }
        return games;
    }

    private List<Game> GetGamesFromRegistry()
    {
        var games = new List<Game>();
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games");
            if (key == null) return games;

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using var gameKey = key.OpenSubKey(subKeyName);
                if (gameKey == null) continue;

                var gameName = gameKey.GetValue("gameName") as string;
                var installPath = gameKey.GetValue("path") as string;
                var gameId = gameKey.GetValue("gameID") as string;

                if (!string.IsNullOrEmpty(gameName))
                {
                    games.Add(new Game
                    {
                        Title = gameName,
                        SortTitle = gameName,
                        Source = "GOG",
                        SourceId = gameId ?? subKeyName,
                        IsInstalled = true,
                        InstallPath = installPath
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to read GOG registry");
        }
        return games;
    }
}
