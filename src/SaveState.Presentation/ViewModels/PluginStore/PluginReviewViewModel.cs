using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.PluginStore;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.PluginStore;

/// <summary>
/// ViewModel for the Plugin Reviews section.
/// </summary>
public partial class PluginReviewViewModel : ObservableObject
{
    private readonly ILogger<PluginReviewViewModel> _logger;
    private readonly IDialogService _dialogService;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private ObservableCollection<PluginReview> _reviews = new();

    [ObservableProperty]
    private ObservableCollection<PluginReview> _filteredReviews = new();

    [ObservableProperty]
    private double _averageRating;

    [ObservableProperty]
    private int _totalReviews;

    [ObservableProperty]
    private int _selectedSortIndex;

    [ObservableProperty]
    private int _selectedFilterIndex;

    [ObservableProperty]
    private bool _hasMoreReviews;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private string? _pluginId;

    public PluginReviewViewModel(
        ILogger<PluginReviewViewModel> logger,
        IDialogService dialogService,
        ITimeProvider? timeProvider = null)
    {
        _logger = logger;
        _dialogService = dialogService;
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;

        // Initialize with sample data for demonstration
        InitializeSampleData();
    }

    /// <summary>
    /// Gets the overall star rating display.
    /// </summary>
    public string OverallStars => new string('★', (int)AverageRating) + new string('☆', 5 - (int)AverageRating);

    /// <summary>
    /// Gets the rating distribution for the rating bars.
    /// </summary>
    public List<RatingDistributionItem> RatingDistribution => CalculateRatingDistribution();

    /// <summary>
    /// Loads reviews for a specific plugin.
    /// </summary>
    public async Task LoadReviewsAsync(string pluginId)
    {
        PluginId = pluginId;

        try
        {
            // In a real implementation, this would load from a service
            await Task.CompletedTask;
            ApplyFiltersAndSort();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load reviews for plugin {PluginId}", pluginId);
        }
    }

    /// <summary>
    /// Writes a new review.
    /// </summary>
    [RelayCommand]
    private async Task WriteReviewAsync()
    {
        if (string.IsNullOrEmpty(PluginId))
        {
            _logger.LogWarning("Cannot write review: PluginId is not set");
            return;
        }

        // Show write review dialog using the review editor
        var result = await _dialogService.ShowReviewEditorAsync();

        // Reload reviews after dialog closes
        if (result != null)
        {
            await LoadReviewsAsync(PluginId);
        }
    }

    /// <summary>
    /// Marks a review as helpful.
    /// </summary>
    [RelayCommand]
    private async Task MarkHelpfulAsync(PluginReview? review)
    {
        if (review == null) return;

        try
        {
            review.HelpfulCount++;
            // In a real implementation, this would call a service
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark review as helpful");
        }
    }

    /// <summary>
    /// Reports a review.
    /// </summary>
    [RelayCommand]
    private async Task ReportReviewAsync(PluginReview? review)
    {
        if (review == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Report Review",
            "Are you sure you want to report this review?",
            confirmText: "Report",
            cancelText: "Cancel");

        if (confirmed)
        {
            _logger.LogInformation("Review {ReviewId} reported", review.Id);
        }
    }

    /// <summary>
    /// Loads more reviews.
    /// </summary>
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        CurrentPage++;
        // In a real implementation, this would load the next page from a service
        await Task.CompletedTask;
    }

    partial void OnSelectedSortIndexChanged(int value)
    {
        ApplyFiltersAndSort();
    }

    partial void OnSelectedFilterIndexChanged(int value)
    {
        ApplyFiltersAndSort();
    }

    private void ApplyFiltersAndSort()
    {
        var filtered = Reviews.AsEnumerable();

        // Apply star filter
        filtered = SelectedFilterIndex switch
        {
            1 => filtered.Where(r => r.Rating == 5),
            2 => filtered.Where(r => r.Rating == 4),
            3 => filtered.Where(r => r.Rating == 3),
            4 => filtered.Where(r => r.Rating == 2),
            5 => filtered.Where(r => r.Rating == 1),
            _ => filtered
        };

        // Apply sorting
        filtered = SelectedSortIndex switch
        {
            0 => filtered.OrderByDescending(r => r.HelpfulCount),
            1 => filtered.OrderByDescending(r => r.CreatedAt),
            2 => filtered.OrderByDescending(r => r.Rating),
            3 => filtered.OrderBy(r => r.Rating),
            _ => filtered
        };

        FilteredReviews = new ObservableCollection<PluginReview>(filtered);
    }

    private List<RatingDistributionItem> CalculateRatingDistribution()
    {
        var distribution = new List<RatingDistributionItem>();

        for (int stars = 5; stars >= 1; stars--)
        {
            var count = Reviews.Count(r => r.Rating == stars);
            var percentage = TotalReviews > 0 ? (count * 100.0) / TotalReviews : 0;

            distribution.Add(new RatingDistributionItem
            {
                Stars = stars,
                Count = count,
                Percentage = percentage
            });
        }

        return distribution;
    }

    private void InitializeSampleData()
    {
        Reviews = new ObservableCollection<PluginReview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Author = "GamerOne",
                AuthorAvatar = "👤",
                Rating = 5,
                Title = "Absolutely amazing plugin!",
                Content = "This plugin has completely transformed how I manage my game library. The integration is seamless and the features are exactly what I needed.",
                CreatedAt = _timeProvider.UtcNow.AddDays(-5),
                HelpfulCount = 24
            },
            new()
            {
                Id = Guid.NewGuid(),
                Author = "RetroFan99",
                AuthorAvatar = "🎮",
                Rating = 4,
                Title = "Great functionality, minor issues",
                Content = "Works really well for the most part. I've noticed a few small bugs when syncing with cloud storage, but overall it's solid.",
                CreatedAt = _timeProvider.UtcNow.AddDays(-12),
                HelpfulCount = 15
            },
            new()
            {
                Id = Guid.NewGuid(),
                Author = "DevTeam",
                Rating = 5,
                Title = "Developer Response",
                Content = "Thank you for the feedback! We've addressed the cloud sync issues in version 2.1.0.",
                CreatedAt = _timeProvider.UtcNow.AddDays(-11),
                HelpfulCount = 8,
                IsDeveloperResponse = true,
                DeveloperResponse = "Developer Response"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Author = "SpeedRunner",
                Rating = 5,
                Title = "Perfect for speedrunners",
                Content = "The save state management features are incredible. Being able to branch and organize attempts has improved my PB tracking significantly.",
                CreatedAt = _timeProvider.UtcNow.AddDays(-20),
                HelpfulCount = 42
            },
            new()
            {
                Id = Guid.NewGuid(),
                Author = "CasualPlayer",
                Rating = 3,
                Title = "Good but complex",
                Content = "There are lots of features, but the learning curve is steep. Would appreciate more tutorials or a simpler mode.",
                CreatedAt = _timeProvider.UtcNow.AddDays(-30),
                HelpfulCount = 7
            }
        };

        TotalReviews = Reviews.Count;
        AverageRating = Reviews.Count > 0 ? Reviews.Average(r => r.Rating) : 0;

        ApplyFiltersAndSort();
    }
}

/// <summary>
/// Represents a rating distribution item for the star rating bars.
/// </summary>
public class RatingDistributionItem
{
    /// <summary>Star rating (1-5).</summary>
    public int Stars { get; set; }

    /// <summary>Number of reviews with this rating.</summary>
    public int Count { get; set; }

    /// <summary>Percentage of total reviews.</summary>
    public double Percentage { get; set; }
}
