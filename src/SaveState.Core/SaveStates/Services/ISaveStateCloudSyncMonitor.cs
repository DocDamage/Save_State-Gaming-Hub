using SaveState.Core.SaveStates.Services.DTOs;

namespace SaveState.Core.SaveStates.Services;

/// <summary>
/// Exposes background save-state cloud daemon status for dashboard and diagnostics.
/// </summary>
public interface ISaveStateCloudSyncMonitor
{
    /// <summary>
    /// Gets the current daemon status snapshot.
    /// </summary>
    SaveStateCloudDaemonStatus CurrentStatus { get; }

    /// <summary>
    /// Raised whenever daemon status changes.
    /// </summary>
    event EventHandler<SaveStateCloudDaemonStatus>? StatusChanged;
}
