namespace SaveState.Presentation.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.UserManagement.Services;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Library;
using SaveState.Presentation.ViewModels.Library.GameDetail;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;

/// <summary>
/// View model for the game library, managing game collections, search, and filtering.
/// Provides the main interface for browsing and managing the user's game collection.
/// </summary>
public partial class GameLibraryViewModel : ObservableObject, INavigationAware
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
    private readonly IClipboardService _clipboardService;
    private readonly IUiGameContextService _gameContextService;
    private readonly INaturalLanguageGameSearch _searchService;
    private readonly ILogger<GameLibraryViewModel> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ITimeProvider _timeProvider;
    // Navigation state
    private GameId? _selectedGameId;

    // New Library Components
    public LibraryViewModel LibraryViewModel { get; }
    public LibrarySidebarViewModel SidebarViewModel => LibraryViewModel.SidebarViewModel;
    public LibraryToolbarViewModel ToolbarViewModel => LibraryViewModel.ToolbarViewModel;

    public GameLibraryViewModel(
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
        INaturalLanguageGameSearch searchService,
        Library.LibraryViewModel libraryViewModel,
        ILogger<GameLibraryViewModel> logger,
        ILoggerFactory loggerFactory,
        ITimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(libraryViewModel);

        _mediator = mediator;
        _navigationService = navigationService;
        _overlayService = overlayService;
        _notificationService = notificationService;
        _aiOrchestrator = aiOrchestrator;
        _userContextService = userContextService;
        _modService = modService;
        _dialogService = dialogService;
        _backlogService = backlogService;
        _clipboardService = clipboardService;
        _gameContextService = gameContextService;
        _searchService = searchService;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _timeProvider = timeProvider;
        LibraryViewModel = libraryViewModel;

        // Subscribe to natural language search requests
        WeakReferenceMessenger.Default.Register<SaveState.Presentation.Messages.NaturalLanguageSearchRequestedMessage>(this, (r, m) =>
        {
            _ = ExecuteNaturalLanguageSearchAsync(m.Value);
        });

        // Set default view
        CurrentView = LibraryViewModel;
    }

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public GameLibraryViewModel()
    {
        // Dependencies are initialized by the framework at runtime
        // LibraryViewModel is set by the source-generated property
    }

    public async Task ExecuteNaturalLanguageSearchAsync(string query)
    {
         _logger.LogInformation("Executing natural language search: {Query}", query);
         try
         {
             var filter = await _searchService.ParseQueryAsync(query);

             // Update LibraryViewModel filter
             LibraryViewModel.ActiveAdHocFilter = filter;

             // Provide feedback
             _notificationService.ShowInfo($"Filtered: {query}", "AI Search");

             // Reload data
             await LibraryViewModel.LoadLibraryDataCommand.ExecuteAsync(null);
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Error executing natural language search");
             _notificationService.ShowError("Failed to process search", "Error");
         }
    }

    // INavigationAware implementation
    public async Task OnNavigatedToAsync(object? parameter)
    {
        if (parameter is GameId gameId)
        {
            // Navigate to game detail view
            await NavigateToGameDetailAsync(gameId);
        }
        else
        {
            // Show library overview
            await NavigateToLibraryAsync();
        }
    }

    public Task OnNavigatedFromAsync()
    {
        // Cleanup if needed
        return Task.CompletedTask;
    }

    // View state properties
    [ObservableProperty]
    private bool _isShowingLibrary = true;

    [ObservableProperty]
    private bool _isShowingGameDetail;

    [ObservableProperty]
    private object? _currentView;

    // Game Detail View Model
    [ObservableProperty]
    private Library.GameDetail.GameDetailViewModel? _gameDetailViewModel;

    private async Task NavigateToGameDetailAsync(GameId gameId)
    {
        _selectedGameId = gameId;
        IsShowingGameDetail = true;
        IsShowingLibrary = false;

        // Create or reuse game detail view model
        GameDetailViewModel = new Library.GameDetail.GameDetailViewModel(
            _mediator,
            _navigationService,
            _overlayService,
            _notificationService,
            _aiOrchestrator,
            _userContextService,
            _modService,
            _dialogService,
            _backlogService,
            _clipboardService,
            _gameContextService,
            gameId,
            _loggerFactory,
            _timeProvider);
        CurrentView = GameDetailViewModel;

        _logger.LogInformation("Navigated to game detail for {GameId}", gameId);
    }

    private async Task NavigateToLibraryAsync()
    {
        _selectedGameId = null;
        IsShowingGameDetail = false;
        IsShowingLibrary = true;

        CurrentView = LibraryViewModel;

        try
        {
            // Ensure library data is loaded
            await LibraryViewModel.LoadLibraryDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load library overview data");
            _notificationService.ShowError("Failed to load library data", "Library");
        }

        _logger.LogInformation("Navigated to library overview");
    }
}
