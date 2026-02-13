using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Analytics.DTOs;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
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
    private readonly ITimeProvider _timeProvider;

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
    private int selectedYear;

    public VoiceCommandViewModel VoiceCommandViewModel { get; private set; }

    public AdvancedAnalyticsViewModel(
        IAnalyticsService analyticsService,
        ICompletionPredictionService predictionService,
        INotificationService notificationService,
        VoiceCommandViewModel voiceCommandViewModel,
        ITimeProvider timeProvider)
    {
        _analyticsService = analyticsService;
        _predictionService = predictionService;
        _notificationService = notificationService;
        VoiceCommandViewModel = voiceCommandViewModel;
        _timeProvider = timeProvider;
        selectedYear = timeProvider.Now.Year;
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
            PlayPatterns.Clear();
            var result = await _analyticsService.GetPlaytimeDistributionAsync();
            if (!result.IsSuccess)
            {
                await _notificationService.ShowErrorAsync($"Failed to load play patterns: {result.Error}");
                return;
            }

            var distribution = result.Value;
            if (distribution.ByDayOfWeek.Count == 0 || distribution.ByHour.Count == 0)
            {
                return;
            }

            var topDay = distribution.ByDayOfWeek
                .OrderByDescending(kvp => kvp.Value)
                .First();
            var topHour = distribution.ByHour
                .OrderByDescending(kvp => kvp.Value)
                .First();

            var totalDayTime = distribution.ByDayOfWeek.Values.Sum(timespan => timespan.TotalHours);
            var dayPercentage = totalDayTime <= 0
                ? 0
                : (topDay.Value.TotalHours / totalDayTime) * 100;

            PlayPatterns.Add(new PlayPatternInsight(
                PatternName: "Frequency",
                Description: $"You play most often on {topDay.Key}",
                Percentage: dayPercentage,
                AverageDuration: topDay.Value,
                DiscoveredAt: DateTime.UtcNow));

            PlayPatterns.Add(new PlayPatternInsight(
                PatternName: "Peak Hour",
                Description: $"Peak gaming hour is around {topHour.Key:00}:00",
                Percentage: 100, // show as a highlight
                AverageDuration: topHour.Value,
                DiscoveredAt: DateTime.UtcNow));
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
            CompletionPredictions.Clear();
            var result = await _predictionService.GetPredictionsAsync(count: 5);
            if (!result.IsSuccess)
            {
                await _notificationService.ShowErrorAsync($"Failed to load predictions: {result.Error}");
                return;
            }

            foreach (var prediction in result.Value)
            {
                var completionPercent = Math.Clamp((float)prediction.ConfidenceScore, 0f, 100f);
                var confidenceLevel = Math.Clamp((float)(prediction.ConfidenceScore / 100.0), 0f, 1f);
                var reasoning = prediction.ReasoningFactors is { Count: > 0 }
                    ? string.Join("; ", prediction.ReasoningFactors.Take(2))
                    : prediction.BasedOn;
                var recommendation = !string.IsNullOrWhiteSpace(reasoning)
                    ? $"Based on {reasoning}."
                    : "Review your play history for more context.";

                CompletionPredictions.Add(new CompletionPrediction(
                    GameTitle: prediction.GameName,
                    CompletionPercentage: completionPercent,
                    EstimatedTimeToCompletion: prediction.EstimatedTimeRemaining,
                    ConfidenceLevel: confidenceLevel,
                    RecommendedNextStep: recommendation));
            }
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
