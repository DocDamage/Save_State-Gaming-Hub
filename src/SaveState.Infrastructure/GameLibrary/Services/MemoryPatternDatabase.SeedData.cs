using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.GameLibrary.Services;

public partial class MemoryPatternDatabase
{
    private void InitializeKnownPatterns()
    {
        // ========== CELESTE ==========
        AddSignature("Celeste", new GameMemorySignature
        {
            GameTitle = "Celeste",
            Name = "Strawberries",
            Pattern = "8B 45 ?? 89 45 ?? A1 ?? ?? ?? ??",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 175,
            Description = "Total strawberries collected",
            Priority = 10,
            Tags = new() { "collectible", "progression" }
        });

        AddSignature("Celeste", new GameMemorySignature
        {
            GameTitle = "Celeste",
            Name = "Deaths",
            Pattern = "A1 ?? ?? ?? ?? 8B 40 ?? 89 45 ??",
            Offset = 0,
            ValueType = "int32",
            MinValue = 0,
            Description = "Total death count",
            Priority = 5,
            Tags = new() { "stats" }
        });

        AddSignature("Celeste", new GameMemorySignature
        {
            GameTitle = "Celeste",
            Name = "Chapter",
            Pattern = "83 F8 ?? 7E ?? 8B ?? ?? ?? ?? ??",
            Offset = 2,
            ValueType = "int32",
            MinValue = 1,
            MaxValue = 9,
            Description = "Current chapter number",
            Priority = 8,
            Tags = new() { "progression" }
        });

        // ========== HOLLOW KNIGHT ==========
        AddSignature("Hollow Knight", new GameMemorySignature
        {
            GameTitle = "Hollow Knight",
            Name = "Health",
            Pattern = "8B 0D ?? ?? ?? ?? 89 4D ?? 83 7D ?? 00",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 20,
            Description = "Current health masks",
            Priority = 10,
            Tags = new() { "critical", "combat" }
        });

        AddSignature("Hollow Knight", new GameMemorySignature
        {
            GameTitle = "Hollow Knight",
            Name = "Geo",
            Pattern = "A1 ?? ?? ?? ?? 89 45 ?? 8B 0D ?? ?? ?? ??",
            Offset = 0,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 999999,
            Description = "Current currency",
            Priority = 9,
            Tags = new() { "currency" }
        });

        AddSignature("Hollow Knight", new GameMemorySignature
        {
            GameTitle = "Hollow Knight",
            Name = "Soul",
            Pattern = "8B ?? ?? ?? ?? ?? 89 ?? ?? 83 ?? ?? 00",
            Offset = 6,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 99,
            Description = "Current soul amount",
            Priority = 8,
            Tags = new() { "combat" }
        });

        AddSignature("Hollow Knight", new GameMemorySignature
        {
            GameTitle = "Hollow Knight",
            Name = "Charms",
            Pattern = "8B 45 ?? A3 ?? ?? ?? ?? 8B 0D ?? ?? ?? ??",
            Offset = 12,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 40,
            Description = "Charms collected count",
            Priority = 5,
            Tags = new() { "collectible" }
        });

        // ========== STARDEW VALLEY ==========
        AddSignature("Stardew Valley", new GameMemorySignature
        {
            GameTitle = "Stardew Valley",
            Name = "Money",
            Pattern = "8B 45 ?? 89 45 ?? A1 ?? ?? ?? ?? 8B 40 ??",
            Offset = 12,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 999999999,
            Description = "Current money",
            Priority = 10,
            Tags = new() { "currency" }
        });

        AddSignature("Stardew Valley", new GameMemorySignature
        {
            GameTitle = "Stardew Valley",
            Name = "Energy",
            Pattern = "A1 ?? ?? ?? ?? D9 40 ?? D9 5D ?? D9 45 ??",
            Offset = 4,
            ValueType = "float",
            MinFloatValue = 0,
            MaxFloatValue = 538,
            Description = "Current energy level",
            Priority = 9,
            Tags = new() { "critical" }
        });

        AddSignature("Stardew Valley", new GameMemorySignature
        {
            GameTitle = "Stardew Valley",
            Name = "Health",
            Pattern = "8B 0D ?? ?? ?? ?? 89 4D ?? D9 45 ?? D9 5D ??",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 400,
            Description = "Current health",
            Priority = 9,
            Tags = new() { "critical", "combat" }
        });

        AddSignature("Stardew Valley", new GameMemorySignature
        {
            GameTitle = "Stardew Valley",
            Name = "Day",
            Pattern = "A1 ?? ?? ?? ?? 8B 40 ?? 3D ?? ?? ?? ?? 7E ??",
            Offset = 0,
            ValueType = "int32",
            MinValue = 1,
            MaxValue = 99999,
            Description = "Current day number",
            Priority = 5,
            Tags = new() { "time" }
        });

        // ========== HADES ==========
        AddSignature("Hades", new GameMemorySignature
        {
            GameTitle = "Hades",
            Name = "Health",
            Pattern = "F3 0F 10 05 ?? ?? ?? ?? F3 0F 11 45 ??",
            Offset = 8,
            ValueType = "float",
            MinFloatValue = 0,
            MaxFloatValue = 500,
            Description = "Current health",
            Priority = 10,
            Tags = new() { "critical", "combat" }
        });

        AddSignature("Hades", new GameMemorySignature
        {
            GameTitle = "Hades",
            Name = "Gold",
            Pattern = "8B 45 ?? 89 45 ?? 8B 0D ?? ?? ?? ??",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 99999,
            Description = "Current gold (Obols)",
            Priority = 9,
            Tags = new() { "currency" }
        });

        AddSignature("Hades", new GameMemorySignature
        {
            GameTitle = "Hades",
            Name = "Heat",
            Pattern = "A1 ?? ?? ?? ?? 8B 40 ?? 89 45 ??",
            Offset = 0,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 64,
            Description = "Current heat level",
            Priority = 6,
            Tags = new() { "difficulty" }
        });

        // ========== DEAD CELLS ==========
        AddSignature("Dead Cells", new GameMemorySignature
        {
            GameTitle = "Dead Cells",
            Name = "Health",
            Pattern = "F3 0F 10 0D ?? ?? ?? ?? F3 0F 11 4D ??",
            Offset = 4,
            ValueType = "float",
            MinFloatValue = 0,
            MaxFloatValue = 10000,
            Description = "Current health",
            Priority = 10,
            Tags = new() { "critical", "combat" }
        });

        AddSignature("Dead Cells", new GameMemorySignature
        {
            GameTitle = "Dead Cells",
            Name = "Cells",
            Pattern = "8B 0D ?? ?? ?? ?? 89 4D ?? 8B 45 ??",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            Description = "Collected cells",
            Priority = 9,
            Tags = new() { "currency", "progression" }
        });

        AddSignature("Dead Cells", new GameMemorySignature
        {
            GameTitle = "Dead Cells",
            Name = "Gold",
            Pattern = "A1 ?? ?? ?? ?? 89 45 ?? 8B 0D ?? ?? ?? ??",
            Offset = 0,
            ValueType = "int32",
            MinValue = 0,
            Description = "Current gold",
            Priority = 8,
            Tags = new() { "currency" }
        });

        // ========== RISK OF RAIN 2 ==========
        AddSignature("Risk of Rain 2", new GameMemorySignature
        {
            GameTitle = "Risk of Rain 2",
            Name = "Health",
            Pattern = "F3 0F 10 0D ?? ?? ?? ?? F3 0F 11 4D ?? F3 0F 10 05",
            Offset = 4,
            ValueType = "float",
            MinFloatValue = 0,
            MaxFloatValue = 999999,
            Description = "Current health",
            Priority = 10,
            Tags = new() { "critical", "combat" }
        });

        AddSignature("Risk of Rain 2", new GameMemorySignature
        {
            GameTitle = "Risk of Rain 2",
            Name = "Gold",
            Pattern = "8B 45 ?? 89 45 ?? A1 ?? ?? ?? ?? 8B 40 ??",
            Offset = 12,
            ValueType = "int32",
            MinValue = 0,
            Description = "Current gold",
            Priority = 9,
            Tags = new() { "currency" }
        });

        AddSignature("Risk of Rain 2", new GameMemorySignature
        {
            GameTitle = "Risk of Rain 2",
            Name = "Stage",
            Pattern = "A1 ?? ?? ?? ?? 8B 40 ?? 3D ?? ?? ?? ??",
            Offset = 0,
            ValueType = "int32",
            MinValue = 1,
            MaxValue = 100,
            Description = "Current stage",
            Priority = 6,
            Tags = new() { "progression" }
        });

        // ========== SLAY THE SPIRE ==========
        AddSignature("Slay the Spire", new GameMemorySignature
        {
            GameTitle = "Slay the Spire",
            Name = "Health",
            Pattern = "8B 45 ?? 89 45 ?? 8B 0D ?? ?? ?? ?? 83 C1",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 999,
            Description = "Current health",
            Priority = 10,
            Tags = new() { "critical", "combat" }
        });

        AddSignature("Slay the Spire", new GameMemorySignature
        {
            GameTitle = "Slay the Spire",
            Name = "Gold",
            Pattern = "A1 ?? ?? ?? ?? 89 45 ?? 8B 0D ?? ?? ?? ?? 89 4D",
            Offset = 0,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 9999,
            Description = "Current gold",
            Priority = 9,
            Tags = new() { "currency" }
        });

        AddSignature("Slay the Spire", new GameMemorySignature
        {
            GameTitle = "Slay the Spire",
            Name = "Floor",
            Pattern = "8B 0D ?? ?? ?? ?? 89 4D ?? 83 7D ?? 00",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 100,
            Description = "Current floor/level",
            Priority = 7,
            Tags = new() { "progression" }
        });

        // ========== HADES II ==========
        AddSignature("Hades II", new GameMemorySignature
        {
            GameTitle = "Hades II",
            Name = "Health",
            Pattern = "F3 0F 10 05 ?? ?? ?? ?? F3 0F 11 45 ?? F3 0F 10",
            Offset = 8,
            ValueType = "float",
            MinFloatValue = 0,
            MaxFloatValue = 500,
            Description = "Current health",
            Priority = 10,
            Tags = new() { "critical", "combat" }
        });

        AddSignature("Hades II", new GameMemorySignature
        {
            GameTitle = "Hades II",
            Name = "Gold",
            Pattern = "8B 45 ?? 89 45 ?? 8B 0D ?? ?? ?? ?? 89 4D",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            Description = "Current gold",
            Priority = 9,
            Tags = new() { "currency" }
        });

        // ========== CUPHEAD ==========
        AddSignature("Cuphead", new GameMemorySignature
        {
            GameTitle = "Cuphead",
            Name = "Health",
            Pattern = "8B 0D ?? ?? ?? ?? 89 4D ?? 83 7D ?? 00 7E ??",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 5,
            Description = "Current HP",
            Priority = 10,
            Tags = new() { "critical", "combat" }
        });

        AddSignature("Cuphead", new GameMemorySignature
        {
            GameTitle = "Cuphead",
            Name = "Super",
            Pattern = "A1 ?? ?? ?? ?? 8B 40 ?? 89 45 ?? 83 7D",
            Offset = 0,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 5,
            Description = "Super meter cards",
            Priority = 8,
            Tags = new() { "combat" }
        });

        // ========== SHOVEL KNIGHT ==========
        AddSignature("Shovel Knight", new GameMemorySignature
        {
            GameTitle = "Shovel Knight",
            Name = "Health",
            Pattern = "8B 45 ?? 89 45 ?? 8B 0D ?? ?? ?? ?? 89 4D",
            Offset = 8,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 10,
            Description = "Current health",
            Priority = 10,
            Tags = new() { "critical", "combat" }
        });

        AddSignature("Shovel Knight", new GameMemorySignature
        {
            GameTitle = "Shovel Knight",
            Name = "Gold",
            Pattern = "A1 ?? ?? ?? ?? 89 45 ?? 8B 0D ?? ?? ?? ??",
            Offset = 0,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 999999,
            Description = "Current gold",
            Priority = 9,
            Tags = new() { "currency" }
        });

        // ========== ORI AND THE BLIND FOREST ==========
        AddSignature("Ori and the Blind Forest", new GameMemorySignature
        {
            GameTitle = "Ori and the Blind Forest",
            Name = "Health",
            Pattern = "F3 0F 10 0D ?? ?? ?? ?? F3 0F 11 4D ??",
            Offset = 4,
            ValueType = "float",
            MinFloatValue = 0,
            MaxFloatValue = 1000,
            Description = "Current health",
            Priority = 10,
            Tags = new() { "critical" }
        });

        AddSignature("Ori and the Blind Forest", new GameMemorySignature
        {
            GameTitle = "Ori and the Blind Forest",
            Name = "Energy",
            Pattern = "8B 0D ?? ?? ?? ?? 89 4D ?? D9 45 ?? D9 5D",
            Offset = 8,
            ValueType = "float",
            MinFloatValue = 0,
            MaxFloatValue = 100,
            Description = "Current energy",
            Priority = 8,
            Tags = new() { "combat" }
        });

        // ========== UNIVERSAL PATTERNS ==========
        // These work across many games for common value types
        AddSignature("*", new GameMemorySignature
        {
            GameTitle = "*",
            Name = "CommonHealthInt",
            Pattern = "?? ?? ?? ??",
            Offset = 0,
            ValueType = "int32",
            MinValue = 1,
            MaxValue = 1000,
            Description = "Common health value pattern (int32)",
            Priority = 1,
            Tags = new() { "universal", "health" }
        });

        AddSignature("*", new GameMemorySignature
        {
            GameTitle = "*",
            Name = "CommonHealthFloat",
            Pattern = "?? ?? ?? ??",
            Offset = 0,
            ValueType = "float",
            MinFloatValue = 1,
            MaxFloatValue = 10000,
            Description = "Common health value pattern (float)",
            Priority = 1,
            Tags = new() { "universal", "health" }
        });

        AddSignature("*", new GameMemorySignature
        {
            GameTitle = "*",
            Name = "CommonScore",
            Pattern = "?? ?? ?? ?? ?? ?? ?? ??",
            Offset = 0,
            ValueType = "int32",
            MinValue = 0,
            MaxValue = 999999999,
            Description = "Common score/currency pattern",
            Priority = 1,
            Tags = new() { "universal", "score" }
        });

        _logger.LogInformation("Initialized memory pattern database with {Count} game signatures covering {Games} games",
            Count, _gameSignatures.Count);
    }
}
