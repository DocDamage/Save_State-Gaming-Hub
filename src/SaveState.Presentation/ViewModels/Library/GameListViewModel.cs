using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Library;

/// <summary>
/// View model for the game list/table view.
/// </summary>
public partial class GameListViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ILogger<GameListViewModel> _logger;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<GameListItemViewModel> _games = new();

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

    public GameListViewModel(
        IMediator mediator,
        ILogger<GameListViewModel> logger,
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
        bool sortDescending = false,
        CollectionFilter? adHocFilter = null)
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
            else if (smartFilter == "not_installed") statusFilter = GameStatus.NotInstalled;

            CollectionFilter? effectiveFilter = adHocFilter;
            if (smartFilter == "favorites")
            {
                effectiveFilter = (effectiveFilter ?? new CollectionFilter()) with { Tag = "Favorite" };
            }
            else if (smartFilter == "backlog")
            {
                effectiveFilter = (effectiveFilter ?? new CollectionFilter()) with { IsInBacklog = true };
            }

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
                SortDescending = sortDescending,
                AdHocFilter = effectiveFilter
            };

            var pagedResult = await _mediator.Send(query).ConfigureAwait(false);

            Games.Clear();

            foreach (var game in pagedResult.Items)
            {
                var listItemViewModel = MapToGameListItemViewModel(game);
                Games.Add(listItemViewModel);
            }

            _totalCount = pagedResult.TotalCount;
            _totalPages = pagedResult.TotalPages;

            _logger.LogInformation("Loaded page {PageNumber}/{TotalPages} with {Count} games (Total: {TotalCount})",
                pageNumber, _totalPages, pagedResult.Items.Count, _totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load games for list view");
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

    private GameListItemViewModel MapToGameListItemViewModel(Game game)
    {
        var (statusText, statusColor) = GetStatusInfo(game.Status);
        var platformName = game.Platform?.Name.Value ?? "Unknown";
        var platformIcon = GetPlatformIcon(game.Platform?.Type ?? Core.GameLibrary.Enums.PlatformType.Computer);
        var lastPlayedText = game.LastPlayedAt.HasValue
            ? game.LastPlayedAt.Value.ToString("MMM d, yyyy")
            : "Never";

        // Create a logger for GameListItemViewModel
        var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var itemLogger = loggerFactory.CreateLogger<GameListItemViewModel>();

        return new GameListItemViewModel(
            gameId: GameId.From(game.Id),
            title: game.Title,
            coverArtUrl: game.CoverImagePath,
            developer: string.Empty, // Developer metadata not yet available in Game entity
            platformName: platformName,
            platformIcon: platformIcon,
            statusText: statusText,
            statusColor: statusColor,
            lastPlayedText: lastPlayedText,
            playtime: game.TotalPlayTime,
            rating: game.UserRating ?? 0.0,
            releaseDate: game.ReleaseDate,
            logger: itemLogger,
            navigationService: _navigationService);
    }

    private static (string Text, string Color) GetStatusInfo(GameStatus status)
    {
        return status switch
        {
            GameStatus.NotInstalled => ("Not Installed", "#888888"),
            GameStatus.Installed => ("Installed", "#4CAF50"),
            GameStatus.Running => ("Running", "#2196F3"),
            GameStatus.Updating => ("Updating", "#FF9800"),
            _ => ("Unknown", "#888888")
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

    public GameListItemViewModel[] GetSelectedGames()
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

/// <summary>
/// View model for individual game items in the list view.
/// </summary>
public partial class GameListItemViewModel : ObservableObject
{
    private readonly ILogger<GameListItemViewModel> _logger;

    [ObservableProperty]
    private GameId _gameId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _coverArtUrl;

    [ObservableProperty]
    private string _developer = string.Empty;

    [ObservableProperty]
    private string _platformName = string.Empty;

    [ObservableProperty]
    private string _platformIcon = "🎮";

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _statusColor = "#888888";

    [ObservableProperty]
    private string _lastPlayedText = "Never";

    [ObservableProperty]
    private string _playtimeText = "0h 0m";

    [ObservableProperty]
    private string _ratingStars = "☆☆☆☆☆";

    [ObservableProperty]
    private string _ratingText = "0/10";

    [ObservableProperty]
    private string _releaseYearText = "----";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isSelectionMode;

    [ObservableProperty]
    private string _backgroundBrush = "Transparent";

    private readonly INavigationService? _navigationService;

    public GameListItemViewModel(ILogger<GameListItemViewModel> logger)
    {
        _logger = logger;
    }

    public GameListItemViewModel(
        GameId gameId,
        string title,
        string? coverArtUrl,
        string developer,
        string platformName,
        string platformIcon,
        string statusText,
        string statusColor,
        string lastPlayedText,
        TimeSpan playtime,
        double rating,
        DateOnly? releaseDate,
        ILogger<GameListItemViewModel> logger,
        INavigationService? navigationService = null)
    {
        _logger = logger;
        _navigationService = navigationService;
        GameId = gameId;
        Title = title;
        CoverArtUrl = coverArtUrl;
        Developer = developer;
        PlatformName = platformName;
        PlatformIcon = platformIcon;
        StatusText = statusText;
        StatusColor = statusColor;
        LastPlayedText = lastPlayedText;
        PlaytimeText = FormatPlaytime(playtime);
        RatingStars = FormatRatingStars(rating);
        RatingText = FormatRatingText(rating);
        ReleaseYearText = releaseDate?.Year.ToString() ?? "----";
    }

    [RelayCommand]
    private async Task OpenGame()
    {
        if (_navigationService != null)
        {
            await _navigationService.NavigateTo("Library", GameId);
            _logger.LogInformation("Navigating to game detail: {Title} ({GameId})", Title, GameId);
        }
        else
        {
            _logger.LogWarning("Cannot navigate to game detail - navigation service not available");
        }
    }

    [RelayCommand]
    private void ToggleSelection()
    {
        if (IsSelectionMode)
        {
            IsSelected = !IsSelected;
            BackgroundBrush = IsSelected ? "{StaticResource SelectionBrush}" : "Transparent";
        }
    }

    private static string FormatPlaytime(TimeSpan playtime)
    {
        if (playtime.TotalHours >= 1)
        {
            return $"{(int)playtime.TotalHours}h {(int)playtime.Minutes}m";
        }
        else if (playtime.TotalMinutes >= 1)
        {
            return $"{(int)playtime.TotalMinutes}m";
        }
        return "--";
    }

    private static string FormatRatingStars(double rating)
    {
        if (rating <= 0) return "☆☆☆☆☆";

        var stars = string.Empty;
        var fullStars = (int)Math.Floor(rating / 2.0); // Assuming 10-point scale
        var hasHalfStar = (rating % 2) >= 1;

        for (int i = 0; i < fullStars; i++)
        {
            stars += "★";
        }

        if (hasHalfStar && fullStars < 5)
        {
            stars += "☆"; // Half star not available in text, using empty
        }

        while (stars.Length < 5)
        {
            stars += "☆";
        }

        return stars;
    }

    private static string FormatRatingText(double rating)
    {
        return rating > 0 ? $"{rating:F1}/10" : "--";
    }
}
