namespace SaveState.Core.Configuration;

/// <summary>
/// Configuration options for ROM scanning and management.
/// </summary>
public class RomScanningOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "RomScanning";

    /// <summary>
    /// Directories to scan for ROM files.
    /// </summary>
    public string[] RomDirectories { get; set; } = new[]
    {
        "data/roms"
    };

    /// <summary>
    /// Directory containing BIOS files for emulators.
    /// </summary>
    public string BiosDirectory { get; set; } = "data/bios";

    /// <summary>
    /// Whether to automatically scan for ROMs on application startup.
    /// </summary>
    public bool AutoScanOnStartup { get; set; } = true;

    /// <summary>
    /// Whether to scan subdirectories recursively.
    /// </summary>
    public bool ScanRecursively { get; set; } = true;

    /// <summary>
    /// File extensions to recognize as ROM files, grouped by platform.
    /// </summary>
    public Dictionary<string, string[]> PlatformExtensions { get; set; } = new()
    {
        ["NES"] = new[] { ".nes", ".unf", ".unif" },
        ["SNES"] = new[] { ".sfc", ".smc", ".fig", ".swc" },
        ["Nintendo 64"] = new[] { ".n64", ".z64", ".v64" },
        ["Game Boy"] = new[] { ".gb", ".gbc" },
        ["Game Boy Advance"] = new[] { ".gba" },
        ["Nintendo DS"] = new[] { ".nds" },
        ["Nintendo 3DS"] = new[] { ".3ds", ".cia" },
        ["GameCube"] = new[] { ".iso", ".gcm", ".gcz" },
        ["Wii"] = new[] { ".wbfs", ".iso" },
        ["PlayStation"] = new[] { ".bin", ".cue", ".iso", ".img" },
        ["PlayStation 2"] = new[] { ".iso", ".bin", ".gz" },
        ["PlayStation Portable"] = new[] { ".iso", ".cso" },
        ["Sega Genesis"] = new[] { ".md", ".gen", ".bin", ".smd" },
        ["Sega Saturn"] = new[] { ".cue", ".bin", ".iso" },
        ["Sega Dreamcast"] = new[] { ".cdi", ".gdi", ".chd" },
        ["Arcade"] = new[] { ".zip", ".7z" },
        ["MAME"] = new[] { ".zip", ".7z" },
        ["Neo Geo"] = new[] { ".zip", ".7z" },
        ["PC Engine"] = new[] { ".pce", ".cue" },
        ["Atari 2600"] = new[] { ".a26", ".bin" },
        ["Atari 7800"] = new[] { ".a78" }
    };
}
