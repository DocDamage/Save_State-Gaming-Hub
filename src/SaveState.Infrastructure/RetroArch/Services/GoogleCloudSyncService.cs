using Microsoft.Extensions.Logging;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.RetroArch.Services;

/// <summary>
/// Service for syncing RetroArch saves to Google Cloud Storage.
/// </summary>
public class GoogleCloudSyncService
{
    private readonly ILogger<GoogleCloudSyncService> _logger;

    public GoogleCloudSyncService(ILogger<GoogleCloudSyncService> logger)
    {
        _logger = logger;
    }

    public Task<Result> SyncAsync(
        List<(string Path, string Hash, DateTime Modified)> files,
        string credentialsPath,
        string bucketName,
        CancellationToken ct)
    {
        // Google Cloud Storage sync implementation pending
        // Requires: Google.Cloud.Storage.V1 NuGet package
        // Issue: #123 - Add Google Cloud Storage provider support
        _logger.LogWarning("Google Cloud sync not yet implemented");
        return Task.FromResult(Result.Failure("Google Cloud sync not implemented"));
    }
}
