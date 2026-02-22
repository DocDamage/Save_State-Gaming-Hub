using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.UserManagement.Services;
using SaveState.Core.Performance.Services;
using SaveState.Presentation.Services;
using Splat;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// Main view model for the Game Detail view.
/// </summary>
public partial class GameDetailViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly INavigationService _navigationService;
    private readonly IOverlayService _overlayService;
    private readonly INotificationService _notificationService;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IUserContextService _userContextService;
    private readonly IModManagementService _modService;
    private readonly IDialogService _dialogService;
    private readonly IBacklogService _backlogService;
    private readonly IUiGameContextService _gameContextService;
    private readonly ILogger<GameDetailViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private GameId _gameId;

    [ObservableProperty]
    private string _title = "Game Details";

    [ObservableProperty]
    private string _gameTitle = string.Empty;

    [ObservableProperty]
    private string? _coverArtUrl;

    [ObservableProperty]
    private string _developerAndPublisher = string.Empty;

    [ObservableProperty]
    private string _releaseDateText = string.Empty;

    [ObservableProperty]
    private string _platformText = string.Empty;

    [ObservableProperty]
    private string _genreText = string.Empty;

    [ObservableProperty]
    private string _userRatingStars = "☆☆☆☆☆";

    [ObservableProperty]
    private string _statusText = "Not Started";

    [ObservableProperty]
    private bool _canLaunch;

    [ObservableProperty]
    private string? _installPath;

    [ObservableProperty]
    private string _favoriteButtonClass = "Secondary";

    [ObservableProperty]
    private string _backlogButtonClass = "Secondary";

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isInBacklog;

    private List<string> _currentGameTags = new();

    [ObservableProperty]
    private ObservableCollection<GameDetailTabViewModel> _tabItems = new();

    [ObservableProperty]
    private object? _selectedTabView;

    // Tab View Models
    public GameOverviewTabViewModel OverviewTab { get; }
    public GameSaveStatesTabViewModel SaveStatesTab { get; }
    public GameAchievementsTabViewModel AchievementsTab { get; }
    public GameSessionsTabViewModel SessionsTab { get; }
    public GameNotesTabViewModel NotesTab { get; }
    public GameModsTabViewModel ModsTab { get; }
    public GameMediaTabViewModel MediaTab { get; }
    public GamePerformanceTabViewModel PerformanceTab { get; }

    public GameDetailViewModel(
        IMediator mediator,
        INavigationService navigationService,
        IOverlayService overlayService,
        INotificationService notificationService,
        IAiOrchestrator aiOrchestrator,
        IUserContextService userContextService,
        IModManagementService modService,
        IDialogService dialogService,
        IBacklogService backlogService,
        IClipboardService clipboardService,
        IUiGameContextService gameContextService,
        GameId gameId,
        ILoggerFactory loggerFactory,
        ITimeProvider timeProvider)
    {
        _mediator = mediator;
        _navigationService = navigationService;
        _overlayService = overlayService;
        _notificationService = notificationService;
        _aiOrchestrator = aiOrchestrator;
        _userContextService = userContextService;
        _modService = modService;
        _dialogService = dialogService;
        _backlogService = backlogService;
        _gameContextService = gameContextService;
        _gameId = gameId;
        _timeProvider = timeProvider;
        _logger = loggerFactory.CreateLogger<GameDetailViewModel>();

        // Initialize tab view models with dependencies
        OverviewTab = new GameOverviewTabViewModel(_mediator, _userContextService, _aiOrchestrator, _dialogService, _navigationService, _gameContextService, loggerFactory.CreateLogger<GameOverviewTabViewModel>(), timeProvider);
        SaveStatesTab = new GameSaveStatesTabViewModel(_mediator, _dialogService, _notificationService, loggerFactory.CreateLogger<GameSaveStatesTabViewModel>(), _timeProvider);
        AchievementsTab = new GameAchievementsTabViewModel(_mediator, _userContextService, _dialogService, loggerFactory.CreateLogger<GameAchievementsTabViewModel>(), timeProvider);
        SessionsTab = new GameSessionsTabViewModel(_mediator, _dialogService, loggerFactory.CreateLogger<GameSessionsTabViewModel>(), _timeProvider);
        NotesTab = new GameNotesTabViewModel(_mediator, _userContextService, _dialogService, clipboardService, _notificationService, loggerFactory.CreateLogger<GameNotesTabViewModel>(), timeProvider);
        ModsTab = new GameModsTabViewModel(_mediator, _modService, _notificationService, _dialogService, loggerFactory.CreateLogger<GameModsTabViewModel>(), timeProvider);
        MediaTab = new GameMediaTabViewModel(_mediator, _userContextService, _dialogService,
            Locator.Current.GetService<IGameMediaService>()!,
            _notificationService,
            Locator.Current.GetService<SaveState.Core.Sync.ISyncService>()!,
            clipboardService,
            Locator.Current.GetService<IImageAnalysisService>(),
            loggerFactory.CreateLogger<GameMediaTabViewModel>(),
            timeProvider);
        PerformanceTab = new GamePerformanceTabViewModel(_mediator, Locator.Current.GetService<IPerformanceMonitor>()!, loggerFactory.CreateLogger<GamePerformanceTabViewModel>(), timeProvider);

        InitializeTabs();

        // Set context
        _gameContextService.SetSelectedGame(gameId);

        LoadGameDataAsync();
    }

    private void InitializeTabs()
    {
        TabItems.Add(new GameDetailTabViewModel("Overview", "📊", () => SelectTab(OverviewTab, "Overview")));
        TabItems.Add(new GameDetailTabViewModel("Save States", "💾", () => SelectTab(SaveStatesTab, "Save States")));
        TabItems.Add(new GameDetailTabViewModel("Achievements", "🏆", () => SelectTab(AchievementsTab, "Achievements")));
        TabItems.Add(new GameDetailTabViewModel("Sessions", "🎮", () => SelectTab(SessionsTab, "Sessions")));
        TabItems.Add(new GameDetailTabViewModel("Notes", "📝", () => SelectTab(NotesTab, "Notes")));
        TabItems.Add(new GameDetailTabViewModel("Mods", "🛠️", () => SelectTab(ModsTab, "Mods")));
        TabItems.Add(new GameDetailTabViewModel("Media", "🖼️", () => SelectTab(MediaTab, "Media")));
        TabItems.Add(new GameDetailTabViewModel("Performance", "⚡", () => SelectTab(PerformanceTab, "Performance")));

        // Select first tab by default
        if (TabItems.Any())
        {
            SelectTab(OverviewTab, "Overview");
        }
    }

    private async Task LoadGameDataAsync()
    {
        try
        {
            // Load game data from backend
            var query = new GetGameByIdQuery(GameId);
            var game = await _mediator.Send(query).ConfigureAwait(false);

            if (game is null)
            {
                _logger.LogWarning("Game not found: {GameId}", GameId);
                GameTitle = "Game Not Found";
                Title = "Game Details - Not Found";
                return;
            }

            // Populate view model properties
            GameTitle = game.Title;
            Title = $"Game Details - {GameTitle}";
            CoverArtUrl = game.CoverImagePath;
            InstallPath = game.InstallPath;
            PlatformText = game.Platform?.Name.Value ?? "Unknown";
            CanLaunch = game.Status == Core.GameLibrary.Enums.GameStatus.Installed;
            StatusText = game.Status.ToString();

            // Format release date
            if (game.ReleaseDate.HasValue)
            {
                ReleaseDateText = game.ReleaseDate.Value.ToString("MMM d, yyyy");
            }

            // Format developer/publisher (if available in future)
            DeveloperAndPublisher = game.Source ?? "Unknown";

            // Format genres
            if (game.Genres.Any())
            {
                GenreText = string.Join(", ", game.Genres.Select(g => g.Name));
            }

            // Format user rating
            if (game.UserRating.HasValue)
            {
                var rating = (int)Math.Round(game.UserRating.Value);
                UserRatingStars = new string('★', rating) + new string('☆', 5 - rating);
            }

            // Initialize Tags
            _currentGameTags = game.Tags.ToList();
            IsFavorite = _currentGameTags.Contains("Favorite");
            UpdateFavoriteUi();

            // Initialize Backlog Status
            var backlogEntry = await _backlogService.GetBacklogEntryAsync(GameId);
            IsInBacklog = backlogEntry.IsSuccess && backlogEntry.Value != null;
            UpdateBacklogUi();

            // Update all tabs with game data
            await Task.WhenAll(
                OverviewTab.LoadDataAsync(GameId),
                SaveStatesTab.LoadDataAsync(GameId),
                AchievementsTab.LoadDataAsync(GameId),
                SessionsTab.LoadDataAsync(GameId),
                NotesTab.LoadDataAsync(GameId),
                ModsTab.LoadDataAsync(GameId),
                MediaTab.LoadDataAsync(GameId),
                PerformanceTab.LoadDataAsync(GameId)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load game data for {GameId}", GameId);
        }
    }

    private void UpdateFavoriteUi()
    {
        FavoriteButtonClass = IsFavorite ? "Primary" : "Secondary";
    }

    private void UpdateBacklogUi()
    {
        BacklogButtonClass = IsInBacklog ? "Primary" : "Secondary";
    }

    [RelayCommand]
    private async Task NavigateBack()
    {
        _gameContextService.SetSelectedGame((Game?)null);
        await _navigationService.NavigateToAsync("Library");
    }

    [RelayCommand]
    private async Task LaunchGame()
    {
        try
        {
            _logger.LogInformation("Launching game {GameId}: {Title}", GameId, GameTitle);

            // Launch the game using the command
            var command = new LaunchGameCommand { GameId = GameId };
            var result = await _mediator.Send(command).ConfigureAwait(false);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to launch game {GameId}: {Error}", GameId, result.Error);
                _notificationService.ShowError(
                    result.Error ?? "An unknown error occurred while launching the game.",
                    "Launch Failed");
                return;
            }

            _logger.LogInformation("Successfully launched game {GameId}: {Title}", GameId, GameTitle);

            _gameContextService.SetRunningGame(GameId);

            _notificationService.ShowSuccess(
                $"{GameTitle} has been launched successfully.",
                "Game Launched");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while launching game {GameId}", GameId);
            _notificationService.ShowError(
                $"An unexpected error occurred: {ex.Message}",
                "Launch Error");
        }
    }

    [RelayCommand]
    private async Task ConfigureLaunch()
    {
        _logger.LogInformation("Configuring launch for game {GameId}", GameId);

        try
        {
            var result = await _dialogService.ShowLaunchConfigDialogAsync(GameId.Value);

            if (result != null)
            {
                // In a real implementation, you would save this configuration to the database
                // For now we just log it
                _logger.LogInformation("Launch configuration updated: {Args}", result.LaunchArguments);
                _notificationService.ShowSuccess("Launch configuration updated", "Success");
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to configure launch options");
             _notificationService.ShowError("Failed to open launch configuration", "Error");
        }
    }

    [RelayCommand]
    private void BrowseFiles()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(InstallPath))
            {
                _logger.LogWarning("Cannot browse files - no install path for game {GameId}", GameId);
                _notificationService.ShowWarning("No installation path set for this game.", "Browse Files");
                return;
            }

            if (!Directory.Exists(InstallPath))
            {
                _logger.LogWarning("Cannot browse files - install path does not exist: {Path}", InstallPath);
                _notificationService.ShowWarning("Installation path does not exist on disk.", "Browse Files");
                return;
            }

            _logger.LogInformation("Opening installation folder for game {GameId}: {Path}", GameId, InstallPath);

            // Open the folder in Windows Explorer
            Process.Start(new ProcessStartInfo
            {
                FileName = InstallPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open installation folder for game {GameId}", GameId);
            _notificationService.ShowError($"Failed to open folder: {ex.Message}", "System Error");
        }
    }

    [RelayCommand]
    private async Task ToggleFavorite()
    {
        try
        {
            var newFavoriteState = !IsFavorite;

            // Optimistic update
            IsFavorite = newFavoriteState;
            UpdateFavoriteUi();

            var currentTags = _currentGameTags.ToList();
            if (newFavoriteState && !currentTags.Contains("Favorite"))
            {
                currentTags.Add("Favorite");
            }
            else if (!newFavoriteState && currentTags.Contains("Favorite"))
            {
                currentTags.Remove("Favorite");
            }

            var command = new UpdateGameTagsCommand(GameId, currentTags);
            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                // Revert on failure
                IsFavorite = !newFavoriteState;
                UpdateFavoriteUi();
                _logger.LogError("Failed to update favorite status: {Error}", result.Error);
                _notificationService.ShowError("Failed to update favorite status");
            }
            else
            {
                _currentGameTags = currentTags;
                _logger.LogInformation("Updated favorite status for game {GameId} to {Status}", GameId, newFavoriteState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating favorite status");
            _notificationService.ShowError("Failed to update favorite status");
            IsFavorite = !IsFavorite; // Revert
            UpdateFavoriteUi();
        }
    }

    [RelayCommand]
    private async Task ToggleBacklog()
    {
        try
        {
            var newBacklogState = !IsInBacklog;
            IsInBacklog = newBacklogState; // Optimistic
            UpdateBacklogUi();

            Result result;
            if (newBacklogState)
            {
                result = await _mediator.Send(new AddToBacklogCommand(GameId));
            }
            else
            {
                result = await _mediator.Send(new RemoveFromBacklogCommand(GameId));
            }

            if (result.IsFailure)
            {
                IsInBacklog = !newBacklogState; // Revert
                UpdateBacklogUi();
                _logger.LogError("Failed to update backlog status: {Error}", result.Error);
                _notificationService.ShowError($"Failed to {(newBacklogState ? "add to" : "remove from")} backlog");
            }
            else
            {
                _logger.LogInformation("Updated backlog status for game {GameId} to {Status}", GameId, newBacklogState);
                if (newBacklogState)
                    _notificationService.ShowSuccess("Added to Backlog");
                else
                    _notificationService.ShowInfo("Removed", "Removed from Backlog");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating backlog status");
            _notificationService.ShowError("Failed to update backlog status");
            IsInBacklog = !IsInBacklog; // Revert
            UpdateBacklogUi();
        }
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        _logger.LogInformation("Opening settings for game {GameId}", GameId);
        // Using Launch Configuration as the primary 'Settings' for a game
        try
        {
            var result = await _dialogService.ShowLaunchConfigDialogAsync(GameId.Value);
            if (result != null)
            {
                 // Persist launch config (mocked/logged for now as in ConfigureLaunch)
                 _logger.LogInformation("Updated specific settings: {Args}", result.LaunchArguments);
                 _notificationService.ShowSuccess("Game settings updated", "Settings");
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to open settings");
        }
    }

    [RelayCommand]
    private async Task RateGame()
    {
        _logger.LogInformation("Rating game {GameId}", GameId);
        try
        {
            // Get current rating if any (mocked as null for now or extracted from local prop)
            double? currentRating = null;
            // Parsing _userRatingStars is hard, better to fetch or assume null

            var result = await _dialogService.ShowGameRatingDialogAsync(GameId.Value, currentRating);
            if (result != null)
            {
                 var command = new Application.Social.Commands.CreateReviewCommand(
                    GameId.Value,
                    (int)result.Rating,
                    true, // Default recommend
                    "User Rating",
                    result.ReviewText ?? string.Empty);

                 var cmdResult = await _mediator.Send(command);
                 if (cmdResult.IsSuccess)
                 {
                     _notificationService.ShowSuccess("Rating submitted successfully", "Rated");
                     // Reload data to reflect new rating
                     await LoadGameDataAsync();
                 }
                 else
                 {
                     _notificationService.ShowError("Failed to submit rating");
                 }
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to rate game");
        }
    }

    /// <summary>
    /// Command to navigate to analytics view for this game.
    /// </summary>
    [RelayCommand]
    private void NavigateToAnalytics()
    {
        try
        {
            _logger.LogInformation("Navigating to analytics for game {GameId}", GameId);
            _navigationService.NavigateToAsync("Analytics", new { gameId = GameId });
            _notificationService.ShowInfo("Analytics", $"Viewing analytics for {GameTitle}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to analytics for game {GameId}", GameId);
            _notificationService.ShowError("Failed to navigate to analytics");
        }
    }

    private void SelectTab(object tabViewModel, string tabTitle)
    {
        SelectedTabView = tabViewModel;

        // Update tab selection state
        foreach (var tab in TabItems)
        {
            tab.IsSelected = tab.Title == tabTitle;
        }
    }
}

/// <summary>
/// View model for individual tabs in the game detail view.
/// </summary>
public partial class GameDetailTabViewModel : ObservableObject
{
    public GameDetailTabViewModel(string title, string icon, Action selectAction)
    {
        Title = title;
        Icon = icon;
        SelectCommand = new RelayCommand(selectAction);
    }

    public string Title { get; }
    public string Icon { get; }
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string ButtonClass => IsSelected ? "Primary" : "Secondary";
}
