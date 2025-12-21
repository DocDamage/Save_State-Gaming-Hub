using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Account
{
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarPath { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LastActive { get; set; }

        // Stats
        public int TotalPlayTime { get; set; }  // Minutes
        public int GamesPlayed { get; set; }
        public int AchievementsUnlocked { get; set; }
        public int BattlesWon { get; set; }
        public int BattlesLost { get; set; }
        public int FusionsCreated { get; set; }
        public int CapsulesCreated { get; set; }
        public int ScreenshotsTaken { get; set; }

        // Preferences
        public bool IsPublic { get; set; } = true;
        public bool ShowPlayTime { get; set; } = true;
        public bool ShowStats { get; set; } = true;
        public List<string> FavoriteGames { get; set; } = new();
        public Dictionary<string, string> SocialLinks { get; set; } = new();

        // Badges/titles
        public List<string> Badges { get; set; } = new();
        public string? CurrentTitle { get; set; }
        public int Level { get; set; } = 1;
        public int XP { get; set; }
    }

    public class ProfileService
    {
        private static ProfileService? _instance;
        private readonly string _profilesPath;
        private readonly Dictionary<string, UserProfile> _profiles = new();
        private readonly AuthService _authService;

        public static ProfileService Instance => _instance ??= new ProfileService();

        private ProfileService()
        {
            _authService = AuthService.Instance;
            _profilesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "profiles");
            if (!Directory.Exists(_profilesPath)) Directory.CreateDirectory(_profilesPath);
            LoadProfiles();

            _authService.UserChanged += (s, user) =>
            {
                if (user != null) EnsureProfileExists(user.UserId, user.Username);
            };
        }

        public UserProfile? GetCurrentProfile()
        {
            var userId = _authService.CurrentUser?.UserId;
            return userId != null ? GetProfile(userId) : null;
        }

        public UserProfile? GetProfile(string userId)
        {
            return _profiles.GetValueOrDefault(userId);
        }

        public UserProfile? GetProfileByUsername(string username)
        {
            return _profiles.Values.FirstOrDefault(p => 
                p.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public List<UserProfile> GetPublicProfiles(int limit = 50)
        {
            return _profiles.Values
                .Where(p => p.IsPublic)
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.XP)
                .Take(limit)
                .ToList();
        }

        public List<UserProfile> SearchProfiles(string query, int limit = 20)
        {
            var queryLower = query.ToLower();
            return _profiles.Values
                .Where(p => p.IsPublic && 
                    (p.Username.ToLower().Contains(queryLower) || 
                     p.DisplayName.ToLower().Contains(queryLower)))
                .Take(limit)
                .ToList();
        }

        private void EnsureProfileExists(string userId, string username)
        {
            if (!_profiles.ContainsKey(userId))
            {
                _profiles[userId] = new UserProfile
                {
                    UserId = userId,
                    Username = username,
                    DisplayName = username,
                    JoinedAt = DateTime.UtcNow
                };
                SaveProfile(_profiles[userId]);
            }
        }

        public async Task<bool> UpdateProfileAsync(UserProfile profile)
        {
            if (_authService.CurrentUser?.UserId != profile.UserId)
                return false; // Can only update own profile

            profile.LastActive = DateTime.UtcNow;
            _profiles[profile.UserId] = profile;
            SaveProfile(profile);
            
            await Task.Yield();
            return true;
        }

        public async Task<bool> UpdateDisplayNameAsync(string displayName)
        {
            var profile = GetCurrentProfile();
            if (profile == null) return false;

            profile.DisplayName = displayName;
            return await UpdateProfileAsync(profile);
        }

        public async Task<bool> UpdateBioAsync(string bio)
        {
            var profile = GetCurrentProfile();
            if (profile == null) return false;

            profile.Bio = bio?.Length > 500 ? bio[..500] : bio;
            return await UpdateProfileAsync(profile);
        }

        public async Task<bool> SetAvatarAsync(string imagePath)
        {
            var profile = GetCurrentProfile();
            if (profile == null || !File.Exists(imagePath)) return false;

            var avatarDir = Path.Combine(_profilesPath, "avatars");
            if (!Directory.Exists(avatarDir)) Directory.CreateDirectory(avatarDir);

            var ext = Path.GetExtension(imagePath);
            var avatarPath = Path.Combine(avatarDir, $"{profile.UserId}{ext}");
            
            File.Copy(imagePath, avatarPath, true);
            profile.AvatarPath = avatarPath;
            
            return await UpdateProfileAsync(profile);
        }

        public async Task<bool> AddFavoriteGameAsync(string gameId)
        {
            var profile = GetCurrentProfile();
            if (profile == null) return false;

            if (!profile.FavoriteGames.Contains(gameId))
            {
                profile.FavoriteGames.Add(gameId);
                return await UpdateProfileAsync(profile);
            }
            return true;
        }

        public async Task<bool> RemoveFavoriteGameAsync(string gameId)
        {
            var profile = GetCurrentProfile();
            if (profile == null) return false;

            profile.FavoriteGames.Remove(gameId);
            return await UpdateProfileAsync(profile);
        }

        public void RecordPlayTime(int minutes)
        {
            var profile = GetCurrentProfile();
            if (profile == null) return;

            profile.TotalPlayTime += minutes;
            profile.LastActive = DateTime.UtcNow;
            AddXP(minutes); // 1 XP per minute
            SaveProfile(profile);
        }

        public void RecordGamePlayed()
        {
            var profile = GetCurrentProfile();
            if (profile == null) return;

            profile.GamesPlayed++;
            AddXP(10);
            SaveProfile(profile);
        }

        public void RecordBattleResult(bool won)
        {
            var profile = GetCurrentProfile();
            if (profile == null) return;

            if (won)
            {
                profile.BattlesWon++;
                AddXP(25);
            }
            else
            {
                profile.BattlesLost++;
                AddXP(5);
            }
            SaveProfile(profile);
        }

        public void RecordFusion()
        {
            var profile = GetCurrentProfile();
            if (profile == null) return;

            profile.FusionsCreated++;
            AddXP(20);
            SaveProfile(profile);
        }

        public void RecordAchievement()
        {
            var profile = GetCurrentProfile();
            if (profile == null) return;

            profile.AchievementsUnlocked++;
            AddXP(50);
            SaveProfile(profile);
        }

        private void AddXP(int amount)
        {
            var profile = GetCurrentProfile();
            if (profile == null) return;

            profile.XP += amount;

            // Level up check (100 XP per level, exponential)
            var xpNeeded = profile.Level * 100;
            while (profile.XP >= xpNeeded)
            {
                profile.XP -= xpNeeded;
                profile.Level++;
                xpNeeded = profile.Level * 100;
                Console.WriteLine($"🎉 Level Up! Now level {profile.Level}");
            }
        }

        public void AwardBadge(string badgeId)
        {
            var profile = GetCurrentProfile();
            if (profile == null) return;

            if (!profile.Badges.Contains(badgeId))
            {
                profile.Badges.Add(badgeId);
                SaveProfile(profile);
            }
        }

        public int GetXPForNextLevel()
        {
            var profile = GetCurrentProfile();
            if (profile == null) return 100;
            return profile.Level * 100;
        }

        public double GetLevelProgress()
        {
            var profile = GetCurrentProfile();
            if (profile == null) return 0;
            return (double)profile.XP / GetXPForNextLevel();
        }

        private void LoadProfiles()
        {
            if (!Directory.Exists(_profilesPath)) return;

            foreach (var file in Directory.GetFiles(_profilesPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<UserProfile>(json);
                    if (profile != null)
                    {
                        _profiles[profile.UserId] = profile;
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}"); }
            }
        }

        private void SaveProfile(UserProfile profile)
        {
            var path = Path.Combine(_profilesPath, $"{profile.UserId}.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
