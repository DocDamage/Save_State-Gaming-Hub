using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Account;
using Serilog;

namespace SaveState.Core.Services.Gamification
{
    public enum ChallengeType
    {
        Daily,
        Weekly,
        Special
    }

    public enum ChallengeDifficulty
    {
        Easy,
        Medium,
        Hard,
        Extreme
    }

    public class Challenge
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "🎯";
        public ChallengeType Type { get; set; }
        public ChallengeDifficulty Difficulty { get; set; }
        public int TargetCount { get; set; }
        public int XPReward { get; set; }
        public string? BadgeReward { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive => DateTime.UtcNow >= StartsAt && DateTime.UtcNow < ExpiresAt;
    }

    public class ChallengeProgress
    {
        public string ChallengeId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int CurrentCount { get; set; }
        public bool IsComplete { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool RewardClaimed { get; set; }
    }

    public class ChallengeService
    {
        private static ChallengeService? _instance;
        private readonly ILogger _logger = Log.ForContext<ChallengeService>();
        private readonly string _dataPath;
        private readonly AuthService _authService;
        private readonly ProfileService _profileService;
        private readonly AchievementService _achievementService;
        private readonly List<Challenge> _challenges = new();
        private readonly Dictionary<string, List<ChallengeProgress>> _userProgress = new();
        private DateTime _lastChallengeGeneration;

        public event EventHandler<Challenge>? ChallengeCompleted;

        public static ChallengeService Instance => _instance ??= new ChallengeService();

        private ChallengeService()
        {
            _authService = AuthService.Instance;
            _profileService = ProfileService.Instance;
            _achievementService = AchievementService.Instance;
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "challenges");
            if (!Directory.Exists(_dataPath)) Directory.CreateDirectory(_dataPath);
            
            LoadChallenges();
            LoadProgress();
            GenerateDailyChallenges();
        }

        public List<Challenge> GetActiveChallenges()
        {
            RefreshChallengesIfNeeded();
            return _challenges.Where(c => c.IsActive).ToList();
        }

        public List<Challenge> GetDailyChallenges()
        {
            return GetActiveChallenges().Where(c => c.Type == ChallengeType.Daily).ToList();
        }

        public List<Challenge> GetWeeklyChallenges()
        {
            return GetActiveChallenges().Where(c => c.Type == ChallengeType.Weekly).ToList();
        }

        public ChallengeProgress? GetProgress(string challengeId)
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return null;

            return _userProgress.GetValueOrDefault(userId)?
                .FirstOrDefault(p => p.ChallengeId == challengeId);
        }

        public async Task<bool> IncrementProgressAsync(string challengeId, int amount = 1)
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return false;

            var challenge = _challenges.FirstOrDefault(c => c.Id == challengeId);
            if (challenge == null || !challenge.IsActive) return false;

            var progress = GetOrCreateProgress(userId, challengeId);
            if (progress.IsComplete) return false;

            progress.CurrentCount += amount;

            if (progress.CurrentCount >= challenge.TargetCount)
            {
                progress.IsComplete = true;
                progress.CompletedAt = DateTime.UtcNow;
                ChallengeCompleted?.Invoke(this, challenge);
                _logger.Information("Challenge complete: {Title}", challenge.Title);
            }

            SaveProgress();
            await Task.Yield();
            return true;
        }

        public async Task<bool> ClaimRewardAsync(string challengeId)
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return false;

            var challenge = _challenges.FirstOrDefault(c => c.Id == challengeId);
            var progress = GetProgress(challengeId);

            if (challenge == null || progress == null || !progress.IsComplete || progress.RewardClaimed)
                return false;

            progress.RewardClaimed = true;

            // Award XP (via profile service)
            for (int i = 0; i < challenge.XPReward / 10; i++)
            {
                _profileService.RecordGamePlayed(); // 10 XP each
            }

            // Award badge if any
            if (!string.IsNullOrEmpty(challenge.BadgeReward))
            {
                _profileService.AwardBadge(challenge.BadgeReward);
            }

            SaveProgress();
            await Task.Yield();
            return true;
        }

        public int GetCompletedTodayCount()
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return 0;

            var today = DateTime.UtcNow.Date;
            return _userProgress.GetValueOrDefault(userId)?
                .Count(p => p.IsComplete && p.CompletedAt?.Date == today) ?? 0;
        }

        public int GetStreak()
        {
            var userId = _authService.CurrentUser?.UserId;
            if (userId == null) return 0;

            var completions = _userProgress.GetValueOrDefault(userId)?
                .Where(p => p.IsComplete && p.CompletedAt != null)
                .Select(p => p.CompletedAt!.Value.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            if (completions == null || completions.Count == 0) return 0;

            int streak = 0;
            var checkDate = DateTime.UtcNow.Date;

            foreach (var date in completions)
            {
                if (date == checkDate || date == checkDate.AddDays(-1))
                {
                    streak++;
                    checkDate = date;
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        private void RefreshChallengesIfNeeded()
        {
            var now = DateTime.UtcNow;
            
            // Generate new daily challenges at midnight
            if (now.Date > _lastChallengeGeneration.Date)
            {
                GenerateDailyChallenges();
            }

            // Generate weekly challenges on Monday
            if (now.DayOfWeek == DayOfWeek.Monday && 
                _lastChallengeGeneration.DayOfWeek != DayOfWeek.Monday)
            {
                GenerateWeeklyChallenges();
            }
        }

        private void GenerateDailyChallenges()
        {
            // Remove expired daily challenges
            _challenges.RemoveAll(c => c.Type == ChallengeType.Daily && !c.IsActive);

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var dailyPool = new List<(string title, string desc, string icon, int target, int xp, ChallengeDifficulty diff)>
            {
                ("Battle Ready", "Win 3 battles", "⚔️", 3, 30, ChallengeDifficulty.Easy),
                ("Fusion Fun", "Create 2 fusions", "🧬", 2, 25, ChallengeDifficulty.Easy),
                ("Game Time", "Play for 30 minutes", "⏰", 30, 20, ChallengeDifficulty.Easy),
                ("Screenshot Spree", "Take 5 screenshots", "📸", 5, 15, ChallengeDifficulty.Easy),
                ("Victory Streak", "Win 5 battles in a row", "🔥", 5, 50, ChallengeDifficulty.Medium),
                ("Perfect Run", "Win a perfect battle", "💎", 1, 40, ChallengeDifficulty.Medium),
                ("Explorer", "Try 3 different features", "🧭", 3, 35, ChallengeDifficulty.Medium),
                ("Marathon", "Play for 2 hours", "🏃", 120, 75, ChallengeDifficulty.Hard),
                ("Battle Master", "Win 10 battles", "👑", 10, 100, ChallengeDifficulty.Hard),
            };

            var rand = new Random((int)today.Ticks);
            var selected = dailyPool.OrderBy(_ => rand.Next()).Take(3).ToList();

            foreach (var (title, desc, icon, target, xp, diff) in selected)
            {
                _challenges.Add(new Challenge
                {
                    Id = $"daily_{today:yyyyMMdd}_{Guid.NewGuid().ToString()[..8]}",
                    Title = title,
                    Description = desc,
                    Icon = icon,
                    Type = ChallengeType.Daily,
                    Difficulty = diff,
                    TargetCount = target,
                    XPReward = xp,
                    StartsAt = today,
                    ExpiresAt = tomorrow
                });
            }

            _lastChallengeGeneration = DateTime.UtcNow;
            SaveChallenges();
        }

        private void GenerateWeeklyChallenges()
        {
            _challenges.RemoveAll(c => c.Type == ChallengeType.Weekly && !c.IsActive);

            var monday = DateTime.UtcNow.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday) monday = monday.AddDays(-1);
            var nextMonday = monday.AddDays(7);

            _challenges.Add(new Challenge
            {
                Id = $"weekly_{monday:yyyyMMdd}_battles",
                Title = "Weekly Warrior",
                Description = "Win 25 battles this week",
                Icon = "🏆",
                Type = ChallengeType.Weekly,
                Difficulty = ChallengeDifficulty.Hard,
                TargetCount = 25,
                XPReward = 250,
                BadgeReward = "weekly_warrior",
                StartsAt = monday,
                ExpiresAt = nextMonday
            });

            _challenges.Add(new Challenge
            {
                Id = $"weekly_{monday:yyyyMMdd}_playtime",
                Title = "Dedicated Gamer",
                Description = "Play for 10 hours this week",
                Icon = "⌛",
                Type = ChallengeType.Weekly,
                Difficulty = ChallengeDifficulty.Hard,
                TargetCount = 600, // minutes
                XPReward = 300,
                StartsAt = monday,
                ExpiresAt = nextMonday
            });

            SaveChallenges();
        }

        private ChallengeProgress GetOrCreateProgress(string userId, string challengeId)
        {
            if (!_userProgress.ContainsKey(userId))
                _userProgress[userId] = new();

            var existing = _userProgress[userId].FirstOrDefault(p => p.ChallengeId == challengeId);
            if (existing == null)
            {
                existing = new ChallengeProgress
                {
                    ChallengeId = challengeId,
                    UserId = userId
                };
                _userProgress[userId].Add(existing);
            }
            return existing;
        }

        private void LoadChallenges()
        {
            var path = Path.Combine(_dataPath, "challenges.json");
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<List<Challenge>>(json);
                    if (loaded != null)
                    {
                        _challenges.AddRange(loaded.Where(c => c.IsActive));
                    }
                }
                catch (Exception ex) { _logger.Warning(ex, "Failed to load challenges"); }
            }
        }

        private void SaveChallenges()
        {
            var path = Path.Combine(_dataPath, "challenges.json");
            var json = JsonSerializer.Serialize(_challenges, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private void LoadProgress()
        {
            var path = Path.Combine(_dataPath, "challenge_progress.json");
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var all = JsonSerializer.Deserialize<List<ChallengeProgress>>(json);
                    if (all != null)
                    {
                        foreach (var p in all)
                        {
                            if (!_userProgress.ContainsKey(p.UserId))
                                _userProgress[p.UserId] = new();
                            _userProgress[p.UserId].Add(p);
                        }
                    }
                }
                catch (Exception ex) { _logger.Warning(ex, "Failed to load challenge progress"); }
            }
        }

        private void SaveProgress()
        {
            var path = Path.Combine(_dataPath, "challenge_progress.json");
            var all = _userProgress.Values.SelectMany(p => p).ToList();
            var json = JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
