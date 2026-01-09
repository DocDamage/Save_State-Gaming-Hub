using Microsoft.Extensions.Logging;
using SaveState.Core.Sync;
using SaveState.Core.Common.Services;
using System.Linq;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Implementation of ISyncService for synchronizing data with cloud storage.
/// </summary>
public class SyncService : ISyncService
{
    private readonly ILogger<SyncService> _logger;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IEnumerable<ICloudStorageProvider> _providers;
    private ICloudStorageProvider? _provider;
    private SyncStatus _status = SyncStatus.NotConfigured;
    private readonly List<SyncConflictEventArgs> _conflicts = new();
    private DateTime _syncStartTime;
    private long _totalBytes;
    private long _processedBytes;

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
    /// <param name="providers">Available cloud storage providers.</param>
    /// <param name="preferencesService">User preferences service.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public SyncService(
        IEnumerable<ICloudStorageProvider> providers,
        IUserPreferencesService preferencesService,
        ILogger<SyncService> logger)
    {
        _logger = logger;
        _preferencesService = preferencesService;
        _providers = providers;
    }

    private async Task<bool> EnsureProviderAsync()
    {
        var preferred = await _preferencesService.GetPreferredCloudProviderAsync();

        // If provider already set and matches preference, we're good
        if (_provider != null && _provider.ProviderName == preferred)
        {
            return true;
        }

        _provider = _providers.FirstOrDefault(p => p.ProviderName == preferred);

        if (_provider == null)
        {
             _provider = _providers.FirstOrDefault(p => p.ProviderName != "Local");
        }

        if (_provider != null)
        {
            _status = SyncStatus.Idle;
            return true;
        }

        _status = SyncStatus.NotConfigured;
        return false;
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
        _conflicts.Clear();
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
        if (!await EnsureProviderAsync().ConfigureAwait(false))
        {
            return new SyncResult(false, 0, 0, 0, new[] { "No provider configured" });
        }

        if (!_provider.IsAuthenticated)
        {
            _logger.LogInformation("Provider {Provider} is not authenticated. Attempting authentication...", _provider.ProviderName);
            if (!await _provider.AuthenticateAsync(ct).ConfigureAwait(false))
            {
                return new SyncResult(false, 0, 0, 0, new[] { "Authentication failed" });
            }
        }

        _status = SyncStatus.Syncing;
        var filesUploaded = 0;
        var errors = new List<string>();

        try
        {
            // Get local sync manifest
            var localManifest = await GetLocalManifestAsync(ct).ConfigureAwait(false);

            // Calculate total bytes for progress
            _totalBytes = localManifest.Keys.Select(k => new FileInfo(GetLocalPath(k)).Length).Sum();
            _processedBytes = 0;
            _syncStartTime = DateTime.UtcNow;

            foreach (var (remotePath, modifiedAt) in localManifest)
            {
                if (ct.IsCancellationRequested) break;

                var localPath = GetLocalPath(remotePath);
                var status = await GetFileSyncStatusAsync(localPath, ct).ConfigureAwait(false);

                if (status == FileSyncStatus.LocalNewer)
                {
                    var fileSize = new FileInfo(localPath).Length;
                    if (await _provider.UploadFileAsync(localPath, remotePath, ct).ConfigureAwait(false))
                    {
                        filesUploaded++;
                        _processedBytes += fileSize;
                        ReportProgress(localManifest.Count, filesUploaded, remotePath);
                    }
                    else
                    {
                        errors.Add($"Failed to upload: {remotePath}");
                    }
                }
                else if (status == FileSyncStatus.Conflict)
                {
                    var remoteInfo = await _provider.GetFileInfoAsync(remotePath, ct).ConfigureAwait(false);
                    var conflict = new SyncConflictEventArgs
                    {
                        LocalPath = localPath,
                        RemotePath = remotePath,
                        LocalModified = modifiedAt,
                        RemoteModified = remoteInfo?.ModifiedAt ?? DateTime.MinValue,
                        RemoteSize = remoteInfo?.SizeBytes ?? 0
                    };
                    _conflicts.Add(conflict);
                    ConflictDetected?.Invoke(this, conflict);
                }
            }

            _status = SyncStatus.Idle;
            _logger.LogInformation("Push completed: {Count} files uploaded", filesUploaded);

            // Update sync history for uploaded files
            if (filesUploaded > 0)
            {
                await UpdateSyncHistoryAsync(localManifest.Keys, ct).ConfigureAwait(false);
            }

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
        if (!await EnsureProviderAsync().ConfigureAwait(false))
        {
            return new SyncResult(false, 0, 0, 0, new[] { "No provider configured" });
        }

        if (!_provider.IsAuthenticated)
        {
            _logger.LogInformation("Provider {Provider} is not authenticated. Attempting authentication...", _provider.ProviderName);
            if (!await _provider.AuthenticateAsync(ct).ConfigureAwait(false))
            {
                return new SyncResult(false, 0, 0, 0, new[] { "Authentication failed" });
            }
        }

        _status = SyncStatus.Syncing;
        var filesDownloaded = 0;
        var conflicts = 0;
        var errors = new List<string>();

        try
        {
            var remoteFiles = await _provider.ListFilesAsync("/", ct).ConfigureAwait(false);
            var totalFiles = remoteFiles.Count;
            _totalBytes = remoteFiles.Where(f => !f.IsDirectory).Select(f => f.SizeBytes).Sum();
            _processedBytes = 0;
            _syncStartTime = DateTime.UtcNow;

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
                        _processedBytes += remoteFile.SizeBytes;
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
                    var conflict = new SyncConflictEventArgs
                    {
                        LocalPath = localPath,
                        RemotePath = remoteFile.Path,
                        RemoteModified = remoteFile.ModifiedAt,
                        LocalModified = File.Exists(localPath) ? File.GetLastWriteTimeUtc(localPath) : DateTime.MinValue,
                        RemoteSize = remoteFile.SizeBytes
                    };
                    _conflicts.Add(conflict);
                    ConflictDetected?.Invoke(this, conflict);
                }
            }

            _status = SyncStatus.Idle;
            _logger.LogInformation("Pull completed: {Downloaded} files downloaded, {Conflicts} conflicts",
                filesDownloaded, conflicts);

            // Update last sync time for successfully downloaded files
            if (filesDownloaded > 0)
            {
                await UpdateSyncHistoryAsync(remoteFiles.Where(f => !f.IsDirectory).Select(f => f.Path), ct).ConfigureAwait(false);
            }

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

        var remotePath = GetRemotePath(localPath);
        if (!File.Exists(localPath))
        {
            var exists = await _provider.FileExistsAsync(remotePath, ct).ConfigureAwait(false);
            return exists ? FileSyncStatus.RemoteNewer : FileSyncStatus.NotTracked;
        }

        var localModified = File.GetLastWriteTimeUtc(localPath);
        var remoteInfo = await _provider.GetFileInfoAsync(remotePath, ct).ConfigureAwait(false);

        if (remoteInfo == null)
        {
            return FileSyncStatus.LocalNewer;
        }

        // Get last known synced state
        var lastSyncedAt = await GetLastSyncTimeAsync(remotePath, ct).ConfigureAwait(false);

        var localChanged = localModified > lastSyncedAt.AddSeconds(1);
        var remoteChanged = remoteInfo.ModifiedAt > lastSyncedAt.AddSeconds(1);

        if (localChanged && remoteChanged)
        {
            // Both changed since last sync - check if they are actually different
            // For now, assume different if timestamps differ significantly
            var timeDiff = Math.Abs((localModified - remoteInfo.ModifiedAt).TotalSeconds);
            if (timeDiff < 2) return FileSyncStatus.Synced;

            return FileSyncStatus.Conflict;
        }

        if (localChanged) return FileSyncStatus.LocalNewer;
        if (remoteChanged) return FileSyncStatus.RemoteNewer;

        return FileSyncStatus.Synced;
    }

    /// <summary>
    /// Gets the list of current sync conflicts.
    /// </summary>
    public Task<IReadOnlyList<SyncConflictEventArgs>> GetConflictsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<SyncConflictEventArgs>>(_conflicts.AsReadOnly());
    }

    /// <summary>
    /// Resolves a sync conflict using the specified strategy.
    /// </summary>
    public async Task<bool> ResolveConflictAsync(string localPath, string strategy, CancellationToken ct = default)
    {
        if (_provider == null) return false;

        var remotePath = GetRemotePath(localPath);
        _logger.LogInformation("Resolving conflict for {Path} using strategy {Strategy}", localPath, strategy);

        try
        {
            switch (strategy)
            {
                case "Keep Local":
                    // Upload local version to override remote
                    return await _provider.UploadFileAsync(localPath, remotePath, ct).ConfigureAwait(false);

                case "Keep Cloud":
                    // Download cloud version to override local
                    return await _provider.DownloadFileAsync(remotePath, localPath, ct).ConfigureAwait(false);

                case "Keep Both":
                    // Rename local and download cloud
                    var extension = Path.GetExtension(localPath);
                    var newLocalPath = localPath.Replace(extension, $".local{extension}");
                    if (File.Exists(localPath))
                    {
                        File.Move(localPath, newLocalPath);
                    }
                    return await _provider.DownloadFileAsync(remotePath, localPath, ct).ConfigureAwait(false);

                case "Skip":
                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve conflict for {Path}", localPath);
            return false;
        }
    }

    private async Task<DateTime> GetLastSyncTimeAsync(string path, CancellationToken ct)
    {
        var history = await GetSyncHistoryAsync(ct).ConfigureAwait(false);
        return history.TryGetValue(path, out var lastSync) ? lastSync : DateTime.MinValue;
    }

    private Task<Dictionary<string, DateTime>> GetSyncHistoryAsync(CancellationToken ct)
    {
        var historyPath = Path.Combine(GetSyncBaseDir(), ".sync_history");
        if (!File.Exists(historyPath)) return Task.FromResult(new Dictionary<string, DateTime>());

        try
        {
            var content = File.ReadAllText(historyPath);
            var history = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, DateTime>>(content);
            return Task.FromResult(history ?? new Dictionary<string, DateTime>());
        }
        catch
        {
            return Task.FromResult(new Dictionary<string, DateTime>());
        }
    }

    private async Task UpdateSyncHistoryAsync(IEnumerable<string> paths, CancellationToken ct)
    {
        var history = await GetSyncHistoryAsync(ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        foreach (var path in paths)
        {
            history[path] = now;
        }

        var historyPath = Path.Combine(GetSyncBaseDir(), ".sync_history");
        var content = System.Text.Json.JsonSerializer.Serialize(history);
        await File.WriteAllTextAsync(historyPath, content, ct).ConfigureAwait(false);
    }

    private Task<Dictionary<string, DateTime>> GetLocalManifestAsync(CancellationToken ct)
    {
        var syncDir = GetSyncBaseDir();
        if (!Directory.Exists(syncDir)) return Task.FromResult(new Dictionary<string, DateTime>());

        var manifest = Directory.GetFiles(syncDir, "*", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != ".sync_history")
            .ToDictionary(
                f => GetRemotePath(f),
                f => File.GetLastWriteTimeUtc(f)
            );

        return Task.FromResult(manifest);
    }

    private static string GetSyncBaseDir()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveState", "Sync");
    }

    private static string GetLocalPath(string remotePath)
    {
        return Path.Combine(GetSyncBaseDir(), remotePath.TrimStart('/', '\\'));
    }

    private static string GetRemotePath(string localPath)
    {
        return Path.GetRelativePath(GetSyncBaseDir(), localPath).Replace('\\', '/');
    }

    private void ReportProgress(int total, int processed, string currentFile)
    {
        var elapsed = DateTime.UtcNow - _syncStartTime;
        var throughput = elapsed.TotalSeconds > 0 ? _processedBytes / elapsed.TotalSeconds : 0;

        TimeSpan? remainingTime = null;
        if (throughput > 0)
        {
            var remainingBytes = _totalBytes - _processedBytes;
            remainingTime = TimeSpan.FromSeconds(remainingBytes / throughput);
        }

        ProgressChanged?.Invoke(this, new SyncProgressEventArgs
        {
            TotalFiles = total,
            ProcessedFiles = processed,
            CurrentFile = currentFile,
            TotalBytes = _totalBytes,
            ProcessedBytes = _processedBytes,
            ThroughputBytesPerSecond = throughput,
            EstimatedRemainingTime = remainingTime
        });
    }
}
