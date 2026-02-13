using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.BigPicture;

public partial class BigPictureShellViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private DateTime currentTime;

    [ObservableProperty]
    private string statusText = "Ready";

    [ObservableProperty]
    private string nowPlayingText = "";

    [ObservableProperty]
    private GameGridViewModel gameGridViewModel;

    [ObservableProperty]
    private GameDetailViewModel gameDetailViewModel;

    [ObservableProperty]
    private LaunchExperienceViewModel? launchExperience;

    [ObservableProperty]
    private bool isLaunchExperienceVisible;

    [ObservableProperty]
    private ObservableCollection<BackgroundTaskViewModel> activeTasks = new();

    [ObservableProperty]
    private bool isTaskOverlayVisible;

    private readonly System.Timers.Timer _timer;
    private readonly ITimeProvider _timeProvider;

    public BigPictureShellViewModel(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        currentTime = timeProvider.Now;
        gameGridViewModel = new GameGridViewModel(timeProvider);
        gameDetailViewModel = new GameDetailViewModel(timeProvider);
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) => CurrentTime = _timeProvider.Now;
        _timer.Start();

        GameGridViewModel.GameSelected += OnGameSelected;

        // Seed current downloads as tasks
        ActiveTasks.Add(new BackgroundTaskViewModel { Name = "UnderNight Pack", Description = "Downloading 5.4GB...", Progress = 12 });
        ActiveTasks.Add(new BackgroundTaskViewModel { Name = "Dragon Ball EX", Description = "Downloading 895MB...", Progress = 45 });
        ActiveTasks.Add(new BackgroundTaskViewModel { Name = "Built-in Roster", Description = "Extracting 1.3GB...", Progress = 88 });
    }

    private void OnGameSelected(GameItemViewModel selectedGame)
    {
        GameDetailViewModel.SelectedGame = selectedGame;
        StatusText = $"Selected: {selectedGame.Title}";
    }

    private async Task LaunchGameAsync()
    {
        if (GameDetailViewModel.SelectedGame == null) return;

        LaunchExperience = new LaunchExperienceViewModel(GameDetailViewModel.SelectedGame.Title);
        IsLaunchExperienceVisible = true;

        await LaunchExperience.StartSequenceAsync();

        await Task.Delay(2000);
        IsLaunchExperienceVisible = false;
        NowPlayingText = $"Now Playing: {GameDetailViewModel.SelectedGame.Title}";
        StatusText = "Game Running";
    }

    [RelayCommand]
    private void ToggleTaskOverlay()
    {
        IsTaskOverlayVisible = !IsTaskOverlayVisible;
    }

    [RelayCommand]
    private void ExitBigPicture()
    {
        StatusText = "Exiting Big Picture mode...";
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GameGridViewModel?.Dispose();
    }
}

public partial class BackgroundTaskViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string status = "In Progress";
}
