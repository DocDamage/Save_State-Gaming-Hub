using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Rom
{
    public class CheatCode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GameId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty; // "GameShark", "GameGenie", "ActionReplay", "Raw"
        public string Platform { get; set; } = string.Empty;
        public string Region { get; set; } = "US";
        public bool IsVerified { get; set; }
        public int UsageCount { get; set; }
        public DateTime AddedAt { get; set; }
        public string AddedBy { get; set; } = "system";
        public List<string> Tags { get; set; } = new();
    }

    public class CheatCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<CheatCode> Cheats { get; set; } = new();
    }

    public class CheatService
    {
        private static CheatService? _instance;
        private readonly string _databasePath;
        private readonly HttpClient _httpClient;
        private readonly RagService _ragService;
        private readonly Dictionary<string, List<CheatCode>> _cheatsByGame = new();
        private readonly List<CheatCode> _allCheats = new();

        public static CheatService Instance => _instance ??= new CheatService();

        private CheatService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _ragService = RagService.Instance;
            _databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "cheats");
            if (!Directory.Exists(_databasePath)) Directory.CreateDirectory(_databasePath);
            LoadDatabase();
        }

        // Get cheats for a specific game
        public List<CheatCode> GetCheatsForGame(string gameId)
        {
            return _cheatsByGame.GetValueOrDefault(gameId) ?? new();
        }

        // Search cheats
        public List<CheatCode> SearchCheats(string query, string? platform = null)
        {
            var results = _allCheats.Where(c =>
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.GameName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Contains(query, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(platform))
            {
                results = results.Where(c => c.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));
            }

            return results.OrderByDescending(c => c.IsVerified).ThenByDescending(c => c.UsageCount).ToList();
        }

        // Get cheats by category
        public List<CheatCategory> GetCategoriesForGame(string gameId)
        {
            var cheats = GetCheatsForGame(gameId);
            var categories = new Dictionary<string, CheatCategory>
            {
                { "Infinite Lives", new CheatCategory { Name = "Infinite Lives", Icon = "❤️" } },
                { "Infinite Health", new CheatCategory { Name = "Infinite Health", Icon = "💚" } },
                { "Infinite Ammo", new CheatCategory { Name = "Infinite Ammo", Icon = "🔫" } },
                { "Unlock All", new CheatCategory { Name = "Unlock All", Icon = "🔓" } },
                { "Debug", new CheatCategory { Name = "Debug", Icon = "🔧" } },
                { "Misc", new CheatCategory { Name = "Misc", Icon = "✨" } }
            };

            foreach (var cheat in cheats)
            {
                var category = DetermineCategory(cheat);
                if (categories.ContainsKey(category))
                {
                    categories[category].Cheats.Add(cheat);
                }
            }

            return categories.Values.Where(c => c.Cheats.Count > 0).ToList();
        }

        private string DetermineCategory(CheatCode cheat)
        {
            var name = cheat.Name.ToLower();
            if (name.Contains("life") || name.Contains("lives")) return "Infinite Lives";
            if (name.Contains("health") || name.Contains("hp") || name.Contains("energy")) return "Infinite Health";
            if (name.Contains("ammo") || name.Contains("bullet") || name.Contains("weapon")) return "Infinite Ammo";
            if (name.Contains("unlock") || name.Contains("all ")) return "Unlock All";
            if (name.Contains("debug") || name.Contains("level select")) return "Debug";
            return "Misc";
        }

        // Add a cheat code
        public CheatCode AddCheat(string gameId, string gameName, string name, string code, 
            string description, string format, string platform)
        {
            var cheat = new CheatCode
            {
                GameId = gameId,
                GameName = gameName,
                Name = name,
                Code = code.ToUpper().Trim(),
                Description = description,
                Format = format,
                Platform = platform,
                AddedAt = DateTime.UtcNow,
                AddedBy = "user"
            };

            _allCheats.Add(cheat);
            if (!_cheatsByGame.ContainsKey(gameId))
                _cheatsByGame[gameId] = new();
            _cheatsByGame[gameId].Add(cheat);

            // Add to RAG knowledge base
            _ragService.AddCheatCode(gameName, name, code, description);

            SaveDatabase();
            return cheat;
        }

        // Import cheats from CHT file (common RetroArch format)
        public int ImportChtFile(string filePath, string gameId, string gameName, string platform)
        {
            if (!File.Exists(filePath)) return 0;

            var lines = File.ReadAllLines(filePath);
            int imported = 0;

            string? currentCheatName = null;
            string? currentCode = null;
            string? currentDesc = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed.StartsWith("cheat") && trimmed.Contains("_desc"))
                {
                    // Save previous cheat if exists
                    if (currentCheatName != null && currentCode != null)
                    {
                        AddCheat(gameId, gameName, currentCheatName, currentCode, currentDesc ?? "", "Raw", platform);
                        imported++;
                    }

                    currentCheatName = ExtractValue(trimmed);
                    currentCode = null;
                    currentDesc = currentCheatName;
                }
                else if (trimmed.StartsWith("cheat") && trimmed.Contains("_code"))
                {
                    currentCode = ExtractValue(trimmed);
                }
            }

            // Don't forget the last one
            if (currentCheatName != null && currentCode != null)
            {
                AddCheat(gameId, gameName, currentCheatName, currentCode, currentDesc ?? "", "Raw", platform);
                imported++;
            }

            return imported;
        }

        private string ExtractValue(string line)
        {
            var idx = line.IndexOf('=');
            if (idx < 0) return line;
            return line[(idx + 1)..].Trim().Trim('"');
        }

        // Convert between cheat formats
        public string ConvertCode(string code, string fromFormat, string toFormat)
        {
            // Simplified conversion - in production would use proper algorithm
            if (fromFormat == toFormat) return code;

            // Game Genie to Raw (NES example)
            if (fromFormat == "GameGenie" && toFormat == "Raw")
            {
                // Placeholder - actual conversion requires complex algorithm
                return $"[Converted from {fromFormat}]: {code}";
            }

            return code;
        }

        // Export cheats to file
        public void ExportCheats(string gameId, string filePath, string format = "cht")
        {
            var cheats = GetCheatsForGame(gameId);
            var lines = new List<string>();

            lines.Add($"# Cheats for game: {cheats.FirstOrDefault()?.GameName ?? gameId}");
            lines.Add($"# Exported by SaveState on {DateTime.Now}");
            lines.Add($"cheats = {cheats.Count}");
            lines.Add("");

            for (int i = 0; i < cheats.Count; i++)
            {
                var cheat = cheats[i];
                lines.Add($"cheat{i}_desc = \"{cheat.Name}\"");
                lines.Add($"cheat{i}_code = \"{cheat.Code}\"");
                lines.Add($"cheat{i}_enable = false");
                lines.Add("");
            }

            File.WriteAllLines(filePath, lines);
        }

        // Verify cheat (mark as tested/working)
        public void VerifyCheat(string cheatId)
        {
            var cheat = _allCheats.FirstOrDefault(c => c.Id == cheatId);
            if (cheat != null)
            {
                cheat.IsVerified = true;
                SaveDatabase();
            }
        }

        // Record usage
        public void RecordUsage(string cheatId)
        {
            var cheat = _allCheats.FirstOrDefault(c => c.Id == cheatId);
            if (cheat != null)
            {
                cheat.UsageCount++;
                SaveDatabase();
            }
        }

        public bool DeleteCheat(string cheatId)
        {
            var cheat = _allCheats.FirstOrDefault(c => c.Id == cheatId);
            if (cheat == null) return false;

            _allCheats.Remove(cheat);
            _cheatsByGame.GetValueOrDefault(cheat.GameId)?.Remove(cheat);
            SaveDatabase();
            return true;
        }

        public List<string> GetSupportedFormats()
        {
            return new() { "GameShark", "GameGenie", "ActionReplay", "ProActionReplay", "Raw" };
        }

        public int GetTotalCheatCount() => _allCheats.Count;

        public List<CheatCode> GetPopularCheats(int limit = 20)
        {
            return _allCheats.OrderByDescending(c => c.UsageCount).Take(limit).ToList();
        }

        private void LoadDatabase()
        {
            var dbPath = Path.Combine(_databasePath, "cheats.json");
            if (File.Exists(dbPath))
            {
                try
                {
                    var json = File.ReadAllText(dbPath);
                    var cheats = JsonSerializer.Deserialize<List<CheatCode>>(json);
                    if (cheats != null)
                    {
                        _allCheats.AddRange(cheats);
                        foreach (var cheat in cheats)
                        {
                            if (!_cheatsByGame.ContainsKey(cheat.GameId))
                                _cheatsByGame[cheat.GameId] = new();
                            _cheatsByGame[cheat.GameId].Add(cheat);
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}"); }
            }
        }

        private void SaveDatabase()
        {
            var dbPath = Path.Combine(_databasePath, "cheats.json");
            var json = JsonSerializer.Serialize(_allCheats, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dbPath, json);
        }
    }
}
