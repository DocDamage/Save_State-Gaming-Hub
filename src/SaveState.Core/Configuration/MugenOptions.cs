using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class MugenOptions
{
    public const string SectionName = "Mugen";

    [Required]
    public string ExecutablePath { get; set; } = "engines/ikemen/Ikemen_GO.exe";
    [Range(0, 10000)]
    public int ProcessStartupDelayMs { get; set; } = 500;

    public string[] CharacterDirectories { get; set; } = new[]
    {
        "data/characters/streetfighter",
        "data/characters/mvc2",
        "data/characters/builtin"
    };
}
