using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Account;

namespace SaveState.Core.Services.Gamification
{
    public enum AchievementCategory
    {
        General,
        Battle,
        Fusion,
        Collection,
        Social,
        Speedrun,
        Explorer,
        Creator,
        Secret
    }

    public enum AchievementRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public class Achievement
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "🏆";
        public AchievementCategory Category { get; set; }
        public AchievementRarity Rarity { get; set; }
        public int XPReward { get; set; }
        public bool IsHidden { get; set; }
        public string? UnlockCondition { get; set; }
        public int RequiredCount { get; set; } = 1;
    }

    public class UnlockedAchievement
    {
        public string AchievementId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime UnlockedAt { get; set; }
        public int Progress { get; set; }
        public bool IsComplete { get; set; }
    }

    public class AchievementService
    {
        private static AchievementService? _instance;
        private readonly string _dataPath;
        private readonly AuthService _authService;
        private readonly ProfileService _profileService;
        private readonly Dictionary<string, Achievement> _achievements = new();
        private readonly Dictionary<string, List<UnlockedAchievement>> _userAchievements = new();

        public event EventHandler<Achievement>? AchievementUnlocked;

        public static AchievementService Instance => _instance ??= new AchievementService();

        private AchievementService()
        {
            _authService = AuthService.Instance;
            _profileService = ProfileService.Instance;
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "achievements");
            if (!Directory.Exists(_dataPath)) Directory.CreateDirectory(_dataPath);
            
            InitializeAchievements();
            LoadUserAchievements();
        }

        private void InitializeAchievements()
        {
            // General
            Register("first_launch", "Welcome!", "Launch SaveState for the first time", "🎮", AchievementCategory.General, AchievementRarity.Common, 10);
            Register("play_10_hours", "Dedicated Gamer", "Play for 10 hours total", "⏰", AchievementCategory.General, AchievementRarity.Uncommon, 50);
            Register("play_100_hours", "Gaming Legend", "Play for 100 hours total", "👑", AchievementCategory.General, AchievementRarity.Epic, 200);

            // Battle
            Register("first_battle", "Fighter", "Win your first battle", "⚔️", AchievementCategory.Battle, AchievementRarity.Common, 15);
            Register("win_10_battles", "Warrior", "Win 10 battles", "🗡️", AchievementCategory.Battle, AchievementRarity.Uncommon, 50);
            Register("win_100_battles", "Champion", "Win 100 battles", "🏆", AchievementCategory.Battle, AchievementRarity.Rare, 150);
            Register("perfect_victory", "Flawless", "Win a battle without taking damage", "💎", AchievementCategory.Battle, AchievementRarity.Rare, 75);
            Register("tournament_winner", "Tournament King", "Win a tournament", "👑", AchievementCategory.Battle, AchievementRarity.Epic, 200);

            // Fusion
            Register("first_fusion", "Mad Scientist", "Create your first fusion", "🧬", AchievementCategory.Fusion, AchievementRarity.Common, 20);
            Register("create_10_fusions", "Fusion Master", "Create 10 fusions", "⚗️", AchievementCategory.Fusion, AchievementRarity.Uncommon, 75);
            Register("legendary_fusion", "Legend Creator", "Create a legendary fusion", "✨", AchievementCategory.Fusion, AchievementRarity.Epic, 150);

            // Collection
            Register("collect_10_games", "Collector", "Add 10 games to your library", "📚", AchievementCategory.Collection, AchievementRarity.Common, 25);
            Register("collect_100_games", "Hoarder", "Add 100 games to your library", "🏛️", AchievementCategory.Collection, AchievementRarity.Rare, 100);
            Register("complete_game", "Completionist", "100% a game", "💯", AchievementCategory.Collection, AchievementRarity.Rare, 100);

            // Social
            Register("add_friend", "Social Butterfly", "Add your first friend", "🤝", AchievementCategory.Social, AchievementRarity.Common, 15);
            Register("10_friends", "Popular", "Have 10 friends", "🌟", AchievementCategory.Social, AchievementRarity.Uncommon, 50);
            Register("share_capsule", "Time Traveler", "Share a time capsule", "📦", AchievementCategory.Social, AchievementRarity.Uncommon, 30);

            // Speedrun
            Register("first_speedrun", "Speed Demon", "Submit a speedrun", "⚡", AchievementCategory.Speedrun, AchievementRarity.Uncommon, 40);
            Register("world_record", "World's Fastest", "Hold a world record", "🌍", AchievementCategory.Speedrun, AchievementRarity.Legendary, 500);

            // Explorer
            Register("use_all_features", "Explorer", "Use every main feature", "🧭", AchievementCategory.Explorer, AchievementRarity.Rare, 100);
            Register("customize_theme", "Stylist", "Customize your theme", "🎨", AchievementCategory.Explorer, AchievementRarity.Common, 15);

            // Creator
            Register("take_screenshot", "Photographer", "Take a screenshot", "📸", AchievementCategory.Creator, AchievementRarity.Common, 10);
            Register("record_gameplay", "Director", "Record gameplay", "🎬", AchievementCategory.Creator, AchievementRarity.Uncommon, 25);
            Register("create_montage", "Editor", "Create a highlight montage", "🎞️", AchievementCategory.Creator, AchievementRarity.Rare, 75);

            // Secret
            Register("konami_code", "Retro Master", "???", "🕹️", AchievementCategory.Secret, AchievementRarity.Legendary, 100, true);
            Register("midnight_gamer", "Night Owl", "Play at midnight", "🦉", AchievementCategory.Secret, AchievementRarity.Rare, 50, true);
        }

        private void Register(string id, string name, string description, string icon,
            AchievementCategory category, AchievementRarity rarity, int xp, bool hidden = false, int count = 1)
        {
            _achievements[id] = new Achievement
            {
                Id = id,
                Name = name,
                Description = description,
                Icon = icon,
                Category = category,
                Rarity = rarity,
                XPReward = xp,
                IsHidden = hidden,
                RequiredCount = count
            };
        }

        public List<Achievement> GetAllAchievements(bool includeHidden = false)
        {
            return _achievements.Values
                .Where(a => !a.IsHidden || includeHidden)
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Rarity)
                .ToList();
        }

        public List<Achievement> GetAchievementsByCategory(AchievementCategory category)
        {
            return _achievements.Values
                .Where(a => a.Category == category && !a.IsHidden)
                .ToList();
        }

        public List<UnlockedAchievement> GetUserAchievements(string? userId = null)
        {
            userId ??= _authService.CurrentUser?.UserId;
            if (userId == null) return new();

            return _userAchievements.GetValueOrDefault(userId) ?? new();
        }

        public List<Achievement> GetUnlockedAchievements(string? userId = null)
        {
            var unlocked = GetUserAchievements(userId);
            return unlocked
                .Where(u => u.IsComplete)
                .Select(u => _achievements.GetValueOrDefault(u.AchievementId))
                .Where(a => a != null)
                .Cast<Achievement>()
                .ToList();
        }

        public double GetCompletionPercentage(string? userId = null)
        {
            var total = _achievements.Count(a => !a.Value.IsHidden);
            var unlocked = GetUnlockedAchievements(userId).Count(a => !a.IsHidden);
            return total > 0 ? (double)unlocked / total * 100 : 0;
        }

        public async Task<bool> UnlockAsync(string achievementId)
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return false;

            if (!_achievements.TryGetValue(achievementId, out var achievement))
                return false;

            var unlocked = GetOrCreateProgress(userId, achievementId);
            if (unlocked.IsComplete) return false; // Already unlocked

            unlocked.Progress = achievement.RequiredCount;
            unlocked.IsComplete = true;
            unlocked.UnlockedAt = DateTime.UtcNow;

            SaveUserAchievements();

            // Award XP
            _profileService.RecordAchievement();

            AchievementUnlocked?.Invoke(this, achievement);
            Console.WriteLine($"🏆 Achievement Unlocked: {achievement.Name}!");

            await Task.Yield();
            return true;
        }

        public async Task<bool> IncrementProgressAsync(string achievementId, int amount = 1)
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return false;

            if (!_achievements.TryGetValue(achievementId, out var achievement))
                return false;

            var unlocked = GetOrCreateProgress(userId, achievementId);
            if (unlocked.IsComplete) return false;

            unlocked.Progress += amount;

            if (unlocked.Progress >= achievement.RequiredCount)
            {
                return await UnlockAsync(achievementId);
            }

            SaveUserAchievements();
            await Task.Yield();
            return true;
        }

        public int GetProgress(string achievementId)
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return 0;

            var unlocked = _userAchievements.GetValueOrDefault(userId)?
                .FirstOrDefault(u => u.AchievementId == achievementId);
            return unlocked?.Progress ?? 0;
        }

        public bool IsUnlocked(string achievementId)
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return false;

            return _userAchievements.GetValueOrDefault(userId)?
                .Any(u => u.AchievementId == achievementId && u.IsComplete) ?? false;
        }

        // Trigger checks for automatic achievements
        public void CheckBattleAchievements(int totalWins, bool isPerfect)
        {
            if (totalWins >= 1) _ = UnlockAsync("first_battle");
            if (totalWins >= 10) _ = UnlockAsync("win_10_battles");
            if (totalWins >= 100) _ = UnlockAsync("win_100_battles");
            if (isPerfect) _ = UnlockAsync("perfect_victory");
        }

        public void CheckFusionAchievements(int totalFusions, bool isLegendary)
        {
            if (totalFusions >= 1) _ = UnlockAsync("first_fusion");
            if (totalFusions >= 10) _ = UnlockAsync("create_10_fusions");
            if (isLegendary) _ = UnlockAsync("legendary_fusion");
        }

        public void CheckTimeAchievements()
        {
            var hour = DateTime.Now.Hour;
            if (hour == 0) _ = UnlockAsync("midnight_gamer");
        }

        private UnlockedAchievement GetOrCreateProgress(string userId, string achievementId)
        {
            if (!_userAchievements.ContainsKey(userId))
                _userAchievements[userId] = new();

            var existing = _userAchievements[userId]
                .FirstOrDefault(u => u.AchievementId == achievementId);

            if (existing == null)
            {
                existing = new UnlockedAchievement
                {
                    AchievementId = achievementId,
                    UserId = userId
                };
                _userAchievements[userId].Add(existing);
            }

            return existing;
        }

        private void LoadUserAchievements()
        {
            var path = Path.Combine(_dataPath, "user_achievements.json");
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var all = JsonSerializer.Deserialize<List<UnlockedAchievement>>(json);
                    if (all != null)
                    {
                        foreach (var ua in all)
                        {
                            if (!_userAchievements.ContainsKey(ua.UserId))
                                _userAchievements[ua.UserId] = new();
                            _userAchievements[ua.UserId].Add(ua);
                        }
                    }
                }
                catch { }
            }
        }

        private void SaveUserAchievements()
        {
            var path = Path.Combine(_dataPath, "user_achievements.json");
            var all = _userAchievements.Values.SelectMany(u => u).ToList();
            var json = JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
