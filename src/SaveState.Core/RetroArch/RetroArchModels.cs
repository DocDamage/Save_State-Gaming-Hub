namespace SaveState.Core.RetroArch;

/// <summary>
/// Represents a RetroArch game detected from the playlist.
/// </summary>
public class RetroArchGame
{
    public string Path { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string CorePath { get; init; } = string.Empty;
    public string CoreName { get; init; } = string.Empty;
    public string? Crc32 { get; init; }
    public string? DbName { get; init; }
    public DateTime? LastPlayed { get; init; }
}

/// <summary>
/// Represents a RetroArch core.
/// </summary>
public class RetroArchCore
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string? Version { get; init; }
    public List<string> SupportedExtensions { get; init; } = new();
    public bool IsInstalled { get; init; }
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
