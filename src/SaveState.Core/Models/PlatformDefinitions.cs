namespace SaveState.Core.Models;

public static class PlatformDefinitions
{
    public static readonly Dictionary<string, PlatformInfo> Platforms = new()
    {
        // Nintendo
        ["NES"] = new("Nintendo Entertainment System", new[] { ".nes", ".unf", ".unif" }, "🎮"),
        ["SNES"] = new("Super Nintendo", new[] { ".sfc", ".smc", ".fig" }, "🎮"),
        ["N64"] = new("Nintendo 64", new[] { ".n64", ".z64", ".v64" }, "🎮"),
        ["GC"] = new("GameCube", new[] { ".iso", ".gcm", ".ciso" }, "💿"),
        ["Wii"] = new("Wii", new[] { ".iso", ".wbfs", ".wad" }, "💿"),
        ["GB"] = new("Game Boy", new[] { ".gb" }, "🎮"),
        ["GBC"] = new("Game Boy Color", new[] { ".gbc" }, "🎮"),
        ["GBA"] = new("Game Boy Advance", new[] { ".gba" }, "🎮"),
        ["NDS"] = new("Nintendo DS", new[] { ".nds" }, "🎮"),
        ["3DS"] = new("Nintendo 3DS", new[] { ".3ds", ".cia" }, "🎮"),
        ["Switch"] = new("Nintendo Switch", new[] { ".nsp", ".xci" }, "🎮"),
        
        // Sony
        ["PS1"] = new("PlayStation", new[] { ".bin", ".cue", ".iso", ".img", ".pbp" }, "💿"),
        ["PS2"] = new("PlayStation 2", new[] { ".iso", ".bin", ".cso" }, "💿"),
        ["PS3"] = new("PlayStation 3", new[] { ".iso", ".pkg" }, "💿"),
        ["PSP"] = new("PlayStation Portable", new[] { ".iso", ".cso", ".pbp" }, "💿"),
        ["PSVita"] = new("PlayStation Vita", new[] { ".vpk" }, "📱"),
        
        // Sega
        ["SMS"] = new("Sega Master System", new[] { ".sms" }, "🎮"),
        ["Genesis"] = new("Sega Genesis", new[] { ".md", ".gen", ".smd", ".bin" }, "🎮"),
        ["SegaCD"] = new("Sega CD", new[] { ".iso", ".bin", ".cue" }, "💿"),
        ["Saturn"] = new("Sega Saturn", new[] { ".iso", ".bin", ".cue" }, "💿"),
        ["Dreamcast"] = new("Sega Dreamcast", new[] { ".cdi", ".gdi", ".iso" }, "💿"),
        ["GameGear"] = new("Sega Game Gear", new[] { ".gg" }, "🎮"),
        
        // Other
        ["Arcade"] = new("Arcade", new[] { ".zip" }, "🕹️"),
        ["Atari2600"] = new("Atari 2600", new[] { ".a26", ".bin" }, "🕹️"),
        ["Atari7800"] = new("Atari 7800", new[] { ".a78", ".bin" }, "🕹️"),
        ["PCEngine"] = new("PC Engine / TurboGrafx-16", new[] { ".pce" }, "🎮"),
        ["NeoGeo"] = new("Neo Geo", new[] { ".zip" }, "🕹️"),
        ["DOS"] = new("DOS", new[] { ".exe", ".com", ".bat" }, "💾"),
    };

    public static PlatformInfo? GetByExtension(string extension)
    {
        var ext = extension.ToLowerInvariant();
        foreach (var kvp in Platforms)
        {
            if (kvp.Value.Extensions.Contains(ext))
                return kvp.Value with { ShortName = kvp.Key };
        }
        return null;
    }
}

public record PlatformInfo(string FullName, string[] Extensions, string Icon)
{
    public string ShortName { get; init; } = string.Empty;
}
