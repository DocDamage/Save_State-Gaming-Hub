using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using SaveState.Infrastructure.RetroArch.Models;

namespace SaveState.Infrastructure.RetroArch.RetroArchCloudSync;

/// <summary>
/// Azure Blob Storage synchronization engine for RetroArch save files.
/// </summary>
public sealed class AzureBlobSyncEngine : ISyncEngine
{
    private readonly ILogger<AzureBlobSyncEngine> _logger;
    private readonly RetroArchOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobSyncEngine"/> class.
    /// </summary>
    public AzureBlobSyncEngine(
        ILogger<AzureBlobSyncEngine> logger,
        IOptions<RetroArchOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<Result> SyncAsync(List<SyncFileInfo> files, string retroArchPath, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.CloudSyncConnectionString))
            {
                return Result.Failure("Azure Blob Storage connection string not configured");
            }

            var blobServiceClient = new BlobServiceClient(_options.CloudSyncConnectionString);
            var containerName = _options.CloudSyncContainerName ?? "retroach-saves";
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            var uploadedCount = 0;
            var skippedCount = 0;

            foreach (var file in files)
            {
                try
                {
                    var blobName = GetRelativePath(file.Path, retroArchPath);
                    var blobClient = containerClient.GetBlobClient(blobName);

                    // Check if blob exists and compare hashes
                    var exists = await blobClient.ExistsAsync(ct);
                    if (exists)
                    {
                        // Get blob properties to check hash
                        var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);
                        var cloudHash = properties.Value.Metadata.TryGetValue("filehash", out var hashValue) ? hashValue : null;

                        if (cloudHash == file.Hash)
                        {
                            // File is up to date
                            skippedCount++;
                            continue;
                        }
                    }

                    // Upload the file
                    using var fileStream = System.IO.File.OpenRead(file.Path);
                    var uploadOptions = new BlobUploadOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            { "filehash", file.Hash },
                            { "modified", file.Modified.ToString("O") },
                            { "source", Environment.MachineName }
                        },
                        Conditions = exists ? new BlobRequestConditions { IfNoneMatch = new Azure.ETag("*") } : null
                    };

                    await blobClient.UploadAsync(fileStream, uploadOptions, ct);
                    uploadedCount++;

                    _logger.LogDebug("Uploaded save file to Azure Blob: {BlobName}", blobName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync file to Azure Blob: {FilePath}", file.Path);
                }
            }

            _logger.LogInformation("Azure Blob sync completed: {Uploaded} uploaded, {Skipped} skipped", uploadedCount, skippedCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing to Azure Blob Storage");
            return Result.Failure($"Azure Blob sync failed: {ex.Message}");
        }
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        var baseDir = Path.GetDirectoryName(basePath) ?? basePath;
        var relativePath = Path.GetRelativePath(baseDir, fullPath);
        // Replace backslashes with forward slashes for consistent blob names
        return relativePath.Replace('\\', '/');
    }
}
