using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.GameLibrary.Detection;

/// <summary>
/// Scans Epic Games Store library for installed games.
/// </summary>
public class EpicLibraryScanner
{
    private readonly ILogger<EpicLibraryScanner> _logger;

    private static readonly string EpicManifestsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Epic", "EpicGamesLauncher", "Data", "Manifests");

    public EpicLibraryScanner(ILogger<EpicLibraryScanner> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanAsync(CancellationToken ct = default)
    {
        var games = new List<DetectedGame>();

        try
        {
            if (!Directory.Exists(EpicManifestsPath))
            {
                _logger.LogDebug("Epic manifests directory not found: {Path}", EpicManifestsPath);
                return games;
            }

            var manifests = Directory.GetFiles(EpicManifestsPath, "*.item");

            foreach (var manifestPath in manifests)
            {
                if (ct.IsCancellationRequested) break;

                var game = await ParseManifestAsync(manifestPath, ct).ConfigureAwait(false);
                if (game != null)
                {
                    games.Add(game);
                }
            }

            _logger.LogInformation("Epic Games scan complete: found {Count} games", games.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning Epic Games library");
        }

        return games;
    }

    private async Task<DetectedGame?> ParseManifestAsync(string manifestPath, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var displayName = GetStringProperty(root, "DisplayName");
            var installLocation = GetStringProperty(root, "InstallLocation");
            var launchExecutable = GetStringProperty(root, "LaunchExecutable");
            var catalogItemId = GetStringProperty(root, "CatalogItemId");
            var appName = GetStringProperty(root, "AppName");
            var installSize = GetLongProperty(root, "InstallSize");

            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(installLocation))
            {
                return null;
            }

            if (!Directory.Exists(installLocation))
            {
                return null;
            }

            var executablePath = string.IsNullOrEmpty(launchExecutable)
                ? installLocation
                : Path.Combine(installLocation, launchExecutable);

            var metadata = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(catalogItemId))
                metadata["CatalogItemId"] = catalogItemId;
            if (!string.IsNullOrEmpty(appName))
                metadata["AppName"] = appName;

            return new DetectedGame(
                Title: displayName,
                ExecutablePath: executablePath,
                Source: "Epic Games",
                PlatformHint: "PC",
                ExternalId: $"epic_{appName ?? catalogItemId}",
                SizeBytes: installSize,
                LaunchCommand: $"com.epicgames.launcher://apps/{appName}?action=launch",
                Metadata: metadata
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse Epic manifest {Path}", manifestPath);
            return null;
        }
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop)
            ? prop.GetString()
            : null;
    }

    private static long? GetLongProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetInt64();
            }
        }
        return null;
    }
}
