using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.Gamification;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class ChallengesViewModel : ViewModelBase
{
    private readonly ChallengeService _challengeService;

    [ObservableProperty]
    private ObservableCollection<ChallengeDisplayInfo> _dailyChallenges = new();

    [ObservableProperty]
    private ObservableCollection<ChallengeDisplayInfo> _weeklyChallenges = new();

    [ObservableProperty]
    private int _streak;

    [ObservableProperty]
    private int _completedToday;

    [ObservableProperty]
    private string _streakMessage = string.Empty;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<string> ClaimRewardCommand { get; }

    public ChallengesViewModel()
    {
        _challengeService = ChallengeService.Instance;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ClaimRewardCommand = new AsyncRelayCommand<string>(ClaimRewardAsync);

        _challengeService.ChallengeCompleted += (s, c) => _ = RefreshAsync();

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        await Task.Run(() =>
        {
            // Daily challenges
            DailyChallenges.Clear();
            foreach (var challenge in _challengeService.GetDailyChallenges())
            {
                var progress = _challengeService.GetProgress(challenge.Id);
                DailyChallenges.Add(new ChallengeDisplayInfo
                {
                    Id = challenge.Id,
                    Title = challenge.Title,
                    Description = challenge.Description,
                    Icon = challenge.Icon,
                    Difficulty = challenge.Difficulty.ToString(),
                    XPReward = challenge.XPReward,
                    TargetCount = challenge.TargetCount,
                    CurrentCount = progress?.CurrentCount ?? 0,
                    IsComplete = progress?.IsComplete ?? false,
                    CanClaim = (progress?.IsComplete ?? false) && !(progress?.RewardClaimed ?? true)
                });
            }

            // Weekly challenges
            WeeklyChallenges.Clear();
            foreach (var challenge in _challengeService.GetWeeklyChallenges())
            {
                var progress = _challengeService.GetProgress(challenge.Id);
                WeeklyChallenges.Add(new ChallengeDisplayInfo
                {
                    Id = challenge.Id,
                    Title = challenge.Title,
                    Description = challenge.Description,
                    Icon = challenge.Icon,
                    Difficulty = challenge.Difficulty.ToString(),
                    XPReward = challenge.XPReward,
                    TargetCount = challenge.TargetCount,
                    CurrentCount = progress?.CurrentCount ?? 0,
                    IsComplete = progress?.IsComplete ?? false,
                    CanClaim = (progress?.IsComplete ?? false) && !(progress?.RewardClaimed ?? true)
                });
            }

            Streak = _challengeService.GetStreak();
            CompletedToday = _challengeService.GetCompletedTodayCount();

            StreakMessage = Streak switch
            {
                0 => "Complete a challenge to start your streak!",
                1 => "🔥 1 day streak! Keep it going!",
                < 7 => $"🔥 {Streak} day streak! You're on fire!",
                < 30 => $"🔥🔥 {Streak} day streak! Incredible dedication!",
                _ => $"🔥🔥🔥 {Streak} day streak! LEGENDARY!"
            };
        });
    }

    private async Task ClaimRewardAsync(string? challengeId)
    {
        if (string.IsNullOrEmpty(challengeId)) return;

        var success = await _challengeService.ClaimRewardAsync(challengeId);
        if (success)
        {
            await RefreshAsync();
        }
    }
}

public class ChallengeDisplayInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎯";
    public string Difficulty { get; set; } = string.Empty;
    public int XPReward { get; set; }
    public int TargetCount { get; set; }
    public int CurrentCount { get; set; }
    public bool IsComplete { get; set; }
    public bool CanClaim { get; set; }
    public double ProgressPercent => TargetCount > 0 ? (double)CurrentCount / TargetCount * 100 : 0;
    public string ProgressText => $"{CurrentCount}/{TargetCount}";
}
