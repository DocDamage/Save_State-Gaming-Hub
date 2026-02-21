using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Launch;
using SaveState.Presentation.ViewModels.Library;
using System.Collections.ObjectModel;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the immersive launch experience overlay.
/// </summary>
public partial class LaunchExperienceViewModel : OverlayViewModelBase, IDisposable
{
    private readonly System.Timers.Timer _tipTimer;
    private int _currentTipIndex;
    private CancellationTokenSource? _launchCts;

    [ObservableProperty]
    private GameCardViewModel? _game;

    [ObservableProperty]
    private GameBriefing? _briefing;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _currentTip = string.Empty;

    [ObservableProperty]
    private string _loadingStatus = "Initializing...";

    [ObservableProperty]
    private bool _canSkip = true;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _showAiBriefing = true;

    [ObservableProperty]
    private bool _showTips = true;

    [ObservableProperty]
    private bool _showLastSessionSummary = true;

    /// <summary>
    /// Loading stages displayed during launch sequence.
    /// </summary>
    public ObservableCollection<string> LoadingStages { get; } = new()
    {
        "Loading game configuration...",
        "Preparing save state...",
        "Optimizing performance...",
        "Starting game..."
    };

    /// <summary>
    /// Creates a new instance of the launch experience view model.
    /// </summary>
    public LaunchExperienceViewModel()
    {
        _tipTimer = new System.Timers.Timer(5000);
        _tipTimer.Elapsed += OnTipTimerElapsed;
        _tipTimer.AutoReset = true;
    }

    partial void OnBriefingChanged(GameBriefing? value)
    {
        if (value?.Tips.Count > 0)
        {
            CurrentTip = value.Tips[0];
            _currentTipIndex = 0;
            _tipTimer.Start();
        }
    }

    private void OnTipTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (Briefing?.Tips.Count > 0)
        {
            _currentTipIndex = (_currentTipIndex + 1) % Briefing.Tips.Count;
            CurrentTip = Briefing.Tips[_currentTipIndex];
        }
    }

    /// <summary>
    /// Skips the launch sequence and closes the overlay.
    /// </summary>
    [RelayCommand]
    private void Skip()
    {
        _tipTimer.Stop();
        _launchCts?.Cancel();
        IsCompleted = true;
        Close();
    }

    /// <summary>
    /// Cancels the game launch and closes the overlay.
    /// </summary>
    [RelayCommand]
    private void CancelLaunch()
    {
        _tipTimer.Stop();
        _launchCts?.Cancel();
        Close();
    }

    /// <summary>
    /// Starts the launch sequence for the specified game.
    /// </summary>
    /// <param name="game">The game to launch.</param>
    public async Task StartLaunchSequenceAsync(GameCardViewModel game)
    {
        Game = game;
        IsVisible = true;
        IsCompleted = false;
        Progress = 0;
        _launchCts = new CancellationTokenSource();

        try
        {
            // Simulate loading progress
            for (int i = 0; i <= 100; i += 2)
            {
                if (_launchCts.Token.IsCancellationRequested)
                    break;

                Progress = i;
                LoadingStatus = LoadingStages[Math.Min(i / 25, LoadingStages.Count - 1)];
                await Task.Delay(50, _launchCts.Token);
            }

            if (!_launchCts.Token.IsCancellationRequested)
            {
                IsCompleted = true;
                _tipTimer.Stop();
            }
        }
        catch (OperationCanceledException)
        {
            // Launch was cancelled
        }
    }

    /// <summary>
    /// Disposes resources used by the view model.
    /// </summary>
    public void Dispose()
    {
        _tipTimer.Dispose();
        _launchCts?.Dispose();
    }
}
