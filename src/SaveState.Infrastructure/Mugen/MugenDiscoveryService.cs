namespace SaveState.Infrastructure.Mugen;

using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Discovers and installs MUGEN content from a local catalog.
/// </summary>
public class MugenDiscoveryService : IMugenDiscoveryService
{
    private readonly MugenOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public MugenDiscoveryService(IOptions<MugenOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<IReadOnlyList<MugenDiscoveryItem>>> SearchAsync(string query, CancellationToken ct = default)
    {
        try
        {
            var path = _options.DiscoveryCatalogPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return Result.Failure<IReadOnlyList<MugenDiscoveryItem>>("Discovery catalog not found.");

            var json = await File.ReadAllTextAsync(path, ct);
            var items = JsonSerializer.Deserialize<List<MugenDiscoveryItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            }) ?? new List<MugenDiscoveryItem>();

            if (string.IsNullOrWhiteSpace(query))
                return Result.Success<IReadOnlyList<MugenDiscoveryItem>>(items);

            var filtered = items
                .Where(item =>
                    item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (item.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    item.Source.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Result.Success<IReadOnlyList<MugenDiscoveryItem>>(filtered);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MugenDiscoveryItem>>($"Discovery search failed: {ex.Message}");
        }
    }

    public async Task<Result> InstallAsync(MugenDiscoveryItem item, CancellationToken ct = default)
    {
        if (item == null)
            return Result.Failure("Discovery item is required.");

        if (string.IsNullOrWhiteSpace(item.DownloadUrl))
            return Result.Failure("Discovery item does not have a download URL.");

        var targetRoot = ResolveInstallRoot(item.ContentType);
        if (string.IsNullOrWhiteSpace(targetRoot))
            return Result.Failure("No installation directory configured.");

        Directory.CreateDirectory(targetRoot);

        try
        {
            var payloadPath = await AcquirePackageAsync(item.DownloadUrl, ct);
            if (string.IsNullOrWhiteSpace(payloadPath))
                return Result.Failure("Failed to resolve package for install.");

            if (Directory.Exists(payloadPath))
            {
                CopyDirectory(payloadPath, Path.Combine(targetRoot, Path.GetFileName(payloadPath)));
                return Result.Success();
            }

            if (!File.Exists(payloadPath))
                return Result.Failure("Package not found.");

            if (Path.GetExtension(payloadPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(payloadPath, targetRoot, true);
                return Result.Success();
            }

            var destination = Path.Combine(targetRoot, Path.GetFileName(payloadPath));
            File.Copy(payloadPath, destination, true);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Install failed: {ex.Message}");
        }
    }

    private string? ResolveInstallRoot(MugenDiscoveryType type)
    {
        return type switch
        {
            MugenDiscoveryType.Stage => ResolveStageRoot(),
            MugenDiscoveryType.Character => ResolveCharacterRoot(),
            _ => _options.DataDirectory
        };
    }

    private string ResolveCharacterRoot()
    {
        var preferred = _options.CharacterDirectories?.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path))
            ?? _options.CharactersDirectory;
        return preferred;
    }

    private string ResolveStageRoot()
    {
        var preferred = _options.StageDirectories?.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path))
            ?? _options.StagesDirectory;
        return preferred;
    }

    private async Task<string?> AcquirePackageAsync(string url, CancellationToken ct)
    {
        if (TryResolveLocalPath(url, out var localPath))
            return localPath;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var downloadDir = _options.DiscoveryDownloadsDirectory;
        if (string.IsNullOrWhiteSpace(downloadDir))
            downloadDir = Path.Combine(Path.GetTempPath(), "savestate-mugen-downloads");

        Directory.CreateDirectory(downloadDir);

        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"mugen-package-{Guid.NewGuid():N}.zip";

        var destination = Path.Combine(downloadDir, fileName);

        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(ct);
        await using var output = File.Create(destination);
        await input.CopyToAsync(output, ct);

        return destination;
    }

    private static bool TryResolveLocalPath(string url, out string path)
    {
        path = string.Empty;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                path = uri.LocalPath;
                return File.Exists(path) || Directory.Exists(path);
            }

            return false;
        }

        path = url;
        return File.Exists(path) || Directory.Exists(path);
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
        {
            var dest = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        foreach (var directory in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
        {
            CopyDirectory(directory, Path.Combine(destinationDir, Path.GetFileName(directory)));
        }
    }
}
