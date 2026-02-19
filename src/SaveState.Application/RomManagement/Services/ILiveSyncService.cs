using SaveState.Core.RomManagement.Entities;
using SaveState.Core.Common;

namespace SaveState.Application.RomManagement.Services;

public interface ILiveSyncService : IAsyncDisposable
{
    Task StartWatchingAsync(string folderPath, string platformName, CancellationToken ct = default);
    Task StopWatchingAsync(string folderPath, CancellationToken ct = default);
    Task<Result<IReadOnlyList<string>>> GetWatchedFoldersAsync(CancellationToken ct = default);
    Task<Result<SyncStatus>> GetSyncStatusAsync(string folderPath, CancellationToken ct = default);
    Task ForceSyncAsync(string folderPath, CancellationToken ct = default);
    Task ClearAllWatchersAsync(CancellationToken ct = default);

    event EventHandler<RomFileEventArgs>? RomFileAdded;
    event EventHandler<RomFileEventArgs>? RomFileRemoved;
    event EventHandler<RomFileEventArgs>? RomFileChanged;
    event EventHandler<SyncEventArgs>? SyncCompleted;
    event EventHandler<SyncErrorEventArgs>? SyncError;
}

public record RomFileEventArgs(
    string FolderPath,
    string FilePath,
    string PlatformName,
    RomFile? RomFile,
    DateTime Timestamp);

public record SyncEventArgs(
    string FolderPath,
    string PlatformName,
    int FilesAdded,
    int FilesRemoved,
    int FilesChanged,
    TimeSpan Duration,
    DateTime Timestamp);

public record SyncErrorEventArgs(
    string FolderPath,
    string PlatformName,
    string ErrorMessage,
    Exception? Exception,
    DateTime Timestamp);

public record SyncStatus(
    string FolderPath,
    string PlatformName,
    bool IsWatching,
    DateTime LastSync,
    int TotalFiles,
    TimeSpan Uptime);
