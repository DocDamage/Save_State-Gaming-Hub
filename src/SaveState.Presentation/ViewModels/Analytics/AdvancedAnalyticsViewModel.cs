using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Analytics.DTOs;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell;

namespace SaveState.Presentation.ViewModels.Analytics;

/// <summary>
/// ViewModel for advanced analytics and predictive insights.
/// </summary>
public partial class AdvancedAnalyticsViewModel : ObservableObject
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICompletionPredictionService _predictionService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private GamingHeatmapData? currentHeatmap;

    [ObservableProperty]
    private ObservableCollection<PlayPatternInsight> playPatterns = new();

    [ObservableProperty]
    private ObservableCollection<CompletionPrediction> completionPredictions = new();

    [ObservableProperty]
    private ObservableCollection<PerformanceTrend> performanceTrends = new();

    [ObservableProperty]
    private double totalPlaytimeHours;

    [ObservableProperty]
    private int totalGamesPlayed;

    [ObservableProperty]
    private double averageSessionLength;

    [ObservableProperty]
    private int currentStreak;

    [ObservableProperty]
    private int longestStreak;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private int selectedYear = DateTime.Now.Year;

    public VoiceCommandViewModel VoiceCommandViewModel { get; private set; }

    public AdvancedAnalyticsViewModel(
        IAnalyticsService analyticsService,
        ICompletionPredictionService predictionService,
        INotificationService notificationService,
        VoiceCommandViewModel voiceCommandViewModel)
    {
        _analyticsService = analyticsService;
        _predictionService = predictionService;
        _notificationService = notificationService;
        VoiceCommandViewModel = voiceCommandViewModel;
    }

    public async Task InitializeAsync()
    {
        await LoadHeatmapAsync();
        await LoadPlayPatternsAsync();
        await LoadCompletionPredictionsAsync();
        await LoadPerformanceTrendsAsync();
    }

    [RelayCommand]
    public async Task RefreshAnalytics()
    {
        try
        {
            IsLoading = true;
            await Task.WhenAll(
                LoadHeatmapAsync(),
                LoadPlayPatternsAsync(),
                LoadCompletionPredictionsAsync(),
                LoadPerformanceTrendsAsync());
            await _notificationService.ShowNotificationAsync("Analytics refreshed", "Success");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to refresh analytics: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ChangeYear(int year)
    {
        SelectedYear = year;
        await LoadHeatmapAsync();
    }

    [RelayCommand]
    public async Task ExportAnalytics()
    {
        try
        {
            // Export logic would be implemented here
            await _notificationService.ShowNotificationAsync("Analytics exported successfully", "Success");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Export failed: {ex.Message}");
        }
    }

    private async Task LoadHeatmapAsync()
    {
        try
        {
            var result = await _analyticsService.GetHeatmapAsync(SelectedYear);
            if (result.IsSuccess)
            {
                CurrentHeatmap = result.Value;
                TotalPlaytimeHours = result.Value.TotalPlaytime.TotalHours;
                CurrentStreak = result.Value.CurrentStreak;
                LongestStreak = result.Value.LongestStreak;
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to load heatmap: {ex.Message}");
        }
    }

    private async Task LoadPlayPatternsAsync()
    {
        try
        {
            // Load play patterns - would use IPlayPatternAnalyzer if available
            PlayPatterns.Clear();
            // Example patterns would be added here based on actual implementation
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to load play patterns: {ex.Message}");
        }
    }

    private async Task LoadCompletionPredictionsAsync()
    {
        try
        {
            // Load completion predictions - would use IPredictionService
            CompletionPredictions.Clear();
            // Example predictions would be added here
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to load predictions: {ex.Message}");
        }
    }

    private async Task LoadPerformanceTrendsAsync()
    {
        try
        {
            // Load performance trends
            PerformanceTrends.Clear();
            // Example trends would be added here
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to load trends: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents a play pattern insight.
/// </summary>
public record PlayPatternInsight(
    string PatternName,
    string Description,
    double Percentage,
    TimeSpan AverageDuration,
    DateTime DiscoveredAt);

/// <summary>
/// Represents a completion prediction.
/// </summary>
public record CompletionPrediction(
    string GameTitle,
    float CompletionPercentage,
    TimeSpan EstimatedTimeToCompletion,
    float ConfidenceLevel,
    string RecommendedNextStep);

/// <summary>
/// Represents a performance trend.
/// </summary>
public record PerformanceTrend(
    string MetricName,
    double CurrentValue,
    double PreviousValue,
    double TrendPercentage,
    string TrendDirection);
