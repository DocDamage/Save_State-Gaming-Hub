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
