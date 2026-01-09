namespace SaveState.Core.Configuration;

/// <summary>
/// Configuration options for emulator scanning and discovery.
/// </summary>
public class EmulatorScanningOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "EmulatorScanning";

    /// <summary>
    /// Whether to automatically scan for emulators on application startup.
    /// </summary>
    public bool AutoScanOnStartup { get; set; } = true;

    /// <summary>
    /// Known directories where emulators are commonly installed.
    /// </summary>
    public string[] KnownEmulatorPaths { get; set; } = new[]
    {
        "C:\\Program Files\\Emulation",
        "C:\\Program Files (x86)\\Emulation",
        "~/RetroArch",
        "~/Emulators",
        "~/Games/Emulators"
    };

    /// <summary>
    /// Common emulator executable names to look for.
    /// </summary>
    public string[] CommonEmulatorExecutables { get; set; } = new[]
    {
        "retroarch.exe",
        "mgba.exe",
        "mesen.exe",
        "fceux.exe",
        "snes9x.exe",
        "zsnes.exe",
        "project64.exe",
        "mupen64plus.exe",
        "dolphin.exe",
        "pcsx2.exe",
        "epsxe.exe",
        "mednafen.exe",
        "fusion.exe"
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
    /// File size threshold (in bytes) for considering a file an emulator executable.
    /// </summary>
    public long MinExecutableSizeBytes { get; set; } = 1024 * 1024; // 1MB
}