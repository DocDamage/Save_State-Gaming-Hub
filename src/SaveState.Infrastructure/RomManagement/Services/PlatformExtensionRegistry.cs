using SaveState.Core.Common;
using SaveState.Core.RomManagement;

namespace SaveState.Infrastructure.RomManagement.Services;

/// <summary>
/// Implementation of IPlatformExtensionRegistry with hardcoded platform mapping.
/// </summary>
public class PlatformExtensionRegistry : IPlatformExtensionRegistry
{
    private static readonly Dictionary<string, string[]> PlatformExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NES"] = new[] { ".nes", ".unf", ".unif" },
        ["SNES"] = new[] { ".sfc", ".smc", ".fig", ".swc" },
        ["Nintendo 64"] = new[] { ".n64", ".z64", ".v64", ".rom" },
        ["GameCube"] = new[] { ".iso", ".gcm", ".dol", ".rvz" },
        ["Wii"] = new[] { ".iso", ".wbfs", ".dol", ".rvz", ".wad" },
        ["Wii U"] = new[] { ".wud", ".wux", ".rpx", ".wua" },
        ["Nintendo Switch"] = new[] { ".nsp", ".xci", ".nro", ".nca" },
        ["Game Boy"] = new[] { ".gb" },
        ["Game Boy Color"] = new[] { ".gbc" },
        ["Game Boy Advance"] = new[] { ".gba" },
        ["Nintendo DS"] = new[] { ".nds", ".dsi" },
        ["Nintendo 3DS"] = new[] { ".3ds", ".cia", ".cci" },
        ["PlayStation"] = new[] { ".bin", ".cue", ".iso", ".img", ".pbp" },
        ["PlayStation 2"] = new[] { ".iso", ".bin", ".cue" },
        ["PlayStation 3"] = new[] { ".pkg", ".iso" },
        ["PlayStation Portable"] = new[] { ".iso", ".cso", ".pbp" },
        ["PlayStation Vita"] = new[] { ".vpk", ".zip" },
        ["Sega Genesis"] = new[] { ".md", ".gen", ".bin", ".smd" },
        ["Sega Master System"] = new[] { ".sms" },
        ["Sega Game Gear"] = new[] { ".gg" },
        ["Sega Saturn"] = new[] { ".iso", ".bin", ".cue" },
        ["Sega Dreamcast"] = new[] { ".cdi", ".gdi", ".iso" },
        ["TurboGrafx-16"] = new[] { ".pce", ".bin" },
        ["Neo Geo Pocket"] = new[] { ".ngp", ".ngc" },
        ["Neo Geo"] = new[] { ".zip", ".7z" },
        ["WonderSwan"] = new[] { ".ws", ".wsc" },
        ["Atari 2600"] = new[] { ".a26", ".bin" },
        ["Atari 5200"] = new[] { ".a52", ".bin" },
        ["Atari 7800"] = new[] { ".a78", ".bin" },
        ["Atari Jaguar"] = new[] { ".j64", ".jag" },
        ["Atari Lynx"] = new[] { ".lnx" },
        ["Atari ST"] = new[] { ".st", ".stx", ".msa" },
        ["Commodore 64"] = new[] { ".d64", ".t64", ".prg", ".crt" },
        ["Commodore Amiga"] = new[] { ".adf", ".ipf", ".lha" },
        ["MSX"] = new[] { ".rom", ".dsk", ".cas" },
        ["ZX Spectrum"] = new[] { ".tap", ".tzx", ".z80", ".scl", ".trd" },
        ["Amstrad CPC"] = new[] { ".dsk", ".sna" },
        ["Xbox"] = new[] { ".iso", ".xbe" },
        ["Xbox 360"] = new[] { ".iso", ".xex" },
        ["3DO"] = new[] { ".iso", ".bin" },
        ["DOS"] = new[] { ".exe", ".com", ".bat" },
        ["MAME"] = new[] { ".zip", ".7z" }
    };

    public string[] GetExtensions(string platformName)
    {
        if (string.IsNullOrWhiteSpace(platformName))
        {
            return Array.Empty<string>();
        }

        if (PlatformExtensions.TryGetValue(platformName, out var extensions))
        {
            return extensions;
        }

        return Array.Empty<string>();
    }

    public bool IsValidExtension(string platformName, string filePath)
    {
        var extensions = GetExtensions(platformName);
        if (extensions.Length == 0) return false;

        if (string.IsNullOrWhiteSpace(filePath)) return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extensions.Contains(extension);
    }

    public Result<string> DetectPlatformName(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Result<string>.Failure("File path cannot be null or empty", ErrorType.Validation);

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            return Result<string>.Failure("File path must have an extension", ErrorType.Validation);

        foreach (var entry in PlatformExtensions)
        {
            if (entry.Value.Contains(extension))
            {
                return Result<string>.Success(entry.Key);
            }
        }

        return Result<string>.Failure($"No platform found for extension '{extension}'", ErrorType.NotFound);
    }
}
