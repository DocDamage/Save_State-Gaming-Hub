using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

/// <summary>
/// Configuration options for MUGEN integration.
/// </summary>
public class MugenOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Mugen";

    /// <summary>
    /// Path to the MUGEN executable.
    /// </summary>
    [Required]
    public string ExecutablePath { get; set; } = "engines/ikemen/Ikemen_GO.exe";

    /// <summary>
    /// Delay in milliseconds before MUGEN process starts.
    /// </summary>
    [Range(0, 10000)]
    public int ProcessStartupDelayMs { get; set; } = 500;

    /// <summary>
    /// Primary characters directory (single path, for backward compatibility).
    /// </summary>
    public string CharactersDirectory { get; set; } = "data/characters";

    /// <summary>
    /// Primary stages directory (single path, for backward compatibility).
    /// </summary>
    public string StagesDirectory { get; set; } = "data/stages";

    /// <summary>
    /// Multiple character directories to scan.
    /// </summary>
    public string[] CharacterDirectories { get; set; } = new[]
    {
        "C:\\mugen\\chars",
        "data/characters"
    };

    /// <summary>
    /// Multiple stage directories to scan.
    /// </summary>
    public string[] StageDirectories { get; set; } = new[]
    {
        "C:\\mugen\\stages",
        "data/stages"
    };

    /// <summary>
    /// Data directory for MUGEN.
    /// </summary>
    public string DataDirectory { get; set; } = "data";

    /// <summary>
    /// Save directory for MUGEN.
    /// </summary>
    public string SaveDirectory { get; set; } = "save";
}
