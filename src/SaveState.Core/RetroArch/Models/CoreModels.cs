namespace SaveState.Core.RetroArch.Models;

/// <summary>
/// Detailed information about a RetroArch core.
/// </summary>
public class RetroArchCoreInfo
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string? Version { get; init; }
    public string? CoreVersion { get; init; }
    public string? SystemName { get; init; }
    public string? SystemId { get; init; }
    public string? Database { get; init; }
    public string? License { get; init; }
    public string? Permissions { get; init; }
    public List<string> SupportedExtensions { get; init; } = new();
    public bool IsInstalled { get; init; }
    public CoreType Type { get; init; }
    public CoreCapabilities Capabilities { get; init; } = new();
    public DateTime? LastUpdated { get; init; }
    public long FileSize { get; init; }
}

/// <summary>
/// Capabilities of a RetroArch core.
/// </summary>
public class CoreCapabilities
{
    public bool SupportsSaveStates { get; init; } = true;
    public bool SupportsCheats { get; init; }
    public bool SupportsAchievements { get; init; }
    public bool SupportsRewind { get; init; }
    public bool SupportsNetplay { get; init; }
    public bool SupportsHwRender { get; init; }
    public int MaxPlayers { get; init; } = 2;
}

/// <summary>
/// Information about available core downloads.
/// </summary>
public class CoreDownloadInfo
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string? SystemName { get; init; }
    public long FileSize { get; init; }
    public string? Checksum { get; init; }
}

/// <summary>
/// Result of a core installation operation.
/// </summary>
public class CoreInstallResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string CorePath { get; init; } = string.Empty;
    public DateTime InstalledAt { get; init; }
}
