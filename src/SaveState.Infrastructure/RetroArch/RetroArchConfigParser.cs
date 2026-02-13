using SaveState.Core.RetroArch;

namespace SaveState.Infrastructure.RetroArch;

/// <summary>
/// Parses RetroArch configuration files.
/// </summary>
public static class RetroArchConfigParser
{
    /// <summary>
    /// Parses a RetroArch configuration file and returns the config object.
    /// </summary>
    public static RetroArchConfig ParseConfig(IEnumerable<string> lines)
    {
        var config = new RetroArchConfig();

        foreach (var line in lines)
        {
            if (line.StartsWith("savefile_directory"))
                config.SavefileDirectory = ExtractConfigValue(line);
            else if (line.StartsWith("savestate_directory"))
                config.SavestateDirectory = ExtractConfigValue(line);
            else if (line.StartsWith("system_directory"))
                config.SystemDirectory = ExtractConfigValue(line);
            else if (line.StartsWith("netplay_enable"))
                config.CloudSyncEnabled = ExtractConfigValue(line) == "true";
        }

        return config;
    }

    /// <summary>
    /// Extracts the value from a configuration line.
    /// </summary>
    public static string ExtractConfigValue(string line)
    {
        var parts = line.Split('=');
        if (parts.Length < 2)
            return string.Empty;

        var value = parts[1].Trim();
        
        // Remove quotes if present
        if (value.StartsWith("\"") && value.EndsWith("\""))
            value = value[1..^1];

        return value;
    }

    /// <summary>
    /// Formats a core name for display.
    /// </summary>
    public static string FormatCoreName(string coreName)
    {
        return coreName switch
        {
            "snes9x" => "Snes9x (SNES)",
            "genesis_plus_gx" => "Genesis Plus GX (Genesis/MD)",
            "mgba" => "mGBA (Game Boy Advance)",
            "mupen64plus_next" => "Mupen64Plus-Next (N64)",
            "pcsx_rearmed" => "PCSX ReARMed (PlayStation)",
            "dolphin" => "Dolphin (GameCube/Wii)",
            "ppsspp" => "PPSSPP (PSP)",
            "nestopia" => "Nestopia (NES)",
            _ => coreName.Replace("_", " ").ToUpperInvariant()
        };
    }
}
