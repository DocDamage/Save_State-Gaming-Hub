using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Services;
using System.Diagnostics;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SaveState.Infrastructure.External;

/// <summary>
/// Game provider for Ubisoft Connect (formerly Uplay) platform.
/// </summary>
public class UbisoftProvider : IGameProvider
{
    private readonly ILogger<UbisoftProvider> _logger;
    private const string UbisoftProtocol = "uplay://";

    public string Name => "Ubisoft Connect";
    public ProviderCapabilities Capabilities => ProviderCapabilities.Discovery | ProviderCapabilities.Launch;

    public UbisoftProvider(ILogger<UbisoftProvider> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<GameInfo>();

            // Get Ubisoft Connect install path from registry
            var ubisoftPath = GetUbisoftInstallPath();
            if (string.IsNullOrEmpty(ubisoftPath))
            {
                _logger.LogDebug("Ubisoft Connect not found");
                return Array.Empty<GameInfo>();
            }

            // Look for game configuration files
            var configPath = Path.Combine(ubisoftPath, "cache", "configuration", "configurations");
            if (!Directory.Exists(configPath))
            {
                _logger.LogDebug("Ubisoft configurations directory not found: {Path}", configPath);
                return Array.Empty<GameInfo>();
            }

            var configFiles = Directory.GetFiles(configPath, "*.yml", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(configPath, "*.yaml", SearchOption.TopDirectoryOnly))
                .ToList();

            foreach (var configFile in configFiles)
            {
                try
                {
                    var gameInfo = await ParseUbisoftConfigAsync(configFile, ct);
                    if (gameInfo != null)
                    {
                        games.Add(gameInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse Ubisoft config: {File}", configFile);
                }
            }

            _logger.LogInformation("Found {Count} Ubisoft Connect games", games.Count);
            return games;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Ubisoft Connect games");
            return Array.Empty<GameInfo>();
        }
    }

    private string? GetUbisoftInstallPath()
    {
        try
        {
            // Try registry first
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Ubisoft\Launcher");
            if (key != null)
            {
                var installDir = key.GetValue("InstallDir") as string;
                if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
                {
                    return installDir;
                }
            }

            // Try common installation paths
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var commonPaths = new[]
            {
                Path.Combine(programFiles, "Ubisoft", "Ubisoft Game Launcher"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ubisoft", "Ubisoft Game Launcher"),
                Path.Combine(programFiles, "Uplay")
            };

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error finding Ubisoft install path");
        }

        return null;
    }

    private async Task<GameInfo?> ParseUbisoftConfigAsync(string configFile, CancellationToken ct)
    {
        try
        {
            var yaml = await File.ReadAllTextAsync(configFile, ct);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<UbisoftGameConfig>(yaml);

            if (config?.Root == null || string.IsNullOrWhiteSpace(config.Root.Name))
            {
                return null;
            }

            // Extract game ID from filename (usually numbered like 12345.yml)
            var gameId = Path.GetFileNameWithoutExtension(configFile);

            return new GameInfo
            {
                Source = "Ubisoft",
                SourceId = gameId,
                Title = config.Root.Name,
                InstallPath = config.Root.InstallDir,
                Platform = "PC"
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse YAML config");
            return null;
        }
    }

    public Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default)
    {
        // Ubisoft Connect doesn't provide a public API for metadata
        return Task.FromResult(new GameMetadata
        {
            Title = gameId
        });
    }

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            // Ubisoft Connect uses uplay:// protocol
            // Format: uplay://launch/{gameId}
            var uri = $"{UbisoftProtocol}launch/{gameId}";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });

            _logger.LogInformation("Launched Ubisoft game: {GameId}", gameId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Ubisoft game: {GameId}", gameId);
            return Task.FromResult(false);
        }
    }

    // Helper classes for YAML deserialization
    private class UbisoftGameConfig
    {
        public RootConfig? Root { get; set; }
    }

    private class RootConfig
    {
        public string? Name { get; set; }
        public string? InstallDir { get; set; }
        public string? Executable { get; set; }
    }
}
