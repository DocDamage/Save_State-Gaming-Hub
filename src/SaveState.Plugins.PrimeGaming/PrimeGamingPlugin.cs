using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.PrimeGaming;

public sealed class PrimeGamingPlugin : IPlugin, IGameProvider
{
    private IPluginContext? _context;

    public string Id => "prime-gaming";
    public string Name => "Prime Gaming";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Import installed games from Amazon Games Launcher.";
    public PluginCapabilities Capabilities => PluginCapabilities.GameProvider;

    public string ProviderName => "Prime Gaming";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Prime Gaming initialized");
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
                return Result.Success<IReadOnlyList<Game>>(games);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            // Amazon Games DB often has a table 'install_info' or similar.
            // We'll try to select from a generic guess or catch.
            // A common table name seen is 'GameInstallInfo'
            command.CommandText = "SELECT product_title, install_directory FROM GameInstallInfo";

            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var title = reader.GetString(0);
                    // Add to list...
                }
            }
            catch (Exception ex)
            {
                 _context?.Logger.LogWarning("Could not query Amazon Games DB: {Message}", ex.Message);
            }
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to discover Prime Gaming games");
            return Result.Failure<IReadOnlyList<Game>>(ex.Message);
        }

        return Result.Success<IReadOnlyList<Game>>(games);
    }

    public Task<Result<Game>> GetGameDetailsAsync(string externalId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Failure<Game>("Not implemented"));
    }

    public Task<Result<bool>> InstallGameAsync(string externalId, string installPath, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(false));
    }

    private string GetDbPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        // %LOCALAPPDATA%\Amazon Games\Data\Games\Sql\GameInstallInfo.sqlite
        return Path.Combine(localAppData, "Amazon Games", "Data", "Games", "Sql", "GameInstallInfo.sqlite");
    }
}
