namespace SaveState.Core.Configuration;

/// <summary>
/// Configuration options for MUGEN scanning and discovery.
/// </summary>
public class MugenScanningOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "MugenScanning";

    /// <summary>
    /// Whether to automatically scan for MUGEN installations on application startup.
    /// </summary>
    public bool AutoScanOnStartup { get; set; } = true;

    /// <summary>
    /// Known directories where MUGEN is commonly installed.
    /// </summary>
    public string[] KnownMugenPaths { get; set; } = new[]
    {
        "C:\\Program Files\\MUGEN",
        "C:\\Program Files (x86)\\MUGEN",
        "~/MUGEN",
        "~/Games/MUGEN",
        "~/mugen"
    };

    /// <summary>
    /// MUGEN executable names to look for.
    /// </summary>
    public string[] MugenExecutables { get; set; } = new[]
    {
        "mugen.exe",
        "ikemen.exe",
        "ikemen_go.exe"
    };

    /// <summary>
    /// Whether to scan recursively in known paths.
    /// </summary>
    public bool ScanRecursively { get; set; } = true;

    /// <summary>
    /// Maximum depth for recursive scanning.
    /// </summary>
    public int MaxScanDepth { get; set; } = 3;

    /// <summary>
    /// File size threshold (in bytes) for considering a file a MUGEN executable.
    /// </summary>
    public long MinExecutableSizeBytes { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Required MUGEN directories that indicate a valid installation.
    /// </summary>
    public string[] RequiredDirectories { get; set; } = new[]
    {
        "chars",
        "stages",
        "data"
    };

    /// <summary>
    /// Common MUGEN data file extensions to look for.
    /// </summary>
    public string[] DataFileExtensions { get; set; } = new[]
    {
        ".def",
        ".cmd",
        ".cns",
        ".st",
        ".air",
        ".snd"
    };
}