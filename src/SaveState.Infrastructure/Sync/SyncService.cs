using Microsoft.Extensions.Logging;
using SaveState.Core.Sync;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Implementation of ISyncService for synchronizing data with cloud storage.
/// </summary>
public class SyncService : ISyncService
{
    private readonly ILogger<SyncService> _logger;
    private ICloudStorageProvider? _provider;
    private SyncStatus _status = SyncStatus.NotConfigured;

    /// <summary>
    /// Gets the current synchronization status.
    /// </summary>
    public SyncStatus Status => _status;

    /// <summary>
    /// Gets the name of the currently active cloud storage provider, if configured.
    /// </summary>
    public string? ActiveProviderName => _provider?.ProviderName;

    /// <summary>
    /// Event raised when synchronization progress changes.
    /// </summary>
    public event EventHandler<SyncProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// Event raised when a synchronization conflict is detected between local and remote files.
    /// </summary>
    public event EventHandler<SyncConflictEventArgs>? ConflictDetected;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public SyncService(ILogger<SyncService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Configures the cloud storage provider for synchronization.
    /// </summary>
    /// <param name="provider">The cloud storage provider to use.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ConfigureProviderAsync(ICloudStorageProvider provider, CancellationToken ct = default)
    {
        _provider = provider;
        _status = SyncStatus.Idle;
        _logger.LogInformation("Sync service configured with provider: {Provider}", provider.ProviderName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs a full synchronization by pulling remote changes then pushing local changes.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing synchronization statistics and any errors.</returns>
    public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
    {
        if (_provider == null)
        {
            return new SyncResult(false, 0, 0, 0, new[] { "No provider configured" });
        }

        _status = SyncStatus.Syncing;
        _logger.LogInformation("Starting full sync with {Provider}", _provider.ProviderName);

        try
        {
            // Pull first, then push
            var pullResult = await PullAsync(ct).ConfigureAwait(false);
            var pushResult = await PushAsync(ct).ConfigureAwait(false);

            _status = SyncStatus.Idle;

            return new SyncResult(
                Success: pullResult.Success && pushResult.Success,
                FilesUploaded: pushResult.FilesUploaded,
                FilesDownloaded: pullResult.FilesDownloaded,
                Conflicts: pullResult.Conflicts + pushResult.Conflicts,
                Errors: pullResult.Errors.Concat(pushResult.Errors).ToList()
            );
        }
        catch (Exception ex)
        {
            _status = SyncStatus.Error;
            _logger.LogError(ex, "Sync failed");
            return new SyncResult(false, 0, 0, 0, new[] { ex.Message });
        }
    }

    /// <summary>
    /// Pushes local changes to the cloud storage provider.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing upload statistics and any errors.</returns>
    public async Task<SyncResult> PushAsync(CancellationToken ct = default)
    {
        if (_provider == null)
        {
            return new SyncResult(false, 0, 0, 0, new[] { "No provider configured" });
        }

        _status = SyncStatus.Syncing;
        var filesUploaded = 0;
        var errors = new List<string>();

        try
        {
            // Get local sync manifest
            var localManifest = await GetLocalManifestAsync(ct).ConfigureAwait(false);

            foreach (var (path, modifiedAt) in localManifest)
            {
                if (ct.IsCancellationRequested) break;

                var remoteInfo = await _provider.GetFileInfoAsync(path, ct).ConfigureAwait(false);

                // Upload if remote doesn't exist or local is newer
                if (remoteInfo == null || modifiedAt > remoteInfo.ModifiedAt)
                {
                    var localPath = GetLocalPath(path);
                    if (await _provider.UploadFileAsync(localPath, path, ct).ConfigureAwait(false))
                    {
                        filesUploaded++;
                        ReportProgress(localManifest.Count, filesUploaded, path);
                    }
                    else
                    {
                        errors.Add($"Failed to upload: {path}");
                    }
                }
            }

            _status = SyncStatus.Idle;
            _logger.LogInformation("Push completed: {Count} files uploaded", filesUploaded);

            return new SyncResult(errors.Count == 0, filesUploaded, 0, 0, errors);
        }
        catch (Exception ex)
        {
            _status = SyncStatus.Error;
            _logger.LogError(ex, "Push failed");
            return new SyncResult(false, filesUploaded, 0, 0, new[] { ex.Message });
        }
    }

    /// <summary>
    /// Pulls remote changes from the cloud storage provider to the local system.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing download statistics and any conflicts or errors.</returns>
    public async Task<SyncResult> PullAsync(CancellationToken ct = default)
    {
        if (_provider == null)
        {
            return new SyncResult(false, 0, 0, 0, new[] { "No provider configured" });
        }

        _status = SyncStatus.Syncing;
        var filesDownloaded = 0;
        var conflicts = 0;
        var errors = new List<string>();

        try
        {
            var remoteFiles = await _provider.ListFilesAsync("/", ct).ConfigureAwait(false);
            var totalFiles = remoteFiles.Count;

            foreach (var remoteFile in remoteFiles.Where(f => !f.IsDirectory))
            {
                if (ct.IsCancellationRequested) break;

                var localPath = GetLocalPath(remoteFile.Path);
                var localStatus = await GetFileSyncStatusAsync(localPath, ct).ConfigureAwait(false);

                if (localStatus == FileSyncStatus.RemoteNewer || localStatus == FileSyncStatus.NotTracked)
                {
                    if (await _provider.DownloadFileAsync(remoteFile.Path, localPath, ct).ConfigureAwait(false))
                    {
                        filesDownloaded++;
                        ReportProgress(totalFiles, filesDownloaded, remoteFile.Path);
                    }
                    else
                    {
                        errors.Add($"Failed to download: {remoteFile.Path}");
                    }
                }
                else if (localStatus == FileSyncStatus.Conflict)
                {
                    conflicts++;
                    ConflictDetected?.Invoke(this, new SyncConflictEventArgs
                    {
                        LocalPath = localPath,
                        RemotePath = remoteFile.Path,
                        RemoteModified = remoteFile.ModifiedAt,
                        LocalModified = File.GetLastWriteTimeUtc(localPath)
                    });
                }
            }

            _status = SyncStatus.Idle;
            _logger.LogInformation("Pull completed: {Downloaded} files downloaded, {Conflicts} conflicts",
                filesDownloaded, conflicts);

            return new SyncResult(errors.Count == 0, 0, filesDownloaded, conflicts, errors);
        }
        catch (Exception ex)
        {
            _status = SyncStatus.Error;
            _logger.LogError(ex, "Pull failed");
            return new SyncResult(false, 0, filesDownloaded, conflicts, new[] { ex.Message });
        }
    }

    /// <summary>
    /// Gets the synchronization status of a local file compared to its remote counterpart.
    /// </summary>
    /// <param name="localPath">The local file path to check.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The synchronization status of the file.</returns>
    public async Task<FileSyncStatus> GetFileSyncStatusAsync(string localPath, CancellationToken ct = default)
    {
        if (_provider == null)
        {
            return FileSyncStatus.NotTracked;
        }

        if (!File.Exists(localPath))
        {
            var remotePath = GetRemotePath(localPath);
            var exists = await _provider.FileExistsAsync(remotePath, ct).ConfigureAwait(false);
            return exists ? FileSyncStatus.RemoteNewer : FileSyncStatus.NotTracked;
        }

        var localModified = File.GetLastWriteTimeUtc(localPath);
        var remoteInfo = await _provider.GetFileInfoAsync(GetRemotePath(localPath), ct).ConfigureAwait(false);

        if (remoteInfo == null)
        {
            return FileSyncStatus.LocalNewer;
        }

        var timeDiff = Math.Abs((localModified - remoteInfo.ModifiedAt).TotalSeconds);

        if (timeDiff < 5) // Within 5 seconds considered synced
        {
            return FileSyncStatus.Synced;
        }

        if (localModified > remoteInfo.ModifiedAt)
        {
            return FileSyncStatus.LocalNewer;
        }

        return FileSyncStatus.RemoteNewer;
    }

    private static Task<Dictionary<string, DateTime>> GetLocalManifestAsync(CancellationToken ct)
    {
        // Placeholder: would scan local sync directory
        return Task.FromResult(new Dictionary<string, DateTime>());
    }

    private static string GetLocalPath(string remotePath)
    {
        var syncDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveState", "Sync");
        return Path.Combine(syncDir, remotePath.TrimStart('/', '\\'));
    }

    private static string GetRemotePath(string localPath)
    {
        var syncDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveState", "Sync");
        return Path.GetRelativePath(syncDir, localPath);
    }

    private void ReportProgress(int total, int processed, string currentFile)
    {
        ProgressChanged?.Invoke(this, new SyncProgressEventArgs
        {
            TotalFiles = total,
            ProcessedFiles = processed,
            CurrentFile = currentFile
        });
    }
}
