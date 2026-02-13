using SaveState.Core.Common;
using SaveState.Infrastructure.RetroArch.Models;

namespace SaveState.Infrastructure.RetroArch.RetroArchCloudSync;

/// <summary>
/// Interface for cloud synchronization engines.
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    /// Synchronizes the specified files to cloud storage.
    /// </summary>
    /// <param name="files">The list of files to synchronize.</param>
    /// <param name="retroArchPath">The base RetroArch path for calculating relative paths.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure of the sync operation.</returns>
    Task<Result> SyncAsync(List<SyncFileInfo> files, string retroArchPath, CancellationToken ct);
}
