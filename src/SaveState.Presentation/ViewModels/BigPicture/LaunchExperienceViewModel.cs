using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Services.DTOs;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.BigPicture;

/// <summary>
/// ViewModel for the cinematic game launch experience.
/// </summary>
public partial class LaunchExperienceViewModel : ObservableObject
{
    private readonly ILaunchExperienceManager _launchExperienceManager;
    private readonly IGameBriefingService _gameBriefingService;

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
    private int totalSteps;

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

    private LaunchSequence? _currentSequence;
    private CancellationTokenSource? _sequenceCts;

    public LaunchExperienceViewModel(
        ILaunchExperienceManager launchExperienceManager,
        IGameBriefingService gameBriefingService)
    {
        _launchExperienceManager = launchExperienceManager;
        _gameBriefingService = gameBriefingService;
    }

    public async Task InitializeForGameAsync(Guid gameId, string title)
    {
        GameTitle = title;
        IsLoading = true;

        try
        {
            // Generate the launch sequence
            var sequenceResult = await _launchExperienceManager.GenerateLaunchSequenceAsync(gameId);
            if (!sequenceResult.IsSuccess || sequenceResult.Value == null)
            {
                // Skip to direct launch if sequence generation fails
                await LaunchGameDirectlyAsync(gameId);
                return;
            }

            _currentSequence = sequenceResult.Value;
            TotalSteps = _currentSequence.Steps.Count;
            CurrentStepIndex = 0;
            ProgressValue = 0;

            // Prepare UI elements for each step
            PrepareSequenceSteps(_currentSequence);

            IsLoading = false;

            // Start the sequence
            await ExecuteSequenceAsync();
        }
        catch (Exception ex)
        {
            // On error, skip to direct launch
            await LaunchGameDirectlyAsync(gameId);
        }
    }

    private void PrepareSequenceSteps(LaunchSequence sequence)
    {
        GameFacts.Clear();

        foreach (var step in sequence.Steps)
        {
            switch (step)
            {
                case GameFactsStep factsStep:
                    foreach (var fact in factsStep.Facts)
                    {
                        GameFacts.Add(new GameFactViewModel { Fact = fact });
                    }
                    break;

                case ProgressSummaryStep progressStep:
                    ProgressSummary = new ProgressSummaryViewModel
                    {
                        TotalPlaytime = progressStep.TotalPlaytime,
                        AchievementsEarned = progressStep.AchievementsEarned
                    };
                    break;

                case AmbientMusicStep musicStep:
                    IsPlayingMusic = true;
                    break;
            }
        }
    }

    private async Task ExecuteSequenceAsync()
    {
        if (_currentSequence == null) return;

        _sequenceCts = new CancellationTokenSource();

        try
        {
            for (int i = 0; i < _currentSequence.Steps.Count; i++)
            {
                _sequenceCts.Token.ThrowIfCancellationRequested();

                var step = _currentSequence.Steps[i];
                CurrentStepIndex = i + 1;

                // Update UI for current step
                await UpdateStepUIAsync(step);

                // Calculate progress
                ProgressValue = (double)(i + 1) / _currentSequence.Steps.Count * 100;

                // Wait for step duration
                await Task.Delay(step.Duration, _sequenceCts.Token);
            }

            // Sequence complete, launch the game
            await LaunchGameDirectlyAsync(_currentSequence.GameId);
        }
        catch (OperationCanceledException)
        {
            // Sequence was skipped
            if (_currentSequence != null)
            {
                await LaunchGameDirectlyAsync(_currentSequence.GameId);
            }
        }
        catch (Exception ex)
        {
            // On error, still try to launch the game
            if (_currentSequence != null)
            {
                await LaunchGameDirectlyAsync(_currentSequence.GameId);
            }
        }
    }

    private Task UpdateStepUIAsync(LaunchStep step)
    {
        switch (step)
        {
            case GameFactsStep factsStep:
                CurrentStepTitle = "Game Facts";
                CurrentStepContent = string.Join("\n\n", factsStep.Facts);
                break;

            case ProgressSummaryStep progressStep:
                CurrentStepTitle = "Your Progress";
                CurrentStepContent = $"Total Playtime: {progressStep.TotalPlaytime.TotalHours:F1} hours\n" +
                                   $"Achievements: {progressStep.AchievementsEarned}";
                break;

            case AmbientMusicStep musicStep:
                CurrentStepTitle = "Preparing Game";
                CurrentStepContent = musicStep.TrackName != null
                    ? $"Playing: {musicStep.TrackName}"
                    : "Loading ambient music...";
                break;

            case LoadingScreenStep loadingStep:
                CurrentStepTitle = "Loading";
                CurrentStepContent = string.Join("\n", loadingStep.Tips);
                break;

            default:
                CurrentStepTitle = "Loading Game";
                CurrentStepContent = "Please wait...";
                break;
        }
        return Task.CompletedTask;
    }

    private Task LaunchGameDirectlyAsync(Guid gameId)
    {
        // Launch logic resides in GameLauncherService.
        // This view model handles the pre-launch cinematic experience only.
        IsLoading = false;
        CurrentStepTitle = "Launching Game";
        CurrentStepContent = "Starting " + GameTitle;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void SkipSequence()
    {
        _sequenceCts?.Cancel();
    }

    [RelayCommand]
    private Task ConfigureLaunchExperience()
    {
        // Navigate to configuration screen
        // Navigation to settings would use INavigationService.
        // Pending implementation of global navigation structure.
        return Task.CompletedTask;
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
