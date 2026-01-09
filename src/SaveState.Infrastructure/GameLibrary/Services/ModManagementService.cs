namespace SaveState.Infrastructure.GameLibrary.Services;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ModManagementService : IModManagementService
{
    private readonly IGameModRepository _modRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<ModManagementService> _logger;

    public ModManagementService(
        IGameModRepository modRepository,
        IGameRepository gameRepository,
        ILogger<ModManagementService> logger)
    {
        _modRepository = modRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<GameMod>> InstallModAsync(GameId gameId, string sourceFilePath, CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(gameId, ct).ConfigureAwait(false);
            if (game == null)
            {
                return Result.Failure<GameMod>("Game not found");
            }

            if (!File.Exists(sourceFilePath))
            {
                return Result.Failure<GameMod>("Source file not found");
            }

            var fileInfo = new FileInfo(sourceFilePath);
            var modName = Path.GetFileNameWithoutExtension(sourceFilePath);
            var extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();

            // Determine install path (managed location in local app data)
            var baseModPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaveState", "Mods", gameId.Value.ToString());
            var installPath = Path.Combine(baseModPath, modName);

            // Ensure the directory exists and is empty
            if (Directory.Exists(installPath))
            {
                _logger.LogInformation("Mod directory already exists, clearing for fresh install: {Path}", installPath);
                Directory.Delete(installPath, true);
            }
            Directory.CreateDirectory(installPath);

            _logger.LogInformation("Installing mod '{ModName}' for game {GameId} to {InstallPath}", modName, gameId, installPath);

            // Perform extraction or copy
            if (extension == ".zip")
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(sourceFilePath, installPath), ct).ConfigureAwait(false);
            }
            else
            {
                // Just copy the single file if it's not a zip
                var targetFile = Path.Combine(installPath, Path.GetFileName(sourceFilePath));
                await Task.Run(() => File.Copy(sourceFilePath, targetFile, true), ct).ConfigureAwait(false);
            }

            // Create the mod entity
            var mod = GameMod.Create(
                gameId,
                modName,
                "1.0.0", // Default version
                installPath,
                fileInfo.Length,
                description: $"Installed from {Path.GetFileName(sourceFilePath)}",
                category: "Other",
                author: "Unknown",
                tags: new List<string> { "Imported", extension.TrimStart('.') }
            );

            await _modRepository.AddAsync(mod, ct).ConfigureAwait(false);
            return Result.Success<GameMod>(mod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install mod {SourceFile}", sourceFilePath);
            return Result.Failure<GameMod>($"Failed to install mod: {ex.Message}");
        }
    }

    public async Task<Result> UninstallModAsync(Guid modId, bool deleteFiles = true, CancellationToken ct = default)
    {
        try
        {
            var mod = await _modRepository.GetByIdAsync(modId, ct).ConfigureAwait(false);
            if (mod == null)
            {
                return Result.Failure("Mod not found");
            }

            if (deleteFiles && !string.IsNullOrEmpty(mod.InstallPath))
            {
                if (Directory.Exists(mod.InstallPath))
                {
                    _logger.LogInformation("Deleting mod files at {InstallPath}", mod.InstallPath);
                    await Task.Run(() => Directory.Delete(mod.InstallPath, true), ct).ConfigureAwait(false);

                    // Clean up parent directory if it's now empty
                    var parentDir = Path.GetDirectoryName(mod.InstallPath);
                    if (parentDir != null && Directory.Exists(parentDir))
                    {
                        if (!Directory.EnumerateFileSystemEntries(parentDir).Any())
                        {
                            Directory.Delete(parentDir);
                        }
                    }
                }
                else if (File.Exists(mod.InstallPath))
                {
                    _logger.LogInformation("Deleting mod file at {InstallPath}", mod.InstallPath);
                    File.Delete(mod.InstallPath);
                }
            }

            await _modRepository.DeleteAsync(modId, ct).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall mod {ModId}", modId);
            return Result.Failure($"Failed to uninstall mod: {ex.Message}");
        }
    }

    public async Task<Result> ToggleModAsync(Guid modId, bool enabled, CancellationToken ct = default)
    {
        try
        {
            var mod = await _modRepository.GetByIdAsync(modId, ct).ConfigureAwait(false);
            if (mod == null)
            {
                return Result.Failure("Mod not found");
            }

            if (enabled)
                mod.Enable();
            else
                mod.Disable();

            await _modRepository.UpdateAsync(mod, ct).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to toggle mod {ModId}", modId);
            return Result.Failure($"Failed to toggle mod: {ex.Message}");
        }
    }

    public async Task<Result> UpdateLoadOrderAsync(GameId gameId, IList<Guid> modIdsInOrder, CancellationToken ct = default)
    {
        try
        {
            var mods = await _modRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
            var modMap = mods.ToDictionary(m => m.Id);

            for (int i = 0; i < modIdsInOrder.Count; i++)
            {
                if (modMap.TryGetValue(modIdsInOrder[i], out var mod))
                {
                    mod.SetLoadOrder(i);
                    await _modRepository.UpdateAsync(mod, ct).ConfigureAwait(false);
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update load order for game {GameId}", gameId);
            return Result.Failure($"Failed to update load order: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<GameMod>>> ScanForModsAsync(GameId gameId, CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(gameId, ct).ConfigureAwait(false);
            if (game == null) return Result.Failure<IReadOnlyList<GameMod>>("Game not found");

            var modsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaveState", "Mods", gameId.Value.ToString());

            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
                return Result.Success<IReadOnlyList<GameMod>>(new List<GameMod>());
            }

            var directories = Directory.GetDirectories(modsFolder);
            var scannedMods = new List<GameMod>();

            var existingMods = await _modRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);

            foreach (var dir in directories)
            {
                var modName = Path.GetFileName(dir);
                var existingMod = existingMods.FirstOrDefault(m => m.Name == modName);

                if (existingMod == null)
                {
                    var mod = GameMod.Create(
                        gameId,
                        modName,
                        "1.0.0",
                        dir,
                        0, // Unknown size without scanning all files
                        description: "Discovered during scan"
                    );
                    await _modRepository.AddAsync(mod, ct).ConfigureAwait(false);
                    scannedMods.Add(mod);
                }
                else
                {
                    scannedMods.Add(existingMod);
                }
            }

            return Result.Success<IReadOnlyList<GameMod>>(scannedMods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan for mods for game {GameId}", gameId);
            return Result.Failure<IReadOnlyList<GameMod>>(ex.Message);
        }
    }

    public Task<Result<IReadOnlyList<ModSource>>> GetExternalModSourcesAsync(CancellationToken ct = default)
    {
        var sources = new List<ModSource>
        {
            new ModSource("Nexus Mods", "https://www.nexusmods.com", "nexus"),
            new ModSource("ModDB", "https://www.moddb.com", "moddb"),
            new ModSource("Steam Workshop", "https://steamcommunity.com/workshop", "steam")
        };
        return Task.FromResult(Result.Success<IReadOnlyList<ModSource>>(sources));
    }
}
