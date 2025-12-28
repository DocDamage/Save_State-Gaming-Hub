using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;
using Serilog;

namespace SaveState.Core.Services.Mugen
{
    public class FusionStats
    {
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public int Special { get; set; }
    }

    public class FusionCharacter
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Parent1Id { get; set; } = string.Empty;
        public string Parent2Id { get; set; } = string.Empty;
        public string FusionType { get; set; } = string.Empty;
        public FusionStats Stats { get; set; } = new();
        public List<string> Abilities { get; set; } = new();
        public string SignatureMove { get; set; } = string.Empty;
        public string Rarity { get; set; } = string.Empty;
        public string? LlmDescription { get; set; }
        public DateTime Created { get; set; }
    }

    public class CharacterFusionService
    {
        private readonly ILogger _logger = Log.ForContext<CharacterFusionService>();
        private List<FusionCharacter> _fusions = new();
        private readonly string _dataPath;
        private readonly string _engineRootPath;
        private readonly ILlmService? _llmService;
        private readonly Random _rand = new();
        private const int MaxGallerySize = 100;

        public CharacterFusionService(ILlmService? llmService = null)
        {
            _llmService = llmService;
            _engineRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SaveState2", "MUGEN");
            _dataPath = Path.Combine(_engineRootPath, "data", "fusions.json");
            LoadFusions();
        }

        public List<FusionCharacter> GetAllFusions() => _fusions;
        public int GetGalleryCount() => _fusions.Count;
        public int GetMaxGallerySize() => MaxGallerySize;
        public bool IsGalleryFull => _fusions.Count >= MaxGallerySize;

        public async Task<FusionCharacter> FuseCharactersAsync(MugenFighter p1, MugenFighter p2, string type = "balanced")
        {
            // Enforce gallery limit
            if (_fusions.Count >= MaxGallerySize)
            {
                // Remove oldest non-legendary fusion
                var oldest = _fusions.Where(f => f.Rarity != "legendary").OrderBy(f => f.Created).FirstOrDefault();
                if (oldest != null) DeleteFusion(oldest.Id);
            }

            var p1Stats = InferStatsFromFighter(p1);
            var p2Stats = InferStatsFromFighter(p2);
            var fusedStats = CombineStats(p1Stats, p2Stats, type);

            // Try LLM-powered name generation
            string fusionName = GenerateFusionName(p1.Name, p2.Name);
            List<string> abilities = GenerateAbilities(p1.Name, p2.Name);
            string signatureMove = GenerateSignatureMove(p1.Name, p2.Name);
            string? llmDescription = null;

            if (_llmService?.IsAvailable == true)
            {
                try
                {
                    var llmName = await GenerateLlmFusionNameAsync(p1.Name, p2.Name);
                    if (!string.IsNullOrEmpty(llmName)) fusionName = llmName;

                    var llmAbilities = await GenerateLlmAbilitiesAsync(p1.Name, p2.Name);
                    if (llmAbilities.Count > 0) abilities = llmAbilities;

                    llmDescription = await GenerateLlmDescriptionAsync(fusionName, p1.Name, p2.Name, type);
                }
                catch { /* Fall back to rule-based generation */ }
            }

            var fusion = new FusionCharacter
            {
                Id = $"fusion-{p1.Name}-{p2.Name}-{Guid.NewGuid().ToString()[..8]}",
                Name = fusionName,
                Parent1Id = p1.Name,
                Parent2Id = p2.Name,
                FusionType = type,
                Stats = fusedStats,
                Abilities = abilities,
                SignatureMove = signatureMove,
                Rarity = CalculateRarity(type, fusedStats),
                LlmDescription = llmDescription,
                Created = DateTime.Now
            };

            _fusions.Add(fusion);
            SaveFusions();
            CreateFusionCharacterFolder(fusion, p1, p2);
            return fusion;
        }

        // REMOVED: Synchronous wrapper removed to eliminate deadlock risk
        // Use FuseCharactersAsync directly instead

        private async Task<string?> GenerateLlmFusionNameAsync(string p1, string p2)
        {
            if (_llmService == null) return null;
            var prompt = $"Create a single creative fusion name for a fighting game character that combines '{p1}' and '{p2}'. Just the name, no explanation, max 15 characters.";
            var result = await _llmService.CompleteAsync(prompt);
            return result?.Trim().Trim('"').Split('\n')[0];
        }

        private async Task<List<string>> GenerateLlmAbilitiesAsync(string p1, string p2)
        {
            if (_llmService == null) return new();
            var prompt = $"Create 4 unique fighting game ability names for a fusion of '{p1}' and '{p2}'. Just list them, one per line, max 20 chars each.";
            var result = await _llmService.CompleteAsync(prompt);
            return result?.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(4).Select(a => a.Trim().TrimStart('-', '*', '1', '2', '3', '4', '.', ' ')).ToList() ?? new();
        }

        private async Task<string?> GenerateLlmDescriptionAsync(string fusionName, string p1, string p2, string type)
        {
            if (_llmService == null) return null;
            var prompt = $"Write a 20-word fighting game character bio for '{fusionName}', a {type} fusion of '{p1}' and '{p2}'.";
            return await _llmService.CompleteAsync(prompt);
        }

        private FusionStats InferStatsFromFighter(MugenFighter fighter)
        {
            int seed = fighter.Name.GetHashCode();
            var r = new Random(seed);
            return new FusionStats
            {
                Health = 80 + r.Next(-20, 20),
                Attack = 70 + r.Next(-20, 30),
                Defense = 70 + r.Next(-20, 30),
                Speed = 70 + r.Next(-20, 30),
                Special = 70 + r.Next(-20, 30)
            };
        }

        private FusionStats CombineStats(FusionStats s1, FusionStats s2, string type)
        {
            return type switch
            {
                "balanced" => new FusionStats
                {
                    Health = (s1.Health + s2.Health) / 2,
                    Attack = (s1.Attack + s2.Attack) / 2,
                    Defense = (s1.Defense + s2.Defense) / 2,
                    Speed = (s1.Speed + s2.Speed) / 2,
                    Special = (s1.Special + s2.Special) / 2
                },
                "dominant-1" => new FusionStats
                {
                    Health = (int)(s1.Health * 0.7 + s2.Health * 0.3),
                    Attack = (int)(s1.Attack * 0.7 + s2.Attack * 0.3),
                    Defense = (int)(s1.Defense * 0.7 + s2.Defense * 0.3),
                    Speed = (int)(s1.Speed * 0.7 + s2.Speed * 0.3),
                    Special = (int)(s1.Special * 0.7 + s2.Special * 0.3)
                },
                "dominant-2" => new FusionStats
                {
                    Health = (int)(s1.Health * 0.3 + s2.Health * 0.7),
                    Attack = (int)(s1.Attack * 0.3 + s2.Attack * 0.7),
                    Defense = (int)(s1.Defense * 0.3 + s2.Defense * 0.7),
                    Speed = (int)(s1.Speed * 0.3 + s2.Speed * 0.7),
                    Special = (int)(s1.Special * 0.3 + s2.Special * 0.7)
                },
                _ => new FusionStats
                {
                    Health = (s1.Health + s2.Health) / 2 + _rand.Next(-15, 15),
                    Attack = (s1.Attack + s2.Attack) / 2 + _rand.Next(-15, 15),
                    Defense = (s1.Defense + s2.Defense) / 2 + _rand.Next(-15, 15),
                    Speed = (s1.Speed + s2.Speed) / 2 + _rand.Next(-15, 15),
                    Special = (s1.Special + s2.Special) / 2 + _rand.Next(-15, 15)
                }
            };
        }

        private string GenerateFusionName(string n1, string n2)
        {
            var strategies = new Func<string>[]
            {
                () => n1[..(Math.Min(n1.Length, n1.Length / 2 + 1))] + n2[(Math.Max(0, n2.Length / 2))..],
                () => n2[..(Math.Min(n2.Length, n2.Length / 2 + 1))] + n1[(Math.Max(0, n1.Length / 2))..],
                () => $"{n1}-{n2}",
                () => CreatePortmanteau(n1, n2)
            };
            return strategies[_rand.Next(strategies.Length)]();
        }

        private string CreatePortmanteau(string w1, string w2)
        {
            if (w1.Length < 3 || w2.Length < 3) return $"{w1}{w2}";
            const string vowels = "aeiouAEIOU";
            int split1 = w1.Length / 2;
            for (int i = split1; i < w1.Length; i++)
            {
                if (vowels.Contains(w1[i]))
                {
                    split1 = i;
                    break;
                }
            }
            return w1[..split1] + w2[(w2.Length / 2)..];
        }

        private List<string> GenerateAbilities(string p1, string p2)
        {
            return new List<string> { "Hybrid Aura", "Fused Might", $"{p1} Stance", $"{p2} Spirit" }.Take(4).ToList();
        }

        private string GenerateSignatureMove(string p1, string p2)
        {
            var signatures = new[] { $"{p1}'s {p2} Strike", $"Fusion: {p1} × {p2}", $"Ultimate {p1}-{p2} Combo", "Chaos Fusion Blast", "Hybrid Ultimate" };
            return signatures[_rand.Next(signatures.Length)];
        }

        private string CalculateRarity(string type, FusionStats stats)
        {
            int totalStats = stats.Health + stats.Attack + stats.Defense + stats.Speed + stats.Special;
            if (type == "chaos" && _rand.NextDouble() > 0.9) return "legendary";
            if (totalStats >= 400) return "legendary";
            if (totalStats >= 350) return "epic";
            if (totalStats >= 300) return "rare";
            return "common";
        }

        private void CreateFusionCharacterFolder(FusionCharacter fusion, MugenFighter p1, MugenFighter p2)
        {
            try
            {
                var charDir = Path.Combine(_engineRootPath, "chars", fusion.Name.ToLower().Replace(" ", "_"));
                if (Directory.Exists(charDir)) return;
                Directory.CreateDirectory(charDir);

                var defContent = $@"; {fusion.Name} - Fusion Character
[Info]
name = ""{fusion.Name}""
displayname = ""{fusion.Name}""
versiondate = {DateTime.Now:MM,dd,yyyy}
mugenversion = 1.1
author = ""SaveState Fusion System""
[Files]
cmd = fusion.cmd
cns = fusion.cns
";
                File.WriteAllText(Path.Combine(charDir, fusion.Name.ToLower().Replace(" ", "_") + ".def"), defContent);

                var cnsContent = $@"; {fusion.Name} Constants
[Data]
life = {fusion.Stats.Health * 10}
attack = {fusion.Stats.Attack}
defence = {fusion.Stats.Defense}
";
                File.WriteAllText(Path.Combine(charDir, "fusion.cns"), cnsContent);
                AddFusionToRoster(fusion.Name.ToLower().Replace(" ", "_"));
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Fusion folder creation error");
            }
        }

        private void AddFusionToRoster(string charName)
        {
            try
            {
                var selectDefPath = Path.Combine(_engineRootPath, "data", "select.def");
                if (!File.Exists(selectDefPath)) return;
                var lines = File.ReadAllLines(selectDefPath).ToList();
                if (lines.Any(l => l.Trim().StartsWith(charName))) return;
                int index = lines.FindIndex(l => l.Trim().Equals("[Characters]", StringComparison.OrdinalIgnoreCase));
                if (index != -1)
                {
                    lines.Insert(index + 1, $"{charName}, random");
                    File.WriteAllLines(selectDefPath, lines);
                }
            }
            catch (Exception ex) { _logger.Debug(ex, "Failed to add fusion to roster"); }
        }

        private void LoadFusions()
        {
            if (File.Exists(_dataPath))
            {
                try
                {
                    _fusions = JsonSerializer.Deserialize<List<FusionCharacter>>(File.ReadAllText(_dataPath)) ?? new();
                }
                catch { _fusions = new(); }
            }
        }

        private void SaveFusions()
        {
            var dir = Path.GetDirectoryName(_dataPath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_dataPath, JsonSerializer.Serialize(_fusions, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void DeleteFusion(string id)
        {
            var f = _fusions.FirstOrDefault(x => x.Id == id);
            if (f != null)
            {
                try
                {
                    var charDir = Path.Combine(_engineRootPath, "chars", f.Name.ToLower().Replace(" ", "_"));
                    if (Directory.Exists(charDir)) Directory.Delete(charDir, true);
                }
                catch (Exception ex) { _logger.Warning(ex, "Failed to delete fusion folder"); }
                _fusions.Remove(f);
                SaveFusions();
            }
        }
    }
}
