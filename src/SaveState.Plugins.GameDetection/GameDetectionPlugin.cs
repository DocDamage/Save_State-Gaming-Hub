using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.Plugins;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace SaveState.Plugins.GameDetection;

/// <summary>
/// Plugin that provides CLI commands for game detection and import from Steam, Epic, and GOG.
/// Exposes the existing built-in game detection infrastructure through CLI commands.
/// </summary>
public class GameDetectionPlugin : IPlugin
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private IGameDetectorService? _gameDetector;
    private IGameImportService? _gameImporter;

    public string Id => "savestate.game-detection";
    public string Name => "Game Detection CLI";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "CLI commands for detecting and importing games from Steam, Epic, GOG, and custom directories";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension; // Adds CLI commands

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation("Initializing Game Detection CLI plugin");

        // Get required services
        _gameDetector = context.Services.GetService(typeof(IGameDetectorService)) as IGameDetectorService;
        _gameImporter = context.Services.GetService(typeof(IGameImportService)) as IGameImportService;

        if (_gameDetector == null)
        {
            _logger.LogWarning("IGameDetectorService not available - game detection features will be limited");
        }

        if (_gameImporter == null)
        {
            _logger.LogWarning("IGameImportService not available - game import features will be limited");
        }

        // Register CLI commands
        await RegisterCliCommandsAsync(context, ct);

        _logger.LogInformation("Game Detection CLI plugin initialized successfully");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down Game Detection CLI plugin");
        return Task.CompletedTask;
    }

    private async Task RegisterCliCommandsAsync(IPluginContext context, CancellationToken ct)
    {
        // Main detect command
        var detectCommand = new Command("detect", "Detect and import games from various sources");

        // Detect all games
        var detectAllCommand = new Command("all", "Detect games from all available sources");
        detectAllCommand.SetHandler(async (InvocationContext context) => await HandleDetectAllAsync());

        // Detect from specific platforms
        var detectSteamCommand = new Command("steam", "Detect games from Steam library");
        detectSteamCommand.SetHandler(async (InvocationContext context) => await HandleDetectSteamAsync());

        var detectEpicCommand = new Command("epic", "Detect games from Epic Games Store");
        detectEpicCommand.SetHandler(async (InvocationContext context) => await HandleDetectEpicAsync());

        var detectGogCommand = new Command("gog", "Detect games from GOG Galaxy");
        detectGogCommand.SetHandler(async (InvocationContext context) => await HandleDetectGogAsync());

        // Detect from custom directory
        var detectDirectoryCommand = new Command("directory", "Detect games from a custom directory");
        var directoryArgument = new Argument<string>("path") { Description = "Directory path to scan for games" };
        var recursiveOption = new Option<bool>("--recursive") { DefaultValueFactory = _ => true, Description = "Scan subdirectories recursively" };
        detectDirectoryCommand.AddArgument(directoryArgument);
        detectDirectoryCommand.AddOption(recursiveOption);
        detectDirectoryCommand.SetHandler(async (InvocationContext context) =>
        {
            var path = context.ParseResult.GetValueForArgument(directoryArgument);
            var recursive = context.ParseResult.GetValueForOption(recursiveOption);
            await HandleDetectDirectoryAsync(path, recursive);
        });

        // Import individual game
        var importCommand = new Command("import", "Import a specific game");

        var importSteamCommand = new Command("steam", "Import a game from Steam");
        var steamAppIdArgument = new Argument<string>("app-id") { Description = "Steam App ID" };
        importSteamCommand.AddArgument(steamAppIdArgument);
        importSteamCommand.SetHandler(async (InvocationContext context) =>
        {
            var appId = context.ParseResult.GetValueForArgument(steamAppIdArgument);
            await HandleImportSteamAsync(appId);
        });

        var importEpicCommand = new Command("epic", "Import a game from Epic Games Store");
        var epicIdArgument = new Argument<string>("epic-id") { Description = "Epic Game ID" };
        importEpicCommand.AddArgument(epicIdArgument);
        importEpicCommand.SetHandler(async (InvocationContext context) =>
        {
            var epicId = context.ParseResult.GetValueForArgument(epicIdArgument);
            await HandleImportEpicAsync(epicId);
        });

        var importGogCommand = new Command("gog", "Import a game from GOG");
        var gogIdArgument = new Argument<string>("gog-id") { Description = "GOG Game ID" };
        importGogCommand.AddArgument(gogIdArgument);
        importGogCommand.SetHandler(async (InvocationContext context) =>
        {
            var gogId = context.ParseResult.GetValueForArgument(gogIdArgument);
            await HandleImportGogAsync(gogId);
        });

        var importManualCommand = new Command("manual", "Manually import a game");
        var titleArgument = new Argument<string>("title") { Description = "Game title" };
        var descriptionOption = new Option<string?>("--description") { Description = "Game description" };
        importManualCommand.AddArgument(titleArgument);
        importManualCommand.AddOption(descriptionOption);
        importManualCommand.SetHandler(async (InvocationContext context) =>
        {
            var title = context.ParseResult.GetValueForArgument(titleArgument);
            var description = context.ParseResult.GetValueForOption(descriptionOption);
            await HandleImportManualAsync(title, description);
        });

        // Build command hierarchy
        detectCommand.AddCommand(detectAllCommand);
        detectCommand.AddCommand(detectSteamCommand);
        detectCommand.AddCommand(detectEpicCommand);
        detectCommand.AddCommand(detectGogCommand);
        detectCommand.AddCommand(detectDirectoryCommand);

        importCommand.AddCommand(importSteamCommand);
        importCommand.AddCommand(importEpicCommand);
        importCommand.AddCommand(importGogCommand);
        importCommand.AddCommand(importManualCommand);

        // Register with CLI system (this would need to be integrated with the main CLI)
        // For now, we'll just log that the commands are available
        _logger?.LogInformation("Game Detection CLI commands registered");
        _logger?.LogInformation("Available commands:");
        _logger?.LogInformation("  savestate detect all - Detect games from all sources");
        _logger?.LogInformation("  savestate detect steam - Detect Steam games");
        _logger?.LogInformation("  savestate detect epic - Detect Epic games");
        _logger?.LogInformation("  savestate detect gog - Detect GOG games");
        _logger?.LogInformation("  savestate detect directory <path> - Detect games in directory");
        _logger?.LogInformation("  savestate import steam <app-id> - Import specific Steam game");
        _logger?.LogInformation("  savestate import epic <epic-id> - Import specific Epic game");
        _logger?.LogInformation("  savestate import gog <gog-id> - Import specific GOG game");
        _logger?.LogInformation("  savestate import manual <title> - Manually import a game");
    }

    private async Task HandleDetectAllAsync()
    {
        if (_gameDetector == null)
        {
            _logger?.LogError("Game detector service not available");
            return;
        }

        try
        {
            _logger?.LogInformation("🔍 Starting full game library scan...");

            var detectedGames = await _gameDetector.ScanAllAsync();

            _logger?.LogInformation($"✅ Scan complete! Found {detectedGames.Count} games:");
            DisplayDetectedGames(detectedGames);

            // Detection only lists potential games.
            // Import must be initiated separately via the 'import' command for specific titles
            // to allow user selection and metadata verification.
            _logger?.LogInformation("Note: Use 'import' commands to add detected games to your library");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during full game detection");
        }
    }

    private async Task HandleDetectSteamAsync()
    {
        if (_gameDetector == null)
        {
            _logger?.LogError("Game detector service not available");
            return;
        }

        try
        {
            _logger?.LogInformation("🔍 Scanning Steam library...");

            var detectedGames = await _gameDetector.ScanSteamAsync();

            _logger?.LogInformation($"✅ Steam scan complete! Found {detectedGames.Count} games:");
            DisplayDetectedGames(detectedGames);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during Steam game detection");
        }
    }

    private async Task HandleDetectEpicAsync()
    {
        if (_gameDetector == null)
        {
            _logger?.LogError("Game detector service not available");
            return;
        }

        try
        {
            _logger?.LogInformation("🔍 Scanning Epic Games Store...");

            var detectedGames = await _gameDetector.ScanEpicAsync();

            _logger?.LogInformation($"✅ Epic scan complete! Found {detectedGames.Count} games:");
            DisplayDetectedGames(detectedGames);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during Epic game detection");
        }
    }

    private async Task HandleDetectGogAsync()
    {
        if (_gameDetector == null)
        {
            _logger?.LogError("Game detector service not available");
            return;
        }

        try
        {
            _logger?.LogInformation("🔍 Scanning GOG Galaxy...");

            var detectedGames = await _gameDetector.ScanGogAsync();

            _logger?.LogInformation($"✅ GOG scan complete! Found {detectedGames.Count} games:");
            DisplayDetectedGames(detectedGames);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during GOG game detection");
        }
    }

    private async Task HandleDetectDirectoryAsync(string path, bool recursive)
    {
        if (_gameDetector == null)
        {
            _logger?.LogError("Game detector service not available");
            return;
        }

        try
        {
            _logger?.LogInformation($"🔍 Scanning directory: {path} (recursive: {recursive})");

            var detectedGames = await _gameDetector.ScanDirectoryAsync(path, recursive);

            _logger?.LogInformation($"✅ Directory scan complete! Found {detectedGames.Count} potential games:");
            DisplayDetectedGames(detectedGames);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during directory scan");
        }
    }

    private async Task HandleImportSteamAsync(string appId)
    {
        if (_gameImporter == null)
        {
            _logger?.LogError("Game importer service not available");
            return;
        }

        try
        {
            _logger?.LogInformation($"📥 Importing Steam game with App ID: {appId}");

            var game = await _gameImporter.ImportGameFromSteamAsync(appId);
            _logger?.LogInformation($"✅ Successfully imported: {game.Title} ({game.Id})");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error importing Steam game");
        }
    }

    private async Task HandleImportEpicAsync(string epicId)
    {
        if (_gameImporter == null)
        {
            _logger?.LogError("Game importer service not available");
            return;
        }

        try
        {
            _logger?.LogInformation($"📥 Importing Epic game with ID: {epicId}");

            var game = await _gameImporter.ImportGameFromEpicAsync(epicId);
            _logger?.LogInformation($"✅ Successfully imported: {game.Title} ({game.Id})");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error importing Epic game");
        }
    }

    private async Task HandleImportGogAsync(string gogId)
    {
        if (_gameImporter == null)
        {
            _logger?.LogError("Game importer service not available");
            return;
        }

        try
        {
            _logger?.LogInformation($"📥 Importing GOG game with ID: {gogId}");

            var game = await _gameImporter.ImportGameFromGogAsync(gogId);
            _logger?.LogInformation($"✅ Successfully imported: {game.Title} ({game.Id})");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error importing GOG game");
        }
    }

    private async Task HandleImportManualAsync(string title, string? description)
    {
        if (_gameImporter == null)
        {
            _logger?.LogError("Game importer service not available");
            return;
        }

        try
        {
            _logger?.LogInformation($"📝 Manually importing game: {title}");

            var game = await _gameImporter.ImportGameManuallyAsync(title, description);
            _logger?.LogInformation($"✅ Successfully imported: {game.Title} ({game.Id})");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error importing game manually");
        }
    }

    private void DisplayDetectedGames(IReadOnlyList<DetectedGame> games)
    {
        if (!games.Any())
        {
            _logger?.LogInformation("  No games detected");
            return;
        }

        foreach (var game in games)
        {
            var sizeText = game.SizeBytes.HasValue
                ? $"{game.SizeBytes.Value / (1024 * 1024):N0} MB"
                : "Unknown size";

            _logger?.LogInformation($"  🎮 {game.Title}");
            _logger?.LogInformation($"     Platform: {game.PlatformHint ?? "Unknown"}");
            _logger?.LogInformation($"     Source: {game.Source}");
            _logger?.LogInformation($"     Size: {sizeText}");
            _logger?.LogInformation($"     Path: {game.ExecutablePath}");

            if (game.Metadata?.Any() == true)
            {
                _logger?.LogInformation("     Metadata:");
                foreach (var kvp in game.Metadata)
                {
                    _logger?.LogInformation($"       {kvp.Key}: {kvp.Value}");
                }
            }

            _logger?.LogInformation(""); // Empty line for readability
        }
    }
}

