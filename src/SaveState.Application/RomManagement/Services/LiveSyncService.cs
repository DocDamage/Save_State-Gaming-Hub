using Microsoft.Extensions.Logging;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.Enums;
using SaveState.Core.GameLibrary;
using System.Collections.Concurrent;

namespace SaveState.Application.RomManagement.Services;

public class LiveSyncService : ILiveSyncService
{
    private readonly IRomScannerService _romScanner;
    private readonly IRomFileRepository _romFileRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly IPlatformExtensionRegistry _extensionRegistry;
    private readonly ILogger<LiveSyncService> _logger;

    private readonly ConcurrentDictionary<string, WatcherContext> _watchers = new();
    private readonly ConcurrentDictionary<string, SyncStatus> _syncStatuses = new();

    public LiveSyncService(
        IRomScannerService romScanner,
        IRomFileRepository romFileRepository,
        IPlatformRepository platformRepository,
        IPlatformExtensionRegistry extensionRegistry,
        ILogger<LiveSyncService> logger)
    {
        _romScanner = romScanner ?? throw new ArgumentNullException(nameof(romScanner));
        _romFileRepository = romFileRepository ?? throw new ArgumentNullException(nameof(romFileRepository));
        _platformRepository = platformRepository ?? throw new ArgumentNullException(nameof(platformRepository));
        _extensionRegistry = extensionRegistry ?? throw new ArgumentNullException(nameof(extensionRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<RomFileEventArgs>? RomFileAdded;
    public event EventHandler<RomFileEventArgs>? RomFileRemoved;
    public event EventHandler<RomFileEventArgs>? RomFileChanged;
    public event EventHandler<SyncEventArgs>? SyncCompleted;
    public event EventHandler<SyncErrorEventArgs>? SyncError;

    public async Task StartWatchingAsync(string folderPath, string platformName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));

        if (string.IsNullOrWhiteSpace(platformName))
            throw new ArgumentException("Platform name cannot be null or empty", nameof(platformName));

        var normalizedPath = Path.GetFullPath(folderPath);

        if (_watchers.ContainsKey(normalizedPath))
        {
            _logger.LogWarning("Already watching folder: {FolderPath}", normalizedPath);
            return;
        }

        if (!Directory.Exists(normalizedPath))
        {
            _logger.LogWarning("Folder does not exist: {FolderPath}", normalizedPath);
            return;
        }

        var platform = await _platformRepository.GetByNameAsync(platformName, ct).ConfigureAwait(false);
        if (platform == null)
        {
            _logger.LogWarning("Platform not found: {PlatformName}", platformName);
            return;
        }

        try
        {
            var watcher = new FileSystemWatcher(normalizedPath)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            var context = new WatcherContext(
                watcher,
                platformName,
                new System.Diagnostics.Stopwatch());

            // Wire up events
            watcher.Created += (s, e) => _ = SafeHandleAsync(() => HandleFileCreatedAsync(normalizedPath, e.FullPath, platformName));
            watcher.Deleted += (s, e) => HandleFileDeletedAsync(normalizedPath, e.FullPath, platformName);
            watcher.Changed += (s, e) => HandleFileChangedAsync(normalizedPath, e.FullPath, platformName);
            watcher.Renamed += (s, e) => _ = SafeHandleAsync(() => HandleFileRenamedAsync(normalizedPath, e.OldFullPath, e.FullPath, platformName));
            watcher.Error += (s, e) => HandleWatcherErrorAsync(normalizedPath, platformName, e.GetException());

            _watchers[normalizedPath] = context;
            context.StartTime.Restart();

            _syncStatuses[normalizedPath] = new SyncStatus(
                normalizedPath,
                platformName,
                true,
                DateTime.UtcNow,
                0,
                TimeSpan.Zero);

            _logger.LogInformation("Started watching folder: {FolderPath} for platform: {PlatformName}",
                normalizedPath, platformName);

            // Perform initial sync
            await PerformInitialSyncAsync(normalizedPath, platformName, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start watching folder: {FolderPath}", normalizedPath);
            throw;
        }
    }

    public Task StopWatchingAsync(string folderPath, CancellationToken ct = default)
    {
        var normalizedPath = Path.GetFullPath(folderPath);

        if (!_watchers.TryRemove(normalizedPath, out var context))
        {
            _logger.LogWarning("Not currently watching folder: {FolderPath}", normalizedPath);
            return Task.CompletedTask;
        }

        try
        {
            context.Watcher.Dispose();
            context.StartTime.Stop();

            if (_syncStatuses.TryGetValue(normalizedPath, out var status))
            {
                _syncStatuses[normalizedPath] = status with { IsWatching = false };
            }

            _logger.LogInformation("Stopped watching folder: {FolderPath}", normalizedPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping watcher for folder: {FolderPath}", normalizedPath);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetWatchedFoldersAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(_watchers.Keys.ToList());
    }

    public Task<SyncStatus> GetSyncStatusAsync(string folderPath, CancellationToken ct = default)
    {
        var normalizedPath = Path.GetFullPath(folderPath);

        if (_syncStatuses.TryGetValue(normalizedPath, out var status))
        {
            var uptime = TimeSpan.Zero;
            if (_watchers.TryGetValue(normalizedPath, out var context))
            {
                uptime = context.StartTime.Elapsed;
            }

            return Task.FromResult(status with { Uptime = uptime });
        }

        return Task.FromResult(new SyncStatus(
            normalizedPath,
            "Unknown",
            false,
            DateTime.MinValue,
            0,
            TimeSpan.Zero));
    }

    public async Task ForceSyncAsync(string folderPath, CancellationToken ct = default)
    {
        var normalizedPath = Path.GetFullPath(folderPath);

        if (!_watchers.TryGetValue(normalizedPath, out var context))
        {
            throw new InvalidOperationException($"Not currently watching folder: {normalizedPath}");
        }

        var platform = await _platformRepository.GetByNameAsync(context.PlatformName, ct).ConfigureAwait(false);
        if (platform != null)
        {
            await PerformSyncAsync(normalizedPath, platform.Id, ct).ConfigureAwait(false);
        }
    }

    public async Task ClearAllWatchersAsync(CancellationToken ct = default)
    {
        var folders = _watchers.Keys.ToList();

        foreach (var folder in folders)
        {
            await StopWatchingAsync(folder, ct).ConfigureAwait(false);
        }

        _watchers.Clear();
        _syncStatuses.Clear();

        _logger.LogInformation("Cleared all watchers");
    }

    public async ValueTask DisposeAsync()
    {
        await ClearAllWatchersAsync().ConfigureAwait(false);
    }

    private async Task PerformInitialSyncAsync(string folderPath, string platformName, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Performing initial sync for {FolderPath}", folderPath);
            var platform = await _platformRepository.GetByNameAsync(platformName, ct).ConfigureAwait(false);
            if (platform != null)
            {
                await PerformSyncAsync(folderPath, platform.Id, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform initial sync for {FolderPath}", folderPath);
            OnSyncError(folderPath, platformName, "Initial sync failed", ex);
        }
    }

    private async Task PerformSyncAsync(string folderPath, Guid platformId, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        var filesAdded = 0;
        var filesRemoved = 0;
        var filesChanged = 0;

        // Get platform name for logging
        var platform = await _platformRepository.GetByIdAsync(platformId, ct).ConfigureAwait(false);
        var platformName = platform?.Name.Value ?? "Unknown";

        try
        {
            // Scan for new ROMs
            var scanResults = await _romScanner.ScanFolderAsync(
                folderPath,
                platformId,
                recursive: true,
                progress: null,
                ct).ConfigureAwait(false);

            filesAdded = scanResults.Count;

            // Detect removed and changed files by comparing with database state
            var existingRomFiles = await _romFileRepository.GetByFolderPathAsync(folderPath, platformId, ct).ConfigureAwait(false);

            // Create lookup for efficient comparison
            var scannedFilesLookup = scanResults.ToDictionary(r => r.FilePath.Value, r => r);
            var existingFilesLookup = existingRomFiles.ToDictionary(r => r.FilePath.Value, r => r);

            // Detect removed files (in database but not in scan)
            var removedFiles = existingFilesLookup.Keys.Except(scannedFilesLookup.Keys).ToList();
            foreach (var removedFilePath in removedFiles)
            {
                var existingRomFile = existingFilesLookup[removedFilePath];
                // Mark as deleted in database (soft delete)
                existingRomFile.IsDeleted = true;
                existingRomFile.DeletedAt = DateTime.UtcNow;
                await _romFileRepository.UpdateAsync(existingRomFile, ct).ConfigureAwait(false);
                filesRemoved++;
            }

            // Detect changed files (file exists but size changed)
            var commonFiles = scannedFilesLookup.Keys.Intersect(existingFilesLookup.Keys).ToList();
            foreach (var filePath in commonFiles)
            {
                var scannedRomFile = scannedFilesLookup[filePath];
                var existingRomFile = existingFilesLookup[filePath];

                // Check if file size changed (indicating the file was modified)
                if (scannedRomFile.FileSize != existingRomFile.FileSize)
                {
                    // File changed - delete old record and let the scan process create a new one
                    await _romFileRepository.DeleteAsync(existingRomFile.Id, ct).ConfigureAwait(false);
                    filesChanged++;
                }
            }

            var duration = DateTime.UtcNow - startTime;

            // Update sync status
            _syncStatuses[folderPath] = new SyncStatus(
                folderPath,
                platformName,
                true,
                DateTime.UtcNow,
                scanResults.Count,
                TimeSpan.Zero); // Will be updated by GetSyncStatusAsync

            _logger.LogInformation("Sync completed for {FolderPath}: {FilesAdded} files added in {Duration}",
                folderPath, filesAdded, duration);

            OnSyncCompleted(folderPath, platformName, filesAdded, filesRemoved, filesChanged, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for {FolderPath}", folderPath);
            OnSyncError(folderPath, platformName, "Sync operation failed", ex);
        }
    }

    private async Task SafeHandleAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling FileSystemWatcher event");
        }
    }

    private async Task HandleFileCreatedAsync(string folderPath, string filePath, string platformName)
    {
        try
        {
            // Check if it's a ROM file
            var platform = await _platformRepository.GetByNameAsync(platformName, CancellationToken.None).ConfigureAwait(false);
            if (platform == null) return;

            // Quick check if file extension matches platform
            if (!_extensionRegistry.IsValidExtension(platformName, filePath)) return;

            _logger.LogDebug("ROM file created: {FilePath}", filePath);

            // Get platform for the file
            var filePlatform = await _platformRepository.GetByNameAsync(platformName, CancellationToken.None).ConfigureAwait(false);
            if (filePlatform == null) return;

            // Scan the specific file
            var scanResults = await _romScanner.ScanFolderAsync(
                Path.GetDirectoryName(filePath)!,
                filePlatform.Id,
                recursive: false,
                progress: null,
                CancellationToken.None).ConfigureAwait(false);

            var romFile = scanResults.FirstOrDefault(r => r.FilePath.Value.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (romFile != null)
            {
                OnRomFileAdded(folderPath, filePath, platformName, romFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file created event for {FilePath}", filePath);
        }
    }

    private void HandleFileDeletedAsync(string folderPath, string filePath, string platformName)
    {
        try
        {
            _logger.LogDebug("ROM file deleted: {FilePath}", filePath);
            OnRomFileRemoved(folderPath, filePath, platformName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file deleted event for {FilePath}", filePath);
        }
    }

    private void HandleFileChangedAsync(string folderPath, string filePath, string platformName)
    {
        try
        {
            _logger.LogDebug("ROM file changed: {FilePath}", filePath);
            OnRomFileChanged(folderPath, filePath, platformName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file changed event for {FilePath}", filePath);
        }
    }

    private async Task HandleFileRenamedAsync(string folderPath, string oldFilePath, string newFilePath, string platformName)
    {
        try
        {
            _logger.LogDebug("ROM file renamed: {OldPath} -> {NewPath}", oldFilePath, newFilePath);

            // Handle as a removal of old file and addition of new file
            OnRomFileRemoved(folderPath, oldFilePath, platformName, null);

            // Check if new file is a ROM
            var renamePlatform = await _platformRepository.GetByNameAsync(platformName, CancellationToken.None).ConfigureAwait(false);
            if (renamePlatform == null) return;

            if (!_extensionRegistry.IsValidExtension(platformName, newFilePath)) return;

            // Scan the new file
            var scanResults = await _romScanner.ScanFolderAsync(
                Path.GetDirectoryName(newFilePath)!,
                renamePlatform.Id,
                recursive: false,
                progress: null,
                CancellationToken.None).ConfigureAwait(false);

            var romFile = scanResults.FirstOrDefault(r => r.FilePath.Value.Equals(newFilePath, StringComparison.OrdinalIgnoreCase));
            if (romFile != null)
            {
                OnRomFileAdded(folderPath, newFilePath, platformName, romFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file renamed event for {OldPath} -> {NewPath}", oldFilePath, newFilePath);
        }
    }

    private void HandleWatcherErrorAsync(string folderPath, string platformName, Exception exception)
    {
        _logger.LogError(exception, "FileSystemWatcher error for {FolderPath}", folderPath);
        OnSyncError(folderPath, platformName, "FileSystemWatcher error", exception);
    }


    private void OnRomFileAdded(string folderPath, string filePath, string platformName, RomFile? romFile)
    {
        RomFileAdded?.Invoke(this, new RomFileEventArgs(
            folderPath, filePath, platformName, romFile, DateTime.UtcNow));
    }

    private void OnRomFileRemoved(string folderPath, string filePath, string platformName, RomFile? romFile)
    {
        RomFileRemoved?.Invoke(this, new RomFileEventArgs(
            folderPath, filePath, platformName, romFile, DateTime.UtcNow));
    }

    private void OnRomFileChanged(string folderPath, string filePath, string platformName, RomFile? romFile)
    {
        RomFileChanged?.Invoke(this, new RomFileEventArgs(
            folderPath, filePath, platformName, romFile, DateTime.UtcNow));
    }

    private void OnSyncCompleted(string folderPath, string platformName, int filesAdded, int filesRemoved, int filesChanged, TimeSpan duration)
    {
        SyncCompleted?.Invoke(this, new SyncEventArgs(
            folderPath, platformName, filesAdded, filesRemoved, filesChanged, duration, DateTime.UtcNow));
    }

    private void OnSyncError(string folderPath, string platformName, string errorMessage, Exception? exception)
    {
        SyncError?.Invoke(this, new SyncErrorEventArgs(
            folderPath, platformName, errorMessage, exception, DateTime.UtcNow));
    }

    private record WatcherContext(
        FileSystemWatcher Watcher,
        string PlatformName,
        System.Diagnostics.Stopwatch StartTime);
}
