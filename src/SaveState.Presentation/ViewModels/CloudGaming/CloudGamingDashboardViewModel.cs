using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.CloudGaming;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.CloudGaming;

/// <summary>
/// ViewModel for the Cloud Gaming Dashboard.
/// </summary>
public partial class CloudGamingDashboardViewModel : ObservableObject
{
    private readonly ILogger<CloudGamingDashboardViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the CloudGamingDashboardViewModel.
    /// </summary>
    public CloudGamingDashboardViewModel(ILogger<CloudGamingDashboardViewModel> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        InitializeMockData();
    }

    #region Observable Properties

    /// <summary>
    /// Collection of cloud games in the library.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CloudGame> _games = new();

    /// <summary>
    /// Collection of connected provider statuses.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProviderStatus> _providers = new();

    /// <summary>
    /// Collection of recent gaming sessions.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CloudSession> _recentSessions = new();

    /// <summary>
    /// Currently selected game.
    /// </summary>
    [ObservableProperty]
    private CloudGame? _selectedGame;

    /// <summary>
    /// Currently selected provider filter.
    /// </summary>
    [ObservableProperty]
    private CloudProvider? _selectedProvider;

    /// <summary>
    /// Search query for filtering games.
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Whether a stream is currently active.
    /// </summary>
    [ObservableProperty]
    private bool _isStreaming;

    /// <summary>
    /// The currently active streaming session.
    /// </summary>
    [ObservableProperty]
    private CloudSession? _activeSession;

    /// <summary>
    /// Current stream settings configuration.
    /// </summary>
    [ObservableProperty]
    private StreamSettings _streamSettings = new();

    /// <summary>
    /// Current filter option.
    /// </summary>
    [ObservableProperty]
    private CloudGameFilter _currentFilter = CloudGameFilter.All;

    /// <summary>
    /// Current sort option.
    /// </summary>
    [ObservableProperty]
    private CloudGameSort _currentSort = CloudGameSort.LastPlayed;

    /// <summary>
    /// Whether connection test is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isTestingConnection;

    /// <summary>
    /// Whether library is being refreshed.
    /// </summary>
    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>
    /// Filtered and sorted games collection.
    /// </summary>
    public IEnumerable<CloudGame> FilteredGames => ApplyFiltersAndSort();

    #endregion

    #region Commands

    /// <summary>
    /// Launches a cloud game.
    /// </summary>
    [RelayCommand]
    private async Task LaunchGameAsync(CloudGame? game)
    {
        if (game is null) return;

        _logger.LogInformation("Launching cloud game: {GameTitle} from {Provider}",
            game.Title, game.Provider);

        // NOTE: This is a demo implementation. Replace with actual StreamLauncherView navigation.

        IsStreaming = true;
        ActiveSession = new CloudSession
        {
            Id = Guid.NewGuid(),
            Game = game,
            Provider = game.Provider,
            StartedAt = _timeProvider.UtcNow,
            Quality = StreamSettings.Quality,
            IsActive = true
        };
    }

    /// <summary>
    /// Connects a cloud provider account.
    /// </summary>
    [RelayCommand]
    private async Task ConnectProviderAsync(CloudProvider? provider)
    {
        if (provider is null) return;

        _logger.LogInformation("Connecting cloud provider: {Provider}", provider);

        // NOTE: This is a demo implementation. Replace with actual OAuth flow integration.

        var status = Providers.FirstOrDefault(p => p.Provider == provider);
        if (status is not null)
        {
            status.IsConnected = true;
        }
    }

    /// <summary>
    /// Disconnects a cloud provider account.
    /// </summary>
    [RelayCommand]
    private async Task DisconnectProviderAsync(CloudProvider? provider)
    {
        if (provider is null) return;

        _logger.LogInformation("Disconnecting cloud provider: {Provider}", provider);

        // NOTE: This is a demo implementation. Replace with actual token revocation.

        var status = Providers.FirstOrDefault(p => p.Provider == provider);
        if (status is not null)
        {
            status.IsConnected = false;
            status.Username = null;
        }
    }

    /// <summary>
    /// Tests connection to a cloud provider.
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync(CloudProvider? provider)
    {
        if (provider is null) return;

        IsTestingConnection = true;
        _logger.LogInformation("Testing connection to: {Provider}", provider);

        try
        {
            // NOTE: This is a demo implementation. Replace with actual connection test service.

            var result = new ConnectionTestResult
            {
                TestedAt = _timeProvider.UtcNow,
                Ping = 12,
                Jitter = 2,
                PacketLoss = 0.1f,
                DownloadSpeed = 85.5f,
                UploadSpeed = 25.3f,
                RecommendedQuality = SessionQuality.High,
                CanStream4K = true
            };

            var status = Providers.FirstOrDefault(p => p.Provider == provider);
            if (status is not null)
            {
                status.LastConnectionTest = result;
            }

            _logger.LogInformation("Connection test complete for {Provider}: {Ping}ms ping",
                provider, result.Ping);
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    /// <summary>
    /// Refreshes the game library from all connected providers.
    /// </summary>
    [RelayCommand]
    private async Task RefreshLibraryAsync()
    {
        IsRefreshing = true;
        _logger.LogInformation("Refreshing cloud game library");

        try
        {
            // NOTE: This is a demo implementation. Replace with actual provider API calls.

            _logger.LogInformation("Library refresh complete. {Count} games loaded", Games.Count);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Toggles favorite status for a game.
    /// </summary>
    [RelayCommand]
    private void ToggleFavorite(CloudGame? game)
    {
        if (game is null) return;

        game.IsFavorite = !game.IsFavorite;
        _logger.LogInformation("{Action} favorite for {GameTitle}",
            game.IsFavorite ? "Added" : "Removed", game.Title);

        OnPropertyChanged(nameof(FilteredGames));
    }

    /// <summary>
    /// Opens the stream settings dialog.
    /// </summary>
    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        _logger.LogInformation("Opening cloud gaming settings");

        // NOTE: This is a demo implementation. Replace with actual settings dialog.
    }

    /// <summary>
    /// Stops the active streaming session.
    /// </summary>
    [RelayCommand]
    private async Task StopStreamingAsync()
    {
        if (ActiveSession is null) return;

        _logger.LogInformation("Stopping stream session: {SessionId}", ActiveSession.Id);

        ActiveSession.IsActive = false;
        ActiveSession.Duration = _timeProvider.UtcNow - ActiveSession.StartedAt;

        // NOTE: This is a demo implementation. Replace with actual session termination.

        RecentSessions.Insert(0, ActiveSession);
        ActiveSession = null;
        IsStreaming = false;
    }

    /// <summary>
    /// Changes the game filter.
    /// </summary>
    [RelayCommand]
    private void SetFilter(CloudGameFilter filter)
    {
        CurrentFilter = filter;
        OnPropertyChanged(nameof(FilteredGames));
    }

    /// <summary>
    /// Changes the sort order.
    /// </summary>
    [RelayCommand]
    private void SetSort(CloudGameSort sort)
    {
        CurrentSort = sort;
        OnPropertyChanged(nameof(FilteredGames));
    }

    /// <summary>
    /// Resumes a recent session.
    /// </summary>
    [RelayCommand]
    private async Task ResumeSessionAsync(CloudSession? session)
    {
        if (session?.Game is null) return;

        await LaunchGameAsync(session.Game);
    }

    #endregion

    #region Helper Methods

    private IEnumerable<CloudGame> ApplyFiltersAndSort()
    {
        IEnumerable<CloudGame> query = Games;

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var search = SearchQuery.ToLowerInvariant();
            query = query.Where(g =>
                g.Title.ToLowerInvariant().Contains(search) ||
                g.Genres.Any(genre => genre.ToLowerInvariant().Contains(search)));
        }

        // Apply provider filter
        if (SelectedProvider.HasValue)
        {
            query = query.Where(g => g.Provider == SelectedProvider.Value);
        }

        // Apply category filter
        query = CurrentFilter switch
        {
            CloudGameFilter.Favorites => query.Where(g => g.IsFavorite),
            CloudGameFilter.RecentlyPlayed => query.Where(g => g.LastPlayed.HasValue),
            CloudGameFilter.Installed => query.Where(g => g.Status == CloudGameStatus.Available),
            _ => query
        };

        // Apply sorting
        query = CurrentSort switch
        {
            CloudGameSort.Name => query.OrderBy(g => g.Title),
            CloudGameSort.LastPlayed => query.OrderByDescending(g => g.LastPlayed ?? DateTime.MinValue),
            CloudGameSort.DateAdded => query.OrderByDescending(g => g.AddedToLibrary ?? DateTime.MinValue),
            CloudGameSort.Rating => query.OrderByDescending(g => g.UserRating ?? 0),
            CloudGameSort.PlayTime => query.OrderByDescending(g => g.TotalPlayTime),
            _ => query
        };

        return query.ToList();
    }

    private void InitializeMockData()
    {
        // Initialize providers
        Providers = new ObservableCollection<ProviderStatus>
        {
            new()
            {
                Provider = CloudProvider.GeForceNow,
                IsConnected = true,
                Username = "gamer123",
                SubscriptionTier = "Ultimate",
                GamesInLibrary = 89,
                HoursPlayedThisMonth = 23,
                HourLimit = 100,
                LastConnectionTest = new ConnectionTestResult
                {
                    Ping = 12,
                    RecommendedQuality = SessionQuality.Ultra,
                    CanStream4K = true
                }
            },
            new()
            {
                Provider = CloudProvider.XboxCloudGaming,
                IsConnected = true,
                Username = "XboxGamer",
                SubscriptionTier = "Game Pass Ultimate",
                GamesInLibrary = 156,
                HoursPlayedThisMonth = 45
            },
            new()
            {
                Provider = CloudProvider.AmazonLuna,
                IsConnected = true,
                Username = "PrimeUser",
                SubscriptionTier = "Luna+",
                GamesInLibrary = 67,
                HoursPlayedThisMonth = 12,
                HourLimit = 100
            }
        };

        // Initialize games
        Games = new ObservableCollection<CloudGame>
        {
            new()
            {
                Id = "gfn-1",
                Title = "Cyberpunk 2077",
                Provider = CloudProvider.GeForceNow,
                Status = CloudGameStatus.Available,
                Genres = new List<string> { "RPG", "Action", "Sci-Fi" },
                IsFavorite = true,
                LastPlayed = _timeProvider.UtcNow.AddHours(-3),
                TotalPlayTime = TimeSpan.FromHours(45),
                MetacriticScore = 86
            },
            new()
            {
                Id = "gfn-2",
                Title = "Elden Ring",
                Provider = CloudProvider.GeForceNow,
                Status = CloudGameStatus.Available,
                Genres = new List<string> { "RPG", "Action", "Souls-like" },
                IsFavorite = true,
                LastPlayed = _timeProvider.UtcNow.AddDays(-1),
                TotalPlayTime = TimeSpan.FromHours(120),
                MetacriticScore = 96
            },
            new()
            {
                Id = "xcloud-1",
                Title = "Halo Infinite",
                Provider = CloudProvider.XboxCloudGaming,
                Status = CloudGameStatus.Available,
                Genres = new List<string> { "Shooter", "FPS", "Sci-Fi" },
                IsFavorite = false,
                LastPlayed = _timeProvider.UtcNow.AddDays(-2),
                TotalPlayTime = TimeSpan.FromHours(25),
                MetacriticScore = 87
            },
            new()
            {
                Id = "xcloud-2",
                Title = "Starfield",
                Provider = CloudProvider.XboxCloudGaming,
                Status = CloudGameStatus.Available,
                Genres = new List<string> { "RPG", "Sci-Fi", "Exploration" },
                IsFavorite = true,
                LastPlayed = _timeProvider.UtcNow.AddDays(-5),
                TotalPlayTime = TimeSpan.FromHours(60),
                MetacriticScore = 83
            },
            new()
            {
                Id = "xcloud-3",
                Title = "Forza Horizon 5",
                Provider = CloudProvider.XboxCloudGaming,
                Status = CloudGameStatus.Available,
                Genres = new List<string> { "Racing", "Open World" },
                IsFavorite = false,
                LastPlayed = _timeProvider.UtcNow.AddDays(-7),
                TotalPlayTime = TimeSpan.FromHours(15),
                MetacriticScore = 92
            },
            new()
            {
                Id = "luna-1",
                Title = "Assassin's Creed Valhalla",
                Provider = CloudProvider.AmazonLuna,
                Status = CloudGameStatus.Available,
                Genres = new List<string> { "Action", "RPG", "Open World" },
                IsFavorite = true,
                LastPlayed = _timeProvider.UtcNow.AddDays(-3),
                TotalPlayTime = TimeSpan.FromHours(40),
                MetacriticScore = 84
            },
            new()
            {
                Id = "luna-2",
                Title = "FIFA 24",
                Provider = CloudProvider.AmazonLuna,
                Status = CloudGameStatus.Available,
                Genres = new List<string> { "Sports", "Soccer" },
                IsFavorite = false,
                LastPlayed = _timeProvider.UtcNow.AddDays(-10),
                TotalPlayTime = TimeSpan.FromHours(8),
                MetacriticScore = 78
            },
            new()
            {
                Id = "gfn-3",
                Title = "Call of Duty: MW3",
                Provider = CloudProvider.GeForceNow,
                Status = CloudGameStatus.Available,
                Genres = new List<string> { "Shooter", "FPS", "Action" },
                IsFavorite = false,
                LastPlayed = _timeProvider.UtcNow.AddDays(-14),
                TotalPlayTime = TimeSpan.FromHours(12),
                MetacriticScore = 65
            }
        };

        // Initialize recent sessions
        RecentSessions = new ObservableCollection<CloudSession>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Game = Games[0],
                Provider = CloudProvider.GeForceNow,
                StartedAt = _timeProvider.UtcNow.AddHours(-3),
                Duration = TimeSpan.FromHours(2),
                Quality = SessionQuality.Ultra,
                AverageLatency = 12.5f,
                FrameRate = 60,
                IsActive = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Game = Games[1],
                Provider = CloudProvider.GeForceNow,
                StartedAt = _timeProvider.UtcNow.AddDays(-1),
                Duration = TimeSpan.FromHours(3.5),
                Quality = SessionQuality.High,
                AverageLatency = 15.2f,
                FrameRate = 60,
                IsActive = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Game = Games[3],
                Provider = CloudProvider.XboxCloudGaming,
                StartedAt = _timeProvider.UtcNow.AddDays(-2),
                Duration = TimeSpan.FromHours(1.5),
                Quality = SessionQuality.High,
                AverageLatency = 18.0f,
                FrameRate = 60,
                IsActive = false
            }
        };
    }

    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredGames));
    }

    partial void OnSelectedProviderChanged(CloudProvider? value)
    {
        OnPropertyChanged(nameof(FilteredGames));
    }

    partial void OnCurrentFilterChanged(CloudGameFilter value)
    {
        OnPropertyChanged(nameof(FilteredGames));
    }

    partial void OnCurrentSortChanged(CloudGameSort value)
    {
        OnPropertyChanged(nameof(FilteredGames));
    }

    #endregion
}
