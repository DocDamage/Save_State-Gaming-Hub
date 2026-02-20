// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Subscriptions;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Presentation.ViewModels.Subscriptions;

/// <summary>
/// ViewModel for managing game subscription services.
/// </summary>
public sealed partial class SubscriptionManagerViewModel : ObservableObject
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionManagerViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SubscriptionServiceViewModel> _services = new();

    [ObservableProperty]
    private ObservableCollection<SubscriptionAlertViewModel> _alerts = new();

    [ObservableProperty]
    private ObservableCollection<TrackedGameViewModel> _trackedGames = new();

    [ObservableProperty]
    private SubscriptionServiceViewModel? _selectedService;

    [ObservableProperty]
    private SubscriptionComparisonViewModel? _comparison;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _newGameTitle = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SubscriptionGameViewModel> _searchResults = new();

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _selectedServiceFilter = "All";

    public ObservableCollection<string> ServiceFilters { get; } = new()
    {
        "All",
        "Xbox Game Pass",
        "PlayStation Plus",
        "EA Play",
        "Ubisoft+",
        "GeForce NOW"
    };

    public SubscriptionManagerViewModel(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionManagerViewModel> logger)
    {
        _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Load data on initialization
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task LoadServicesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _subscriptionService.GetAvailableServicesAsync();
            
            if (result.IsSuccess && result.Value != null)
            {
                Services.Clear();
                foreach (var service in result.Value)
                {
                    Services.Add(new SubscriptionServiceViewModel(service));
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load services";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load subscription services");
            ErrorMessage = "An error occurred while loading services";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadAlertsAsync()
    {
        try
        {
            var result = await _subscriptionService.GetLeavingSoonAlertsAsync();
            
            if (result.IsSuccess && result.Value != null)
            {
                Alerts.Clear();
                foreach (var alert in result.Value)
                {
                    Alerts.Add(new SubscriptionAlertViewModel(alert));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load alerts");
        }
    }

    [RelayCommand]
    private async Task CompareServicesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _subscriptionService.CompareSubscriptionsAsync();
            
            if (result.IsSuccess && result.Value != null)
            {
                Comparison = new SubscriptionComparisonViewModel(result.Value);
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to compare services";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare subscriptions");
            ErrorMessage = "An error occurred while comparing services";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanTrackGame))]
    private async Task TrackGameAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGameTitle))
            return;

        IsLoading = true;
        
        try
        {
            var userId = Guid.NewGuid(); // Placeholder - use actual user ID
            
            var result = await _subscriptionService.TrackGameAsync(userId, NewGameTitle);
            
            if (result.IsSuccess)
            {
                NewGameTitle = string.Empty;
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to track game";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track game {GameTitle}", NewGameTitle);
            ErrorMessage = "An error occurred while tracking the game";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanTrackGame() => !string.IsNullOrWhiteSpace(NewGameTitle);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await Task.WhenAll(
            LoadServicesAsync(),
            LoadAlertsAsync(),
            CompareServicesAsync());
    }

    partial void OnNewGameTitleChanged(string value)
    {
        TrackGameCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task SearchGamesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            return;
        }

        IsSearching = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _subscriptionService.SearchGamesAsync(SearchQuery);

            if (result.IsSuccess && result.Value != null)
            {
                SearchResults.Clear();
                foreach (var game in result.Value)
                {
                    SearchResults.Add(new SubscriptionGameViewModel(game));
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "Search failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search games");
            ErrorMessage = "An error occurred during search";
        }
        finally
        {
            IsSearching = false;
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _ = SearchGamesAsync();
        }
    }
}

/// <summary>
/// ViewModel for a subscription service.
/// </summary>
public sealed class SubscriptionServiceViewModel : ObservableObject
{
    private readonly SubscriptionServiceInfo _service;

    public SubscriptionServiceViewModel(SubscriptionServiceInfo service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public string Id => _service.Id;
    public string Name => _service.Name;
    public string Description => _service.Description;
    public SubscriptionType Type => _service.SubscriptionType;
    public decimal MonthlyPrice => _service.MonthlyPrice;
    public int GameCount => _service.GameCount;
    public bool SupportsCloudGaming => _service.SupportsCloudGaming;
    public bool SupportsEaPlay => _service.SupportsEaPlay;

    public string FormattedPrice => $"${_service.MonthlyPrice:F2}/month";
    public string GameCountText => $"{_service.GameCount:N0} games";
    public string FeaturesText => GetFeaturesText();

    private string GetFeaturesText()
    {
        var features = new List<string>();
        if (_service.SupportsCloudGaming)
            features.Add("Cloud Gaming");
        if (_service.SupportsEaPlay)
            features.Add("EA Play Included");
        return features.Count > 0 ? string.Join(", ", features) : "Standard";
    }
}

/// <summary>
/// ViewModel for a subscription alert.
/// </summary>
public sealed class SubscriptionAlertViewModel : ObservableObject
{
    private readonly SubscriptionAlert _alert;

    public SubscriptionAlertViewModel(SubscriptionAlert alert)
    {
        _alert = alert ?? throw new ArgumentNullException(nameof(alert));
    }

    public string ServiceName => _alert.ServiceName;
    public string GameTitle => _alert.GameTitle;
    public DateTime LeavingDate => _alert.LeavingDate;
    public AlertType Type => _alert.Type;
    public string? Message => _alert.Message;

    public string DaysRemainingText
    {
        get
        {
            var days = (_alert.LeavingDate - SystemTimeProvider.Instance.UtcNow).Days;
            return days switch
            {
                0 => "Leaving today!",
                1 => "Leaving tomorrow!",
                <= 7 => $"Leaving in {days} days",
                _ => $"Leaving on {_alert.LeavingDate:MMM dd}"
            };
        }
    }

    public bool IsUrgent => (_alert.LeavingDate - SystemTimeProvider.Instance.UtcNow).Days <= 3;
}

/// <summary>
/// ViewModel for a tracked game.
/// </summary>
public sealed class TrackedGameViewModel : ObservableObject
{
    private readonly GameSubscription _game;

    public TrackedGameViewModel(GameSubscription game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
    }

    public string GameTitle => _game.GameTitle;
    public string? ServiceName => _game.ServiceName;
    public DateTime? DateAdded => _game.DateAdded;
    public DateTime? DateLeaving => _game.DateLeaving;

    public string StatusText => _game.DateLeaving.HasValue 
        ? $"Available until {_game.DateLeaving.Value:MMM dd}" 
        : "Available now";
}

/// <summary>
/// ViewModel for subscription comparison.
/// </summary>
public sealed class SubscriptionComparisonViewModel : ObservableObject
{
    private readonly SubscriptionComparison _comparison;

    public SubscriptionComparisonViewModel(SubscriptionComparison comparison)
    {
        _comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
    }

    public decimal TotalMonthlyCost => _comparison.TotalMonthlyCost;
    public int TotalUniqueGames => _comparison.TotalUniqueGames;
    public string BestValueRecommendation => _comparison.BestValueRecommendation;

    public string FormattedTotalCost => $"${_comparison.TotalMonthlyCost:F2}/month";
    public string FormattedGameCount => $"{_comparison.TotalUniqueGames:N0} games";
}

/// <summary>
/// ViewModel for a subscription game.
/// </summary>
public sealed class SubscriptionGameViewModel : ObservableObject
{
    private readonly SubscriptionGame _game;

    public SubscriptionGameViewModel(SubscriptionGame game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
    }

    public string GameId => _game.GameId;
    public string Title => _game.Title;
    public string? Description => _game.Description;
    public string? CoverImageUrl => _game.CoverImageUrl;
    public DateTime? AddedDate => _game.AddedDate;
    public DateTime? LeavingSoonDate => _game.LeavingSoonDate;
    public bool IsLeavingSoon => _game.IsLeavingSoon;
    public bool IsNewArrival => _game.IsNewArrival;
    public List<string> Genres => _game.Genres;
    public int? MetacriticScore => _game.MetacriticScore;

    public string AvailableOnText => string.Join(", ", _game.AvailableOn.Select(t => t.ToString()));
    public string GenresText => string.Join(", ", _game.Genres);
    public string StatusText => GetStatusText();

    private string GetStatusText()
    {
        if (_game.IsLeavingSoon)
            return $"⚠️ Leaving in {(_game.LeavingSoonDate!.Value - DateTime.UtcNow).Days} days";
        if (_game.IsNewArrival)
            return "✨ New arrival";
        return "✓ Available";
    }
}
