using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the review editor dialog.
/// </summary>
public partial class ReviewEditorDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _reviewText = string.Empty;

    [ObservableProperty]
    private int _rating;

    [ObservableProperty]
    private bool _recommendToFriends = true;

    public int CharacterCount => ReviewText?.Length ?? 0;
    public bool CanSave => Rating > 0 && !string.IsNullOrWhiteSpace(ReviewText) && ReviewText.Length >= 20;

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
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnRatingChanged(int value)
    {
        OnPropertyChanged(nameof(Star1));
        OnPropertyChanged(nameof(Star2));
        OnPropertyChanged(nameof(Star3));
        OnPropertyChanged(nameof(Star4));
        OnPropertyChanged(nameof(Star5));
        OnPropertyChanged(nameof(RatingText));
        OnPropertyChanged(nameof(CanSave));
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
            ReviewText: ReviewText,
            Rating: Rating,
            RecommendToFriends: RecommendToFriends);

        // Close dialog with result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        // Close dialog without result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(null);
        }
    }
}
