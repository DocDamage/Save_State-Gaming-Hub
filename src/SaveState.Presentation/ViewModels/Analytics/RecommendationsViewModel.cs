using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Recommendations.DTOs;
using SaveState.Core.Recommendations.Queries;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Analytics;

/// <summary>
/// ViewModel for displaying smart game recommendations.
/// </summary>
public partial class RecommendationsViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ILogger<RecommendationsViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<SmartGameRecommendation> _recommendations = new();

    [ObservableProperty]
    private ObservableCollection<SmartTrendingGame> _trendingGames = new();

    [ObservableProperty]
    private ObservableCollection<SmartBacklogRecommendation> _backlogRecommendations = new();

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public RecommendationsViewModel(
        IMediator mediator,
        ILogger<RecommendationsViewModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadRecommendationsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // TODO: Get actual user ID from authentication service
            var userId = Guid.NewGuid();

            // Load personalized recommendations
            var recommendationsResult = await _mediator.Send(
                new GetGameRecommendationsQuery(userId, 10));

            if (recommendationsResult.IsSuccess)
            {
                Recommendations.Clear();
                foreach (var rec in recommendationsResult.Value)
                {
                    Recommendations.Add(rec);
                }
            }

            // Load trending games
            var trendingResult = await _mediator.Send(
                new GetTrendingGamesQuery(10));

            if (trendingResult.IsSuccess)
            {
                TrendingGames.Clear();
                foreach (var game in trendingResult.Value)
                {
                    TrendingGames.Add(game);
                }
            }

            // Load backlog recommendations
            var backlogResult = await _mediator.Send(
                new GetBacklogRecommendationsQuery(userId, 5));

            if (backlogResult.IsSuccess)
            {
                BacklogRecommendations.Clear();
                foreach (var rec in backlogResult.Value)
                {
                    BacklogRecommendations.Add(rec);
                }
            }

            _logger.LogInformation("Loaded recommendations successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recommendations");
            ErrorMessage = "Failed to load recommendations. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadRecommendationsAsync();
    }
}
