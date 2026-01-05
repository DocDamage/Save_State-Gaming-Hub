using Microsoft.Extensions.Logging;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.Common.Enums;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using System.Text.Json;

namespace SaveState.Application.CloudServices.Services;

public class BackupService : IBackupService
{
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IGameRepository gameRepository, ILogger<BackupService> logger)
    {
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<BackupResult> CreateBackupAsync(
        Core.Common.Enums.BackupType type,
        string? name,
        IEnumerable<GameId>? gameIds,
        bool includeSettings,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var backupId = BackupId.NewId();

        try
        {
            _logger.LogInformation("Starting backup {BackupId} of type {Type}", backupId, type);

            // Determine which games to backup
            var gamesToBackup = await GetGamesToBackupAsync(gameIds, ct).ConfigureAwait(false);

        // Create backup directory
        var backupPath = CreateBackupDirectory(backupId, name);

        // Perform backup based on type
        var result = await PerformBackupAsync(type, gamesToBackup, backupPath, includeSettings, backupId, startTime, ct).ConfigureAwait(false);

        return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup {BackupId} failed", backupId);
            throw;
        }
    }

    private async Task<IReadOnlyList<Game>> GetGamesToBackupAsync(IEnumerable<GameId>? gameIds, CancellationToken ct)
    {
        if (gameIds?.Any() == true)
        {
            // Backup specific games
            var games = new List<Game>();
            foreach (var gameId in gameIds)
            {
                var game = await _gameRepository.GetByIdAsync(gameId, ct).ConfigureAwait(false);
                if (game != null)
                {
                    games.Add(game);
                }
            }
            return games;
        }
        else
        {
            // Backup all games
            return await _gameRepository.GetAllAsync(ct).ConfigureAwait(false);
        }
    }

    private static string CreateBackupDirectory(BackupId backupId, string? name)
    {
        var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupName = name ?? $"backup_{timestamp}";
        var backupPath = Path.Combine(basePath, backupName);

        Directory.CreateDirectory(backupPath);

        // Create metadata file
        var metadataPath = Path.Combine(backupPath, "backup.json");
        var metadata = new
        {
            BackupId = backupId.Value,
            CreatedAt = DateTime.UtcNow,
            Name = backupName
        };

        File.WriteAllText(metadataPath, System.Text.Json.JsonSerializer.Serialize(metadata));

        return backupPath;
    }

    private async Task<BackupResult> PerformBackupAsync(
        Core.Common.Enums.BackupType type,
        IReadOnlyList<Game> games,
        string backupPath,
        bool includeSettings,
        BackupId backupId,
        DateTime startTime,
        CancellationToken ct)
    {
        long totalSize = 0;
        var gamesBackedUp = 0;

        foreach (var game in games)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var gameBackupSize = await BackupGameAsync(game, backupPath, type, ct).ConfigureAwait(false);
                totalSize += gameBackupSize;
                gamesBackedUp++;

                _logger.LogDebug("Backed up game {GameId}: {Title}", game.Id, game.Title);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to backup game {GameId}: {Title}", game.Id, game.Title);
                // Continue with other games
            }
        }

        // Backup settings if requested
        if (includeSettings)
        {
            var settingsSize = await BackupSettingsAsync(backupPath, ct).ConfigureAwait(false);
            totalSize += settingsSize;
        }

        // Update metadata with final results
        await UpdateBackupMetadataAsync(backupPath, totalSize, gamesBackedUp, ct).ConfigureAwait(false);

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation("Backup {BackupId} completed in {Duration}. Backed up {GameCount} games, {Size} bytes",
            backupId, duration, gamesBackedUp, totalSize);

        return new BackupResult(
            BackupId: backupId,
            BackupPath: backupPath,
            TotalSize: totalSize,
            GamesBackedUp: gamesBackedUp,
            Duration: duration
        );
    }

    private async Task UpdateBackupMetadataAsync(string backupPath, long totalSize, int gamesBackedUp, CancellationToken ct)
    {
        var metadataPath = Path.Combine(backupPath, "backup.json");
        if (File.Exists(metadataPath))
        {
            var json = await File.ReadAllTextAsync(metadataPath, ct);
            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (metadata != null)
            {
                metadata["TotalSize"] = totalSize;
                metadata["GamesBackedUp"] = gamesBackedUp;
                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata), ct);
            }
        }
    }

    private static async Task<long> BackupGameAsync(Game game, string backupPath, Core.Common.Enums.BackupType type, CancellationToken ct)
    {
        long totalSize = 0;

        // Create game-specific directory
        var gamePath = Path.Combine(backupPath, "games", game.Id.ToString());
        Directory.CreateDirectory(gamePath);

        // Backup game metadata
        var metadataPath = Path.Combine(gamePath, "metadata.json");
        var metadata = new
        {
            game.Id,
            game.Title,
            game.Description,
            game.InstallPath,
            game.Status,
            game.CreatedAt,
            game.UpdatedAt
        };

        await File.WriteAllTextAsync(metadataPath,
            System.Text.Json.JsonSerializer.Serialize(metadata), ct).ConfigureAwait(false);

        // Backup game files based on type
        if (type == Core.Common.Enums.BackupType.Full || type == Core.Common.Enums.BackupType.SaveStatesOnly)
        {
            // In a real implementation, this would copy save files, configurations, etc.
            // For demo purposes, we'll just create a placeholder
            var saveDataPath = Path.Combine(gamePath, "save_data.txt");
            await File.WriteAllTextAsync(saveDataPath, "Mock save data", ct).ConfigureAwait(false);
            totalSize += "Mock save data".Length;
        }

        return totalSize;
    }

    private static async Task<long> BackupSettingsAsync(string backupPath, CancellationToken ct)
    {
        var settingsPath = Path.Combine(backupPath, "settings");
        Directory.CreateDirectory(settingsPath);

        // Mock settings backup
        var settingsFile = Path.Combine(settingsPath, "app_settings.json");
        var mockSettings = new { Theme = "Dark", Language = "en", AutoBackup = true };
        var settingsJson = System.Text.Json.JsonSerializer.Serialize(mockSettings);

        await File.WriteAllTextAsync(settingsFile, settingsJson, ct).ConfigureAwait(false);

        return settingsJson.Length;
    }
    public async Task<IReadOnlyList<BackupMetadata>> GetBackupHistoryAsync(CancellationToken ct = default)
    {
        var history = new List<BackupMetadata>();
        var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

        if (!Directory.Exists(basePath))
        {
            return history;
        }

        try
        {
            var directories = Directory.GetDirectories(basePath);
            foreach (var dir in directories)
            {
                var metadataPath = Path.Combine(dir, "backup.json");
                if (File.Exists(metadataPath))
                {
                    var json = await File.ReadAllTextAsync(metadataPath, ct);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    history.Add(new BackupMetadata(
                        BackupId: BackupId.From(root.GetProperty("BackupId").GetGuid()),
                        Name: root.GetProperty("Name").GetString() ?? "Unknown",
                        CreatedAt: root.GetProperty("CreatedAt").GetDateTime(),
                        TotalSize: root.TryGetProperty("TotalSize", out var sizeProp) ? sizeProp.GetInt64() : 0,
                        GamesBackedUp: root.TryGetProperty("GamesBackedUp", out var gamesProp) ? gamesProp.GetInt32() : 0
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load backup history");
        }

        return history.OrderByDescending(b => b.CreatedAt).ToList();
    }
}
