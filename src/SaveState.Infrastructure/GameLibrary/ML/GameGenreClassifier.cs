using System.Diagnostics;
using System.Text.RegularExpressions;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.ML;

/// <summary>
/// Classifies games by genre based on process characteristics, window titles,
/// loaded modules, and memory patterns. Uses heuristic rules and keyword matching.
/// </summary>
public sealed class GameGenreClassifier
{
    private readonly Dictionary<string, GameGenre> _processNameMappings;
    private readonly Dictionary<string, GameGenre> _windowTitleKeywords;
    private readonly Dictionary<string, GameGenre> _moduleMappings;
    private readonly Dictionary<GameGenre, List<string>> _genreIndicators;

    /// <summary>
    /// Initializes a new instance of the genre classifier with built-in mappings.
    /// </summary>
    public GameGenreClassifier()
    {
        _processNameMappings = InitializeProcessNameMappings();
        _windowTitleKeywords = InitializeWindowTitleKeywords();
        _moduleMappings = InitializeModuleMappings();
        _genreIndicators = InitializeGenreIndicators();
    }

    /// <summary>
    /// Classifies a game based on process information and context.
    /// </summary>
    /// <param name="context">Classification context containing process information.</param>
    /// <returns>The detected game genre.</returns>
    public GameGenre ClassifyGame(GenreClassificationContext context)
    {
        if (context is null)
            return GameGenre.Unknown;

        var scores = new Dictionary<GameGenre, int>();

        // Score based on process name
        ScoreProcessName(context.ProcessName, scores);

        // Score based on window title
        if (!string.IsNullOrWhiteSpace(context.WindowTitle))
        {
            ScoreWindowTitle(context.WindowTitle, scores);
        }

        // Score based on loaded modules
        if (context.LoadedModules?.Count > 0)
        {
            ScoreLoadedModules(context.LoadedModules, scores);
        }

        // Score based on game title
        if (!string.IsNullOrWhiteSpace(context.GameTitle))
        {
            ScoreGameTitle(context.GameTitle, scores);
        }

        // Score based on engine
        ScoreEngine(context.Engine, scores);

        // Return genre with highest score, or Unknown if no clear match
        var bestMatch = scores.OrderByDescending(s => s.Value).FirstOrDefault();
        return bestMatch.Value > 0 ? bestMatch.Key : GameGenre.Unknown;
    }

    /// <summary>
    /// Classifies a game from a running process.
    /// </summary>
    /// <param name="process">The process to analyze.</param>
    /// <param name="gameTitle">Optional known game title.</param>
    /// <returns>The detected game genre.</returns>
    public GameGenre ClassifyGame(Process process, string? gameTitle = null)
    {
        if (process is null)
            return GameGenre.Unknown;

        var context = new GenreClassificationContext
        {
            ProcessName = process.ProcessName.ToLowerInvariant(),
            WindowTitle = process.MainWindowTitle,
            GameTitle = gameTitle,
            LoadedModules = GetModuleNames(process)
        };

        return ClassifyGame(context);
    }

    /// <summary>
    /// Gets the recommended memory pattern templates for a given genre.
    /// Returns templates most likely to be relevant for games of this genre.
    /// </summary>
    /// <param name="genre">The game genre.</param>
    /// <returns>List of recommended pattern template names.</returns>
    public List<string> GetRecommendedTemplates(GameGenre genre)
    {
        return genre switch
        {
            GameGenre.FirstPersonShooter => new List<string>
            {
                "Health", "Armor", "Ammo", "Shield", "Score", "Kills", "Deaths"
            },
            GameGenre.ThirdPersonShooter => new List<string>
            {
                "Health", "Armor", "Ammo", "Shield", "Score", "Kills"
            },
            GameGenre.RolePlayingGame => new List<string>
            {
                "Health", "Mana", "Experience", "Level", "Currency", "Strength", "Dexterity", "Intelligence"
            },
            GameGenre.ActionRPG => new List<string>
            {
                "Health", "Stamina", "Experience", "Level", "Currency", "Score"
            },
            GameGenre.Platformer => new List<string>
            {
                "Health", "Lives", "Score", "Position", "Timer"
            },
            GameGenre.Metroidvania => new List<string>
            {
                "Health", "Energy", "Position", "MapProgress", "Abilities"
            },
            GameGenre.Fighting => new List<string>
            {
                "Health", "SuperMeter", "Timer", "Score", "RoundsWon"
            },
            GameGenre.Racing => new List<string>
            {
                "Speed", "Position", "LapTime", "Timer", "Score"
            },
            GameGenre.Strategy => new List<string>
            {
                "Resources", "Units", "Population", "Score", "TurnNumber"
            },
            GameGenre.Roguelike => new List<string>
            {
                "Health", "Currency", "Level", "Score", "Seeds"
            },
            GameGenre.Survival => new List<string>
            {
                "Health", "Hunger", "Thirst", "Stamina", "Temperature"
            },
            GameGenre.Simulation => new List<string>
            {
                "Currency", "Population", "Happiness", "Resources"
            },
            GameGenre.Sports => new List<string>
            {
                "Score", "Timer", "Stamina", "Position", "Stats"
            },
            GameGenre.Puzzle => new List<string>
            {
                "Score", "Moves", "Timer", "Level"
            },
            GameGenre.VisualNovel => new List<string>
            {
                "SceneProgress", "Choices", "Flags"
            },
            _ => new List<string> { "Health", "Score", "Timer" }
        };
    }

    /// <summary>
    /// Gets the priority order for scanning patterns based on genre.
    /// Higher priority patterns should be scanned first for better UX.
    /// </summary>
    /// <param name="genre">The game genre.</param>
    /// <returns>Ordered list of pattern names by priority.</returns>
    public List<string> GetScanPriorityOrder(GameGenre genre)
    {
        var templates = GetRecommendedTemplates(genre);
        
        // Move health to front if present (most commonly wanted)
        if (templates.Contains("Health"))
        {
            templates.Remove("Health");
            templates.Insert(0, "Health");
        }

        return templates;
    }

    /// <summary>
    /// Gets typical value ranges for patterns in a given genre.
    /// </summary>
    /// <param name="genre">The game genre.</param>
    /// <param name="patternType">The pattern type.</param>
    /// <returns>Typical min/max values, or null if unknown.</returns>
    public (double Min, double Max, string Type)? GetTypicalValueRange(GameGenre genre, string patternType)
    {
        return (genre, patternType.ToLowerInvariant()) switch
        {
            // FPS Games
            (GameGenre.FirstPersonShooter, "health") => (1, 200, "int"),
            (GameGenre.FirstPersonShooter, "armor") => (0, 200, "int"),
            (GameGenre.FirstPersonShooter, "ammo") => (0, 999, "int"),
            (GameGenre.FirstPersonShooter, "shield") => (0, 200, "int"),

            // RPG Games
            (GameGenre.RolePlayingGame, "health") => (1, 99999, "int"),
            (GameGenre.RolePlayingGame, "mana") => (0, 9999, "int"),
            (GameGenre.RolePlayingGame, "experience") => (0, 999999999, "int"),
            (GameGenre.RolePlayingGame, "level") => (1, 999, "int"),
            (GameGenre.RolePlayingGame, "currency") => (0, 999999999, "int"),

            // Platformer
            (GameGenre.Platformer, "health") => (0, 10, "int"),
            (GameGenre.Platformer, "lives") => (0, 99, "int"),
            (GameGenre.Platformer, "score") => (0, 9999999, "int"),

            // Fighting
            (GameGenre.Fighting, "health") => (0, 200, "int"),
            (GameGenre.Fighting, "supermeter") => (0, 300, "int"),
            (GameGenre.Fighting, "timer") => (0, 99, "float"),

            // Racing
            (GameGenre.Racing, "speed") => (0, 500, "float"),
            (GameGenre.Racing, "laptime") => (0, 600, "float"),

            // Survival
            (GameGenre.Survival, "hunger") => (0, 100, "float"),
            (GameGenre.Survival, "thirst") => (0, 100, "float"),
            (GameGenre.Survival, "temperature") => (-50, 100, "float"),

            _ => null
        };
    }

    private void ScoreProcessName(string processName, Dictionary<GameGenre, int> scores)
    {
        var normalized = processName.ToLowerInvariant().Replace(".exe", "");

        foreach (var mapping in _processNameMappings)
        {
            if (normalized.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase))
            {
                scores[mapping.Value] = scores.GetValueOrDefault(mapping.Value) + 3;
            }
        }
    }

    private void ScoreWindowTitle(string windowTitle, Dictionary<GameGenre, int> scores)
    {
        var normalized = windowTitle.ToLowerInvariant();

        foreach (var keyword in _windowTitleKeywords)
        {
            if (normalized.Contains(keyword.Key, StringComparison.OrdinalIgnoreCase))
            {
                scores[keyword.Value] = scores.GetValueOrDefault(keyword.Value) + 2;
            }
        }
    }

    private void ScoreLoadedModules(List<string> modules, Dictionary<GameGenre, int> scores)
    {
        foreach (var module in modules)
        {
            var normalized = module.ToLowerInvariant();
            foreach (var mapping in _moduleMappings)
            {
                if (normalized.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase))
                {
                    scores[mapping.Value] = scores.GetValueOrDefault(mapping.Value) + 2;
                }
            }
        }
    }

    private void ScoreGameTitle(string gameTitle, Dictionary<GameGenre, int> scores)
    {
        var normalized = gameTitle.ToLowerInvariant();

        foreach (var indicator in _genreIndicators)
        {
            foreach (var keyword in indicator.Value)
            {
                if (normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    scores[indicator.Key] = scores.GetValueOrDefault(indicator.Key) + 2;
                }
            }
        }
    }

    private void ScoreEngine(GameEngine engine, Dictionary<GameGenre, int> scores)
    {
        // Certain engines are more common in specific genres
        switch (engine)
        {
            case GameEngine.Unreal:
                scores[GameGenre.FirstPersonShooter] = scores.GetValueOrDefault(GameGenre.FirstPersonShooter) + 1;
                scores[GameGenre.ThirdPersonShooter] = scores.GetValueOrDefault(GameGenre.ThirdPersonShooter) + 1;
                break;
            case GameEngine.Unity:
                scores[GameGenre.Platformer] = scores.GetValueOrDefault(GameGenre.Platformer) + 1;
                scores[GameGenre.Roguelike] = scores.GetValueOrDefault(GameGenre.Roguelike) + 1;
                break;
            case GameEngine.Source:
            case GameEngine.Source2:
                scores[GameGenre.FirstPersonShooter] = scores.GetValueOrDefault(GameGenre.FirstPersonShooter) + 2;
                break;
            case GameEngine.CryEngine:
                scores[GameGenre.FirstPersonShooter] = scores.GetValueOrDefault(GameGenre.FirstPersonShooter) + 1;
                break;
        }
    }

    private static List<string> GetModuleNames(Process process)
    {
        try
        {
            return process.Modules
                .Cast<ProcessModule>()
                .Select(m => m.ModuleName.ToLowerInvariant())
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static Dictionary<string, GameGenre> InitializeProcessNameMappings()
    {
        return new Dictionary<string, GameGenre>(StringComparer.OrdinalIgnoreCase)
        {
            // FPS Games
            ["cod"] = GameGenre.FirstPersonShooter,
            ["csgo"] = GameGenre.FirstPersonShooter,
            ["cs2"] = GameGenre.FirstPersonShooter,
            ["valorant"] = GameGenre.FirstPersonShooter,
            ["apex"] = GameGenre.FirstPersonShooter,
            ["overwatch"] = GameGenre.FirstPersonShooter,
            ["doom"] = GameGenre.FirstPersonShooter,
            ["quake"] = GameGenre.FirstPersonShooter,
            ["half-life"] = GameGenre.FirstPersonShooter,
            ["halo"] = GameGenre.FirstPersonShooter,
            ["battlefield"] = GameGenre.FirstPersonShooter,
            ["titanfall"] = GameGenre.FirstPersonShooter,
            ["rainbow6"] = GameGenre.FirstPersonShooter,
            ["r6"] = GameGenre.FirstPersonShooter,
            ["teamfortress"] = GameGenre.FirstPersonShooter,
            ["tf2"] = GameGenre.FirstPersonShooter,
            ["borderlands"] = GameGenre.FirstPersonShooter,
            ["destiny"] = GameGenre.FirstPersonShooter,
            ["warframe"] = GameGenre.FirstPersonShooter,

            // Third Person Shooters
            ["gears"] = GameGenre.ThirdPersonShooter,
            ["division"] = GameGenre.ThirdPersonShooter,
            ["ghostrecon"] = GameGenre.ThirdPersonShooter,
            ["masseffect"] = GameGenre.ThirdPersonShooter,

            // RPG Games
            ["skyrim"] = GameGenre.RolePlayingGame,
            ["fallout"] = GameGenre.RolePlayingGame,
            ["witcher"] = GameGenre.RolePlayingGame,
            ["dragonage"] = GameGenre.RolePlayingGame,
            ["baldur"] = GameGenre.RolePlayingGame,
            ["divinity"] = GameGenre.RolePlayingGame,
            ["pathofexile"] = GameGenre.RolePlayingGame,
            ["poe"] = GameGenre.RolePlayingGame,
            ["diablo"] = GameGenre.RolePlayingGame,
            ["torchlight"] = GameGenre.RolePlayingGame,
            ["grimdawn"] = GameGenre.RolePlayingGame,
            ["pokemon"] = GameGenre.RolePlayingGame,
            ["finalfantasy"] = GameGenre.RolePlayingGame,
            ["ff"] = GameGenre.RolePlayingGame,
            ["chronotrigger"] = GameGenre.RolePlayingGame,
            ["earthbound"] = GameGenre.RolePlayingGame,
            ["undertale"] = GameGenre.RolePlayingGame,
            ["deltarune"] = GameGenre.RolePlayingGame,

            // Action RPG
            ["souls"] = GameGenre.ActionRPG,
            ["eldenring"] = GameGenre.ActionRPG,
            ["sekiro"] = GameGenre.ActionRPG,
            ["nioh"] = GameGenre.ActionRPG,
            ["monsterhunter"] = GameGenre.ActionRPG,
            ["mhw"] = GameGenre.ActionRPG,
            ["darksouls"] = GameGenre.ActionRPG,
            ["bloodborne"] = GameGenre.ActionRPG,

            // Platformers
            ["mario"] = GameGenre.Platformer,
            ["sonic"] = GameGenre.Platformer,
            ["celeste"] = GameGenre.Platformer,
            ["hollowknight"] = GameGenre.Metroidvania,
            ["ori"] = GameGenre.Metroidvania,
            ["guacamelee"] = GameGenre.Metroidvania,
            ["axiomverge"] = GameGenre.Metroidvania,
            ["cavestory"] = GameGenre.Platformer,
            ["shovelknight"] = GameGenre.Platformer,
            ["cuphead"] = GameGenre.Platformer,
            ["rayman"] = GameGenre.Platformer,
            ["crash"] = GameGenre.Platformer,
            ["spyro"] = GameGenre.Platformer,

            // Fighting Games
            ["mugen"] = GameGenre.Fighting,
            ["ikemen"] = GameGenre.Fighting,
            ["streetfighter"] = GameGenre.Fighting,
            ["tekken"] = GameGenre.Fighting,
            ["soulcalibur"] = GameGenre.Fighting,
            ["mortalkombat"] = GameGenre.Fighting,
            ["mk"] = GameGenre.Fighting,
            ["guiltygear"] = GameGenre.Fighting,
            ["blazblue"] = GameGenre.Fighting,
            ["kof"] = GameGenre.Fighting,
            ["smash"] = GameGenre.Fighting,
            ["rivals"] = GameGenre.Fighting,
            ["skullgirls"] = GameGenre.Fighting,
            ["dragonball"] = GameGenre.Fighting,
            ["dbz"] = GameGenre.Fighting,

            // Racing
            ["forza"] = GameGenre.Racing,
            ["granturismo"] = GameGenre.Racing,
            ["needforspeed"] = GameGenre.Racing,
            ["nfs"] = GameGenre.Racing,
            ["burnout"] = GameGenre.Racing,
            ["mario_kart"] = GameGenre.Racing,
            ["fzero"] = GameGenre.Racing,
            ["wipeout"] = GameGenre.Racing,

            // Strategy
            ["starcraft"] = GameGenre.Strategy,
            ["ageofempires"] = GameGenre.Strategy,
            ["aoe"] = GameGenre.Strategy,
            ["civilization"] = GameGenre.Strategy,
            ["civ"] = GameGenre.Strategy,
            ["totalwar"] = GameGenre.Strategy,
            ["xcom"] = GameGenre.Strategy,
            ["fireemblem"] = GameGenre.Strategy,
            ["advancewars"] = GameGenre.Strategy,
            ["commandandconquer"] = GameGenre.Strategy,
            ["redalert"] = GameGenre.Strategy,
            ["warcraft"] = GameGenre.Strategy,

            // Roguelike
            ["bindingofisaac"] = GameGenre.Roguelike,
            ["isaac"] = GameGenre.Roguelike,
            ["spelunky"] = GameGenre.Roguelike,
            ["nuclearthrone"] = GameGenre.Roguelike,
            ["enterthegungeon"] = GameGenre.Roguelike,
            ["gungeon"] = GameGenre.Roguelike,
            ["slaythespire"] = GameGenre.Roguelike,
            ["deadcells"] = GameGenre.Roguelike,
            ["hades"] = GameGenre.Roguelike,
            ["riskofrain"] = GameGenre.Roguelike,
            ["roguelegacy"] = GameGenre.Roguelike,

            // Survival
            ["minecraft"] = GameGenre.Survival,
            ["terraria"] = GameGenre.Survival,
            ["subnautica"] = GameGenre.Survival,
            ["theforest"] = GameGenre.Survival,
            ["greenhell"] = GameGenre.Survival,
            ["stranded"] = GameGenre.Survival,
            ["raft"] = GameGenre.Survival,
            ["dontstarve"] = GameGenre.Survival,
            ["7daystodie"] = GameGenre.Survival,
            ["rust"] = GameGenre.Survival,
            ["ark"] = GameGenre.Survival,

            // Simulation
            ["sims"] = GameGenre.Simulation,
            ["simcity"] = GameGenre.Simulation,
            ["cities"] = GameGenre.Simulation,
            ["factorio"] = GameGenre.Simulation,
            ["satisfactory"] = GameGenre.Simulation,
            ["dyson"] = GameGenre.Simulation,
            ["stardew"] = GameGenre.Simulation,
            ["harvestmoon"] = GameGenre.Simulation,
            ["rancher"] = GameGenre.Simulation,

            // Sports
            ["fifa"] = GameGenre.Sports,
            ["madden"] = GameGenre.Sports,
            ["nba"] = GameGenre.Sports,
            ["tonyhawk"] = GameGenre.Sports,
            ["skate"] = GameGenre.Sports,

            // Puzzle
            ["tetris"] = GameGenre.Puzzle,
            ["puzzle"] = GameGenre.Puzzle,
            ["portal"] = GameGenre.Puzzle,
            ["baba"] = GameGenre.Puzzle,
            ["witness"] = GameGenre.Puzzle,
            ["talos"] = GameGenre.Puzzle,

            // Visual Novel
            ["danganronpa"] = GameGenre.VisualNovel,
            ["aceattorney"] = GameGenre.VisualNovel,
            ["steinsgate"] = GameGenre.VisualNovel,
            ["clannad"] = GameGenre.VisualNovel,
            [" DDLC "] = GameGenre.VisualNovel,
        };
    }

    private static Dictionary<string, GameGenre> InitializeWindowTitleKeywords()
    {
        return new Dictionary<string, GameGenre>(StringComparer.OrdinalIgnoreCase)
        {
            ["shooter"] = GameGenre.FirstPersonShooter,
            ["fps"] = GameGenre.FirstPersonShooter,
            ["rpg"] = GameGenre.RolePlayingGame,
            ["role-playing"] = GameGenre.RolePlayingGame,
            ["platformer"] = GameGenre.Platformer,
            ["fighting"] = GameGenre.Fighting,
            ["racing"] = GameGenre.Racing,
            ["strategy"] = GameGenre.Strategy,
            ["rogue"] = GameGenre.Roguelike,
            ["survival"] = GameGenre.Survival,
            ["simulation"] = GameGenre.Simulation,
            ["sports"] = GameGenre.Sports,
            ["puzzle"] = GameGenre.Puzzle,
            ["visual novel"] = GameGenre.VisualNovel,
            ["vn"] = GameGenre.VisualNovel,
        };
    }

    private static Dictionary<string, GameGenre> InitializeModuleMappings()
    {
        return new Dictionary<string, GameGenre>(StringComparer.OrdinalIgnoreCase)
        {
            // Source Engine
            ["engine.dll"] = GameGenre.FirstPersonShooter,
            ["client.dll"] = GameGenre.FirstPersonShooter,
            ["server.dll"] = GameGenre.FirstPersonShooter,

            // Unreal Engine
            ["unrealengine"] = GameGenre.ThirdPersonShooter,
            ["ue4"] = GameGenre.ThirdPersonShooter,
            ["ue5"] = GameGenre.ThirdPersonShooter,

            // Unity
            ["unityplayer"] = GameGenre.Platformer,

            // RPG Maker
            ["rgss"] = GameGenre.RolePlayingGame,
            ["rpgmv"] = GameGenre.RolePlayingGame,

            // Fighting Game Frameworks
            ["mugen"] = GameGenre.Fighting,
            ["ikemen"] = GameGenre.Fighting,
        };
    }

    private static Dictionary<GameGenre, List<string>> InitializeGenreIndicators()
    {
        return new Dictionary<GameGenre, List<string>>
        {
            [GameGenre.FirstPersonShooter] = new()
            {
                "shooter", "fps", "call of duty", "counter-strike", "battlefield",
                "valorant", "apex", "overwatch", "doom", "quake", "half-life"
            },
            [GameGenre.RolePlayingGame] = new()
            {
                "rpg", "elder scrolls", "final fantasy", "pokemon", "dragon quest",
                "chrono", "persona", "shin megami", "baldur", "divinity"
            },
            [GameGenre.Platformer] = new()
            {
                "mario", "sonic", "platform", "jump", "castle", "kong"
            },
            [GameGenre.Metroidvania] = new()
            {
                "metroid", "castlevania", "hollow knight", "metroidvania", "exploration"
            },
            [GameGenre.Fighting] = new()
            {
                "fighter", "fighting", "street fighter", "tekken", "mortal kombat",
                "smash bros", "guilty gear", "king of fighters"
            },
            [GameGenre.Racing] = new()
            {
                "racing", "kart", "formula", "nascar", "rally", "speed"
            },
            [GameGenre.Strategy] = new()
            {
                "strategy", "rts", "turn-based", "tactics", "empire", "war"
            },
            [GameGenre.Roguelike] = new()
            {
                "rogue", "procedural", "permadeath", "dungeon", "spire"
            },
            [GameGenre.Survival] = new()
            {
                "survival", "craft", "zombie", "forest", "island"
            },
            [GameGenre.Simulation] = new()
            {
                "sim", "simulation", "tycoon", "builder", "factory", "city"
            },
            [GameGenre.Sports] = new()
            {
                "football", "soccer", "basketball", "baseball", "skate", "golf"
            },
        };
    }
}
