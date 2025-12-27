using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Account
{
    public enum LeaderboardType
    {
        BattleWins,
        BattleRating,
        TotalPlayTime,
        GamesPlayed,
        Achievements,
        Fusions,
        Speedrun,
        Level
    }

    public class LeaderboardEntry
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Rank { get; set; }
        public long Score { get; set; }
        public string? AvatarPath { get; set; }
        public int Level { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class SpeedrunEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string Category { get; set; } = "Any%";
        public TimeSpan Time { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsVerified { get; set; }
        public string? VideoProof { get; set; }
    }

    public class LeaderboardService
    {
        private static LeaderboardService? _instance;
        private readonly string _dataPath;
        private readonly ProfileService _profileService;
        private readonly AuthService _authService;
        private readonly Dictionary<LeaderboardType, List<LeaderboardEntry>> _leaderboards = new();
        private readonly Dictionary<string, List<SpeedrunEntry>> _speedruns = new(); // By gameId

        public static LeaderboardService Instance => _instance ??= new LeaderboardService();

        private LeaderboardService()
        {
            _profileService = ProfileService.Instance;
            _authService = AuthService.Instance;
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "leaderboards");
            if (!Directory.Exists(_dataPath)) Directory.CreateDirectory(_dataPath);
            LoadLeaderboards();
        }

        public List<LeaderboardEntry> GetLeaderboard(LeaderboardType type, int limit = 100)
        {
            RefreshLeaderboard(type);
            return _leaderboards.GetValueOrDefault(type)?
                .Take(limit)
                .ToList() ?? new();
        }

        public LeaderboardEntry? GetUserRank(LeaderboardType type, string userId)
        {
            RefreshLeaderboard(type);
            return _leaderboards.GetValueOrDefault(type)?
                .FirstOrDefault(e => e.UserId == userId);
        }

        public int? GetMyRank(LeaderboardType type)
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return null;

            var entry = GetUserRank(type, userId);
            return entry?.Rank;
        }

        private void RefreshLeaderboard(LeaderboardType type)
        {
            var profiles = _profileService.GetPublicProfiles(1000);
            
            var entries = type switch
            {
                LeaderboardType.BattleWins => profiles.OrderByDescending(p => p.BattlesWon),
                LeaderboardType.BattleRating => profiles.OrderByDescending(p => 
                    p.BattlesWon + p.BattlesLost > 0 
                        ? (double)p.BattlesWon / (p.BattlesWon + p.BattlesLost) * 1000 
                        : 0),
                LeaderboardType.TotalPlayTime => profiles.OrderByDescending(p => p.TotalPlayTime),
                LeaderboardType.GamesPlayed => profiles.OrderByDescending(p => p.GamesPlayed),
                LeaderboardType.Achievements => profiles.OrderByDescending(p => p.AchievementsUnlocked),
                LeaderboardType.Fusions => profiles.OrderByDescending(p => p.FusionsCreated),
                LeaderboardType.Level => profiles.OrderByDescending(p => p.Level).ThenByDescending(p => p.XP),
                _ => profiles.OrderByDescending(p => p.Level)
            };

            var leaderboard = entries.Select((p, i) => new LeaderboardEntry
            {
                UserId = p.UserId,
                Username = p.Username,
                DisplayName = p.DisplayName,
                Rank = i + 1,
                Score = GetScoreForType(p, type),
                AvatarPath = p.AvatarPath,
                Level = p.Level,
                LastUpdated = DateTime.UtcNow
            }).ToList();

            _leaderboards[type] = leaderboard;
        }

        private long GetScoreForType(UserProfile profile, LeaderboardType type)
        {
            return type switch
            {
                LeaderboardType.BattleWins => profile.BattlesWon,
                LeaderboardType.BattleRating => (long)(profile.BattlesWon + profile.BattlesLost > 0 
                    ? (double)profile.BattlesWon / (profile.BattlesWon + profile.BattlesLost) * 1000 
                    : 0),
                LeaderboardType.TotalPlayTime => profile.TotalPlayTime,
                LeaderboardType.GamesPlayed => profile.GamesPlayed,
                LeaderboardType.Achievements => profile.AchievementsUnlocked,
                LeaderboardType.Fusions => profile.FusionsCreated,
                LeaderboardType.Level => profile.Level * 10000 + profile.XP,
                _ => profile.Level
            };
        }

        // Speedrun leaderboards
        public async Task<bool> SubmitSpeedrunAsync(string gameId, string gameName, TimeSpan time, 
            string category = "Any%", string? videoProof = null)
        {
            var userId = _authService.CurrentUser?.UserId;
            var username = _authService.CurrentUser?.Username;
            if (userId == null || username == null) return false;

            var entry = new SpeedrunEntry
            {
                UserId = userId,
                Username = username,
                GameId = gameId,
                GameName = gameName,
                Category = category,
                Time = time,
                SubmittedAt = DateTime.UtcNow,
                VideoProof = videoProof,
                IsVerified = false
            };

            if (!_speedruns.ContainsKey(gameId))
                _speedruns[gameId] = new();

            // Check if this beats user's previous time
            var existing = _speedruns[gameId]
                .FirstOrDefault(s => s.UserId == userId && s.Category == category);

            if (existing != null && existing.Time <= time)
                return false; // Not a new record

            if (existing != null)
                _speedruns[gameId].Remove(existing);

            _speedruns[gameId].Add(entry);
            SaveSpeedruns();

            await Task.Yield();
            return true;
        }

        public List<SpeedrunEntry> GetSpeedrunLeaderboard(string gameId, string category = "Any%", int limit = 50)
        {
            if (!_speedruns.TryGetValue(gameId, out var runs)) return new();

            return runs
                .Where(r => r.Category == category)
                .OrderBy(r => r.Time)
                .Take(limit)
                .ToList();
        }

        public List<SpeedrunEntry> GetUserSpeedruns(string userId)
        {
            return _speedruns.Values
                .SelectMany(r => r)
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.Time)
                .ToList();
        }

        public SpeedrunEntry? GetWorldRecord(string gameId, string category = "Any%")
        {
            return GetSpeedrunLeaderboard(gameId, category, 1).FirstOrDefault();
        }

        public async Task<bool> VerifySpeedrunAsync(string entryId)
        {
            // In production: Would require mod/admin privileges
            foreach (var runs in _speedruns.Values)
            {
                var entry = runs.FirstOrDefault(r => r.Id == entryId);
                if (entry != null)
                {
                    entry.IsVerified = true;
                    SaveSpeedruns();
                    await Task.Yield();
                    return true;
                }
            }
            return false;
        }

        public List<string> GetLeaderboardTypes()
        {
            return Enum.GetNames<LeaderboardType>().ToList();
        }

        public string FormatScore(LeaderboardType type, long score)
        {
            return type switch
            {
                LeaderboardType.TotalPlayTime => FormatPlayTime(score),
                LeaderboardType.BattleRating => $"{score / 10.0:F1} rating",
                _ => score.ToString("N0")
            };
        }

        private string FormatPlayTime(long minutes)
        {
            var hours = minutes / 60;
            var mins = minutes % 60;
            return hours > 0 ? $"{hours}h {mins}m" : $"{mins}m";
        }

        private void LoadLeaderboards()
        {
            // Load speedruns
            var speedrunPath = Path.Combine(_dataPath, "speedruns.json");
            if (File.Exists(speedrunPath))
            {
                try
                {
                    var json = File.ReadAllText(speedrunPath);
                    var runs = JsonSerializer.Deserialize<List<SpeedrunEntry>>(json);
                    if (runs != null)
                    {
                        foreach (var run in runs)
                        {
                            if (!_speedruns.ContainsKey(run.GameId))
                                _speedruns[run.GameId] = new();
                            _speedruns[run.GameId].Add(run);
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}"); }
            }
        }

        private void SaveSpeedruns()
        {
            var speedrunPath = Path.Combine(_dataPath, "speedruns.json");
            var allRuns = _speedruns.Values.SelectMany(r => r).ToList();
            var json = JsonSerializer.Serialize(allRuns, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(speedrunPath, json);
        }
    }
}
