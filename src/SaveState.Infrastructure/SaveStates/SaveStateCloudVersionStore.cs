using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.SaveStates.Services.DTOs;
using SaveState.Core.Sync;

namespace SaveState.Infrastructure.SaveStates;

/// <summary>
/// Handles cloud/local persistence of save-state version metadata.
/// </summary>
internal sealed class SaveStateCloudVersionStore
{
    private const string CloudRootPath = "savestates";

    private readonly ILogger _logger;
    private readonly string _versionHistoryRootPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public SaveStateCloudVersionStore(
        ILogger logger,
        string versionHistoryRootPath,
        JsonSerializerOptions jsonOptions)
    {
        _logger = logger;
        _versionHistoryRootPath = versionHistoryRootPath;
        _jsonOptions = jsonOptions;

        Directory.CreateDirectory(_versionHistoryRootPath);
    }

    public async Task<SaveStateCloudVersion?> GetCloudLatestVersionAsync(
        ICloudStorageProvider provider,
        Guid gameId,
        CancellationToken ct)
    {
        var tempMetadataPath = Path.Combine(Path.GetTempPath(), $"savestate-cloud-latest-{Guid.NewGuid():N}.json");
        try
        {
            var downloadResult = await provider.DownloadFileAsync(
                BuildCloudLatestVersionPath(gameId),
                tempMetadataPath,
                ct).ConfigureAwait(false);

            if (downloadResult.IsFailure || !downloadResult.Value || !File.Exists(tempMetadataPath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(tempMetadataPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SaveStateCloudVersion>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cloud latest version metadata for game {GameId}", gameId);
            return null;
        }
        finally
        {
            TryDelete(tempMetadataPath);
        }
    }

    public async Task UploadVersionMetadataAsync(
        ICloudStorageProvider provider,
        SaveStateCloudVersion version,
        CancellationToken ct)
    {
        var tempMetadataPath = Path.Combine(Path.GetTempPath(), $"savestate-cloud-version-{Guid.NewGuid():N}.json");
        try
        {
            var json = JsonSerializer.Serialize(version, _jsonOptions);
            await File.WriteAllTextAsync(tempMetadataPath, json, ct).ConfigureAwait(false);

            var uploadedVersionMetadata = await provider.UploadFileAsync(
                tempMetadataPath,
                BuildCloudVersionPath(version.GameId, version.Id),
                ct).ConfigureAwait(false);

            if (uploadedVersionMetadata.IsFailure || !uploadedVersionMetadata.Value)
            {
                _logger.LogWarning(
                    "Failed to upload version metadata for game {GameId}, version {VersionId}. Error: {Error}",
                    version.GameId,
                    version.Id,
                    uploadedVersionMetadata.Error);
            }

            var uploadedLatestMetadata = await provider.UploadFileAsync(
                tempMetadataPath,
                BuildCloudLatestVersionPath(version.GameId),
                ct).ConfigureAwait(false);

            if (uploadedLatestMetadata.IsFailure || !uploadedLatestMetadata.Value)
            {
                _logger.LogWarning(
                    "Failed to upload latest metadata marker for game {GameId}, version {VersionId}. Error: {Error}",
                    version.GameId,
                    version.Id,
                    uploadedLatestMetadata.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish cloud version metadata for game {GameId}", version.GameId);
        }
        finally
        {
            TryDelete(tempMetadataPath);
        }
    }

    public async Task AppendVersionAsync(Guid gameId, SaveStateCloudVersion version, CancellationToken ct)
    {
        var existing = await LoadVersionHistoryAsync(gameId, ct).ConfigureAwait(false);
        var updated = existing
            .Where(v => v.Id != version.Id)
            .Append(version)
            .OrderByDescending(v => v.CreatedAtUtc)
            .ToList();

        await SaveVersionHistoryAsync(gameId, updated, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SaveStateCloudVersion>> LoadVersionHistoryAsync(Guid gameId, CancellationToken ct)
    {
        var historyPath = GetVersionHistoryFilePath(gameId);
        if (!File.Exists(historyPath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(historyPath, ct).ConfigureAwait(false);
            var items = JsonSerializer.Deserialize<List<SaveStateCloudVersion>>(json, _jsonOptions) ?? [];
            return items.OrderByDescending(v => v.CreatedAtUtc).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cloud version history for game {GameId}", gameId);
            return [];
        }
    }

    private async Task SaveVersionHistoryAsync(
        Guid gameId,
        IReadOnlyList<SaveStateCloudVersion> versions,
        CancellationToken ct)
    {
        var historyPath = GetVersionHistoryFilePath(gameId);
        var json = JsonSerializer.Serialize(versions, _jsonOptions);
        await File.WriteAllTextAsync(historyPath, json, ct).ConfigureAwait(false);
    }

    private string GetVersionHistoryFilePath(Guid gameId)
    {
        Directory.CreateDirectory(_versionHistoryRootPath);
        return Path.Combine(_versionHistoryRootPath, $"{gameId:N}.json");
    }

    private static string BuildCloudLatestVersionPath(Guid gameId) =>
        $"{CloudRootPath}/{gameId}/latest.json";

    private static string BuildCloudVersionPath(Guid gameId, Guid versionId) =>
        $"{CloudRootPath}/{gameId}/versions/{versionId}.json";

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}
