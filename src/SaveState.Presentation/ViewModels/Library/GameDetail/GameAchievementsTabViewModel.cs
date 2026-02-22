using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.UserManagement.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Achievements tab.
/// </summary>
public partial class GameAchievementsTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IUserContextService _userContextService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GameAchievementsTabViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private string _achievementProgressText = "0/0 (0%)";

    [ObservableProperty]
    private double _completionPercentage;

    [ObservableProperty]
    private int _totalAchievements;

    [ObservableProperty]
    private int _unlockedCount;

    [ObservableProperty]
    private int _lockedCount;

    [ObservableProperty]
    private int _totalPoints;

    // Rarity breakdown
    [ObservableProperty]
    private int _commonCount;

    [ObservableProperty]
    private int _uncommonCount;

    [ObservableProperty]
    private int _rareCount;

    [ObservableProperty]
    private int _epicCount;

    [ObservableProperty]
    private int _legendaryCount;

    [ObservableProperty]
    private double _commonPercentage;

    [ObservableProperty]
    private double _uncommonPercentage;

    [ObservableProperty]
    private double _rarePercentage;

    [ObservableProperty]
    private double _epicPercentage;

    [ObservableProperty]
    private double _legendaryPercentage;

    [ObservableProperty]
    private ObservableCollection<GameAchievementViewModel> _achievements = new();

    [ObservableProperty]
    private ObservableCollection<GameAchievementViewModel> _recentUnlocks = new();

    [ObservableProperty]
    private ObservableCollection<string> _filterOptions = new() { "All", "Unlocked", "Locked", "Recent" };

    [ObservableProperty]
    private string _selectedFilter = "All";

    [ObservableProperty]
    private bool _showHiddenAchievements;

    [ObservableProperty]
    private bool _showProgressBars = true;

    [ObservableProperty]
    private bool _sortByRarity;

    [ObservableProperty]
    private string _searchText = string.Empty;

    private List<GameAchievementViewModel> _allAchievements = new();

    partial void OnSelectedFilterChanged(string value) => ApplyFilters();
    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnShowHiddenAchievementsChanged(bool value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allAchievements.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(a =>
                a.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        filtered = SelectedFilter switch
        {
            "Unlocked" => filtered.Where(a => a.IsUnlocked),
            "Locked" => filtered.Where(a => !a.IsUnlocked),
            "Recent" => filtered.Where(a => RecentUnlocks.Contains(a)),
            _ => filtered
        };

        Achievements.Clear();
        foreach (var item in filtered)
        {
            Achievements.Add(item);
        }
    }

    public GameAchievementsTabViewModel(
        IMediator mediator,
        IUserContextService userContextService,
        IDialogService dialogService,
        ILogger<GameAchievementsTabViewModel> logger,
        ITimeProvider timeProvider)
    {
        _mediator = mediator;
        _userContextService = userContextService;
        _dialogService = dialogService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task LoadDataAsync(GameId gameId)
    {
        try
        {
            // Get current user ID
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("No current user context available for loading achievements");
                await LoadPlaceholderData();
                return;
            }

            // Load achievements from backend
            var query = new GetUserAchievementsQuery(userId.Value, GameId: gameId.Value, IncludeLocked: true);
            var achievements = await _mediator.Send(query).ConfigureAwait(false);

            // Calculate statistics
            TotalAchievements = achievements.Count;
            UnlockedCount = achievements.Count(a => a.IsUnlocked);
            LockedCount = achievements.Count(a => !a.IsUnlocked);
            TotalPoints = achievements.Sum(a => a.AchievementPoints);

            CompletionPercentage = TotalAchievements > 0
                ? (double)UnlockedCount / TotalAchievements * 100.0
                : 0.0;

            AchievementProgressText = $"{UnlockedCount}/{TotalAchievements} ({CompletionPercentage:F0}%)";

            // Calculate rarity counts (based on achievement type as proxy for rarity)
            CommonCount = achievements.Count(a => a.AchievementType == AchievementType.GameCompletion);
            UncommonCount = achievements.Count(a => a.AchievementType == AchievementType.PlayTime);
            RareCount = achievements.Count(a => a.AchievementType == AchievementType.Collection);
            EpicCount = achievements.Count(a => a.AchievementType == AchievementType.Social);
            LegendaryCount = achievements.Count(a => a.AchievementType == AchievementType.Special);

            // Calculate rarity percentages
            if (TotalAchievements > 0)
            {
                CommonPercentage = (double)CommonCount / TotalAchievements * 100.0;
                UncommonPercentage = (double)UncommonCount / TotalAchievements * 100.0;
                RarePercentage = (double)RareCount / TotalAchievements * 100.0;
                EpicPercentage = (double)EpicCount / TotalAchievements * 100.0;
                LegendaryPercentage = (double)LegendaryCount / TotalAchievements * 100.0;
            }

            // Populate achievements collection
            _allAchievements.Clear();
            Achievements.Clear();
            RecentUnlocks.Clear();

            foreach (var achievement in achievements)
            {
                var (rarityText, rarityColor) = GetRarityInfo(achievement.AchievementType);

                var vm = new GameAchievementViewModel
                {
                    Name = achievement.AchievementName,
                    Description = achievement.AchievementDescription,
                    IconUrl = achievement.AchievementIconPath,
                    RarityText = rarityText,
                    RarityColor = rarityColor,
                    PointsText = $"{achievement.AchievementPoints} pts",
                    StatusText = achievement.IsUnlocked ? "Unlocked" : "Locked",
                    UnlockedText = achievement.IsUnlocked
                        ? (achievement.UnlockedAt.HasValue ? $"Unlocked {FormatDateTime(achievement.UnlockedAt.Value)}" : "Unlocked")
                        : "Locked",
                    StatusBadgeText = achievement.IsUnlocked ? "✅" : "🔒",
                    StatusBadgeColor = achievement.IsUnlocked ? "#4CAF50" : "#666666",
                    ProgressValue = achievement.CurrentProgress,
                    ProgressMax = achievement.TargetProgress,
                    ProgressText = $"{achievement.CurrentProgress}/{achievement.TargetProgress}",
                    ShowProgress = achievement.CurrentProgress > 0 && achievement.TargetProgress > achievement.CurrentProgress
                };

                _allAchievements.Add(vm);

                // Add to recent unlocks if unlocked in last 7 days
                if (achievement.IsUnlocked && achievement.UnlockedAt.HasValue &&
                    (_timeProvider.UtcNow - achievement.UnlockedAt.Value).TotalDays <= 7)
                {
                    RecentUnlocks.Add(vm);
                }
            }

            ApplyFilters();

            _logger.LogInformation("Loaded {Count} achievements for game {GameId}", achievements.Count, gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load achievements for game {GameId}", gameId);
            await LoadPlaceholderData();
        }
    }

    private async Task LoadPlaceholderData()
    {
        TotalAchievements = 0;
        UnlockedCount = 0;
        LockedCount = 0;
        TotalPoints = 0;
        CompletionPercentage = 0.0;
        AchievementProgressText = "0/0 (0%)";

        CommonCount = 0;
        UncommonCount = 0;
        RareCount = 0;
        EpicCount = 0;
        LegendaryCount = 0;

        CommonPercentage = 0;
        UncommonPercentage = 0;
        RarePercentage = 0;
        EpicPercentage = 0;
        LegendaryPercentage = 0;

        Achievements.Clear();
        RecentUnlocks.Clear();

        await Task.CompletedTask;
    }

    private static (string Text, string Color) GetRarityInfo(AchievementType type)
    {
        return type switch
        {
            AchievementType.GameCompletion => ("Common", "#999999"),
            AchievementType.PlayTime => ("Uncommon", "#4CAF50"),
            AchievementType.Collection => ("Rare", "#2196F3"),
            AchievementType.Social => ("Epic", "#9C27B0"),
            AchievementType.Special => ("Legendary", "#FF9800"),
            _ => ("Unknown", "#666666")
        };
    }

    private string FormatDateTime(DateTime dateTime)
    {
        var timeSince = _timeProvider.UtcNow - dateTime;
        if (timeSince.TotalDays < 1)
            return "today";
        else if (timeSince.TotalDays < 2)
            return "yesterday";
        else if (timeSince.TotalDays < 7)
            return $"{(int)timeSince.TotalDays} days ago";
        else
            return dateTime.ToString("MMM d, yyyy");
    }

    [RelayCommand]
    private void ToggleSearch()
    {
        // Search is handled by SearchText property binding
        _logger.LogInformation("Search toggled (SearchText: {SearchText})", SearchText);
    }

    [RelayCommand]
    private async Task ViewStats()
    {
        var stats = $"Achievement Statistics:\n\n" +
                   $"Total: {TotalAchievements}\n" +
                   $"Unlocked: {UnlockedCount}\n" +
                   $"Locked: {LockedCount}\n" +
                   $"Completion: {CompletionPercentage:F1}%\n\n" +
                   $"Rarity Breakdown:\n" +
                   $"• Legendary: {LegendaryCount}\n" +
                   $"• Epic: {EpicCount}\n" +
                   $"• Rare: {RareCount}\n" +
                   $"• Uncommon: {UncommonCount}\n" +
                   $"• Common: {CommonCount}";

        await _dialogService.ShowInformationAsync("Achievement Stats", stats);
    }
}

/// <summary>
/// View model for individual achievements.
/// </summary>
public partial class GameAchievementViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string? _iconUrl;

    [ObservableProperty]
    private string _rarityText = string.Empty;

    [ObservableProperty]
    private string _rarityColor = "#666666";

    [ObservableProperty]
    private string _pointsText = "0 pts";

    [ObservableProperty]
    private string _unlockedText = "Locked";

    [ObservableProperty]
    private string _statusText = "Locked";

    [ObservableProperty]
    private string _statusBadgeText = "🔒";

    [ObservableProperty]
    private string _statusBadgeColor = "#666666";

    [ObservableProperty]
    private string _backgroundBrush = "Transparent";

    [ObservableProperty]
    private string _borderBrush = "Transparent";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private double _progressMax = 100;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private bool _showProgress;

    public bool IsUnlocked => StatusText == "Unlocked";
    public bool HasProgress => ShowProgress && ProgressValue > 0;
}
