using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.MugenAchievements;

/// <summary>
/// MUGEN achievement and progression system plugin.
/// Tracks player progress, unlocks achievements, and provides goals.
/// </summary>
public class MugenAchievementSystemPlugin : IPlugin
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private readonly PlayerProgress _playerProgress = new();
    private readonly List<MugenAchievement> _achievements = new();
    private readonly List<ProgressionGoal> _goals = new();

    public string Id => "savestate.mugen.achievements";
    public string Name => "MUGEN Achievement System";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Track progress and unlock achievements in MUGEN";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation("Initializing MUGEN Achievement System plugin");

        // Initialize achievements and goals
        InitializeAchievements();
        InitializeGoals();

        // Register menu items
        var achievementsMenuItem = new PluginMenuItem(
            Id: "mugen.achievements.view",
            Label: "View Achievements",
            Icon: "🏆",
            SortOrder: 330,
            Action: ShowAchievementsAsync);

        var goalsMenuItem = new PluginMenuItem(
            Id: "mugen.achievements.goals",
            Label: "Progression Goals",
            Icon: "🎯",
            SortOrder: 331,
            Action: ShowGoalsAsync);

        var statsMenuItem = new PluginMenuItem(
            Id: "mugen.achievements.stats",
            Label: "Player Statistics",
            Icon: "📊",
            SortOrder: 332,
            Action: ShowStatisticsAsync);

        var leaderboardMenuItem = new PluginMenuItem(
            Id: "mugen.achievements.leaderboard",
            Label: "Achievement Leaderboard",
            Icon: "🥇",
            SortOrder: 333,
            Action: ShowLeaderboardAsync);

        await context.RegisterMenuItemAsync(achievementsMenuItem);
        await context.RegisterMenuItemAsync(goalsMenuItem);
        await context.RegisterMenuItemAsync(statsMenuItem);
        await context.RegisterMenuItemAsync(leaderboardMenuItem);

        // Load player progress
        await LoadPlayerProgressAsync(ct);

        _logger.LogInformation("MUGEN Achievement System plugin initialized successfully");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down MUGEN Achievement System plugin");

        // Save progress before shutdown
        _ = SavePlayerProgressAsync();

        return Task.CompletedTask;
    }

    private void InitializeAchievements()
    {
        _achievements.AddRange(new[]
        {
            new MugenAchievement
            {
                Id = "first_victory",
                Name = "First Victory",
                Description = "Win your first MUGEN match",
                Icon = "🥊",
                Category = AchievementCategory.Combat,
                Difficulty = AchievementDifficulty.Bronze,
                Points = 10,
                IsHidden = false
            },

            new MugenAchievement
            {
                Id = "combo_master",
                Name = "Combo Master",
                Description = "Land a 20-hit combo",
                Icon = "💥",
                Category = AchievementCategory.Combat,
                Difficulty = AchievementDifficulty.Silver,
                Points = 25,
                IsHidden = false
            },

            new MugenAchievement
            {
                Id = "perfect_round",
                Name = "Perfect Round",
                Description = "Win a round without taking damage",
                Icon = "🛡️",
                Category = AchievementCategory.Combat,
                Difficulty = AchievementDifficulty.Gold,
                Points = 50,
                IsHidden = false
            },

            new MugenAchievement
            {
                Id = "character_collector",
                Name = "Character Collector",
                Description = "Use 10 different characters",
                Icon = "👥",
                Category = AchievementCategory.Collection,
                Difficulty = AchievementDifficulty.Silver,
                Points = 30,
                IsHidden = false
            },

            new MugenAchievement
            {
                Id = "stage_explorer",
                Name = "Stage Explorer",
                Description = "Fight on 5 different stages",
                Icon = "🏟️",
                Category = AchievementCategory.Exploration,
                Difficulty = AchievementDifficulty.Bronze,
                Points = 15,
                IsHidden = false
            },

            new MugenAchievement
            {
                Id = "training_devotee",
                Name = "Training Devotee",
                Description = "Spend 10 hours in training mode",
                Icon = "🎓",
                Category = AchievementCategory.Training,
                Difficulty = AchievementDifficulty.Silver,
                Points = 40,
                IsHidden = false
            },

            new MugenAchievement
            {
                Id = "replay_enthusiast",
                Name = "Replay Enthusiast",
                Description = "Save 50 match replays",
                Icon = "🎥",
                Category = AchievementCategory.Social,
                Difficulty = AchievementDifficulty.Gold,
                Points = 60,
                IsHidden = false
            },

            new MugenAchievement
            {
                Id = "legendary_warrior",
                Name = "Legendary Warrior",
                Description = "Achieve 100 total victories",
                Icon = "👑",
                Category = AchievementCategory.Combat,
                Difficulty = AchievementDifficulty.Platinum,
                Points = 100,
                IsHidden = true
            }
        });

        _logger?.LogInformation("Initialized {Count} achievements", _achievements.Count);
    }

    private void InitializeGoals()
    {
        _goals.AddRange(new[]
        {
            new ProgressionGoal
            {
                Id = "daily_matches",
                Title = "Daily Warrior",
                Description = "Play 5 matches today",
                TargetValue = 5,
                CurrentValue = 0,
                GoalType = GoalType.Daily,
                Reward = new GoalReward { Type = RewardType.Experience, Value = 50 }
            },

            new ProgressionGoal
            {
                Id = "weekly_training",
                Title = "Training Week",
                Description = "Train for 2 hours this week",
                TargetValue = 120, // minutes
                CurrentValue = 0,
                GoalType = GoalType.Weekly,
                Reward = new GoalReward { Type = RewardType.Title, Value = "Training Enthusiast" }
            },

            new ProgressionGoal
            {
                Id = "character_mastery",
                Title = "Character Mastery",
                Description = "Win 10 matches with the same character",
                TargetValue = 10,
                CurrentValue = 0,
                GoalType = GoalType.CharacterSpecific,
                Reward = new GoalReward { Type = RewardType.Badge, Value = "Character Master" }
            },

            new ProgressionGoal
            {
                Id = "combo_challenge",
                Title = "Combo Challenger",
                Description = "Land combos of increasing difficulty",
                TargetValue = 5,
                CurrentValue = 0,
                GoalType = GoalType.Progressive,
                Reward = new GoalReward { Type = RewardType.Unlocks, Value = "Advanced Combos" }
            }
        });

        _logger?.LogInformation("Initialized {Count} progression goals", _goals.Count);
    }

    private async Task ShowAchievementsAsync()
    {
        try
        {
            _logger?.LogInformation("Showing MUGEN achievements");

            _logger?.LogInformation("🏆 MUGEN Achievements");
            _logger?.LogInformation("Total Achievements: {Total} | Unlocked: {Unlocked}",
                _achievements.Count,
                _playerProgress.UnlockedAchievements.Count);

            var categories = _achievements.GroupBy(a => a.Category);

            foreach (var category in categories)
            {
                _logger?.LogInformation("📂 {Category}:", category.Key);

                foreach (var achievement in category)
                {
                    var isUnlocked = _playerProgress.UnlockedAchievements.Contains(achievement.Id);
                    var status = isUnlocked ? "✅" : "❌";
                    var difficulty = GetDifficultyIcon(achievement.Difficulty);

                    _logger?.LogInformation("  {Status} {Icon} {Name} ({Difficulty}) - {Points}pts",
                        status, achievement.Icon, achievement.Name, difficulty, achievement.Points);

                    if (!isUnlocked && !achievement.IsHidden)
                    {
                        _logger?.LogInformation("    {Description}", achievement.Description);
                    }
                }
            }

            // Show progress towards next level
            var currentLevel = GetPlayerLevel(_playerProgress.TotalPoints);
            var nextLevelPoints = GetPointsForLevel(currentLevel + 1);
            var progressPercent = (_playerProgress.TotalPoints / (float)nextLevelPoints) * 100;

            _logger?.LogInformation("📊 Level Progress:");
            _logger?.LogInformation("Current Level: {Level} ({Points}/{NextPoints} points - {Percent:F1}%)",
                currentLevel, _playerProgress.TotalPoints, nextLevelPoints, progressPercent);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing achievements");
        }
    }

    private async Task ShowGoalsAsync()
    {
        try
        {
            _logger?.LogInformation("Showing progression goals");

            _logger?.LogInformation("🎯 Progression Goals");

            foreach (var goal in _goals)
            {
                var progressPercent = (goal.CurrentValue / (float)goal.TargetValue) * 100;
                var status = goal.IsCompleted ? "✅" : "⏳";

                _logger?.LogInformation("{Status} {Title} - {Current}/{Target} ({Percent:F1}%)",
                    status, goal.Title, goal.CurrentValue, goal.TargetValue, progressPercent);

                if (!goal.IsCompleted)
                {
                    _logger?.LogInformation("  {Description}", goal.Description);
                    _logger?.LogInformation("  Reward: {Type} - {Value}", goal.Reward.Type, goal.Reward.Value);
                }
            }

            // Show daily/weekly reset timers
            var now = DateTime.UtcNow;
            var tomorrow = now.Date.AddDays(1);
            var nextWeek = now.Date.AddDays(7 - (int)now.DayOfWeek);

            _logger?.LogInformation("⏰ Reset Timers:");
            _logger?.LogInformation("- Daily goals reset in: {Time}", (tomorrow - now).ToString(@"hh\:mm\:ss"));
            _logger?.LogInformation("- Weekly goals reset in: {Time}", (nextWeek - now).ToString(@"dd\:hh\:mm\:ss"));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing goals");
        }
    }

    private async Task ShowStatisticsAsync()
    {
        try
        {
            _logger?.LogInformation("Showing player statistics");

            _logger?.LogInformation("📊 Player Statistics - {PlayerName}", _playerProgress.PlayerName);

            _logger?.LogInformation("Combat Stats:");
            _logger?.LogInformation("- Total Matches: {Count}", _playerProgress.TotalMatches);
            _logger?.LogInformation("- Victories: {Count} ({Percent:F1}%)",
                _playerProgress.TotalVictories,
                _playerProgress.TotalMatches > 0 ? (_playerProgress.TotalVictories / (float)_playerProgress.TotalMatches) * 100 : 0);
            _logger?.LogInformation("- Total Combos Landed: {Count}", _playerProgress.TotalCombos);
            _logger?.LogInformation("- Average Combo Length: {Average:F1}",
                _playerProgress.TotalCombos > 0 ? _playerProgress.TotalComboHits / (float)_playerProgress.TotalCombos : 0);

            _logger?.LogInformation("Training Stats:");
            _logger?.LogInformation("- Training Time: {Time}", _playerProgress.TotalTrainingTime);
            _logger?.LogInformation("- Characters Used: {Count}", _playerProgress.CharactersUsed.Count);
            _logger?.LogInformation("- Stages Played: {Count}", _playerProgress.StagesPlayed.Count);

            _logger?.LogInformation("Collection Stats:");
            _logger?.LogInformation("- Replays Saved: {Count}", _playerProgress.ReplaysSaved);
            _logger?.LogInformation("- Achievements Unlocked: {Count}/{Total}",
                _playerProgress.UnlockedAchievements.Count, _achievements.Count);

            var currentLevel = GetPlayerLevel(_playerProgress.TotalPoints);
            _logger?.LogInformation("Progression:");
            _logger?.LogInformation("- Current Level: {Level}", currentLevel);
            _logger?.LogInformation("- Total Points: {Points}", _playerProgress.TotalPoints);
            _logger?.LogInformation("- Next Level: {Points} points needed",
                GetPointsForLevel(currentLevel + 1) - _playerProgress.TotalPoints);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing statistics");
        }
    }

    private async Task ShowLeaderboardAsync()
    {
        try
        {
            _logger?.LogInformation("Showing achievement leaderboard");

            _logger?.LogInformation("🥇 Achievement Leaderboard");

            // In a real implementation, this would fetch from a server
            // For demo, show simulated leaderboard
            var leaderboard = new[]
            {
                new { Name = "You", Points = _playerProgress.TotalPoints, Level = GetPlayerLevel(_playerProgress.TotalPoints) },
                new { Name = "MUGEN Master", Points = 2500, Level = 15 },
                new { Name = "Combo King", Points = 2200, Level = 14 },
                new { Name = "Training Devotee", Points = 1800, Level = 12 },
                new { Name = "Achievement Hunter", Points = 1600, Level = 11 }
            };

            for (int i = 0; i < leaderboard.Length; i++)
            {
                var entry = leaderboard[i];
                var medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => "📊" };

                _logger?.LogInformation("{Medal} #{Rank} {Name} - Level {Level} ({Points} points)",
                    medal, i + 1, entry.Name, entry.Level, entry.Points);
            }

            _logger?.LogInformation("🏆 Global Challenges:");
            _logger?.LogInformation("- Most Victories: MUGEN Master (500 wins)");
            _logger?.LogInformation("- Longest Combo: Combo King (47 hits)");
            _logger?.LogInformation("- Training Time: Training Devotee (50 hours)");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing leaderboard");
        }
    }

    private async Task LoadPlayerProgressAsync(CancellationToken ct = default)
    {
        try
        {
            if (_context == null) return;

            var progressPath = Path.Combine(_context.PluginDirectory, "player_progress.json");
            if (File.Exists(progressPath))
            {
                var json = await File.ReadAllTextAsync(progressPath, ct);
                var progress = JsonSerializer.Deserialize<PlayerProgress>(json);
                if (progress != null)
                {
                    // Copy loaded progress to our instance
                    _playerProgress.PlayerName = progress.PlayerName;
                    _playerProgress.TotalPoints = progress.TotalPoints;
                    _playerProgress.TotalMatches = progress.TotalMatches;
                    _playerProgress.TotalVictories = progress.TotalVictories;
                    _playerProgress.TotalCombos = progress.TotalCombos;
                    _playerProgress.TotalComboHits = progress.TotalComboHits;
                    _playerProgress.TotalTrainingTime = progress.TotalTrainingTime;
                    _playerProgress.UnlockedAchievements.Clear();
                    _playerProgress.UnlockedAchievements.AddRange(progress.UnlockedAchievements);
                    _playerProgress.CharactersUsed.Clear();
                    _playerProgress.CharactersUsed.UnionWith(progress.CharactersUsed);
                    _playerProgress.StagesPlayed.Clear();
                    _playerProgress.StagesPlayed.UnionWith(progress.StagesPlayed);
                    _playerProgress.ReplaysSaved = progress.ReplaysSaved;
                }
            }

            _logger?.LogInformation("Loaded player progress: {Points} points, {Achievements} achievements",
                _playerProgress.TotalPoints, _playerProgress.UnlockedAchievements.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading player progress");
        }
    }

    private async Task SavePlayerProgressAsync()
    {
        try
        {
            if (_context == null) return;

            var progressPath = Path.Combine(_context.PluginDirectory, "player_progress.json");
            var json = JsonSerializer.Serialize(_playerProgress, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(progressPath, json);

            _logger?.LogInformation("Saved player progress");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving player progress");
        }
    }

    private static string GetDifficultyIcon(AchievementDifficulty difficulty) =>
        difficulty switch
        {
            AchievementDifficulty.Bronze => "🥉",
            AchievementDifficulty.Silver => "🥈",
            AchievementDifficulty.Gold => "🥇",
            AchievementDifficulty.Platinum => "💎",
            _ => "❓"
        };

    private static int GetPlayerLevel(int points)
    {
        // Simple leveling: 100 points per level
        return (points / 100) + 1;
    }

    private static int GetPointsForLevel(int level)
    {
        // Cumulative points needed for level
        return level * 100;
    }

    // Public methods for other plugins to report progress
    public async Task ReportMatchResultAsync(bool victory, string character, string opponent, string stage, int comboCount)
    {
        _playerProgress.TotalMatches++;
        if (victory) _playerProgress.TotalVictories++;
        if (comboCount > 0)
        {
            _playerProgress.TotalCombos++;
            _playerProgress.TotalComboHits += comboCount;
        }

        _playerProgress.CharactersUsed.Add(character);
        _playerProgress.StagesPlayed.Add(stage);

        // Check for achievement unlocks
        await CheckAchievementUnlocksAsync();

        // Update goals
        await UpdateGoalsAsync("match_completed", victory ? 1 : 0);

        await SavePlayerProgressAsync();
    }

    public async Task ReportTrainingTimeAsync(TimeSpan duration)
    {
        _playerProgress.TotalTrainingTime += duration;
        await CheckAchievementUnlocksAsync();
        await SavePlayerProgressAsync();
    }

    public async Task ReportReplaySavedAsync()
    {
        _playerProgress.ReplaysSaved++;
        await CheckAchievementUnlocksAsync();
        await SavePlayerProgressAsync();
    }

    private async Task CheckAchievementUnlocksAsync()
    {
        foreach (var achievement in _achievements)
        {
            if (_playerProgress.UnlockedAchievements.Contains(achievement.Id))
                continue;

            var shouldUnlock = achievement.Id switch
            {
                "first_victory" => _playerProgress.TotalVictories >= 1,
                "combo_master" => _playerProgress.TotalComboHits >= 20,
                "perfect_round" => false, // Would need specific tracking
                "character_collector" => _playerProgress.CharactersUsed.Count >= 10,
                "stage_explorer" => _playerProgress.StagesPlayed.Count >= 5,
                "training_devotee" => _playerProgress.TotalTrainingTime.TotalHours >= 10,
                "replay_enthusiast" => _playerProgress.ReplaysSaved >= 50,
                "legendary_warrior" => _playerProgress.TotalVictories >= 100,
                _ => false
            };

            if (shouldUnlock)
            {
                _playerProgress.UnlockedAchievements.Add(achievement.Id);
                _playerProgress.TotalPoints += achievement.Points;

                _logger?.LogInformation("🎉 Achievement Unlocked: {Name} (+{Points} points)",
                    achievement.Name, achievement.Points);
            }
        }
    }

    private async Task UpdateGoalsAsync(string eventType, int value)
    {
        foreach (var goal in _goals)
        {
            if (goal.IsCompleted) continue;

            // Update goal progress based on event type
            switch (eventType)
            {
                case "match_completed":
                    if (goal.Id == "daily_matches")
                        goal.CurrentValue += value;
                    break;
                case "training_time":
                    if (goal.Id == "weekly_training")
                        goal.CurrentValue += value; // value in minutes
                    break;
            }

            if (goal.CurrentValue >= goal.TargetValue)
            {
                _logger?.LogInformation("🎯 Goal Completed: {Title} - Reward: {Type} - {Value}",
                    goal.Title, goal.Reward.Type, goal.Reward.Value);
            }
        }
    }
}

/// <summary>
/// Player progress tracking.
/// </summary>
public class PlayerProgress
{
    public string PlayerName { get; set; } = "MUGEN Player";
    public int TotalPoints { get; set; }
    public int TotalMatches { get; set; }
    public int TotalVictories { get; set; }
    public int TotalCombos { get; set; }
    public int TotalComboHits { get; set; }
    public TimeSpan TotalTrainingTime { get; set; }
    public List<string> UnlockedAchievements { get; set; } = new();
    public HashSet<string> CharactersUsed { get; set; } = new();
    public HashSet<string> StagesPlayed { get; set; } = new();
    public int ReplaysSaved { get; set; }
}

/// <summary>
/// MUGEN achievement definition.
/// </summary>
public class MugenAchievement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public AchievementCategory Category { get; set; }
    public AchievementDifficulty Difficulty { get; set; }
    public int Points { get; set; }
    public bool IsHidden { get; set; }
}

/// <summary>
/// Achievement categories.
/// </summary>
public enum AchievementCategory
{
    Combat,
    Training,
    Collection,
    Exploration,
    Social,
    Special
}

/// <summary>
/// Achievement difficulty levels.
/// </summary>
public enum AchievementDifficulty
{
    Bronze,
    Silver,
    Gold,
    Platinum
}

/// <summary>
/// Progression goal definition.
/// </summary>
public class ProgressionGoal
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TargetValue { get; set; }
    public int CurrentValue { get; set; }
    public GoalType GoalType { get; set; }
    public GoalReward Reward { get; set; } = new();
    public bool IsCompleted => CurrentValue >= TargetValue;
}

/// <summary>
/// Goal types.
/// </summary>
public enum GoalType
{
    Daily,
    Weekly,
    CharacterSpecific,
    Progressive
}

/// <summary>
/// Goal reward definition.
/// </summary>
public class GoalReward
{
    public RewardType Type { get; set; }
    public object Value { get; set; } = string.Empty;
}

/// <summary>
/// Reward types.
/// </summary>
public enum RewardType
{
    Experience,
    Title,
    Badge,
    Unlocks,
    Cosmetic
}