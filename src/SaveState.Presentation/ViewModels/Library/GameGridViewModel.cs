using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Library;

/// <summary>
/// View model for the game grid view.
/// </summary>
public partial class GameGridViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ILogger<GameGridViewModel> _logger;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<GameCardViewModel> _games = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isSelectionMode;

    [ObservableProperty]
    private bool _isAllSelected;

    // Pagination state
    private int _currentPage = 1;
    private int _pageSize = 24;
    private int _totalCount = 0;
    private int _totalPages = 0;

    public GameGridViewModel(
        IMediator mediator,
        ILogger<GameGridViewModel> logger,
        INavigationService navigationService)
    {
        _mediator = mediator;
        _logger = logger;
        _navigationService = navigationService;
    }

    /// <summary>
    /// Loads games for the current page.
    /// </summary>
    /// <param name="pageNumber">The page number to load (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    public async Task LoadGamesAsync(
        int pageNumber = 1,
        int pageSize = 24,
        string? searchTerm = null,
        string? smartFilter = null,
        string? collectionId = null,
        string? platformId = null,
        string? sortBy = null,
        bool sortDescending = false)
    {
        if (IsLoading) return;

        _currentPage = pageNumber;
        _pageSize = pageSize;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            GameStatus? statusFilter = null;
            if (smartFilter == "playing") statusFilter = GameStatus.Running;
            else if (smartFilter == "installed") statusFilter = GameStatus.Installed;

            Guid? pId = null;
            string? pFilter = null;

            if (!string.IsNullOrEmpty(platformId))
            {
                if (Guid.TryParse(platformId, out var parsed))
                    pId = parsed;
                else
                    pFilter = platformId;
            }

            // Map sort
            var sortEnum = GameSortBy.Title;
            if (!string.IsNullOrEmpty(sortBy))
            {
                if (sortBy.StartsWith("title")) sortEnum = GameSortBy.Title;
                else if (sortBy.StartsWith("playtime")) sortEnum = GameSortBy.PlayTime;
                else if (sortBy.StartsWith("last_played")) sortEnum = GameSortBy.LastPlayed;
                else if (sortBy.StartsWith("added")) sortEnum = GameSortBy.DateAdded;
                // New mappings
                else if (sortBy.StartsWith("release")) sortEnum = GameSortBy.ReleaseDate;
                else if (sortBy.StartsWith("rating")) sortEnum = GameSortBy.UserRating;
            }

            Guid? cId = null;
            if (Guid.TryParse(collectionId, out var parsedC))
            {
                cId = parsedC;
            }

            var query = new GetGamesQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                PlatformId = pId,
                PlatformFilter = pFilter,
                CollectionId = cId,
                SortBy = sortEnum,
                SortDescending = sortDescending
            };

            var pagedResult = await _mediator.Send(query).ConfigureAwait(false);

            Games.Clear();

            foreach (var game in pagedResult.Items)
            {
                var cardViewModel = MapToGameCardViewModel(game);
                Games.Add(cardViewModel);
            }

            _totalCount = pagedResult.TotalCount;
            _totalPages = pagedResult.TotalPages;

            _logger.LogInformation("Loaded page {PageNumber}/{TotalPages} with {Count} games (Total: {TotalCount})",
                pageNumber, _totalPages, pagedResult.Items.Count, _totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load games for grid view");
            ErrorMessage = "Failed to load games. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Gets the current pagination state.
    /// </summary>
    public (int CurrentPage, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage) GetPaginationState()
    {
        return (_currentPage, _pageSize, _totalCount, _totalPages, _currentPage > 1, _currentPage < _totalPages);
    }

    [RelayCommand]
    private async Task LoadGamesCommandAsync()
    {
        await LoadGamesAsync(_currentPage, _pageSize);
    }

    private GameCardViewModel MapToGameCardViewModel(Game game)
    {
        var (statusText, statusIcon, statusColor) = GetStatusInfo(game.Status);
        var platformName = game.Platform?.Name.Value ?? "Unknown";
        var platformIcon = GetPlatformIcon(game.Platform?.Type ?? Core.GameLibrary.Enums.PlatformType.Computer);

        // Create a logger for GameCardViewModel
        var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var cardLogger = loggerFactory.CreateLogger<GameCardViewModel>();

        return new GameCardViewModel(
            gameId: GameId.From(game.Id),
            title: game.Title,
            coverArtUrl: game.CoverImagePath,
            platformName: platformName,
            platformIcon: platformIcon,
            statusText: statusText,
            statusIcon: statusIcon,
            statusColor: statusColor,
            playtime: game.TotalPlayTime,
            rating: game.UserRating ?? 0.0,
            releaseDate: game.ReleaseDate,
            logger: cardLogger,
            navigationService: _navigationService);
    }

    private static (string Text, string Icon, string Color) GetStatusInfo(GameStatus status)
    {
        return status switch
        {
            GameStatus.NotInstalled => ("Not Installed", "📥", "#888888"),
            GameStatus.Installed => ("Installed", "✅", "#4CAF50"),
            GameStatus.Running => ("Running", "▶️", "#2196F3"),
            GameStatus.Updating => ("Updating", "🔄", "#FF9800"),
            _ => ("Unknown", "❓", "#888888")
        };
    }

    private static string GetPlatformIcon(Core.GameLibrary.Enums.PlatformType platformType)
    {
        return platformType switch
        {
            Core.GameLibrary.Enums.PlatformType.Computer => "💻",
            Core.GameLibrary.Enums.PlatformType.Console => "🎮",
            Core.GameLibrary.Enums.PlatformType.Handheld => "📱",
            Core.GameLibrary.Enums.PlatformType.Arcade => "🕹️",
            Core.GameLibrary.Enums.PlatformType.Other => "🎮",
            _ => "🎮"
        };
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        IsAllSelected = !IsAllSelected;
        foreach (var game in Games)
        {
            game.IsSelected = IsAllSelected;
        }
    }

    [RelayCommand]
    private void EnterSelectionMode()
    {
        IsSelectionMode = true;
        foreach (var game in Games)
        {
            game.IsSelectionMode = true;
        }
    }

    [RelayCommand]
    private void ExitSelectionMode()
    {
        IsSelectionMode = false;
        IsAllSelected = false;
        foreach (var game in Games)
        {
            game.IsSelected = false;
            game.IsSelectionMode = false;
        }
    }

    public void UpdateSelectionMode(bool isSelectionMode)
    {
        IsSelectionMode = isSelectionMode;
        foreach (var game in Games)
        {
            game.IsSelectionMode = isSelectionMode;
        }
    }

    public GameCardViewModel[] GetSelectedGames()
    {
        return Games.Where(g => g.IsSelected).ToArray();
    }

    public void ClearSelection()
    {
        IsAllSelected = false;
        foreach (var game in Games)
        {
            game.IsSelected = false;
        }
    }
}
