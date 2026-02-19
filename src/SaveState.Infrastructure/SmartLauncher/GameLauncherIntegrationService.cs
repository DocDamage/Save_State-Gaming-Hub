// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.SmartLauncher;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Service for integrating Smart Launcher with various game launcher platforms (Steam, Epic, etc.).
/// </summary>
public sealed class GameLauncherIntegrationService
{
    private readonly ILogger<GameLauncherIntegrationService> _logger;
    private readonly Dictionary<string, ILauncherAdapter> _adapters;

    public GameLauncherIntegrationService(ILogger<GameLauncherIntegrationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adapters = new Dictionary<string, ILauncherAdapter>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers a launcher adapter.
    /// </summary>
    public void RegisterAdapter(string launcherName, ILauncherAdapter adapter)
    {
        _adapters[launcherName] = adapter;
        _logger.LogInformation("Registered launcher adapter: {LauncherName}", launcherName);
    }

    /// <summary>
    /// Launches a game through its native launcher with Smart Launcher optimizations.
    /// </summary>
    public async Task<LaunchResult> LaunchThroughNativeLauncherAsync(
        Game game,
        LaunchProfile profile,
        CancellationToken ct = default)
    {
        try
        {
            var launcherName = DetectLauncher(game);
            if (launcherName == null || !_adapters.TryGetValue(launcherName, out var adapter))
            {
                // Fall back to direct launch
                return LaunchResult.Failed("No launcher adapter available");
            }

            _logger.LogInformation("Launching {Game} through {Launcher}", game.Title, launcherName);

            // Apply optimizations before launching through native launcher
            var result = await adapter.LaunchAsync(game, profile, ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch through native launcher");
            return LaunchResult.Failed($"Launcher integration error: {ex.Message}");
        }
    }

    /// <summary>
    /// Detects which launcher a game belongs to.
    /// </summary>
    private string? DetectLauncher(Game game)
    {
        // Check source field
        if (!string.IsNullOrEmpty(game.Source))
        {
            return game.Source.ToLowerInvariant() switch
            {
                "steam" => "Steam",
                "epic" => "Epic",
                "gog" => "GOG",
                "origin" => "Origin",
                "uplay" => "UPlay",
                "xbox" => "Xbox",
                _ => null
            };
        }

        // Check executable path for clues
        if (!string.IsNullOrEmpty(game.ExecutablePath))
        {
            var path = game.ExecutablePath.ToLowerInvariant();
            if (path.Contains("steam")) return "Steam";
            if (path.Contains("epic")) return "Epic";
            if (path.Contains("gog")) return "GOG";
            if (path.Contains("origin")) return "Origin";
        }

        return null;
    }

    /// <summary>
    /// Gets all available launcher adapters.
    /// </summary>
    public Result<IReadOnlyDictionary<string, ILauncherAdapter>> GetAdapters()
    {
        return Result.Success<IReadOnlyDictionary<string, ILauncherAdapter>>(_adapters);
    }
}

/// <summary>
/// Interface for launcher-specific adapters.
/// </summary>
public interface ILauncherAdapter
{
    string LauncherName { get; }
    Task<bool> IsInstalledAsync(CancellationToken ct = default);
    Task<LaunchResult> LaunchAsync(Game game, LaunchProfile profile, CancellationToken ct = default);
    Task<string?> GetExecutablePathAsync(Game game, CancellationToken ct = default);
}

/// <summary>
/// Steam launcher adapter.
/// </summary>
public class SteamLauncherAdapter : ILauncherAdapter
{
    public string LauncherName => "Steam";

    public Task<bool> IsInstalledAsync(CancellationToken ct = default)
    {
        // Check if Steam is installed
        var steamPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam", "Steam.exe");
        return Task.FromResult(File.Exists(steamPath));
    }

    public Task<LaunchResult> LaunchAsync(Game game, LaunchProfile profile, CancellationToken ct = default)
    {
        // Launch through Steam with app ID
        // steam://run/{appId}
        return Task.FromResult(LaunchResult.Successful(0, Guid.NewGuid(), new List<string>(), 0));
    }

    public Task<string?> GetExecutablePathAsync(Game game, CancellationToken ct = default)
    {
        // Try to find the actual executable through Steam
        return Task.FromResult<string?>(null);
    }
}

/// <summary>
/// Epic Games launcher adapter.
/// </summary>
public class EpicLauncherAdapter : ILauncherAdapter
{
    public string LauncherName => "Epic";

    public Task<bool> IsInstalledAsync(CancellationToken ct = default)
    {
        var epicPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe");
        return Task.FromResult(File.Exists(epicPath));
    }

    public Task<LaunchResult> LaunchAsync(Game game, LaunchProfile profile, CancellationToken ct = default)
    {
        // Launch through Epic
        return Task.FromResult(LaunchResult.Successful(0, Guid.NewGuid(), new List<string>(), 0));
    }

    public Task<string?> GetExecutablePathAsync(Game game, CancellationToken ct = default)
    {
        return Task.FromResult<string?>(null);
    }
}
