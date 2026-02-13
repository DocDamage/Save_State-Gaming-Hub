using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.RetroArch.Services;

/// <summary>
/// Service for syncing RetroArch saves to Azure Blob Storage.
/// </summary>
public class AzureBlobSyncService
{
    private readonly ILogger<AzureBlobSyncService> _logger;

    public AzureBlobSyncService(ILogger<AzureBlobSyncService> logger)
    {
        _logger = logger;
    }

    public async Task<Result> SyncAsync(
        List<(string Path, string Hash, DateTime Modified)> files,
        string connectionString,
        string containerName,
        CancellationToken ct)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            foreach (var (path, hash, modified) in files)
            {
                var blobName = Path.GetFileName(path);
                var blobClient = containerClient.GetBlobClient(blobName);

                // Check if blob exists and compare hashes
                if (await blobClient.ExistsAsync(ct))
                {
                    var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);
                    var existingHash = properties.Value.Metadata.TryGetValue("hash", out var h) ? h : "";

                    if (existingHash != hash)
                    {
                        await UploadBlobAsync(blobClient, path, hash, ct);
                    }
                }
                else
                {
                    await UploadBlobAsync(blobClient, path, hash, ct);
                }
            }

            _logger.LogInformation("Synced {Count} saves to Azure Blob Storage", files.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Blob sync failed");
            return Result.Failure("Azure Blob sync failed: " + ex.Message);
        }
    }

    private async Task UploadBlobAsync(BlobClient blobClient, string filePath, string hash, CancellationToken ct)
    {
        using var fileStream = File.OpenRead(filePath);
        await blobClient.UploadAsync(fileStream, new BlobUploadOptions { Metadata = new Dictionary<string, string> { ["hash"] = hash } }, ct);
    }
}
