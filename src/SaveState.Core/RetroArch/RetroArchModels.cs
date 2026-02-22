namespace SaveState.Core.RetroArch;

/// <summary>
/// Represents a RetroArch game detected from the playlist.
/// </summary>
public class RetroArchGame
{
    /// <summary>
    /// Unique identifier for the game (file path or hash).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display title of the game.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Original path to the game file.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Label displayed in RetroArch.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Path to the core used to run this game.
    /// </summary>
    public string CorePath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the core used to run this game.
    /// </summary>
    public string CoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gaming system/platform (e.g., SNES, Genesis, PS1).
    /// </summary>
    public string System { get; set; } = string.Empty;

    /// <summary>
    /// CRC32 hash of the game file.
    /// </summary>
    public string? Crc32 { get; set; }

    /// <summary>
    /// Database name for this game.
    /// </summary>
    public string? DbName { get; set; }

    /// <summary>
    /// Path to the cover/box art image.
    /// </summary>
    public string? CoverPath { get; set; }

    /// <summary>
    /// When the game was last played.
    /// </summary>
    public DateTime? LastPlayed { get; set; }

    /// <summary>
    /// Total play time for this game.
    /// </summary>
    public TimeSpan PlayTime { get; set; }
}

/// <summary>
/// Represents a RetroArch core.
/// </summary>
public class RetroArchCore
{
    /// <summary>
    /// Name of the core.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the core.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gaming system this core emulates.
    /// </summary>
    public string System { get; set; } = string.Empty;

    /// <summary>
    /// Path to the core file.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Version of the core.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// List of supported file extensions.
    /// </summary>
    public List<string> SupportedExtensions { get; set; } = new();

    /// <summary>
    /// Whether the core is installed locally.
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Whether an update is available for this core.
    /// </summary>
    public bool IsUpdateAvailable { get; set; }
}

/// <summary>
/// Represents RetroArch configuration.
/// </summary>
public class RetroArchConfig
{
    public string? SavefileDirectory { get; set; }
    public string? SavestateDirectory { get; set; }
    public string? SystemDirectory { get; set; }
    public string? CoreAssetsDirectory { get; set; }
    public bool CloudSyncEnabled { get; set; }
    public string? CloudSyncUrl { get; set; }
    public string? CloudSyncUsername { get; set; }
}

/// <summary>
/// Represents a RetroArch playlist (collection of games).
/// </summary>
public class RetroArchPlaylist
{
    /// <summary>
    /// Name of the playlist.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File path to the playlist.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Number of games in the playlist.
    /// </summary>
    public int GameCount { get; set; }

    /// <summary>
    /// Games contained in this playlist.
    /// </summary>
    public List<RetroArchGame> Games { get; set; } = new();
}

/// <summary>
/// Represents a Netplay lobby for multiplayer gaming.
/// </summary>
public class NetplayLobby
{
    /// <summary>
    /// Unique identifier for the lobby.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the game being hosted.
    /// </summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>
    /// Username of the host.
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Core required to join this lobby.
    /// </summary>
    public string CoreName { get; set; } = string.Empty;

    /// <summary>
    /// Ping to the host in milliseconds.
    /// </summary>
    public int Ping { get; set; }

    /// <summary>
    /// Current number of connected players.
    /// </summary>
    public int PlayerCount { get; set; }

    /// <summary>
    /// Maximum number of players allowed.
    /// </summary>
    public int MaxPlayers { get; set; }

    /// <summary>
    /// Whether the lobby requires a password to join.
    /// </summary>
    public bool HasPassword { get; set; }
}
