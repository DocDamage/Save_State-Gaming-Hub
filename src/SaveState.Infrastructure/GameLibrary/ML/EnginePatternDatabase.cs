using System.Diagnostics;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.ML;

/// <summary>
/// Database of known memory patterns for specific game engines.
/// </summary>
public sealed class EnginePatternDatabase
{
    private readonly Dictionary<GameEngine, List<EngineMemoryPattern>> _patterns;

    public EnginePatternDatabase()
    {
        _patterns = InitializePatterns();
    }

    public List<EngineMemoryPattern> GetPatternsForEngine(GameEngine engine)
    {
        return _patterns.TryGetValue(engine, out var patterns)
            ? patterns.ToList()
            : new List<EngineMemoryPattern>();
    }

    public GameEngine DetectEngine(Process process)
    {
        try
        {
            var modules = process.Modules.Cast<ProcessModule>()
                .Select(m => m.ModuleName.ToLowerInvariant())
                .ToList();

            if (modules.Any(m => m.Contains("unityplayer")))
                return GameEngine.Unity;

            if (modules.Any(m => m.Contains("ue4") || m.Contains("unreal")))
                return GameEngine.Unreal;

            if (modules.Any(m => m.Contains("ue5")))
                return GameEngine.Unreal;

            if (modules.Any(m => m.Contains("godot")))
                return GameEngine.Godot;

            if (modules.Any(m => m.Contains("gm") || m.Contains("gamemaker")))
                return GameEngine.GameMaker;

            if (modules.Any(m => m.Contains("crysystem")))
                return GameEngine.CryEngine;

            if (modules.Any(m => m.Contains("engine") && m.Contains("source")))
                return GameEngine.Source2;

            if (modules.Any(m => m.Contains("engine") || m.Contains("client") || m.Contains("server")))
                return GameEngine.Source;

            if (modules.Any(m => m.Contains("idtech") || m.Contains("doom") || m.Contains("rage")))
                return GameEngine.IdTech;

            if (modules.Any(m => m.Contains("frostbite")))
                return GameEngine.Frostbite;

            return GameEngine.Custom;
        }
        catch
        {
            return GameEngine.Custom;
        }
    }

    private static Dictionary<GameEngine, List<EngineMemoryPattern>> InitializePatterns()
    {
        return new Dictionary<GameEngine, List<EngineMemoryPattern>>
        {
            [GameEngine.Unity] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x20,
                    ValueType = "float",
                    Engine = GameEngine.Unity,
                    Category = "Health",
                    SuccessRate = 65
                },
                new EngineMemoryPattern
                {
                    Name = "PlayerPosition",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x10,
                    ValueType = "float",
                    Engine = GameEngine.Unity,
                    Category = "Position",
                    SuccessRate = 70
                },
                new EngineMemoryPattern
                {
                    Name = "Currency",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x28,
                    ValueType = "int32",
                    Engine = GameEngine.Unity,
                    Category = "Economy",
                    SuccessRate = 55
                }
            },
            [GameEngine.Unreal] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "A0 ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x2B0,
                    ValueType = "float",
                    Engine = GameEngine.Unreal,
                    Category = "Health",
                    SuccessRate = 72
                },
                new EngineMemoryPattern
                {
                    Name = "PlayerStamina",
                    Pattern = "A0 ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x2B4,
                    ValueType = "float",
                    Engine = GameEngine.Unreal,
                    Category = "Stamina",
                    SuccessRate = 60
                },
                new EngineMemoryPattern
                {
                    Name = "PlayerLocation",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x60,
                    ValueType = "float",
                    Engine = GameEngine.Unreal,
                    Category = "Position",
                    SuccessRate = 75
                }
            },
            [GameEngine.Source] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0xA0,
                    ValueType = "int32",
                    Engine = GameEngine.Source,
                    Category = "Health",
                    SuccessRate = 80
                },
                new EngineMemoryPattern
                {
                    Name = "PlayerArmor",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0xA4,
                    ValueType = "int32",
                    Engine = GameEngine.Source,
                    Category = "Armor",
                    SuccessRate = 75
                },
                new EngineMemoryPattern
                {
                    Name = "CurrentWeaponAmmo",
                    Pattern = "?? ?? ?? ??",
                    Offset = 0x1D4,
                    ValueType = "int32",
                    Engine = GameEngine.Source,
                    Category = "Ammo",
                    SuccessRate = 78
                }
            },
            [GameEngine.Godot] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x40,
                    ValueType = "float",
                    Engine = GameEngine.Godot,
                    Category = "Health",
                    SuccessRate = 50
                }
            },
            [GameEngine.GameMaker] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "?? ?? ?? ??",
                    Offset = 0x8,
                    ValueType = "int32",
                    Engine = GameEngine.GameMaker,
                    Category = "Health",
                    SuccessRate = 45
                }
            }
        };
    }
}
