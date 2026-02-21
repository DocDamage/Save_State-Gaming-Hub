using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the review editor dialog.
/// </summary>
public partial class ReviewEditorDialogViewModel : ObservableObject
{
    // Validation constants
    private const int MaxReviewLength = 2000;
    private const int MinReviewLength = 20;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReviewTextValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _reviewText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private int _rating;

    [ObservableProperty]
    private bool _recommendToFriends = true;

    [ObservableProperty]
    private string _validationError = string.Empty;

    public int CharacterCount => ReviewText?.Length ?? 0;

    /// <summary>
    /// Gets whether the review text is valid.
    /// </summary>
    public bool IsReviewTextValid => 
        !string.IsNullOrWhiteSpace(ReviewText) && 
        ReviewText.Length >= MinReviewLength && 
        ReviewText.Length <= MaxReviewLength &&
        !InvalidCharsPattern.IsMatch(ReviewText);

    /// <summary>
    /// Gets whether there are any validation errors.
    /// </summary>
    public bool HasValidationErrors => !IsReviewTextValid || Rating <= 0;

    /// <summary>
    /// Gets whether the save button should be enabled.
    /// </summary>
    public bool CanSave => Rating > 0 && IsReviewTextValid;

    public string Star1 => Rating >= 1 ? "⭐" : "☆";
    public string Star2 => Rating >= 2 ? "⭐" : "☆";
    public string Star3 => Rating >= 3 ? "⭐" : "☆";
    public string Star4 => Rating >= 4 ? "⭐" : "☆";
    public string Star5 => Rating >= 5 ? "⭐" : "☆";

    public string RatingText => Rating switch
    {
        0 => "No rating",
        1 => "Poor",
        2 => "Fair",
        3 => "Good",
        4 => "Very Good",
        5 => "Excellent",
        _ => string.Empty
    };

    public ReviewEditorDialogViewModel(string? existingReview = null, int? existingRating = null)
    {
        if (!string.IsNullOrEmpty(existingReview))
        {
            ReviewText = existingReview;
        }

        if (existingRating.HasValue && existingRating.Value >= 1 && existingRating.Value <= 5)
        {
            Rating = existingRating.Value;
        }
    }

    partial void OnReviewTextChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxReviewLength)
        {
            ReviewText = value[..MaxReviewLength];
            return;
        }

        UpdateValidationError();
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnRatingChanged(int value)
    {
        UpdateValidationError();
        OnPropertyChanged(nameof(Star1));
        OnPropertyChanged(nameof(Star2));
        OnPropertyChanged(nameof(Star3));
        OnPropertyChanged(nameof(Star4));
        OnPropertyChanged(nameof(Star5));
        OnPropertyChanged(nameof(RatingText));
        OnPropertyChanged(nameof(CanSave));
    }

    private void UpdateValidationError()
    {
        if (Rating <= 0)
        {
            ValidationError = "Please select a rating.";
        }
        else if (!IsReviewTextValid)
        {
            if (string.IsNullOrWhiteSpace(ReviewText))
                ValidationError = "Review text is required.";
            else if (ReviewText.Length < MinReviewLength)
                ValidationError = $"Review must be at least {MinReviewLength} characters.";
            else if (ReviewText.Length > MaxReviewLength)
                ValidationError = $"Review must not exceed {MaxReviewLength} characters.";
            else
                ValidationError = "Review contains invalid characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }
    }

    [RelayCommand]
    private void SetRating(string ratingStr)
    {
        if (int.TryParse(ratingStr, out int rating))
        {
            Rating = rating;
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        var result = new ReviewEditorResult(
            ReviewText: ReviewText.Trim(),
            Rating: Rating,
            RecommendToFriends: RecommendToFriends);

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private void CloseDialog(ReviewEditorResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}
