using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.ValueObjects;
using SaveState.Presentation.Services;
using System;

namespace SaveState.Presentation.ViewModels.Library;

/// <summary>
/// View model for individual game cards in the library.
/// </summary>
public partial class GameCardViewModel : ObservableObject
{
    private readonly ILogger<GameCardViewModel> _logger;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private GameId _gameId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _coverArtUrl;

    [ObservableProperty]
    private string _platformName = string.Empty;

    [ObservableProperty]
    private string _platformIcon = "🎮";

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _statusIcon = string.Empty;

    [ObservableProperty]
    private string _statusColor = "#888888";

    [ObservableProperty]
    private string _playtimeText = "0h";

    [ObservableProperty]
    private string _ratingStars = "☆☆☆☆☆";

    [ObservableProperty]
    private string _releaseYearText = "----";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isSelectionMode;

    public GameCardViewModel(
        ILogger<GameCardViewModel> logger,
        INavigationService navigationService)
    {
        _logger = logger;
        _navigationService = navigationService;
    }

    public GameCardViewModel(
        GameId gameId,
        string title,
        string? coverArtUrl,
        string platformName,
        string platformIcon,
        string statusText,
        string statusIcon,
        string statusColor,
        TimeSpan playtime,
        double rating,
        DateOnly? releaseDate,
        ILogger<GameCardViewModel> logger,
        INavigationService navigationService)
    {
        _logger = logger;
        _navigationService = navigationService;
        GameId = gameId;
        Title = title;
        CoverArtUrl = coverArtUrl;
        PlatformName = platformName;
        PlatformIcon = platformIcon;
        StatusText = statusText;
        StatusIcon = statusIcon;
        StatusColor = statusColor;
        PlaytimeText = FormatPlaytime(playtime);
        RatingStars = FormatRating(rating);
        ReleaseYearText = FormatReleaseYear(releaseDate);
    }

    [RelayCommand]
    private async Task OpenGame()
    {
        // Navigate to game detail view
        await _navigationService.NavigateTo("Library", GameId);
        _logger.LogInformation("Navigating to game detail: {Title} ({GameId})", Title, GameId);
    }

    [RelayCommand]
    private void ToggleSelection()
    {
        if (IsSelectionMode)
        {
            IsSelected = !IsSelected;
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

    private static string FormatRating(double rating)
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

    private static string FormatReleaseYear(DateOnly? releaseDate)
    {
        return releaseDate?.Year.ToString() ?? "----";
    }
}
