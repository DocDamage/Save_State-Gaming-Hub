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
            var game = await _gameRepository.GetByIdAsync(gameId, ct);
            if (game == null)
            {
                return Result<GameMod>.Failure("Game not found");
            }

            if (!File.Exists(sourceFilePath))
            {
                return Result<GameMod>.Failure("Source file not found");
            }

            var fileInfo = new FileInfo(sourceFilePath);
            var modName = Path.GetFileNameWithoutExtension(sourceFilePath);

            // TODO: Implement proper mod extraction/installation logic
            // For now, we simulate installation by creating the entity

            // Determine install path (placeholder)
            var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaveState", "Mods", gameId.Value.ToString(), modName);

            var mod = GameMod.Create(
                gameId,
                modName, // Name
                "1.0.0", // Version (placeholder)
                installPath,
                fileInfo.Length,
                description: "Imported mod",
                category: "Other",
                author: "Unknown",
                tags: new List<string> { "Imported" }
            );

            await _modRepository.AddAsync(mod, ct);
            return Result<GameMod>.Success(mod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install mod {SourceFile}", sourceFilePath);
            return Result<GameMod>.Failure($"Failed to install mod: {ex.Message}");
        }
    }

    public async Task<Result> UninstallModAsync(Guid modId, bool deleteFiles = true, CancellationToken ct = default)
    {
        try
        {
            var mod = await _modRepository.GetByIdAsync(modId, ct);
            if (mod == null)
            {
                return Result.Failure("Mod not found");
            }

            // TODO: Delete files if requested
            if (deleteFiles && !string.IsNullOrEmpty(mod.InstallPath))
            {
               // Delete logic here
            }

            await _modRepository.DeleteAsync(modId, ct);
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
            var mod = await _modRepository.GetByIdAsync(modId, ct);
            if (mod == null)
            {
                return Result.Failure("Mod not found");
            }

            if (enabled)
                mod.Enable();
            else
                mod.Disable();

            await _modRepository.UpdateAsync(mod, ct);
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
            var mods = await _modRepository.GetByGameIdAsync(gameId, ct);
            var modMap = mods.ToDictionary(m => m.Id);

            for (int i = 0; i < modIdsInOrder.Count; i++)
            {
                if (modMap.TryGetValue(modIdsInOrder[i], out var mod))
                {
                    mod.SetLoadOrder(i);
                    await _modRepository.UpdateAsync(mod, ct);
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
            var game = await _gameRepository.GetByIdAsync(gameId, ct);
            if (game == null) return Result<IReadOnlyList<GameMod>>.Failure("Game not found");

            var modsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaveState", "Mods", gameId.Value.ToString());

            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
                return Result<IReadOnlyList<GameMod>>.Success(new List<GameMod>());
            }

            var directories = Directory.GetDirectories(modsFolder);
            var scannedMods = new List<GameMod>();

            foreach (var dir in directories)
            {
                var modName = Path.GetFileName(dir);
                var existingMod = (await _modRepository.GetByGameIdAsync(gameId, ct)).FirstOrDefault(m => m.Name == modName);

                if (existingMod == null)
                {
                    var mod = GameMod.Create(
                        gameId,
                        modName,
                        "1.0.0",
                        dir,
                        0, // Unknown size
                        description: "Discovered during scan"
                    );
                    await _modRepository.AddAsync(mod, ct);
                    scannedMods.Add(mod);
                }
                else
                {
                    scannedMods.Add(existingMod);
                }
            }

            return Result<IReadOnlyList<GameMod>>.Success(scannedMods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan for mods for game {GameId}", gameId);
            return Result<IReadOnlyList<GameMod>>.Failure(ex.Message);
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
        return Task.FromResult(Result<IReadOnlyList<ModSource>>.Success(sources));
    }
}
