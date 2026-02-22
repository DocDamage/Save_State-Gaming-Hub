using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Analytics.Models.GamerProfile;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Models.Recommendations;
using SaveState.Core.GameLibrary.Services;
using TimeOfDay = SaveState.Core.GameLibrary.Models.Recommendations.TimeOfDay;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.GameLibrary;

/// <summary>
/// ViewModel for the Smart Recommendations 2.0 view with hybrid recommendation engine.
/// </summary>
public partial class SmartRecommendationsViewModel : ObservableObject
{
    private readonly IRecommendationEngineV2 _recommendationEngine;
    private readonly ITimeProvider _timeProvider;
    private readonly ISessionTrackingService _sessionTrackingService;
    private readonly IGamerDnaService _gamerDnaService;
    private readonly IUserPreferencesService _userPreferencesService;

    /// <summary>
    /// Collection of personalized recommendations.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GameRecommendation> _recommendations = new();

    /// <summary>
    /// Collection of "Play Next" recommendations.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GameRecommendation> _playNext = new();

    /// <summary>
    /// Collection of trending games.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GameRecommendation> _trending = new();

    /// <summary>
    /// Collection of hidden gem recommendations.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GameRecommendation> _hiddenGems = new();

    /// <summary>
    /// The currently selected mood for recommendations.
    /// </summary>
    [ObservableProperty]
    private Mood? _selectedMood;

    /// <summary>
    /// Available time for gaming session.
    /// </summary>
    [ObservableProperty]
    private TimeSpan _availableTime = TimeSpan.FromHours(1);

    /// <summary>
    /// Number of players (1 for solo, 2+ for multiplayer).
    /// </summary>
    [ObservableProperty]
    private int _playerCount = 1;

    /// <summary>
    /// Whether recommendations are currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Currently selected recommendation reason filter.
    /// </summary>
    [ObservableProperty]
    private string? _selectedRecommendationReason;

    /// <summary>
    /// Error message if loading fails.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets the list of available moods.
    /// </summary>
    public List<Mood> AvailableMoods { get; } = Enum.GetValues<Mood>().ToList();

    /// <summary>
    /// Gets the list of reason filters.
    /// </summary>
    public List<string> ReasonFilters { get; } = new() { "All", "Genre", "Time", "Mood", "Trending", "Hidden Gems" };

    /// <summary>
    /// Gets the minimum available time for the slider (15 minutes).
    /// </summary>
    public TimeSpan MinAvailableTime => TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets the maximum available time for the slider (8 hours).
    /// </summary>
    public TimeSpan MaxAvailableTime => TimeSpan.FromHours(8);

    /// <summary>
    /// Gets the filtered recommendations based on selected filter.
    /// </summary>
    public ObservableCollection<GameRecommendation> FilteredRecommendations =>
        string.IsNullOrEmpty(SelectedRecommendationReason) || SelectedRecommendationReason == "All"
            ? Recommendations
            : new ObservableCollection<GameRecommendation>(
                Recommendations.Where(r => MatchesFilter(r, SelectedRecommendationReason)).ToList());

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartRecommendationsViewModel"/> class.
    /// </summary>
    public SmartRecommendationsViewModel(
        IRecommendationEngineV2 recommendationEngine,
        ITimeProvider timeProvider,
        ISessionTrackingService sessionTrackingService,
        IGamerDnaService gamerDnaService,
        IUserPreferencesService userPreferencesService)
    {
        _recommendationEngine = recommendationEngine;
        _timeProvider = timeProvider;
        _sessionTrackingService = sessionTrackingService;
        _gamerDnaService = gamerDnaService;
        _userPreferencesService = userPreferencesService;
    }

    /// <summary>
    /// Loads personalized recommendations based on current context.
    /// </summary>
    [RelayCommand]
    private async Task LoadRecommendationsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Get recently played games from session service
            var recentlyPlayed = await GetRecentlyPlayedAsync();

            // Get preferred genres from user DNA/profile
            var preferredGenres = await GetPreferredGenresAsync();

            var context = new RecommendationContext
            {
                TimeOfDay = GetCurrentTimeOfDay(),
                DayOfWeek = _timeProvider.Now.DayOfWeek,
                AvailableTime = AvailableTime,
                CurrentMood = SelectedMood,
                RecentlyPlayed = recentlyPlayed,
                PreferredGenres = preferredGenres,
                PreferredPlatforms = new List<string>(),
                PlayerCount = PlayerCount
            };

            var result = await _recommendationEngine.GetRecommendationsAsync(context, 12);
            if (result.IsSuccess)
            {
                Recommendations = new ObservableCollection<GameRecommendation>(result.Value);
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load recommendations";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading recommendations: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads Play Next recommendations.
    /// </summary>
    [RelayCommand]
    private async Task LoadPlayNextAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Get recently completed games
            var justFinished = await GetJustFinishedAsync();

            var context = new PlayNextContext
            {
                AvailableTime = AvailableTime,
                CurrentMood = SelectedMood,
                JustFinished = justFinished,
                CurrentTime = _timeProvider.Now
            };

            var result = await _recommendationEngine.GetPlayNextAsync(context, 5);
            if (result.IsSuccess)
            {
                PlayNext = new ObservableCollection<GameRecommendation>(result.Value);
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load Play Next recommendations";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading Play Next: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads trending games.
    /// </summary>
    [RelayCommand]
    private async Task LoadTrendingAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _recommendationEngine.GetTrendingAsync();
            if (result.IsSuccess)
            {
                Trending = new ObservableCollection<GameRecommendation>(result.Value);
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load trending games";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading trending: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads hidden gems recommendations.
    /// </summary>
    [RelayCommand]
    private async Task LoadHiddenGemsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _recommendationEngine.GetHiddenGemsAsync(10);
            if (result.IsSuccess)
            {
                HiddenGems = new ObservableCollection<GameRecommendation>(result.Value);
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load hidden gems";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading hidden gems: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Filters recommendations by reason.
    /// </summary>
    [RelayCommand]
    private void FilterByReason(string? reason)
    {
        SelectedRecommendationReason = reason;
        // The FilteredRecommendations property automatically applies the filter
        OnPropertyChanged(nameof(FilteredRecommendations));
    }

    /// <summary>
    /// Refreshes all recommendation sections.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        await Task.WhenAll(
            LoadRecommendationsAsync(),
            LoadPlayNextAsync(),
            LoadTrendingAsync(),
            LoadHiddenGemsAsync()
        );
    }

    /// <summary>
    /// Clears the selected mood filter.
    /// </summary>
    [RelayCommand]
    private void ClearMood()
    {
        SelectedMood = null;
    }

    /// <summary>
    /// Sets the player count to solo (1).
    /// </summary>
    [RelayCommand]
    private void SetSoloPlay()
    {
        PlayerCount = 1;
    }

    /// <summary>
    /// Sets the player count to multiplayer (2).
    /// </summary>
    [RelayCommand]
    private void SetMultiplayer()
    {
        PlayerCount = 2;
    }

    /// <summary>
    /// Gets recently played game IDs from session tracking service.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> GetRecentlyPlayedAsync()
    {
        try
        {
            // Get all active sessions and recent sessions
            var activeSessionsResult = await _sessionTrackingService.GetAllActiveSessionsAsync();
            if (activeSessionsResult.IsSuccess && activeSessionsResult.Value.Count > 0)
            {
                return activeSessionsResult.Value.Select(s => s.GameId).ToList();
            }

            // If no active sessions, return empty list
            return new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    /// <summary>
    /// Gets preferred genres from gamer DNA service.
    /// </summary>
    private async Task<IReadOnlyList<string>> GetPreferredGenresAsync()
    {
        try
        {
            // Get current user ID from preferences service
            // For now, we'll use a default approach since IUserPreferencesService
            // doesn't expose a GetCurrentUserId method directly
            // In a real implementation, this would come from IUserContextService

            // Try to get gamer DNA profile for the current user
            // Note: This would typically require the current user ID
            // For now, return an empty list as fallback
            return new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets recently completed game IDs.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> GetJustFinishedAsync()
    {
        try
        {
            // Get session history and find recently completed sessions
            // For now, return empty list as placeholder
            // In a real implementation, this would query the game completion tracking
            return new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    /// <summary>
    /// Checks if a recommendation matches the selected filter.
    /// </summary>
    private static bool MatchesFilter(GameRecommendation recommendation, string filter)
    {
        return filter switch
        {
            "Genre" => recommendation.Reason == RecommendationReason.GenrePreference,
            "Time" => recommendation.Reason == RecommendationReason.TimeAppropriate,
            "Mood" => recommendation.Reason == RecommendationReason.MoodMatch,
            "Trending" => recommendation.Reason == RecommendationReason.Trending,
            "Hidden Gems" => recommendation.Reason == RecommendationReason.HiddenGem,
            _ => true
        };
    }

    private TimeOfDay GetCurrentTimeOfDay()
    {
        var hour = _timeProvider.Now.Hour;
        return hour switch
        {
            >= 6 and < 12 => TimeOfDay.Morning,
            >= 12 and < 17 => TimeOfDay.Afternoon,
            >= 17 and < 22 => TimeOfDay.Evening,
            _ => TimeOfDay.Night
        };
    }
}
