using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Services;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace SaveState.Infrastructure.External;

/// <summary>
/// Game provider for Xbox Game Pass for PC platform.
/// </summary>
public class XboxGamePassProvider : IGameProvider
{
    private readonly ILogger<XboxGamePassProvider> _logger;
    private const string MicrosoftStoreProtocol = "ms-xbl-";

    public string Name => "Xbox Game Pass (PC)";
    public ProviderCapabilities Capabilities => ProviderCapabilities.Discovery | ProviderCapabilities.Launch;

    public XboxGamePassProvider(ILogger<XboxGamePassProvider> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<GameInfo>();

            // Xbox/Microsoft Store games are installed as UWP apps
            // They're located in WindowsApps folder
            var windowsAppsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "WindowsApps");

            if (!Directory.Exists(windowsAppsPath))
            {
                _logger.LogDebug("WindowsApps directory not found");
                return Array.Empty<GameInfo>();
            }

            // Scan for AppxManifest.xml files (UWP app manifests)
            try
            {
                var appFolders = Directory.GetDirectories(windowsAppsPath);

                foreach (var appFolder in appFolders)
                {
                    try
                    {
                        var manifestPath = Path.Combine(appFolder, "AppxManifest.xml");
                        if (File.Exists(manifestPath))
                        {
                            var gameInfo = await ParseAppxManifestAsync(manifestPath, appFolder, ct);
                            if (gameInfo != null && IsGameApp(gameInfo))
                            {
                                games.Add(gameInfo);
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // WindowsApps has strict permissions, some folders may be inaccessible
                        continue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to process app folder: {Folder}", appFolder);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Access denied to WindowsApps folder. Try running as administrator or using alternative methods.");

                // Fallback: Read from Windows Registry
                var registryGames = GetGamesFromRegistry();
                games.AddRange(registryGames);
            }

            _logger.LogInformation("Found {Count} Xbox Game Pass games", games.Count);
            return games;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Xbox Game Pass games");
            return Array.Empty<GameInfo>();
        }
    }

    private async Task<GameInfo?> ParseAppxManifestAsync(string manifestPath, string installPath, CancellationToken ct)
    {
        try
        {
            var xml = await File.ReadAllTextAsync(manifestPath, ct);
            var doc = XDocument.Parse(xml);

            var ns = doc.Root?.GetDefaultNamespace();
            if (ns == null) return null;

            // Get Identity element
            var identity = doc.Root?.Element(ns + "Identity");
            if (identity == null) return null;

            var packageName = identity.Attribute("Name")?.Value;
            var publisher = identity.Attribute("Publisher")?.Value;

            // Get display name from Properties
            var properties = doc.Root?.Element(ns + "Properties");
            var displayName = properties?.Element(ns + "DisplayName")?.Value;

            // Get application properties
            var applications = doc.Root?.Element(ns + "Applications");
            var application = applications?.Element(ns + "Application");
            var appId = application?.Attribute("Id")?.Value;

            if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(packageName))
            {
                return null;
            }

            // Filter out resource packages (they start with ms-resource:)
            if (displayName.StartsWith("ms-resource:"))
            {
                return null;
            }

            return new GameInfo
            {
                Source = "Xbox",
                SourceId = packageName,
                Title = displayName,
                InstallPath = installPath,
                Platform = "PC"
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse AppxManifest: {Path}", manifestPath);
            return null;
        }
    }

    private bool IsGameApp(GameInfo gameInfo)
    {
        // Filter out non-game apps (system apps, utilities, etc.)
        var excludePatterns = new[]
        {
            "Microsoft.Windows",
            "Microsoft.Services",
            "Microsoft.NET",
            "Microsoft.VCLibs",
            "Microsoft.UI",
            "Microsoft.Advertising",
            "Microsoft.Desktop",
            "RealtekSemiconductorCorp",
            "NVIDIACorp"
        };

        return !excludePatterns.Any(pattern =>
            gameInfo.SourceId.StartsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private List<GameInfo> GetGamesFromRegistry()
    {
        var games = new List<GameInfo>();

        try
        {
            // Alternative method: Check installed packages via PowerShell/WMI
            // This is a simplified version - a full implementation would use Windows.Management APIs
            _logger.LogDebug("Registry fallback for Xbox games not fully implemented");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get Xbox games from registry");
        }

        return games;
    }

    public Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default)
    {
        // Xbox doesn't provide a simple public API for metadata
        return Task.FromResult(new GameMetadata
        {
            Title = gameId
        });
    }

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            // Xbox games can be launched via their protocol
            // Format: ms-xbl-{packageFamilyName}://
            // However, we need the Package Family Name which we might not have

            // Try launching via explorer with the app protocol
            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"shell:appsFolder\\{gameId}!App",
                UseShellExecute = true
            };

            Process.Start(startInfo);

            _logger.LogInformation("Launched Xbox game: {GameId}", gameId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Xbox game: {GameId}", gameId);
            return Task.FromResult(false);
        }
    }
}
