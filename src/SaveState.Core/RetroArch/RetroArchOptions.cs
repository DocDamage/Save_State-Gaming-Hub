namespace SaveState.Core.RetroArch;

/// <summary>
/// Configuration options for RetroArch integration.
/// </summary>
public class RetroArchOptions
{
    /// <summary>
    /// The section name in appsettings.json.
    /// </summary>
    public const string SectionName = "RetroArch";

    /// <summary>
    /// Gets or sets the RetroArch installation path.
    /// If empty, auto-detection will be used.
    /// </summary>
    public string InstallPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the RetroArch playlists directory path.
    /// If empty, defaults to {InstallPath}/playlists.
    /// </summary>
    public string PlaylistsPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the RetroArch cores directory path.
    /// If empty, defaults to {InstallPath}/cores.
    /// </summary>
    public string CoresPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to auto-detect RetroArch installation.
    /// </summary>
    public bool AutoDetect { get; set; } = true;

    /// <summary>
    /// Gets or sets whether cloud sync is enabled.
    /// </summary>
    public bool CloudSyncEnabled { get; set; }

    /// <summary>
    /// Gets or sets the cloud sync provider (AzureBlob, AwsS3, GoogleCloud).
    /// </summary>
    public string CloudSyncProvider { get; set; } = "AzureBlob";

    /// <summary>
    /// Gets or sets the cloud storage connection string or credentials.
    /// </summary>
    public string? CloudSyncConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the cloud storage container/bucket name.
    /// </summary>
    public string CloudSyncContainerName { get; set; } = "retroarch-saves";

    /// <summary>
    /// Gets or sets whether to automatically sync on game launch/exit.
    /// </summary>
    public bool AutoSyncOnLaunch { get; set; } = true;

    /// <summary>
    /// Gets or sets the RetroAchievements username.
    /// </summary>
    public string? RetroAchievementsUsername { get; set; }

    /// <summary>
    /// Gets or sets the RetroAchievements API key.
    /// </summary>
    public string? RetroAchievementsApiKey { get; set; }

    /// <summary>
    /// Gets or sets whether RetroAchievements integration is enabled.
    /// </summary>
    public bool RetroAchievementsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether RetroArch network command interface is enabled.
    /// RetroArch must be started with --network-cmd-enable flag.
    /// </summary>
    public bool NetworkCommandEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the network command interface port.
    /// Default: 55355 (RetroArch default).
    /// </summary>
    public int NetworkCommandPort { get; set; } = 55355;

    /// <summary>
    /// Gets or sets the network command interface host.
    /// Default: localhost (127.0.0.1).
    /// </summary>
    public string NetworkCommandHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the network command timeout in milliseconds.
    /// Default: 5000ms (5 seconds).
    /// </summary>
    public int NetworkCommandTimeout { get; set; } = 5000;
}
