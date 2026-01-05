using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.BigPicture;

public partial class LaunchExperienceViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private string gameTitle = string.Empty;

    [ObservableProperty]
    private string currentStepTitle = string.Empty;

    [ObservableProperty]
    private string currentStepContent = string.Empty;

    [ObservableProperty]
    private int currentStepIndex;

    [ObservableProperty]
    private int totalSteps = 4;

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private bool showSkipButton = true;

    [ObservableProperty]
    private ObservableCollection<GameFactViewModel> gameFacts = new();

    [ObservableProperty]
    private ProgressSummaryViewModel? progressSummary;

    [ObservableProperty]
    private bool isPlayingMusic;

    private CancellationTokenSource? _sequenceCts;

    public LaunchExperienceViewModel(string title)
    {
        GameTitle = title;

        // Seed some mock data for the experience
        GameFacts.Add(new GameFactViewModel { Fact = "Did you know? This game was developed by a team of over 200 people." });
        GameFacts.Add(new GameFactViewModel { Fact = "Pro-tip: You can parry most physical attacks by timing your block." });
        GameFacts.Add(new GameFactViewModel { Fact = "The game's world is precisely 42 square kilometers." });

        ProgressSummary = new ProgressSummaryViewModel
        {
            TotalPlaytime = TimeSpan.FromHours(45),
            AchievementsEarned = 12
        };
    }

    public async Task StartSequenceAsync()
    {
        IsLoading = true;
        await Task.Delay(1500); // Simulate initial loading
        IsLoading = false;

        _sequenceCts = new CancellationTokenSource();

        try
        {
            // Step 1: Checking Requirements
            await RunStepAsync("System Check", "Optimizing GPU performance and checking for updates...", 25, 2000);

            // Step 2: Syncing Saves
            await RunStepAsync("Cloud Sync", "Synchronizing save data with the cloud...", 50, 1500);

            // Step 3: Game Facts
            CurrentStepTitle = "Game Insight";
            CurrentStepContent = "Preparing your gaming session...";
            ProgressValue = 75;
            CurrentStepIndex = 3;
            await Task.Delay(3000, _sequenceCts.Token);

            // Step 4: Finalizing
            await RunStepAsync("Ready to Play", "Launch imminent. Enjoy your session!", 100, 1000);
        }
        catch (TaskCanceledException)
        {
            // Handled via skip
        }
    }

    private async Task RunStepAsync(string title, string content, double progress, int delay)
    {
        CurrentStepTitle = title;
        CurrentStepContent = content;
        ProgressValue = progress;
        CurrentStepIndex++;
        await Task.Delay(delay, _sequenceCts.Token);
    }

    [RelayCommand]
    private void SkipSequence()
    {
        _sequenceCts?.Cancel();
    }
}

public class GameFactViewModel
{
    public string Fact { get; set; } = string.Empty;
}

public class ProgressSummaryViewModel
{
    public TimeSpan TotalPlaytime { get; set; }
    public int AchievementsEarned { get; set; }
}
