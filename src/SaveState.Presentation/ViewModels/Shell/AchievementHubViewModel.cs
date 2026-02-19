// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Achievements;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// ViewModel for the Achievement Hub page.
/// </summary>
public sealed partial class AchievementHubViewModel : ObservableObject
{
    private readonly IAchievementTrackingService _achievementService;
    private readonly ILogger<AchievementHubViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<AchievementViewModel> _achievements = new();

    [ObservableProperty]
    private ObservableCollection<AchievementViewModel> _recentAchievements = new();

    [ObservableProperty]
    private ObservableCollection<AchievementViewModel> _rareAchievements = new();

    [ObservableProperty]
    private ObservableCollection<AchievementRecommendationViewModel> _recommendations = new();

    [ObservableProperty]
    private AchievementStatisticsViewModel? _statistics;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedFilter = "All";

    [ObservableProperty]
    private AchievementViewModel? _selectedAchievement;

    public ObservableCollection<string> FilterOptions { get; } = new()
    {
        "All",
        "Unlocked",
        "Locked",
        "In Progress",
        "Rare"
    };

    public AchievementHubViewModel(
        IAchievementTrackingService achievementService,
        ILogger<AchievementHubViewModel> logger)
    {
        _achievementService = achievementService ?? throw new ArgumentNullException(nameof(achievementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;

        try
        {
            var userId = Guid.NewGuid(); // Placeholder

            // Load statistics
            var statsResult = await _achievementService.GetStatisticsAsync(userId);
            if (statsResult.IsSuccess && statsResult.Value is not null)
            {
                Statistics = new AchievementStatisticsViewModel(statsResult.Value);
            }
            else
            {
                _logger.LogWarning("Failed to load achievement statistics: {Error}", statsResult.Error);
            }

            // Load achievements
            var achievementsResult = await _achievementService.GetUserAchievementsAsync(userId);
            Achievements.Clear();
            if (achievementsResult.IsSuccess && achievementsResult.Value is not null)
            {
                foreach (var a in achievementsResult.Value)
                {
                    Achievements.Add(new AchievementViewModel(a));
                }
            }
            else
            {
                _logger.LogWarning("Failed to load achievements: {Error}", achievementsResult.Error);
            }

            // Load recent
            var recentResult = await _achievementService.GetRecentAchievementsAsync(userId, 5);
            RecentAchievements.Clear();
            if (recentResult.IsSuccess && recentResult.Value is not null)
            {
                foreach (var a in recentResult.Value)
                {
                    RecentAchievements.Add(new AchievementViewModel(a));
                }
            }
            else
            {
                _logger.LogWarning("Failed to load recent achievements: {Error}", recentResult.Error);
            }

            // Load rare
            var rareResult = await _achievementService.GetRareAchievementsAsync(userId);
            RareAchievements.Clear();
            if (rareResult.IsSuccess && rareResult.Value is not null)
            {
                foreach (var a in rareResult.Value)
                {
                    RareAchievements.Add(new AchievementViewModel(a));
                }
            }
            else
            {
                _logger.LogWarning("Failed to load rare achievements: {Error}", rareResult.Error);
            }

            // Load recommendations
            var recommendationsResult = await _achievementService.GetRecommendationsAsync(userId);
            Recommendations.Clear();
            if (recommendationsResult.IsSuccess && recommendationsResult.Value is not null)
            {
                foreach (var r in recommendationsResult.Value)
                {
                    Recommendations.Add(new AchievementRecommendationViewModel(r));
                }
            }
            else
            {
                _logger.LogWarning("Failed to load achievement recommendations: {Error}", recommendationsResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load achievement data");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SyncAchievementsAsync()
    {
        try
        {
            IsLoading = true;
            var userId = Guid.NewGuid(); // Placeholder
            await _achievementService.SyncExternalAchievementsAsync(userId);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync achievements");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void FilterAchievements()
    {
        // Apply filter logic
        var filtered = Achievements.AsEnumerable();

        filtered = SelectedFilter switch
        {
            "Unlocked" => filtered.Where(a => a.IsUnlocked),
            "Locked" => filtered.Where(a => !a.IsUnlocked),
            "In Progress" => filtered.Where(a => !a.IsUnlocked && a.CurrentProgress > 0),
            "Rare" => filtered.Where(a => a.IsRare),
            _ => filtered
        };

        if (!string.IsNullOrEmpty(SearchQuery))
        {
            filtered = filtered.Where(a => a.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        // Update display (would use a separate filtered collection in real implementation)
    }

    partial void OnSearchQueryChanged(string value) => FilterAchievements();
    partial void OnSelectedFilterChanged(string value) => FilterAchievements();
}

/// <summary>
/// ViewModel for an achievement.
/// </summary>
public sealed class AchievementViewModel : ObservableObject
{
    private readonly UserAchievementProgress _achievement;

    public AchievementViewModel(UserAchievementProgress achievement)
    {
        _achievement = achievement ?? throw new ArgumentNullException(nameof(achievement));
    }

    public string Name => _achievement.Name;
    public string Description => _achievement.Description;
    public string IconUrl => _achievement.IconUrl;
    public int Points => _achievement.Points;
    public AchievementType Type => _achievement.Type;
    public string? GameName => _achievement.GameName;
    public bool IsUnlocked => _achievement.IsUnlocked;
    public DateTime? UnlockedAt => _achievement.UnlockedAt;
    public int CurrentProgress => _achievement.CurrentProgress;
    public int TargetValue => _achievement.TargetValue;
    public double ProgressPercent => _achievement.ProgressPercent;
    public string Platform => _achievement.Platform;
    public bool IsRare => _achievement.IsRare;
    public string ProgressText => _achievement.ProgressText;

    public string StatusText => IsUnlocked ? $"✓ Unlocked {UnlockedAt?.ToString("MMM dd")}" : ProgressText;

    public string RarityText => IsRare ? "🔥 Rare Achievement" : string.Empty;
}

/// <summary>
/// ViewModel for achievement statistics.
/// </summary>
public sealed class AchievementStatisticsViewModel : ObservableObject
{
    private readonly AchievementStatistics _stats;

    public AchievementStatisticsViewModel(AchievementStatistics stats)
    {
        _stats = stats ?? throw new ArgumentNullException(nameof(stats));
    }

    public int TotalAchievements => _stats.TotalAchievements;
    public int UnlockedCount => _stats.UnlockedCount;
    public int TotalPoints => _stats.TotalPoints;
    public int MaxPoints => _stats.MaxPoints;
    public double CompletionPercent => _stats.CompletionPercent;
    public int RareAchievementsCount => _stats.RareAchievementsCount;
    public int CurrentStreak => _stats.CurrentStreak;
    public int LongestStreak => _stats.LongestStreak;
    public int AchievementsThisMonth => _stats.AchievementsThisMonth;
    public int AchievementsToday => _stats.AchievementsToday;

    public string CompletionText => $"{CompletionPercent:F1}%";
    public string PointsText => $"{TotalPoints} / {MaxPoints}";

    public string StreakText => CurrentStreak > 0 
        ? $"🔥 {CurrentStreak} day streak!" 
        : "Start your streak today!";
}

/// <summary>
/// ViewModel for achievement recommendation.
/// </summary>
public sealed class AchievementRecommendationViewModel : ObservableObject
{
    private readonly AchievementRecommendation _recommendation;

    public AchievementRecommendationViewModel(AchievementRecommendation recommendation)
    {
        _recommendation = recommendation ?? throw new ArgumentNullException(nameof(recommendation));
    }

    public AchievementViewModel Achievement => new(_recommendation.Achievement);
    public string Reason => _recommendation.Reason;
    public int Difficulty => _recommendation.Difficulty;
    public double CompletionPercent => _recommendation.CompletionPercent;
    public int PointsReward => _recommendation.PointsReward;

    public string DifficultyText => $"Difficulty: {Difficulty}/10";
    public string CompletionText => $"{CompletionPercent:F0}% complete";
}
