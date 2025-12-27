using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;
using Serilog;

namespace SaveState.Core.Services.EmulatorEnhancements
{
    public enum PlayStyle { Aggressive, Defensive, Explorer, Speedrunner, Completionist, Balanced }
    
    public enum MutationType
    {
        HyperMode,      // 2x game speed
        ChaosEnemies,   // Random AI behaviors
        ItemStorm,      // More item spawns
        MirrorWorld,    // Flipped level layouts
        TimeCrisis      // Countdown timer pressure
    }

    public class PlaystyleProfile
    {
        public string GameId { get; set; } = string.Empty;
        public int TotalDeaths { get; set; }
        public int TotalPlayTime { get; set; }
        public int SkillLevel { get; set; } = 50;
        public int EvolutionLevel { get; set; } = 1;  // 1-10
        public int EvolutionXP { get; set; }
        public PlayStyle DetectedPlaystyle { get; set; } = PlayStyle.Balanced;
        public Dictionary<string, int> DeathLocations { get; set; } = new();
        public Dictionary<string, int> ActionCounts { get; set; } = new();
        public List<string> ActiveMutations { get; set; } = new();
        public DateTime LastPlayed { get; set; }
        
        // Playstyle metrics
        public int CombatActions { get; set; }
        public int ExploreActions { get; set; }
        public int SpeedActions { get; set; }
        public int CollectActions { get; set; }
        public int DefenseActions { get; set; }
    }

    public class EvolutionUnlock
    {
        public int Level { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int XPRequired { get; set; }
        public List<string> Rewards { get; set; } = new();
    }

    public class MemoryEvolutionService
    {
        private readonly ILogger _logger = Log.ForContext<MemoryEvolutionService>();
        private readonly Dictionary<string, PlaystyleProfile> _profiles = new();
        private readonly string _profilesPath;
        private readonly ILlmService? _llmService;
        private readonly Random _rand = new();

        // Evolution level requirements
        private static readonly EvolutionUnlock[] EvolutionLevels = new[]
        {
            new EvolutionUnlock { Level = 1, Name = "Novice", XPRequired = 0, Rewards = new() },
            new EvolutionUnlock { Level = 2, Name = "Apprentice", XPRequired = 100, Rewards = new() { "HyperMode" } },
            new EvolutionUnlock { Level = 3, Name = "Journeyman", XPRequired = 300, Rewards = new() { "ChaosEnemies" } },
            new EvolutionUnlock { Level = 4, Name = "Expert", XPRequired = 600, Rewards = new() { "ItemStorm" } },
            new EvolutionUnlock { Level = 5, Name = "Veteran", XPRequired = 1000, Rewards = new() { "MirrorWorld" } },
            new EvolutionUnlock { Level = 6, Name = "Elite", XPRequired = 1500, Rewards = new() { "TimeCrisis" } },
            new EvolutionUnlock { Level = 7, Name = "Master", XPRequired = 2200, Rewards = new() { "Custom Palette" } },
            new EvolutionUnlock { Level = 8, Name = "Champion", XPRequired = 3000, Rewards = new() { "Speed Boost" } },
            new EvolutionUnlock { Level = 9, Name = "Legend", XPRequired = 4000, Rewards = new() { "Infinite Lives" } },
            new EvolutionUnlock { Level = 10, Name = "Transcendent", XPRequired = 5500, Rewards = new() { "Dev Mode" } },
        };

        public MemoryEvolutionService(ILlmService? llmService = null)
        {
            _llmService = llmService;
            _profilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "evolution_profiles");
            if (!Directory.Exists(_profilesPath)) Directory.CreateDirectory(_profilesPath);
            LoadProfiles();
        }

        public PlaystyleProfile GetOrCreateProfile(string gameId)
        {
            if (!_profiles.ContainsKey(gameId))
            {
                _profiles[gameId] = new PlaystyleProfile { GameId = gameId, LastPlayed = DateTime.Now };
            }
            return _profiles[gameId];
        }

        public void RecordDeath(string gameId, string location)
        {
            var profile = GetOrCreateProfile(gameId);
            profile.TotalDeaths++;
            profile.LastPlayed = DateTime.Now;

            if (!profile.DeathLocations.ContainsKey(location))
                profile.DeathLocations[location] = 0;
            profile.DeathLocations[location]++;

            UpdateSkillLevel(profile);
            CheckAndApplyMutations(profile);
            SaveProfile(profile);
        }

        public void RecordAction(string gameId, string actionType)
        {
            var profile = GetOrCreateProfile(gameId);
            
            if (!profile.ActionCounts.ContainsKey(actionType))
                profile.ActionCounts[actionType] = 0;
            profile.ActionCounts[actionType]++;

            // Track playstyle metrics
            switch (actionType.ToLower())
            {
                case "attack": case "combo": case "kill":
                    profile.CombatActions++;
                    break;
                case "explore": case "secret": case "wander":
                    profile.ExploreActions++;
                    break;
                case "run": case "dash": case "skip":
                    profile.SpeedActions++;
                    break;
                case "collect": case "item": case "upgrade":
                    profile.CollectActions++;
                    break;
                case "block": case "dodge": case "heal":
                    profile.DefenseActions++;
                    break;
            }

            DetectPlaystyle(profile);
            AddXP(profile, 1);
        }

        public void RecordPlayTime(string gameId, int seconds)
        {
            var profile = GetOrCreateProfile(gameId);
            profile.TotalPlayTime += seconds;
            profile.LastPlayed = DateTime.Now;
            AddXP(profile, seconds / 60); // 1 XP per minute
            SaveProfile(profile);
        }

        private void AddXP(PlaystyleProfile profile, int amount)
        {
            profile.EvolutionXP += amount;
            
            // Check for level up
            for (int i = EvolutionLevels.Length - 1; i >= 0; i--)
            {
                if (profile.EvolutionXP >= EvolutionLevels[i].XPRequired)
                {
                    if (profile.EvolutionLevel < EvolutionLevels[i].Level)
                    {
                        profile.EvolutionLevel = EvolutionLevels[i].Level;
                    }
                    break;
                }
            }
        }

        private void DetectPlaystyle(PlaystyleProfile profile)
        {
            int max = new[] { profile.CombatActions, profile.DefenseActions, profile.ExploreActions, 
                              profile.SpeedActions, profile.CollectActions }.Max();

            if (max == 0)
            {
                profile.DetectedPlaystyle = PlayStyle.Balanced;
            }
            else if (max == profile.CombatActions)
            {
                profile.DetectedPlaystyle = PlayStyle.Aggressive;
            }
            else if (max == profile.DefenseActions)
            {
                profile.DetectedPlaystyle = PlayStyle.Defensive;
            }
            else if (max == profile.ExploreActions)
            {
                profile.DetectedPlaystyle = PlayStyle.Explorer;
            }
            else if (max == profile.SpeedActions)
            {
                profile.DetectedPlaystyle = PlayStyle.Speedrunner;
            }
            else if (max == profile.CollectActions)
            {
                profile.DetectedPlaystyle = PlayStyle.Completionist;
            }
        }

        private void UpdateSkillLevel(PlaystyleProfile profile)
        {
            double deathPenalty = Math.Min(profile.TotalDeaths * 1.5, 40);
            double playTimeBonus = Math.Min(profile.TotalPlayTime / 60.0 * 0.3, 25);
            double evolutionBonus = profile.EvolutionLevel * 3;
            
            profile.SkillLevel = (int)Math.Clamp(50 - deathPenalty + playTimeBonus + evolutionBonus, 0, 100);
        }

        private void CheckAndApplyMutations(PlaystyleProfile profile)
        {
            var hotspots = profile.DeathLocations.Where(kvp => kvp.Value >= 3).ToList();

            if (hotspots.Count > 0 && profile.SkillLevel < 40 && profile.EvolutionLevel >= 2)
            {
                // Struggling player - auto-suggest helpful mutations
            }
            else if (profile.SkillLevel > 80 && profile.TotalDeaths < 5 && profile.EvolutionLevel >= 2)
            {
                // Skilled player - challenge mutations available
            }
        }

        public void ActivateMutation(string gameId, MutationType mutation)
        {
            var profile = GetOrCreateProfile(gameId);
            var mutationName = mutation.ToString();
            
            int requiredLevel = mutation switch
            {
                MutationType.HyperMode => 2,
                MutationType.ChaosEnemies => 3,
                MutationType.ItemStorm => 4,
                MutationType.MirrorWorld => 5,
                MutationType.TimeCrisis => 6,
                _ => 1
            };

            if (profile.EvolutionLevel >= requiredLevel && !profile.ActiveMutations.Contains(mutationName))
            {
                profile.ActiveMutations.Add(mutationName);
                SaveProfile(profile);
            }
        }

        public List<MutationType> GetAvailableMutations(string gameId)
        {
            var profile = GetOrCreateProfile(gameId);
            var available = new List<MutationType>();

            if (profile.EvolutionLevel >= 2) available.Add(MutationType.HyperMode);
            if (profile.EvolutionLevel >= 3) available.Add(MutationType.ChaosEnemies);
            if (profile.EvolutionLevel >= 4) available.Add(MutationType.ItemStorm);
            if (profile.EvolutionLevel >= 5) available.Add(MutationType.MirrorWorld);
            if (profile.EvolutionLevel >= 6) available.Add(MutationType.TimeCrisis);

            return available;
        }

        public Dictionary<string, int> GetDeathHeatmap(string gameId)
        {
            var profile = GetOrCreateProfile(gameId);
            return profile.DeathLocations;
        }

        public List<string> GetActiveMutations(string gameId)
        {
            var profile = GetOrCreateProfile(gameId);
            return profile.ActiveMutations;
        }

        public void RemoveMutation(string gameId, string mutation)
        {
            var profile = GetOrCreateProfile(gameId);
            profile.ActiveMutations.Remove(mutation);
            SaveProfile(profile);
        }

        public void ClearMutations(string gameId)
        {
            var profile = GetOrCreateProfile(gameId);
            profile.ActiveMutations.Clear();
            SaveProfile(profile);
        }

        public EvolutionUnlock[] GetEvolutionLevels() => EvolutionLevels;

        // LLM-powered personalized suggestions
        public async Task<string> GetPersonalizedTipAsync(string gameId, string? currentSituation = null)
        {
            var profile = GetOrCreateProfile(gameId);
            
            if (_llmService?.IsAvailable != true)
            {
                return GetPlaystyleSuggestion(gameId);
            }

            var deadliestLocation = profile.DeathLocations.OrderByDescending(x => x.Value).FirstOrDefault();
            var situationContext = currentSituation != null ? $" Current situation: {currentSituation}." : "";
            
            var prompt = $@"You're a helpful gaming coach. Player profile:
- Playstyle: {profile.DetectedPlaystyle}
- Skill level: {profile.SkillLevel}/100
- Deaths: {profile.TotalDeaths}
- Most deaths at: {deadliestLocation.Key ?? "nowhere"} ({deadliestLocation.Value} times)
- Evolution level: {profile.EvolutionLevel}/10
- Active mutations: {string.Join(", ", profile.ActiveMutations)}
{situationContext}

Give ONE short, personalized tip (max 25 words). Be encouraging and specific.";

            return await _llmService.CompleteAsync(prompt, 
                "You are a supportive gaming coach. Give brief, actionable tips.");
        }

        public async Task<string> GetDeathAnalysisAsync(string gameId, string location)
        {
            var profile = GetOrCreateProfile(gameId);
            
            if (_llmService?.IsAvailable != true)
            {
                return $"You've died {profile.DeathLocations.GetValueOrDefault(location, 0)} times at {location}. Try a different approach!";
            }

            var deathCount = profile.DeathLocations.GetValueOrDefault(location, 0);
            var prompt = $"Player died at '{location}' for the {deathCount}th time. Their playstyle is {profile.DetectedPlaystyle}. Give a short (15 words max) encouraging tip to overcome this challenge.";

            return await _llmService.CompleteAsync(prompt,
                "You are a wise gaming mentor. Be brief and helpful.");
        }

        public async Task<string> GetMutationRecommendationAsync(string gameId)
        {
            var profile = GetOrCreateProfile(gameId);
            var available = GetAvailableMutations(gameId);

            if (_llmService?.IsAvailable != true || available.Count == 0)
            {
                return profile.SkillLevel < 40 
                    ? "Try ItemStorm for extra power-ups!" 
                    : "Challenge yourself with HyperMode or TimeCrisis!";
            }

            var prompt = $@"Player has skill level {profile.SkillLevel}/100, is a {profile.DetectedPlaystyle} player, and has died {profile.TotalDeaths} times.
Available mutations: {string.Join(", ", available)}
- HyperMode: 2x game speed (for skilled players)
- ChaosEnemies: Random AI (for those who want chaos)
- ItemStorm: More items (helps struggling players)
- MirrorWorld: Flipped levels (for a fresh challenge)
- TimeCrisis: Time pressure (for speedrunners)

Recommend ONE mutation with a brief reason (15 words max).";

            return await _llmService.CompleteAsync(prompt,
                "You are a game modifier advisor. Be concise.");
        }

        public async Task<string> GetLevelUpMessageAsync(int newLevel)
        {
            var levelInfo = EvolutionLevels.FirstOrDefault(l => l.Level == newLevel);
            if (levelInfo == null) return $"Level {newLevel} reached!";

            if (_llmService?.IsAvailable != true)
            {
                return $"🎉 You've evolved to {levelInfo.Name}! New rewards: {string.Join(", ", levelInfo.Rewards)}";
            }

            var prompt = $"Player just reached evolution level {newLevel}: '{levelInfo.Name}'. They unlocked: {string.Join(", ", levelInfo.Rewards)}. Write a brief celebratory message (20 words max).";

            return await _llmService.CompleteAsync(prompt,
                "You are an enthusiastic game announcer celebrating achievements.");
        }

        public string GetPlaystyleSuggestion(string gameId)
        {
            var profile = GetOrCreateProfile(gameId);
            return profile.DetectedPlaystyle switch
            {
                PlayStyle.Aggressive => "Try exploring more - you might find powerful upgrades!",
                PlayStyle.Defensive => "Nice caution! Consider taking more risks for bonus points.",
                PlayStyle.Explorer => "Great exploration! Don't forget to practice combat skills.",
                PlayStyle.Speedrunner => "Lightning fast! Try collecting secrets for 100% completion.",
                PlayStyle.Completionist => "Thorough! Time to speed things up for time attack modes.",
                _ => "You have a balanced playstyle - try specializing for bonuses!"
            };
        }

        private void LoadProfiles()
        {
            if (!Directory.Exists(_profilesPath)) return;

            foreach (var file in Directory.GetFiles(_profilesPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<PlaystyleProfile>(json);
                    if (profile != null)
                    {
                        _profiles[profile.GameId] = profile;
                    }
                }
                catch (Exception ex) { _logger.Warning(ex, "Failed to load evolution profile"); }
            }
        }

        private void SaveProfile(PlaystyleProfile profile)
        {
            var path = Path.Combine(_profilesPath, $"{profile.GameId}.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public PlaystyleProfile? GetProfile(string gameId)
        {
            return _profiles.GetValueOrDefault(gameId);
        }
    }
}
