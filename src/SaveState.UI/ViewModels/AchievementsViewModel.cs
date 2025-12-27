using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.Gamification;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class AchievementsViewModel : ViewModelBase
{
    private readonly AchievementService _achievementService;

    [ObservableProperty]
    private ObservableCollection<AchievementDisplayInfo> _achievements = new();

    [ObservableProperty]
    private ObservableCollection<AchievementDisplayInfo> _recentUnlocks = new();

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private double _completionPercent;

    [ObservableProperty]
    private int _totalUnlocked;

    [ObservableProperty]
    private int _totalAchievements;

    public ObservableCollection<string> Categories { get; } = new()
    {
        "All", "General", "Battle", "Fusion", "Collection", "Social", "Speedrun", "Explorer", "Creator"
    };

    public IAsyncRelayCommand RefreshCommand { get; }

    public AchievementsViewModel()
    {
        _achievementService = AchievementService.Instance;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        _achievementService.AchievementUnlocked += (s, a) => _ = RefreshAsync();

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        await Task.Run(() =>
        {
            var all = _achievementService.GetAllAchievements();
            var unlocked = _achievementService.GetUnlockedAchievements();
            var unlockedIds = unlocked.Select(a => a.Id).ToHashSet();

            Achievements.Clear();

            var filtered = SelectedCategory == "All" 
                ? all 
                : all.Where(a => a.Category.ToString() == SelectedCategory);

            foreach (var achievement in filtered)
            {
                Achievements.Add(new AchievementDisplayInfo
                {
                    Id = achievement.Id,
                    Name = achievement.Name,
                    Description = achievement.IsHidden && !unlockedIds.Contains(achievement.Id) 
                        ? "???" 
                        : achievement.Description,
                    Icon = achievement.Icon,
                    Category = achievement.Category.ToString(),
                    Rarity = achievement.Rarity.ToString(),
                    XPReward = achievement.XPReward,
                    IsUnlocked = unlockedIds.Contains(achievement.Id),
                    Progress = _achievementService.GetProgress(achievement.Id),
                    MaxProgress = achievement.RequiredCount
                });
            }

            // Recent unlocks
            RecentUnlocks.Clear();
            foreach (var achievement in unlocked.Take(5))
            {
                RecentUnlocks.Add(new AchievementDisplayInfo
                {
                    Id = achievement.Id,
                    Name = achievement.Name,
                    Icon = achievement.Icon,
                    IsUnlocked = true
                });
            }

            TotalUnlocked = unlocked.Count;
            TotalAchievements = all.Count;
            CompletionPercent = _achievementService.GetCompletionPercentage();
        });
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        _ = RefreshAsync();
    }
}

public class AchievementDisplayInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🏆";
    public string Category { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public int XPReward { get; set; }
    public bool IsUnlocked { get; set; }
    public int Progress { get; set; }
    public int MaxProgress { get; set; } = 1;
    public double ProgressPercent => MaxProgress > 0 ? (double)Progress / MaxProgress * 100 : 0;
}
