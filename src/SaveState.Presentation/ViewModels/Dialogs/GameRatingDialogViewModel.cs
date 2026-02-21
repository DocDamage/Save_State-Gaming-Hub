using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for rating a game.
/// </summary>
public partial class GameRatingDialogViewModel : ObservableObject
{
    private readonly ILogger<GameRatingDialogViewModel> _logger;
    private Action<GameRatingResult?>? _closeAction;

    // Validation constants
    private const int MaxReviewLength = 2000;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    private Guid _gameId;

    [ObservableProperty]
    private double _rating = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReviewTextValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _reviewText = string.Empty;

    [ObservableProperty]
    private bool _hasRating;

    [ObservableProperty]
    private string _ratingText = "No Rating";

    [ObservableProperty]
    private string _ratingEmoji = "⭐";

    [ObservableProperty]
    private string _validationError = string.Empty;

    public GameRatingDialogViewModel(ILogger<GameRatingDialogViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sets the action to invoke when the dialog is closed.
    /// </summary>
    public void SetCloseAction(Action<GameRatingResult?> closeAction)
    {
        _closeAction = closeAction;
    }

    /// <summary>
    /// Gets whether the review text is valid.
    /// </summary>
    public bool IsReviewTextValid => 
        string.IsNullOrEmpty(ReviewText) || 
        (ReviewText.Length <= MaxReviewLength && !InvalidCharsPattern.IsMatch(ReviewText));

    /// <summary>
    /// Gets whether the save button should be enabled.
    /// </summary>
    public bool CanSave => HasRating && IsReviewTextValid;

    /// <summary>
    /// Initializes the dialog with current rating if available.
    /// </summary>
    public void Initialize(Guid gameId, double? currentRating = null)
    {
        GameId = gameId;

        if (currentRating.HasValue)
        {
            Rating = Math.Clamp(currentRating.Value, 0, 5);
            HasRating = Rating > 0;
        }
        else
        {
            Rating = 0.0;
            HasRating = false;
        }

        UpdateRatingDisplay();
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnRatingChanged(double value)
    {
        HasRating = value > 0;
        UpdateRatingDisplay();
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnReviewTextChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxReviewLength)
        {
            ReviewText = value[..MaxReviewLength];
            return;
        }

        // Update validation error
        if (!IsReviewTextValid)
        {
            ValidationError = $"Review must not exceed {MaxReviewLength} characters or contain invalid characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }

        OnPropertyChanged(nameof(CanSave));
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

    private void CloseDialog(GameRatingResult? result)
    {
        if (_closeAction != null)
        {
            _closeAction(result);
        }
        else
        {
            // Fallback to direct window close
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
            if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
                window?.Close(result);
            }
        }
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

        var result = new GameRatingResult(
            GameId,
            Rating,
            ReviewText.Trim());

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        _logger.LogDebug("Game rating cancelled");
        CloseDialog(null);
    }
}

/// <summary>
/// Result from the game rating dialog.
/// </summary>
public record GameRatingResult(Guid GameId, double Rating, string ReviewText);
