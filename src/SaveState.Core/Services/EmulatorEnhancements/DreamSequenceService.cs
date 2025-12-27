using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.EmulatorEnhancements
{
    public enum DreamMood
    {
        Nostalgic,    // Sepia tones, old-timey effects
        Surreal,      // Weird color shifts, impossible geometry
        Nightmare,    // Dark, distorted, threatening
        Euphoric,     // Bright, colorful, positive
        Chaotic       // Digital artifacts, corruption, randomness
    }

    public enum ElementTransform
    {
        Normal,
        Scaled,       // Size modified
        Inverted,     // Colors/behavior inverted
        Glitched,     // Visual corruption
        Fused         // Combined with another element
    }

    public class DreamElement
    {
        public string Id { get; set; } = string.Empty;
        public string SourceGame { get; set; } = string.Empty;
        public string ElementType { get; set; } = string.Empty;
        public ElementTransform Transform { get; set; } = ElementTransform.Normal;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class DreamLevel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DreamCode { get; set; } = string.Empty;  // 8-char shareable code
        public DreamMood Mood { get; set; }
        public int Difficulty { get; set; } = 5;  // 1-10
        public List<string> SourceGames { get; set; } = new();
        public List<DreamElement> Elements { get; set; } = new();
        public DateTime Generated { get; set; }
        public int Seed { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class DreamSequenceService
    {
        private readonly List<DreamElement> _elementLibrary = new();
        private readonly List<DreamLevel> _generatedLevels = new();
        private readonly string _levelsPath;
        private readonly ILlmService? _llmService;
        private readonly IAdvancedAiService? _advancedAi;
        private readonly Random _rand = new();
        private const int MaxFavorites = 50;

        public DreamSequenceService(ILlmService? llmService = null, IAdvancedAiService? advancedAi = null)
        {
            _llmService = llmService ?? new LlmService();
            _advancedAi = advancedAi;
            _levelsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "dream_levels");
            if (!Directory.Exists(_levelsPath)) Directory.CreateDirectory(_levelsPath);
            
            InitializeElementLibrary();
            LoadGeneratedLevels();
        }

        private void InitializeElementLibrary()
        {
            _elementLibrary.AddRange(new[]
            {
                // Mario elements
                new DreamElement { Id = "goomba", SourceGame = "Super Mario Bros", ElementType = "enemy", Properties = { ["speed"] = 1.0, ["damage"] = 1, ["behavior"] = "walk" } },
                new DreamElement { Id = "koopa", SourceGame = "Super Mario Bros", ElementType = "enemy", Properties = { ["speed"] = 1.5, ["damage"] = 1, ["behavior"] = "shell" } },
                new DreamElement { Id = "pipe", SourceGame = "Super Mario Bros", ElementType = "platform", Properties = { ["warp"] = true } },
                new DreamElement { Id = "brick", SourceGame = "Super Mario Bros", ElementType = "platform", Properties = { ["breakable"] = true } },
                new DreamElement { Id = "coin", SourceGame = "Super Mario Bros", ElementType = "powerup", Properties = { ["value"] = 1 } },
                new DreamElement { Id = "mushroom", SourceGame = "Super Mario Bros", ElementType = "powerup", Properties = { ["effect"] = "grow" } },
                
                // Sonic elements
                new DreamElement { Id = "ring", SourceGame = "Sonic", ElementType = "powerup", Properties = { ["value"] = 1, ["protection"] = true } },
                new DreamElement { Id = "spring", SourceGame = "Sonic", ElementType = "platform", Properties = { ["bounce"] = 3.0 } },
                new DreamElement { Id = "loop", SourceGame = "Sonic", ElementType = "platform", Properties = { ["speed_required"] = true } },
                new DreamElement { Id = "badnik", SourceGame = "Sonic", ElementType = "enemy", Properties = { ["speed"] = 2.0, ["damage"] = 1 } },
                
                // Zelda elements
                new DreamElement { Id = "heart", SourceGame = "Legend of Zelda", ElementType = "powerup", Properties = { ["heal"] = 1 } },
                new DreamElement { Id = "octorok", SourceGame = "Legend of Zelda", ElementType = "enemy", Properties = { ["projectile"] = true } },
                new DreamElement { Id = "bush", SourceGame = "Legend of Zelda", ElementType = "obstacle", Properties = { ["cuttable"] = true, ["drops"] = true } },
                
                // Mega Man elements
                new DreamElement { Id = "spike", SourceGame = "Mega Man", ElementType = "obstacle", Properties = { ["damage"] = 999, ["instant_death"] = true } },
                new DreamElement { Id = "met", SourceGame = "Mega Man", ElementType = "enemy", Properties = { ["shielded"] = true } },
                new DreamElement { Id = "disappearing_block", SourceGame = "Mega Man", ElementType = "platform", Properties = { ["pattern"] = true } },
                
                // Metroid elements
                new DreamElement { Id = "metroid", SourceGame = "Metroid", ElementType = "enemy", Properties = { ["flying"] = true, ["damage"] = 2, ["latch"] = true } },
                new DreamElement { Id = "energy_tank", SourceGame = "Metroid", ElementType = "powerup", Properties = { ["health_upgrade"] = true } },
                new DreamElement { Id = "morph_ball_tunnel", SourceGame = "Metroid", ElementType = "platform", Properties = { ["requires_morph"] = true } },
                
                // Castlevania elements
                new DreamElement { Id = "candle", SourceGame = "Castlevania", ElementType = "obstacle", Properties = { ["drops"] = true } },
                new DreamElement { Id = "skeleton", SourceGame = "Castlevania", ElementType = "enemy", Properties = { ["throws_bones"] = true } },
                new DreamElement { Id = "staircase", SourceGame = "Castlevania", ElementType = "platform", Properties = { ["fixed_movement"] = true } },
            });
        }

        public async Task<DreamLevel> GenerateLevelAsync(DreamMood mood, List<string>? sourceGames = null, int difficulty = 5, int? seed = null)
        {
            int actualSeed = seed ?? _rand.Next();
            var gen = new Random(actualSeed);

            // Filter elements by source games if specified
            var availableElements = sourceGames?.Count > 0
                ? _elementLibrary.Where(e => sourceGames.Any(g => e.SourceGame.Contains(g, StringComparison.OrdinalIgnoreCase))).ToList()
                : _elementLibrary;

            if (availableElements.Count == 0) availableElements = _elementLibrary;

            var level = new DreamLevel
            {
                Id = Guid.NewGuid().ToString(),
                DreamCode = GenerateDreamCode(actualSeed),
                Mood = mood,
                Difficulty = difficulty,
                SourceGames = sourceGames ?? availableElements.Select(e => e.SourceGame).Distinct().ToList(),
                Seed = actualSeed,
                Generated = DateTime.Now
            };

            // Generate name
            level.Name = GenerateDreamName(mood, gen);

            // Pick and transform elements based on mood and difficulty
            int elementCount = 5 + (difficulty / 2);
            var shuffled = availableElements.OrderBy(_ => gen.Next()).Take(elementCount).ToList();

            foreach (var sourceElement in shuffled)
            {
                var dreamElement = TransformElement(sourceElement, mood, difficulty, gen);
                level.Elements.Add(dreamElement);
            }

            // Generate description using LLM if available
            if (_llmService?.IsAvailable == true)
            {
                level.Description = await GenerateLevelDescriptionAsync(level);
            }
            else
            {
                level.Description = GenerateOfflineDescription(mood, level.Elements.Count);
            }

            _generatedLevels.Add(level);
            SaveLevel(level);
            return level;
        }

        // Synchronous wrapper
        public DreamLevel GenerateLevel(DreamMood mood, int? seed = null)
        {
            return GenerateLevelAsync(mood, null, 5, seed).GetAwaiter().GetResult();
        }

        private string GenerateDreamCode(int seed)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No ambiguous chars
            var sb = new StringBuilder(8);
            var r = new Random(seed);
            for (int i = 0; i < 8; i++)
                sb.Append(chars[r.Next(chars.Length)]);
            return sb.ToString();
        }

        public DreamLevel? LoadFromDreamCode(string code)
        {
            // Try to find existing level with this code
            var existing = _generatedLevels.FirstOrDefault(l => l.DreamCode == code.ToUpperInvariant());
            if (existing != null) return existing;

            // Otherwise, decode seed from code and regenerate
            int seed = 0;
            foreach (char c in code.ToUpperInvariant())
            {
                seed = seed * 31 + c;
            }
            
            // Can't fully recreate without knowing mood, but we can try
            return null;
        }

        private async Task<string> GenerateLevelDescriptionAsync(DreamLevel level)
        {
            // Try AdvancedAiService first for memory-enhanced narratives
            if (_advancedAi != null)
            {
                try
                {
                    var elements = string.Join(", ", level.Elements.Take(5).Select(e => e.Id));
                    var result = await _advancedAi.GenerateNarrativeAsync(
                        $"A dream level with elements: {elements}",
                        new NarrativeContext
                        {
                            Location = level.Name,
                            Mood = level.Mood.ToString().ToLower()
                        });
                    if (!string.IsNullOrEmpty(result))
                        return result;
                }
                catch { /* Fall through to basic LLM */ }
            }

            // Fallback to direct LLM call
            if (_llmService == null) return GenerateOfflineDescription(level.Mood, level.Elements.Count);

            var elementsStr = string.Join(", ", level.Elements.Take(5).Select(e => e.Id));
            var prompt = $"Describe a surreal dream level with mood '{level.Mood}' containing elements: {elementsStr}. Max 30 words, mysterious and evocative.";

            return await _llmService.CompleteAsync(prompt, "You are a poetic dream narrator. Be mystical and brief.");
        }

        private string GenerateOfflineDescription(DreamMood mood, int elementCount)
        {
            return mood switch
            {
                DreamMood.Nostalgic => $"Memories of {elementCount} gaming artifacts shimmer in golden light...",
                DreamMood.Surreal => $"Reality bends as {elementCount} impossible formations defy logic...",
                DreamMood.Nightmare => $"Shadows twist around {elementCount} dark manifestations...",
                DreamMood.Euphoric => $"Joy radiates from {elementCount} crystalline wonders...",
                DreamMood.Chaotic => $"Digital chaos spawns {elementCount} glitched entities...",
                _ => $"A dream of {elementCount} elements awaits..."
            };
        }

        private string GenerateDreamName(DreamMood mood, Random gen)
        {
            var adjectives = new Dictionary<DreamMood, string[]>
            {
                [DreamMood.Surreal] = new[] { "Melting", "Floating", "Impossible", "Twisted", "Shifting" },
                [DreamMood.Nostalgic] = new[] { "Faded", "Golden", "Forgotten", "Childhood", "Warm" },
                [DreamMood.Nightmare] = new[] { "Shadow", "Dread", "Cursed", "Haunted", "Void" },
                [DreamMood.Euphoric] = new[] { "Rainbow", "Crystal", "Starlit", "Paradise", "Radiant" },
                [DreamMood.Chaotic] = new[] { "Corrupted", "Fragmented", "Glitched", "Static", "Binary" }
            };

            var nouns = new[] { "Realm", "Memory", "Vision", "World", "Dimension", "Echo", "Labyrinth", "Cascade" };

            var adj = adjectives[mood][gen.Next(adjectives[mood].Length)];
            var noun = nouns[gen.Next(nouns.Length)];
            return $"{adj} {noun}";
        }

        private DreamElement TransformElement(DreamElement source, DreamMood mood, int difficulty, Random gen)
        {
            var transform = DetermineTransform(mood, gen);
            
            var mutated = new DreamElement
            {
                Id = $"dream-{source.Id}-{gen.Next(1000)}",
                SourceGame = source.SourceGame,
                ElementType = source.ElementType,
                Transform = transform,
                Properties = new Dictionary<string, object>(source.Properties)
            };

            // Apply transformations
            switch (transform)
            {
                case ElementTransform.Scaled:
                    mutated.Properties["scale"] = 0.5 + gen.NextDouble() * 2.5;
                    break;
                case ElementTransform.Inverted:
                    mutated.Properties["inverted"] = true;
                    if (mutated.Properties.ContainsKey("damage"))
                        mutated.Properties["heals"] = true;
                    break;
                case ElementTransform.Glitched:
                    mutated.Properties["glitch_intensity"] = gen.NextDouble();
                    mutated.Properties["unstable"] = gen.NextDouble() > 0.5;
                    break;
                case ElementTransform.Fused:
                    mutated.Properties["fusion_source"] = _elementLibrary[gen.Next(_elementLibrary.Count)].Id;
                    break;
            }

            // Apply mood effects
            switch (mood)
            {
                case DreamMood.Nightmare:
                    if (mutated.Properties.ContainsKey("damage"))
                        mutated.Properties["damage"] = Convert.ToDouble(mutated.Properties["damage"]) * (1.5 + difficulty * 0.1);
                    mutated.Properties["dark"] = true;
                    break;
                case DreamMood.Euphoric:
                    if (mutated.Properties.ContainsKey("damage"))
                        mutated.Properties["damage"] = 0;
                    mutated.Properties["glowing"] = true;
                    break;
                case DreamMood.Chaotic:
                    mutated.Properties["random_behavior"] = true;
                    break;
            }

            // Apply difficulty scaling
            if (mutated.Properties.ContainsKey("speed"))
                mutated.Properties["speed"] = Convert.ToDouble(mutated.Properties["speed"]) * (0.8 + difficulty * 0.1);

            return mutated;
        }

        private ElementTransform DetermineTransform(DreamMood mood, Random gen)
        {
            var weights = mood switch
            {
                DreamMood.Surreal => new[] { 0.3, 0.3, 0.2, 0.2 },
                DreamMood.Chaotic => new[] { 0.1, 0.2, 0.4, 0.3 },
                DreamMood.Nightmare => new[] { 0.2, 0.3, 0.3, 0.2 },
                _ => new[] { 0.5, 0.2, 0.15, 0.15 }
            };

            double roll = gen.NextDouble();
            double cumulative = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return (ElementTransform)(i + 1);
            }
            return ElementTransform.Normal;
        }

        public void ToggleFavorite(string id)
        {
            var level = _generatedLevels.FirstOrDefault(l => l.Id == id);
            if (level == null) return;

            var favoriteCount = _generatedLevels.Count(l => l.IsFavorite);
            if (!level.IsFavorite && favoriteCount >= MaxFavorites) return;

            level.IsFavorite = !level.IsFavorite;
            SaveLevel(level);
        }

        public List<DreamLevel> GetFavorites() => 
            _generatedLevels.Where(l => l.IsFavorite).ToList();

        public List<DreamLevel> GetGeneratedLevels() => _generatedLevels;

        public DreamLevel? GetLevel(string id) => _generatedLevels.FirstOrDefault(l => l.Id == id);

        public void AddElement(DreamElement element) => _elementLibrary.Add(element);

        private void SaveLevel(DreamLevel level)
        {
            var path = Path.Combine(_levelsPath, $"{level.Id}.json");
            var json = JsonSerializer.Serialize(level, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private void LoadGeneratedLevels()
        {
            if (!Directory.Exists(_levelsPath)) return;

            foreach (var file in Directory.GetFiles(_levelsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var level = JsonSerializer.Deserialize<DreamLevel>(json);
                    if (level != null) _generatedLevels.Add(level);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}"); }
            }
        }

        public void DeleteLevel(string id)
        {
            var level = _generatedLevels.FirstOrDefault(l => l.Id == id);
            if (level != null)
            {
                _generatedLevels.Remove(level);
                var path = Path.Combine(_levelsPath, $"{level.Id}.json");
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
