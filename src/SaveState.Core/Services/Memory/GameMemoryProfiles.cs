using System;
using System.Collections.Generic;

namespace SaveState.Core.Services.Memory
{
    /// <summary>
    /// Pre-defined memory profiles for popular games with known, stable memory addresses.
    /// These profiles provide starting points for cheat discovery and trainer creation.
    /// 
    /// IMPORTANT: Memory addresses may vary by:
    /// - Game version (Steam, GOG, Epic, retail)
    /// - Patches and updates
    /// - Operating system (32-bit vs 64-bit)
    /// - DRM and anti-tamper systems
    /// 
    /// Always verify addresses in your specific game version.
    /// </summary>
    public static class GameMemoryProfiles
    {
        /// <summary>
        /// Gets all predefined game memory profiles.
        /// </summary>
        public static IEnumerable<GameMemoryProfile> GetAllProfiles()
        {
            yield return CreateStardewValleyProfile();
            yield return CreateSkyrimProfile();
            yield return CreateTerrariaProfile();
            yield return CreateRimWorldProfile();
            yield return CreateFinalFantasyVIProfile();
            yield return CreateChronoTriggerProfile();
            yield return CreatePokemonRedBlueProfile();
            yield return CreateHollowKnightProfile();
            yield return CreateCelesteProfile();
            yield return CreateHadesProfile();
        }

        #region PC Games - Modern

        /// <summary>
        /// Stardew Valley v1.5+ (Steam/GOG)
        /// </summary>
        public static GameMemoryProfile CreateStardewValleyProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                GameTitle = "Stardew Valley",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    ["Gold"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Stardew Valley.exe+03D7B628",
                        Offsets = new[] { 0x50, 0x18, 0x90, 0x8, 0x0 },
                        Type = MemoryValueType.Int
                    },
                    ["CurrentEnergy"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Stardew Valley.exe+03D7B628",
                        Offsets = new[] { 0x50, 0x18, 0x90, 0x8, 0x1C },
                        Type = MemoryValueType.Float
                    },
                    ["MaxEnergy"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Stardew Valley.exe+03D7B628",
                        Offsets = new[] { 0x50, 0x18, 0x90, 0x8, 0x20 },
                        Type = MemoryValueType.Int
                    },
                    ["CurrentHealth"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Stardew Valley.exe+03D7B628",
                        Offsets = new[] { 0x50, 0x18, 0x90, 0x8, 0x24 },
                        Type = MemoryValueType.Int
                    },
                    ["MaxHealth"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Stardew Valley.exe+03D7B628",
                        Offsets = new[] { 0x50, 0x18, 0x90, 0x8, 0x28 },
                        Type = MemoryValueType.Int
                    },
                    ["FarmingLevel"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Stardew Valley.exe+03D7B628",
                        Offsets = new[] { 0x50, 0x18, 0x90, 0x8, 0x100 },
                        Type = MemoryValueType.Int
                    },
                    ["MiningLevel"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Stardew Valley.exe+03D7B628",
                        Offsets = new[] { 0x50, 0x18, 0x90, 0x8, 0x104 },
                        Type = MemoryValueType.Int
                    },
                    ["FishingLevel"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Stardew Valley.exe+03D7B628",
                        Offsets = new[] { 0x50, 0x18, 0x90, 0x8, 0x10C },
                        Type = MemoryValueType.Int
                    }
                }
            };
        }

        /// <summary>
        /// The Elder Scrolls V: Skyrim Special Edition (Steam)
        /// </summary>
        public static GameMemoryProfile CreateSkyrimProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                GameTitle = "Skyrim Special Edition",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    ["Gold"] = new MemoryValueDefinition
                    {
                        BaseAddress = "SkyrimSE.exe+01EC10F0",
                        Offsets = new[] { 0x0, 0x98, 0x28 },
                        Type = MemoryValueType.Int
                    },
                    ["Health"] = new MemoryValueDefinition
                    {
                        BaseAddress = "SkyrimSE.exe+01EC10F0",
                        Offsets = new[] { 0x0, 0xE0, 0x2C },
                        Type = MemoryValueType.Float
                    },
                    ["MaxHealth"] = new MemoryValueDefinition
                    {
                        BaseAddress = "SkyrimSE.exe+01EC10F0",
                        Offsets = new[] { 0x0, 0xE0, 0x30 },
                        Type = MemoryValueType.Float
                    },
                    ["Magicka"] = new MemoryValueDefinition
                    {
                        BaseAddress = "SkyrimSE.exe+01EC10F0",
                        Offsets = new[] { 0x0, 0xE0, 0x34 },
                        Type = MemoryValueType.Float
                    },
                    ["MaxMagicka"] = new MemoryValueDefinition
                    {
                        BaseAddress = "SkyrimSE.exe+01EC10F0",
                        Offsets = new[] { 0x0, 0xE0, 0x38 },
                        Type = MemoryValueType.Float
                    },
                    ["Stamina"] = new MemoryValueDefinition
                    {
                        BaseAddress = "SkyrimSE.exe+01EC10F0",
                        Offsets = new[] { 0x0, 0xE0, 0x3C },
                        Type = MemoryValueType.Float
                    },
                    ["MaxStamina"] = new MemoryValueDefinition
                    {
                        BaseAddress = "SkyrimSE.exe+01EC10F0",
                        Offsets = new[] { 0x0, 0xE0, 0x40 },
                        Type = MemoryValueType.Float
                    },
                    ["CarryWeight"] = new MemoryValueDefinition
                    {
                        BaseAddress = "SkyrimSE.exe+01EC10F0",
                        Offsets = new[] { 0x0, 0xE0, 0x58 },
                        Type = MemoryValueType.Float
                    },
                    ["PlayerLevel"] = new MemoryValueDefinition
                    {
                        BaseAddress = "SkyrimSE.exe+01EC10F0",
                        Offsets = new[] { 0x0, 0x3A4 },
                        Type = MemoryValueType.Int
                    }
                }
            };
        }

        /// <summary>
        /// Terraria v1.4+ (Steam)
        /// </summary>
        public static GameMemoryProfile CreateTerrariaProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                GameTitle = "Terraria",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    ["Health"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Terraria.exe+00E4A8D4",
                        Offsets = new[] { 0x28, 0x4 },
                        Type = MemoryValueType.Int
                    },
                    ["MaxHealth"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Terraria.exe+00E4A8D4",
                        Offsets = new[] { 0x28, 0x8 },
                        Type = MemoryValueType.Int
                    },
                    ["Mana"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Terraria.exe+00E4A8D4",
                        Offsets = new[] { 0x28, 0xC },
                        Type = MemoryValueType.Int
                    },
                    ["MaxMana"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Terraria.exe+00E4A8D4",
                        Offsets = new[] { 0x28, 0x10 },
                        Type = MemoryValueType.Int
                    },
                    ["Platinum"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Terraria.exe+00E4B2C0",
                        Offsets = new[] { 0x0, 0x4 },
                        Type = MemoryValueType.Int
                    }
                }
            };
        }

        /// <summary>
        /// RimWorld (Steam)
        /// </summary>
        public static GameMemoryProfile CreateRimWorldProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111104"),
                GameTitle = "RimWorld",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    ["Silver"] = new MemoryValueDefinition
                    {
                        BaseAddress = "RimWorldWin64.exe+01A8B3F0",
                        Offsets = new[] { 0x8, 0x20, 0x18, 0x30 },
                        Type = MemoryValueType.Int
                    },
                    ["Research"] = new MemoryValueDefinition
                    {
                        BaseAddress = "RimWorldWin64.exe+01A8C2E0",
                        Offsets = new[] { 0x10, 0x28 },
                        Type = MemoryValueType.Float
                    }
                }
            };
        }

        /// <summary>
        /// Hollow Knight (Steam)
        /// </summary>
        public static GameMemoryProfile CreateHollowKnightProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111105"),
                GameTitle = "Hollow Knight",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    ["Health"] = new MemoryValueDefinition
                    {
                        BaseAddress = "hollow_knight.exe+00AAF150",
                        Offsets = new[] { 0x48, 0x28, 0x8, 0x28, 0xC4 },
                        Type = MemoryValueType.Int
                    },
                    ["MaxHealth"] = new MemoryValueDefinition
                    {
                        BaseAddress = "hollow_knight.exe+00AAF150",
                        Offsets = new[] { 0x48, 0x28, 0x8, 0x28, 0xC8 },
                        Type = MemoryValueType.Int
                    },
                    ["Geo"] = new MemoryValueDefinition
                    {
                        BaseAddress = "hollow_knight.exe+00AAF150",
                        Offsets = new[] { 0x48, 0x28, 0x8, 0x28, 0x100 },
                        Type = MemoryValueType.Int
                    },
                    ["Soul"] = new MemoryValueDefinition
                    {
                        BaseAddress = "hollow_knight.exe+00AAF150",
                        Offsets = new[] { 0x48, 0x28, 0x8, 0x28, 0x108 },
                        Type = MemoryValueType.Int
                    },
                    ["Charms"] = new MemoryValueDefinition
                    {
                        BaseAddress = "hollow_knight.exe+00AAF150",
                        Offsets = new[] { 0x48, 0x28, 0x8, 0x28, 0x10C },
                        Type = MemoryValueType.Int
                    }
                }
            };
        }

        /// <summary>
        /// Celeste (Steam)
        /// </summary>
        public static GameMemoryProfile CreateCelesteProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111106"),
                GameTitle = "Celeste",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    ["Deaths"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Celeste.exe+00B5D9E8",
                        Offsets = new[] { 0x0, 0x20, 0x8 },
                        Type = MemoryValueType.Int
                    },
                    ["Strawberries"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Celeste.exe+00B5D9E8",
                        Offsets = new[] { 0x0, 0x20, 0xC },
                        Type = MemoryValueType.Int
                    },
                    ["Dashes"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Celeste.exe+00B5D9E8",
                        Offsets = new[] { 0x0, 0x30, 0x10 },
                        Type = MemoryValueType.Int
                    },
                    ["Stamina"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Celeste.exe+00B5D9E8",
                        Offsets = new[] { 0x0, 0x30, 0x14 },
                        Type = MemoryValueType.Float
                    }
                }
            };
        }

        /// <summary>
        /// Hades (Steam)
        /// </summary>
        public static GameMemoryProfile CreateHadesProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111107"),
                GameTitle = "Hades",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    ["Health"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Hades.exe+0291CDE0",
                        Offsets = new[] { 0x8, 0x280 },
                        Type = MemoryValueType.Float
                    },
                    ["MaxHealth"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Hades.exe+0291CDE0",
                        Offsets = new[] { 0x8, 0x284 },
                        Type = MemoryValueType.Float
                    },
                    ["Darkness"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Hades.exe+02922FB0",
                        Offsets = new[] { 0x0, 0x18, 0x4 },
                        Type = MemoryValueType.Int
                    },
                    ["Gems"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Hades.exe+02922FB0",
                        Offsets = new[] { 0x0, 0x18, 0x8 },
                        Type = MemoryValueType.Int
                    },
                    ["Keys"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Hades.exe+02922FB0",
                        Offsets = new[] { 0x0, 0x18, 0xC },
                        Type = MemoryValueType.Int
                    },
                    ["Nectar"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Hades.exe+02922FB0",
                        Offsets = new[] { 0x0, 0x18, 0x10 },
                        Type = MemoryValueType.Int
                    },
                    ["Ambrosia"] = new MemoryValueDefinition
                    {
                        BaseAddress = "Hades.exe+02922FB0",
                        Offsets = new[] { 0x0, 0x18, 0x14 },
                        Type = MemoryValueType.Int
                    }
                }
            };
        }

        #endregion

        #region Classic/Retro Games (Emulator)

        /// <summary>
        /// Final Fantasy VI (SNES) - for use with SNES emulators
        /// Addresses are relative to WRAM start
        /// </summary>
        public static GameMemoryProfile CreateFinalFantasyVIProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111201"),
                GameTitle = "Final Fantasy VI (SNES)",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    // GP (Gil)
                    ["Gold"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x1860",
                        Type = MemoryValueType.Int
                    },
                    // Character 1 (Terra) HP
                    ["Terra_HP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x1609",
                        Type = MemoryValueType.Int
                    },
                    ["Terra_MaxHP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x160B",
                        Type = MemoryValueType.Int
                    },
                    ["Terra_MP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x160D",
                        Type = MemoryValueType.Int
                    },
                    ["Terra_MaxMP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x160F",
                        Type = MemoryValueType.Int
                    },
                    ["Terra_Level"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x1608",
                        Type = MemoryValueType.Byte
                    },
                    // Inventory slot 1 quantity
                    ["Inventory_Slot1_Qty"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x1869",
                        Type = MemoryValueType.Byte
                    },
                    // Experience
                    ["Terra_Experience"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x1611",
                        Type = MemoryValueType.Int
                    }
                }
            };
        }

        /// <summary>
        /// Chrono Trigger (SNES) - for use with SNES emulators
        /// </summary>
        public static GameMemoryProfile CreateChronoTriggerProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111202"),
                GameTitle = "Chrono Trigger (SNES)",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    ["Gold"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x0C1A",
                        Type = MemoryValueType.Int
                    },
                    // Crono stats
                    ["Crono_HP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x2651",
                        Type = MemoryValueType.Int
                    },
                    ["Crono_MaxHP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x2653",
                        Type = MemoryValueType.Int
                    },
                    ["Crono_MP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x2655",
                        Type = MemoryValueType.Int
                    },
                    ["Crono_MaxMP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x2657",
                        Type = MemoryValueType.Int
                    },
                    ["Crono_Level"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x2650",
                        Type = MemoryValueType.Byte
                    },
                    ["Crono_Power"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x2659",
                        Type = MemoryValueType.Byte
                    },
                    ["Crono_Speed"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0x265B",
                        Type = MemoryValueType.Byte
                    }
                }
            };
        }

        /// <summary>
        /// Pokemon Red/Blue (Game Boy) - for use with GB emulators
        /// </summary>
        public static GameMemoryProfile CreatePokemonRedBlueProfile()
        {
            return new GameMemoryProfile
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111203"),
                GameTitle = "Pokemon Red/Blue (GB)",
                MemoryMap = new Dictionary<string, MemoryValueDefinition>
                {
                    ["Money"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0xD347",
                        Type = MemoryValueType.Int  // BCD encoded, 3 bytes
                    },
                    ["Badges"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0xD356",
                        Type = MemoryValueType.Byte  // Bitmask
                    },
                    // Party Pokemon 1
                    ["Pokemon1_Species"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0xD164",
                        Type = MemoryValueType.Byte
                    },
                    ["Pokemon1_HP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0xD16C",
                        Type = MemoryValueType.Int  // 2 bytes
                    },
                    ["Pokemon1_MaxHP"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0xD18D",
                        Type = MemoryValueType.Int
                    },
                    ["Pokemon1_Level"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0xD18C",
                        Type = MemoryValueType.Byte
                    },
                    // Pokeballs
                    ["Pokeballs"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0xD31E",
                        Type = MemoryValueType.Byte
                    },
                    ["MasterBalls"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0xD31F",
                        Type = MemoryValueType.Byte
                    },
                    // Rare Candy
                    ["RareCandy"] = new MemoryValueDefinition
                    {
                        BaseAddress = "WRAM+0xD320",
                        Type = MemoryValueType.Byte
                    }
                }
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets a memory profile by game title (case-insensitive partial match).
        /// </summary>
        public static GameMemoryProfile? GetProfileByTitle(string title)
        {
            var normalizedTitle = title.ToLowerInvariant();
            foreach (var profile in GetAllProfiles())
            {
                if (profile.GameTitle.ToLowerInvariant().Contains(normalizedTitle))
                {
                    return profile;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a memory profile by game ID.
        /// </summary>
        public static GameMemoryProfile? GetProfileById(Guid gameId)
        {
            foreach (var profile in GetAllProfiles())
            {
                if (profile.GameId == gameId)
                {
                    return profile;
                }
            }
            return null;
        }

        #endregion
    }
}
