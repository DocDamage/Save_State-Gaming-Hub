using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.ValueObjects;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the database with RetroArch emulator configuration.
/// </summary>
public class RetroArchSeeder
{
    private readonly SaveStateDbContext _context;
    private readonly ILogger<RetroArchSeeder> _logger;
    private const string RetroArchPath = @"C:\RetroArch-Win64\retroarch.exe";

    public RetroArchSeeder(SaveStateDbContext context, ILogger<RetroArchSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds RetroArch emulator for all supported platforms.
    /// </summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!File.Exists(RetroArchPath))
        {
            _logger.LogWarning("RetroArch not found at {Path}. Skipping seeding.", RetroArchPath);
            return;
        }

        _logger.LogInformation("Seeding RetroArch emulator configuration...");

        // Get or create platforms
        var platforms = await GetOrCreatePlatformsAsync(ct);

        // Create RetroArch emulator entries for each platform
        var seededCount = 0;
        foreach (var (platformName, coreName, coreArgs) in GetRetroArchCoreMapping())
        {
            var platform = platforms.FirstOrDefault(p => p.Name.Value == platformName);
            if (platform == null)
            {
                _logger.LogWarning("Platform {PlatformName} not found. Skipping.", platformName);
                continue;
            }

            // Check if emulator already exists for this platform
            var existingEmulator = await _context.Emulators
                .FirstOrDefaultAsync(e => e.PlatformId == platform.Id && e.Name.Contains("RetroArch"), ct);

            if (existingEmulator != null)
            {
                _logger.LogDebug("RetroArch already configured for {Platform}", platformName);
                continue;
            }

            // Create new emulator entry
            var emulator = new Emulator(
                $"RetroArch ({coreName})",
                new FilePath(RetroArchPath),
                platform.Id);

            emulator.SetCommandLineArgs(coreArgs);

            await _context.Emulators.AddAsync(emulator, ct);
            seededCount++;
            _logger.LogDebug("Added RetroArch for {Platform} with core {Core}", platformName, coreName);
        }

        if (seededCount > 0)
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully seeded {Count} RetroArch emulator configurations", seededCount);
        }
        else
        {
            _logger.LogInformation("No new RetroArch configurations needed");
        }
    }

    private async Task<List<Platform>> GetOrCreatePlatformsAsync(CancellationToken ct)
    {
        var platforms = await _context.Platforms.ToListAsync(ct);

        if (platforms.Count > 0)
            return platforms;

        // Create essential platforms if they don't exist
        _logger.LogInformation("Creating essential gaming platforms...");

        var platformsToCreate = new[]
        {
            "NES", "SNES", "Nintendo 64", "Game Boy", "Game Boy Color", "Game Boy Advance",
            "Nintendo DS", "GameCube", "Wii", "PlayStation", "PlayStation 2", "PlayStation Portable",
            "Sega Genesis", "Sega Master System", "Sega Game Gear", "Sega Saturn", "Sega Dreamcast",
            "Atari 2600", "Atari 7800", "Neo Geo", "TurboGrafx-16", "WonderSwan"
        };

        foreach (var platformName in platformsToCreate)
        {
            var shortNameStr = GenerateShortName(platformName);
            var type = DeterminePlatformType(platformName);

            var platform = new Platform(
                PlatformName.From(platformName),
                PlatformShortName.From(shortNameStr),
                type);

            await _context.Platforms.AddAsync(platform, ct);
            platforms.Add(platform);
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Created {Count} gaming platforms", platformsToCreate.Length);

        return platforms;
    }

    private static string GenerateShortName(string platformName)
    {
        // Simple mapping for common platforms
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Nintendo 64"] = "N64",
            ["Super Nintendo"] = "SNES",
            ["Game Boy Advance"] = "GBA",
            ["Game Boy Color"] = "GBC",
            ["Game Boy"] = "GB",
            ["Nintendo DS"] = "NDS",
            ["GameCube"] = "GC",
            ["PlayStation"] = "PS1",
            ["PlayStation 2"] = "PS2",
            ["PlayStation 3"] = "PS3",
            ["PlayStation Portable"] = "PSP",
            ["Sega Genesis"] = "GENESIS",
            ["Sega Dreamcast"] = "DREAMCAST",
            ["TurboGrafx-16"] = "TG16"
        };

        if (mapping.TryGetValue(platformName, out var shortName))
            return shortName;

        // Fallback: Remove spaces and non-alphanumeric characters, take first 10
        var clean = new string(platformName.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return clean.Length > 10 ? clean.Substring(0, 10) : clean;
    }

    private static PlatformType DeterminePlatformType(string platformName)
    {
        if (platformName.Contains("Game Boy") || platformName.Contains("DS") ||
            platformName.Contains("PSP") || platformName.Contains("Gear") ||
            platformName.Contains("Neo Geo Pocket") || platformName.Contains("WonderSwan"))
            return PlatformType.Handheld;

        if (platformName.Contains("Arcade") || platformName.Contains("MAME") || platformName.Contains("Neo Geo"))
            return PlatformType.Arcade;

        if (platformName.Contains("DOS") || platformName.Contains("Commodore") ||
            platformName.Contains("Amiga") || platformName.Contains("MSX") ||
            platformName.Contains("Spectrum") || platformName.Contains("Computer"))
            return PlatformType.Computer;

        return PlatformType.Console;
    }

    /// <summary>
    /// Returns mapping of platforms to RetroArch cores and launch arguments.
    /// </summary>
    private static IEnumerable<(string PlatformName, string CoreName, string Arguments)> GetRetroArchCoreMapping()
    {
        return new[]
        {
            // Nintendo
            ("NES", "Mesen", "-L cores\\mesen_libretro.dll \"{0}\""),
            ("SNES", "Snes9x", "-L cores\\snes9x_libretro.dll \"{0}\""),
            ("Nintendo 64", "Mupen64Plus-Next", "-L cores\\mupen64plus_next_libretro.dll \"{0}\""),
            ("Game Boy", "Gambatte", "-L cores\\gambatte_libretro.dll \"{0}\""),
            ("Game Boy Color", "Gambatte", "-L cores\\gambatte_libretro.dll \"{0}\""),
            ("Game Boy Advance", "mGBA", "-L cores\\mgba_libretro.dll \"{0}\""),
            ("Nintendo DS", "melonDS", "-L cores\\melonds_libretro.dll \"{0}\""),
            ("GameCube", "Dolphin", "-L cores\\dolphin_libretro.dll \"{0}\""),
            ("Wii", "Dolphin", "-L cores\\dolphin_libretro.dll \"{0}\""),

            // PlayStation
            ("PlayStation", "Beetle PSX HW", "-L cores\\mednafen_psx_hw_libretro.dll \"{0}\""),
            ("PlayStation 2", "PCSX2", "-L cores\\pcsx2_libretro.dll \"{0}\""),
            ("PlayStation Portable", "PPSSPP", "-L cores\\ppsspp_libretro.dll \"{0}\""),

            // Sega
            ("Sega Genesis", "Genesis Plus GX", "-L cores\\genesis_plus_gx_libretro.dll \"{0}\""),
            ("Sega Master System", "Genesis Plus GX", "-L cores\\genesis_plus_gx_libretro.dll \"{0}\""),
            ("Sega Game Gear", "Genesis Plus GX", "-L cores\\genesis_plus_gx_libretro.dll \"{0}\""),
            ("Sega Saturn", "Beetle Saturn", "-L cores\\mednafen_saturn_libretro.dll \"{0}\""),
            ("Sega Dreamcast", "Flycast", "-L cores\\flycast_libretro.dll \"{0}\""),

            // Other
            ("Atari 2600", "Stella", "-L cores\\stella_libretro.dll \"{0}\""),
            ("Atari 7800", "ProSystem", "-L cores\\prosystem_libretro.dll \"{0}\""),
            ("Neo Geo", "FinalBurn Neo", "-L cores\\fbneo_libretro.dll \"{0}\""),
            ("TurboGrafx-16", "Beetle PCE", "-L cores\\mednafen_pce_libretro.dll \"{0}\""),
            ("WonderSwan", "Beetle Cygne", "-L cores\\mednafen_wswan_libretro.dll \"{0}\"")
        };
    }
}
