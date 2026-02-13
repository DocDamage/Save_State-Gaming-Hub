using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using SaveState.Infrastructure.RetroArch.Models;

namespace SaveState.Infrastructure.RetroArch.RetroArchCloudSync;

/// <summary>
/// AWS S3 synchronization engine for RetroArch save files.
/// </summary>
public sealed class AwsS3SyncEngine : ISyncEngine
{
    private readonly ILogger<AwsS3SyncEngine> _logger;
    private readonly RetroArchOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsS3SyncEngine"/> class.
    /// </summary>
    public AwsS3SyncEngine(
        ILogger<AwsS3SyncEngine> logger,
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
                return Result.Failure("AWS S3 credentials not configured");
            }

            // Parse connection string format: "AccessKey=xxx;SecretKey=yyy;Region=zzz;Bucket=bbb"
            var credentials = ParseAwsCredentials(_options.CloudSyncConnectionString);
            if (credentials == null)
            {
                return Result.Failure("Invalid AWS S3 connection string format");
            }

            using var s3Client = new AmazonS3Client(
                credentials.Value.AccessKey,
                credentials.Value.SecretKey,
                Amazon.RegionEndpoint.GetBySystemName(credentials.Value.Region));

            var uploadedCount = 0;
            var skippedCount = 0;

            foreach (var file in files)
            {
                try
                {
                    var key = GetRelativePath(file.Path, retroArchPath);

                    // Check if object exists and compare hashes
                    var headRequest = new GetObjectMetadataRequest
                    {
                        BucketName = credentials.Value.Bucket,
                        Key = key
                    };

                    string? cloudHash = null;
                    try
                    {
                        var response = await s3Client.GetObjectMetadataAsync(headRequest, ct);
                        try
                        {
                            cloudHash = response.Metadata["filehash"];
                        }
                        catch (KeyNotFoundException)
                        {
                            // Metadata key not present - this is expected for new objects
                            cloudHash = null;
                        }
                    }
                    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Object doesn't exist, will upload
                    }

                    if (cloudHash == file.Hash)
                    {
                        // File is up to date
                        skippedCount++;
                        continue;
                    }

                    // Upload the file using TransferUtility for better performance
                    using var transferUtility = new TransferUtility(s3Client);
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = credentials.Value.Bucket,
                        Key = key,
                        FilePath = file.Path,
                        Metadata =
                        {
                            ["filehash"] = file.Hash,
                            ["modified"] = file.Modified.ToString("O"),
                            ["source"] = Environment.MachineName
                        }
                    };

                    await transferUtility.UploadAsync(uploadRequest, ct);
                    uploadedCount++;

                    _logger.LogDebug("Uploaded save file to AWS S3: {Key}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync file to AWS S3: {FilePath}", file.Path);
                }
            }

            _logger.LogInformation("AWS S3 sync completed: {Uploaded} uploaded, {Skipped} skipped", uploadedCount, skippedCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing to AWS S3");
            return Result.Failure($"AWS S3 sync failed: {ex.Message}");
        }
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        var baseDir = Path.GetDirectoryName(basePath) ?? basePath;
        var relativePath = Path.GetRelativePath(baseDir, fullPath);
        // Replace backslashes with forward slashes for consistent blob names
        return relativePath.Replace('\\', '/');
    }

    private static (string AccessKey, string SecretKey, string Region, string Bucket)? ParseAwsCredentials(string connectionString)
    {
        var parts = connectionString.Split(';');
        var dict = new Dictionary<string, string>();

        foreach (var part in parts)
        {
            var kvp = part.Split('=', 2);
            if (kvp.Length == 2)
            {
                dict[kvp[0].Trim()] = kvp[1].Trim();
            }
        }

        if (dict.TryGetValue("AccessKey", out var accessKey) &&
            dict.TryGetValue("SecretKey", out var secretKey) &&
            dict.TryGetValue("Region", out var region) &&
            dict.TryGetValue("Bucket", out var bucket))
        {
            return (accessKey, secretKey, region, bucket);
        }

        return null;
    }
}
