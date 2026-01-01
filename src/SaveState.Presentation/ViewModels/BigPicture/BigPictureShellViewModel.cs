using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.BigPicture;

public partial class BigPictureShellViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime currentTime = DateTime.Now;

    [ObservableProperty]
    private string statusText = "Ready";

    [ObservableProperty]
    private string nowPlayingText = "";

    [ObservableProperty]
    private GameGridViewModel gameGridViewModel = new();

    [ObservableProperty]
    private GameDetailViewModel gameDetailViewModel = new();

    private readonly System.Timers.Timer _timer;

    public BigPictureShellViewModel()
    {
        // Update time every second
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) => CurrentTime = DateTime.Now;
        _timer.Start();

        // Connect the view models
        GameGridViewModel.GameSelected += OnGameSelected;
    }

    private void OnGameSelected(GameItemViewModel selectedGame)
    {
        GameDetailViewModel.SelectedGame = selectedGame;
        StatusText = $"Selected: {selectedGame.Title}";
    }

    [RelayCommand]
    private void ExitBigPicture()
    {
        // This would navigate back to normal mode
        StatusText = "Exiting Big Picture mode...";
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GameGridViewModel?.Dispose();
        // GameDetailViewModel doesn't implement IDisposable
    }
}