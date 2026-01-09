using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for rating a game.
/// </summary>
public partial class GameRatingDialogViewModel : ObservableObject
{
    private readonly ILogger<GameRatingDialogViewModel> _logger;

    [ObservableProperty]
    private Guid _gameId;

    [ObservableProperty]
    private double _rating = 0.0;

    [ObservableProperty]
    private string _reviewText = string.Empty;

    [ObservableProperty]
    private bool _hasRating;

    [ObservableProperty]
    private string _ratingText = "No Rating";

    [ObservableProperty]
    private string _ratingEmoji = "⭐";

    [ObservableProperty]
    private bool _canSave;

    public GameRatingDialogViewModel(ILogger<GameRatingDialogViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the dialog with current rating if available.
    /// </summary>
    public void Initialize(Guid gameId, double? currentRating = null)
    {
        GameId = gameId;

        if (currentRating.HasValue)
        {
            Rating = currentRating.Value;
            HasRating = true;
        }
        else
        {
            Rating = 0.0;
            HasRating = false;
        }

        UpdateRatingDisplay();
        UpdateCanSave();
    }

    partial void OnRatingChanged(double value)
    {
        HasRating = value > 0;
        UpdateRatingDisplay();
        UpdateCanSave();
    }

    partial void OnReviewTextChanged(string value)
    {
        UpdateCanSave();
    }

    private void UpdateRatingDisplay()
    {
        if (Rating <= 0)
        {
            RatingText = "No Rating";
            RatingEmoji = "⭐";
        }
        else if (Rating <= 2.0)
        {
            RatingText = $"{Rating:F1}/5.0 - Poor";
            RatingEmoji = "😞";
        }
        else if (Rating <= 3.5)
        {
            RatingText = $"{Rating:F1}/5.0 - Fair";
            RatingEmoji = "😐";
        }
        else if (Rating <= 4.5)
        {
            RatingText = $"{Rating:F1}/5.0 - Good";
            RatingEmoji = "😊";
        }
        else
        {
            RatingText = $"{Rating:F1}/5.0 - Excellent";
            RatingEmoji = "⭐";
        }
    }

    private void UpdateCanSave()
    {
        CanSave = Rating > 0 || !string.IsNullOrWhiteSpace(ReviewText);
    }

    [RelayCommand]
    private void SetRating(double rating)
    {
        Rating = Math.Clamp(rating, 0, 5);
    }

    [RelayCommand]
    private void SetStarRating(int stars)
    {
        Rating = Math.Clamp(stars, 0, 5);
    }

    [RelayCommand]
    private void ClearRating()
    {
        Rating = 0.0;
        ReviewText = string.Empty;
        HasRating = false;
        UpdateRatingDisplay();

        _logger.LogInformation("Rating cleared for game {GameId}", GameId);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        _logger.LogInformation(
            "Saving rating {Rating}/5.0 for game {GameId}",
            Rating,
            GameId);
    }

    [RelayCommand]
    private void Cancel()
    {
        _logger.LogDebug("Game rating cancelled");
    }
}
