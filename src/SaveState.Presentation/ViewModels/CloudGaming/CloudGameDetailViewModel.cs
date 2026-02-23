using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.CloudGaming;

namespace SaveState.Presentation.ViewModels.CloudGaming;

/// <summary>
/// ViewModel for the Cloud Game Detail view.
/// </summary>
public partial class CloudGameDetailViewModel : ObservableObject
{
    private readonly ILogger<CloudGameDetailViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the CloudGameDetailViewModel.
    /// </summary>
    public CloudGameDetailViewModel(ILogger<CloudGameDetailViewModel> logger)
    {
        _logger = logger;
    }

    #region Observable Properties

    /// <summary>
    /// The game being displayed.
    /// </summary>
    [ObservableProperty]
    private CloudGame? _game;

    /// <summary>
    /// Session history for this game.
    /// </summary>
    [ObservableProperty]
    private List<CloudSession> _sessionHistory = new();

    /// <summary>
    /// Whether the game is available on other providers.
    /// </summary>
    [ObservableProperty]
    private List<CloudProvider> _availableOnProviders = new();

    /// <summary>
    /// Recommended quality based on connection test.
    /// </summary>
    [ObservableProperty]
    private SessionQuality _recommendedQuality = SessionQuality.High;

    /// <summary>
    /// Whether the game can be launched.
    /// </summary>
    public bool CanLaunch => Game?.Status == CloudGameStatus.Available;

    /// <summary>
    /// Provider display name.
    /// </summary>
    public string ProviderDisplayName => Game?.Provider.ToString() ?? "Unknown";

    /// <summary>
    /// Formatted play time.
    /// </summary>
    public string FormattedPlayTime => Game?.TotalPlayTime.TotalHours >= 1
        ? $"{Game.TotalPlayTime.TotalHours:F1}h played"
        : $"{Game?.TotalPlayTime.TotalMinutes:F0}m played";

    /// <summary>
    /// Last played relative time.
    /// </summary>
    public string LastPlayedRelative
    {
        get
        {
            if (Game?.LastPlayed is null) return "Never played";

            var diff = DateTime.UtcNow - Game.LastPlayed.Value;
            if (diff.TotalMinutes < 60) return $"{diff.TotalMinutes:F0}m ago";
            if (diff.TotalHours < 24) return $"{diff.TotalHours:F0}h ago";
            return $"{diff.TotalDays:F0}d ago";
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Launches the game.
    /// </summary>
    [RelayCommand]
    private async Task LaunchGameAsync()
    {
        if (Game is null) return;

        _logger.LogInformation("Launching {GameTitle} from game detail", Game.Title);

        // TODO: Navigate to stream launcher or start session directly
        await Task.Delay(100);
    }

    /// <summary>
    /// Toggles favorite status.
    /// </summary>
    [RelayCommand]
    private void ToggleFavorite()
    {
        if (Game is null) return;

        Game.IsFavorite = !Game.IsFavorite;
        OnPropertyChanged(nameof(Game));

        _logger.LogInformation("{Action} {GameTitle} from favorites",
            Game.IsFavorite ? "Added" : "Removed", Game.Title);
    }

    /// <summary>
    /// Views the game on the provider's store/page.
    /// </summary>
    [RelayCommand]
    private async Task ViewOnStoreAsync()
    {
        if (Game is null) return;

        _logger.LogInformation("Opening store page for {GameTitle}", Game.Title);

        // TODO: Open browser to provider store
        await Task.Delay(100);
    }

    /// <summary>
    /// Switches to a different provider for this game.
    /// </summary>
    [RelayCommand]
    private async Task SwitchProviderAsync(CloudProvider? provider)
    {
        if (provider is null || Game is null) return;

        _logger.LogInformation("Switching {GameTitle} to {Provider}",
            Game.Title, provider);

        // TODO: Find the same game on different provider and switch
        await Task.Delay(100);
    }

    /// <summary>
    /// Closes the detail view.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // TODO: Navigate back or close dialog
    }

    #endregion

    /// <summary>
    /// Loads game details.
    /// </summary>
    public void LoadGame(CloudGame game)
    {
        Game = game;

        // TODO: Load session history from service
        // TODO: Check availability on other providers
        // TODO: Get recommended quality based on connection

        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(ProviderDisplayName));
        OnPropertyChanged(nameof(FormattedPlayTime));
        OnPropertyChanged(nameof(LastPlayedRelative));
    }

    partial void OnGameChanged(CloudGame? value)
    {
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(ProviderDisplayName));
        OnPropertyChanged(nameof(FormattedPlayTime));
        OnPropertyChanged(nameof(LastPlayedRelative));
    }
}
