using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

/// <summary>
/// Configuration options for cloud synchronization services.
/// </summary>
public sealed class CloudSyncOptions
{
    public const string SectionName = "CloudSync";

    /// <summary>
    /// Gets or sets the preferred cloud provider (e.g., "OneDrive", "Google Drive").
    /// </summary>
    public string PreferredProvider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to automatically sync after a game exits.
    /// </summary>
    public bool AutoSyncOnExit { get; set; } = true;

    /// <summary>
    /// Gets or sets OneDrive specific configuration.
    /// </summary>
    public OneDriveOptions OneDrive { get; set; } = new();

    /// <summary>
    /// Gets or sets Google Drive specific configuration.
    /// </summary>
    public GoogleDriveOptions GoogleDrive { get; set; } = new();

    /// <summary>
    /// Gets or sets settings for background save-state cloud synchronization.
    /// </summary>
    public SaveStateCloudDaemonOptions SaveStateDaemon { get; set; } = new();
}

public sealed class OneDriveOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty; // Optional for desktop apps usually
}

public sealed class GoogleDriveOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for the background daemon that syncs save states to the cloud.
/// </summary>
public sealed class SaveStateCloudDaemonOptions
{
    /// <summary>
    /// Enables or disables the background save-state cloud sync daemon.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval in seconds between daemon sync cycles.
    /// </summary>
    [Range(15, 3600)]
    public int IntervalSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum number of games to evaluate per daemon sync cycle.
    /// </summary>
    [Range(1, 100)]
    public int MaxGamesPerCycle { get; set; } = 10;

    /// <summary>
    /// When true, daemon sync can force upload even when a conflict is detected.
    /// </summary>
    public bool ForceUploadOnConflict { get; set; }
}
