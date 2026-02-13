using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using System.Security.Cryptography;
using System.Text;

namespace SaveState.Infrastructure.RetroArch.Services;

/// <summary>
/// Service for syncing RetroArch saves to AWS S3.
/// </summary>
public class AwsS3SyncService
{
    private readonly ILogger<AwsS3SyncService> _logger;

    public AwsS3SyncService(ILogger<AwsS3SyncService> logger)
    {
        _logger = logger;
    }

    public async Task<Result> SyncAsync(
        List<(string Path, string Hash, DateTime Modified)> files,
        string accessKey,
        string secretKey,
        string bucketName,
        string region,
        CancellationToken ct)
    {
        try
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
            };

            using var client = new AmazonS3Client(accessKey, secretKey, config);
            var transferUtility = new TransferUtility(client);

            foreach (var (path, hash, modified) in files)
            {
                var key = Path.GetFileName(path);

                // Check if object exists
                try
                {
                    var metadata = await client.GetObjectMetadataAsync(bucketName, key, ct);
                    var existingHash = metadata.Metadata["x-amz-meta-hash"];

                    if (existingHash != hash)
                    {
                        await UploadToS3Async(transferUtility, path, bucketName, key, hash, ct);
                    }
                }
                catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await UploadToS3Async(transferUtility, path, bucketName, key, hash, ct);
                }
            }

            _logger.LogInformation("Synced {Count} saves to AWS S3", files.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AWS S3 sync failed");
            return Result.Failure("AWS S3 sync failed: " + ex.Message);
        }
    }

    private async Task UploadToS3Async(
        TransferUtility transferUtility,
        string filePath,
        string bucketName,
        string key,
        string hash,
        CancellationToken ct)
    {
        var uploadRequest = new TransferUtilityUploadRequest
        {
            FilePath = filePath,
            BucketName = bucketName,
            Key = key,
            Metadata = { ["hash"] = hash }
        };

        await transferUtility.UploadAsync(uploadRequest, ct);
    }
}
