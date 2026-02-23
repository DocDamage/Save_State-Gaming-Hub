using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Services.DTOs;
using SaveState.Presentation.ViewModels.Library;
using System.Collections.ObjectModel;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the immersive launch experience overlay with cinematic animations.
/// </summary>
public sealed partial class LaunchExperienceViewModel : OverlayViewModelBase, IDisposable
{
    private readonly ILaunchExperienceManager _launchExperienceManager;
    private readonly IGameBriefingService _gameBriefingService;
    private readonly ILogger<LaunchExperienceViewModel> _logger;
    private readonly System.Timers.Timer _tipTimer;
    private readonly System.Timers.Timer _animationTimer;
    private int _currentTipIndex;
    private CancellationTokenSource? _launchCts;
    private GameCardViewModel? _currentGame;

    #region Observable Properties

    [ObservableProperty]
    private GameCardViewModel? _game;

    [ObservableProperty]
    private GameBriefingPresentation? _briefing;

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

    [ObservableProperty]
    private bool _showAchievements = true;

    [ObservableProperty]
    private bool _showPlaytime = true;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _currentObjective = string.Empty;

    [ObservableProperty]
    private TimeSpan _totalPlaytime;

    [ObservableProperty]
    private ObservableCollection<RecentAchievementPresentation> _recentAchievements = new();

    [ObservableProperty]
    private bool _isAnimating;

    [ObservableProperty]
    private double _coverArtScale = 0.8;

    [ObservableProperty]
    private double _coverArtOpacity = 0;

    [ObservableProperty]
    private double _titleOpacity = 0;

    [ObservableProperty]
    private double _contentOpacity = 0;

    #endregion

    /// <summary>
    /// Loading stages displayed during launch sequence.
    /// </summary>
    public ObservableCollection<string> LoadingStages { get; } = new()
    {
        "Initializing game environment...",
        "Loading save state data...",
        "Optimizing performance settings...",
        "Preparing AI briefing...",
        "Launching game..."
    };

    /// <summary>
    /// Creates a new instance of the launch experience view model.
    /// </summary>
    public LaunchExperienceViewModel(
        ILaunchExperienceManager launchExperienceManager,
        IGameBriefingService gameBriefingService,
        ILogger<LaunchExperienceViewModel> logger)
    {
        _launchExperienceManager = launchExperienceManager;
        _gameBriefingService = gameBriefingService;
        _logger = logger;

        // Initialize timers
        _tipTimer = new System.Timers.Timer(5000);
        _tipTimer.Elapsed += OnTipTimerElapsed;
        _tipTimer.AutoReset = true;

        _animationTimer = new System.Timers.Timer(50); // 20fps animation updates
        _animationTimer.Elapsed += OnAnimationTimerElapsed;
        _animationTimer.AutoReset = true;
    }

    /// <summary>
    /// Design-time constructor.
    /// </summary>
    public LaunchExperienceViewModel()
    {
        // Design-time initialization
        _launchExperienceManager = null!;
        _gameBriefingService = null!;
        _logger = null!;

        _tipTimer = new System.Timers.Timer(5000);
        _animationTimer = new System.Timers.Timer(50);

        // Set design-time data
        CurrentTip = "Tip: Use stealth in the sewers to avoid the boss fight";
        LoadingStatus = "Initializing game environment...";
        Progress = 45;
        ProgressPercentage = 65;
        CurrentObjective = "Complete Chapter 3";
        TotalPlaytime = TimeSpan.FromHours(12.5);
    }

    #region Partial Methods

    partial void OnBriefingChanged(GameBriefingPresentation? value)
    {
        if (value?.Tips.Count > 0)
        {
            CurrentTip = value.Tips[0];
            _currentTipIndex = 0;
            _tipTimer.Start();
        }
        else
        {
            _tipTimer.Stop();
        }

        if (value != null)
        {
            ProgressPercentage = value.ProgressPercentage;
            CurrentObjective = value.CurrentObjective;
            TotalPlaytime = value.TotalPlaytime;
            RecentAchievements.Clear();
            foreach (var achievement in value.RecentAchievements)
            {
                RecentAchievements.Add(achievement);
            }
        }
    }

    #endregion

    #region Timer Handlers

    private void OnTipTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (Briefing?.Tips.Count > 0)
        {
            _currentTipIndex = (_currentTipIndex + 1) % Briefing.Tips.Count;
            CurrentTip = Briefing.Tips[_currentTipIndex];
        }
    }

    private void OnAnimationTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // Smooth animation updates can be handled here
        // This runs on a background thread, so use proper dispatching if updating UI-bound properties
    }

    #endregion

    #region Commands

    /// <summary>
    /// Skips the launch sequence and closes the overlay.
    /// </summary>
    [RelayCommand]
    private async Task SkipAsync()
    {
        _logger.LogInformation("User skipped launch experience for {Game}", Game?.Title ?? "Unknown");
        await CleanupAndCloseAsync();
    }

    /// <summary>
    /// Cancels the game launch and closes the overlay.
    /// </summary>
    [RelayCommand]
    private async Task CancelLaunchAsync()
    {
        _logger.LogInformation("User cancelled launch for {Game}", Game?.Title ?? "Unknown");
        await CleanupAndCloseAsync();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Starts the launch sequence for the specified game.
    /// </summary>
    /// <param name="game">The game to launch.</param>
    /// <param name="config">Optional launch experience configuration.</param>
    public async Task StartLaunchSequenceAsync(GameCardViewModel game, LaunchExperienceSettings? config = null)
    {
        _currentGame = game;
        Game = game;
        IsVisible = true;
        IsCompleted = false;
        Progress = 0;
        _launchCts = new CancellationTokenSource();

        try
        {
            // Load configuration
            var settingsConfig = config ?? await LoadConfigurationAsync(game);
            ApplyConfiguration(settingsConfig);

            // Generate briefing
            await LoadBriefingAsync(game, _launchCts.Token);

            // Start animations
            await StartAnimationsAsync();

            // Simulate loading progress
            await RunLaunchSequenceAsync(settingsConfig, _launchCts.Token);

            if (!_launchCts.Token.IsCancellationRequested)
            {
                IsCompleted = true;
                _tipTimer.Stop();
                _animationTimer.Stop();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Launch sequence cancelled for {Game}", game.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during launch sequence for {Game}", game.Title);
        }
    }

    /// <summary>
    /// Shows the launch experience overlay.
    /// </summary>
    public override Task ShowAsync()
    {
        IsVisible = true;
        IsAnimating = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Hides the launch experience overlay.
    /// </summary>
    public override Task HideAsync()
    {
        CleanupAndCloseAsync().Wait();
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private async Task<LaunchExperienceSettings> LoadConfigurationAsync(GameCardViewModel game)
    {
        try
        {
            var result = await _launchExperienceManager.GetLaunchExperienceConfigAsync(game.GameId.Value, CancellationToken.None);
            if (result.IsSuccess && result.Value != null)
            {
                // Map from core config to settings
                return new LaunchExperienceSettings
                {
                    IsEnabled = true,
                    ShowAiBriefing = result.Value.ShowGameFacts,
                    ShowLastSession = result.Value.ShowLastProgress,
                    ShowAchievements = result.Value.ShowAchievementProgress,
                    Duration = AnimationDuration.Medium
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load launch config for {Game}", game.Title);
        }

        return new LaunchExperienceSettings();
    }

    private void ApplyConfiguration(LaunchExperienceSettings config)
    {
        ShowAiBriefing = config.ShowAiBriefing;
        ShowTips = config.ShowTips;
        ShowLastSessionSummary = config.ShowLastSession;
        ShowAchievements = config.ShowAchievements;
        ShowPlaytime = config.ShowPlaytime;
        CanSkip = config.AllowSkip;
    }

    private async Task LoadBriefingAsync(GameCardViewModel game, CancellationToken ct)
    {
        try
        {
            var result = await _gameBriefingService.GenerateBriefingAsync(game.GameId.Value, ct);
            if (result.IsSuccess)
            {
                var briefing = result.Value;
                Briefing = new GameBriefingPresentation
                {
                    GameTitle = game.Title,
                    Tagline = await GetTaglineAsync(game, ct),
                    LastSessionSummary = briefing.LastSessionSummary,
                    CurrentObjective = briefing.CurrentObjectives.FirstOrDefault() ?? "Continue your adventure",
                    ProgressPercentage = 45, // TODO: Calculate from actual progress
                    Tips = briefing.Tips.ToList(),
                    RecentAchievements = new List<RecentAchievementPresentation>(),
                    TotalPlaytime = TimeSpan.FromHours(12.5), // TODO: Get from actual data
                    CoverArtPath = game.CoverArtUrl,
                    BackgroundPath = game.CoverArtUrl
                };

                // Load tips if briefing didn't include them
                if (Briefing.Tips.Count == 0)
                {
                    var tipsResult = await _gameBriefingService.GetGameTipsAsync(game.GameId.Value, ct);
                    if (tipsResult.IsSuccess)
                    {
                        Briefing = Briefing with { Tips = tipsResult.Value.ToList() };
                    }
                }
            }
            else
            {
                // Create default briefing
                Briefing = CreateDefaultBriefing(game);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate briefing for {Game}", game.Title);
            Briefing = CreateDefaultBriefing(game);
        }
    }

    private async Task<string> GetTaglineAsync(GameCardViewModel game, CancellationToken ct)
    {
        // In a real implementation, this would come from AI service or metadata
        var taglines = new[]
        {
            "Your adventure awaits...",
            "Ready to dive back in?",
            "The journey continues...",
            "Epic moments ahead...",
            "Your next great story begins now..."
        };

        return await Task.FromResult(taglines[Random.Shared.Next(taglines.Length)]);
    }

    private GameBriefingPresentation CreateDefaultBriefing(GameCardViewModel game)
    {
        return new GameBriefingPresentation
        {
            GameTitle = game.Title,
            Tagline = "Ready to play?",
            LastSessionSummary = "Continue where you left off",
            CurrentObjective = "Enjoy your game!",
            ProgressPercentage = 0,
            Tips = new List<string>
            {
                "Tip: Take breaks every hour to stay refreshed",
                "Tip: Use quick save (F5) frequently to preserve progress",
                "Tip: Check the settings menu to optimize performance"
            },
            RecentAchievements = new List<RecentAchievementPresentation>(),
            TotalPlaytime = TimeSpan.Zero,
            CoverArtPath = game.CoverArtUrl,
            BackgroundPath = game.CoverArtUrl
        };
    }

    private async Task StartAnimationsAsync()
    {
        IsAnimating = true;
        _animationTimer.Start();

        // Animate cover art
        await AnimatePropertyAsync(value => CoverArtScale = value, 0.8, 1.0, 500);
        await AnimatePropertyAsync(value => CoverArtOpacity = value, 0, 1, 500);

        // Animate text with stagger
        await Task.Delay(200);
        await AnimatePropertyAsync(value => TitleOpacity = value, 0, 1, 300);
        await Task.Delay(200);
        await AnimatePropertyAsync(value => ContentOpacity = value, 0, 1, 300);
    }

    private async Task RunLaunchSequenceAsync(LaunchExperienceSettings config, CancellationToken ct)
    {
        var duration = config.DurationSeconds;
        var steps = LoadingStages.Count;
        var stepDuration = duration > 0 ? (duration * 1000) / steps : 5000;

        for (int i = 0; i <= 100; i += 2)
        {
            if (ct.IsCancellationRequested)
                break;

            Progress = i;
            var stageIndex = Math.Min(i / (100 / steps), LoadingStages.Count - 1);
            LoadingStatus = LoadingStages[stageIndex];

            var delay = duration > 0 ? stepDuration / 50 : 50;
            await Task.Delay(delay, ct);
        }

        if (!ct.IsCancellationRequested && duration > 0)
        {
            // Auto-close after animation completes (unless manual mode)
            await Task.Delay(500, ct);
            await CleanupAndCloseAsync();
        }
    }

    private async Task AnimatePropertyAsync(Action<double> setter, double from, double to, int durationMs)
    {
        var steps = 20;
        var increment = (to - from) / steps;
        var delay = durationMs / steps;

        for (int i = 0; i <= steps; i++)
        {
            setter(from + (increment * i));
            await Task.Delay(delay);
        }
    }

    private async Task CleanupAndCloseAsync()
    {
        _tipTimer.Stop();
        _animationTimer.Stop();
        _launchCts?.Cancel();

        // Fade out animation
        await AnimatePropertyAsync(value => ContentOpacity = value, 1, 0, 200);

        IsVisible = false;
        IsAnimating = false;
        Progress = 0;
        _currentTipIndex = 0;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources used by the view model.
    /// </summary>
    public void Dispose()
    {
        _tipTimer.Dispose();
        _animationTimer.Dispose();
        _launchCts?.Dispose();
    }

    #endregion
}

/// <summary>
/// Presentation model for game briefing data.
/// </summary>
public sealed record GameBriefingPresentation
{
    public string GameTitle { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string LastSessionSummary { get; set; } = string.Empty;
    public string CurrentObjective { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public List<string> Tips { get; set; } = new();
    public List<RecentAchievementPresentation> RecentAchievements { get; set; } = new();
    public TimeSpan TotalPlaytime { get; set; }
    public string? CoverArtPath { get; set; }
    public string? BackgroundPath { get; set; }
}

/// <summary>
/// Presentation model for recent achievement data.
/// </summary>
public sealed record RecentAchievementPresentation
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public DateTime UnlockedAt { get; set; }
}
