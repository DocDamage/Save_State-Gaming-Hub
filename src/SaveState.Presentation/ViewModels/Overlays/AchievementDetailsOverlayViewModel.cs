using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the achievement details overlay.
/// </summary>
public partial class AchievementDetailsOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private string _achievementName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _icon = "🏆";

    [ObservableProperty]
    private string _iconBorderColor = "#FFD700";

    [ObservableProperty]
    private string _status = "Locked";

    [ObservableProperty]
    private string _statusColor = "#6B7280";

    [ObservableProperty]
    private string _rarity = "Common";

    [ObservableProperty]
    private string _rarityColor = "#10B981";

    [ObservableProperty]
    private bool _isUnlocked;

    [ObservableProperty]
    private bool _showProgress;

    [ObservableProperty]
    private string _progressText = "0%";

    [ObservableProperty]
    private double _progressBarWidth;

    [ObservableProperty]
    private string _progressDescription = string.Empty;

    [ObservableProperty]
    private string _unlockDate = string.Empty;

    [ObservableProperty]
    private string _unlockTime = string.Empty;

    [ObservableProperty]
    private string _globalUnlockRate = "0%";

    [ObservableProperty]
    private bool _showTips;

    [ObservableProperty]
    private ObservableCollection<string> _tips = new();

    [ObservableProperty]
    private bool _hasRewards;

    [ObservableProperty]
    private int _rewardPoints;

    [ObservableProperty]
    private string _rewardDescription = string.Empty;

    private readonly SaveState.Presentation.Services.IOverlayService _overlayService;

    public AchievementDetailsOverlayViewModel(SaveState.Presentation.Services.IOverlayService overlayService)
    {
        _overlayService = overlayService;
        // Design-time data
        LoadDesignTimeData();
    }

    private void LoadDesignTimeData()
    {
        AchievementName = "Master Collector";
        Description = "Collect all items in the game";
        Icon = "🏆";
        IconBorderColor = "#FFD700";
        Status = "In Progress";
        StatusColor = "#F59E0B";
        Rarity = "Rare";
        RarityColor = "#8B5CF6";

        ShowProgress = true;
        ProgressText = "75%";
        ProgressBarWidth = 450; // 75% of 600px
        ProgressDescription = "You've collected 75 out of 100 items. Keep going!";

        ShowTips = true;
        Tips.Add("Check the abandoned warehouse for rare items");
        Tips.Add("Complete side quests to unlock unique collectibles");
        Tips.Add("Some items only appear during specific weather conditions");

        HasRewards = true;
        RewardPoints = 500;
        RewardDescription = "Unlock this achievement to earn 500 XP and a special badge";

        GlobalUnlockRate = "12.5%";
    }

    public void Initialize(Guid achievementId)
    {
        AchievementName = $"Achievement {achievementId}";
        // Load actual achievement data
    }

    public void LoadUnlockedAchievement()
    {
        IsUnlocked = true;
        Status = "Unlocked";
        StatusColor = "#10B981";
        UnlockDate = "Jan 3, 2026";
        UnlockTime = "8:45 PM";
        ProgressText = "100%";
        ProgressBarWidth = 600;
        ShowProgress = false;
        ShowTips = false;
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideAchievementDetailsOverlay();
    }
}
