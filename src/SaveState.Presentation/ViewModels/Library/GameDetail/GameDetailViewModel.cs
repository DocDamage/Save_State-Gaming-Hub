using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common.ValueObjects;
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
    private readonly ILogger<GameDetailViewModel> _logger;

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
        GameId gameId,
        ILoggerFactory loggerFactory)
    {
        _mediator = mediator;
        _navigationService = navigationService;
        _overlayService = overlayService;
        _notificationService = notificationService;
        _aiOrchestrator = aiOrchestrator;
        _userContextService = userContextService;
        _modService = modService;
        _dialogService = dialogService;
        _gameId = gameId;
        _logger = loggerFactory.CreateLogger<GameDetailViewModel>();

        // Initialize tab view models with dependencies
        OverviewTab = new GameOverviewTabViewModel(_mediator, _userContextService, _aiOrchestrator, _dialogService, loggerFactory.CreateLogger<GameOverviewTabViewModel>());
        SaveStatesTab = new GameSaveStatesTabViewModel(_mediator, _dialogService, loggerFactory.CreateLogger<GameSaveStatesTabViewModel>());
        AchievementsTab = new GameAchievementsTabViewModel(_mediator, _userContextService, _dialogService, loggerFactory.CreateLogger<GameAchievementsTabViewModel>());
        SessionsTab = new GameSessionsTabViewModel(_mediator, _dialogService, loggerFactory.CreateLogger<GameSessionsTabViewModel>());
        NotesTab = new GameNotesTabViewModel(_mediator, _userContextService, _dialogService, loggerFactory.CreateLogger<GameNotesTabViewModel>());
        ModsTab = new GameModsTabViewModel(_mediator, _modService, _notificationService, _dialogService, loggerFactory.CreateLogger<GameModsTabViewModel>());
        MediaTab = new GameMediaTabViewModel(_mediator, _userContextService, _dialogService, loggerFactory.CreateLogger<GameMediaTabViewModel>());
        PerformanceTab = new GamePerformanceTabViewModel(_mediator, Locator.Current.GetService<IPerformanceMonitor>()!, loggerFactory.CreateLogger<GamePerformanceTabViewModel>());

        InitializeTabs();
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

    [RelayCommand]
    private async Task NavigateBack()
    {
        await _navigationService.NavigateTo("Library");
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
    private void ConfigureLaunch()
    {
        // TODO: Open launch configuration dialog
        _logger.LogInformation("Configuring launch for game {GameId}", GameId);
    }

    [RelayCommand]
    private void BrowseFiles()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(InstallPath))
            {
                _logger.LogWarning("Cannot browse files - no install path for game {GameId}", GameId);
                // TODO: Show warning notification to user
                return;
            }

            if (!Directory.Exists(InstallPath))
            {
                _logger.LogWarning("Cannot browse files - install path does not exist: {Path}", InstallPath);
                // TODO: Show warning notification to user
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
            // TODO: Show error notification to user
        }
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        // TODO: Toggle favorite status
        FavoriteButtonClass = FavoriteButtonClass == "Secondary" ? "Primary" : "Secondary";
        _logger.LogInformation("Toggling favorite for game {GameId}", GameId);
    }

    [RelayCommand]
    private void AddToBacklog()
    {
        // TODO: Add to backlog
        _logger.LogInformation("Adding game {GameId} to backlog", GameId);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        // TODO: Open game settings
        _logger.LogInformation("Opening settings for game {GameId}", GameId);
    }

    [RelayCommand]
    private void RateGame()
    {
        // TODO: Open rating dialog
        _logger.LogInformation("Rating game {GameId}", GameId);
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
