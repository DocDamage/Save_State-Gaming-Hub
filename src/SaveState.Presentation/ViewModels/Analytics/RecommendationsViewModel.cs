using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Recommendations.DTOs;
using SaveState.Core.Recommendations.Queries;
using SaveState.Core.UserManagement.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Analytics;

/// <summary>
/// ViewModel for displaying smart game recommendations.
/// </summary>
public partial class RecommendationsViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ILogger<RecommendationsViewModel> _logger;
    private readonly IUserContextService? _userContextService;

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
        ILogger<RecommendationsViewModel> logger,
        IUserContextService? userContextService = null)
    {
        _mediator = mediator;
        _logger = logger;
        _userContextService = userContextService;
    }

    [RelayCommand]
    private async Task LoadRecommendationsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var userId = _userContextService?.GetCurrentUserId()
                ?? _userContextService?.CurrentUserId;

            var hasUser = userId.HasValue && userId.Value != Guid.Empty;
            if (!hasUser)
            {
                _logger.LogWarning("No authenticated user found for personalized recommendations.");
                ErrorMessage = "Sign in to see personalized recommendations.";
            }

            if (hasUser && userId is not null)
            {
                var recommendationsResult = await _mediator.Send(
                    new GetGameRecommendationsQuery(userId.Value, 10));

                if (recommendationsResult.IsSuccess)
                {
                    Recommendations.Clear();
                    foreach (var rec in recommendationsResult.Value)
                    {
                        Recommendations.Add(rec);
                    }
                }
            }
            else
            {
                Recommendations.Clear();
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
            if (hasUser && userId is not null)
            {
                var backlogResult = await _mediator.Send(
                    new GetBacklogRecommendationsQuery(userId.Value, 5));

                if (backlogResult.IsSuccess)
                {
                    BacklogRecommendations.Clear();
                    foreach (var rec in backlogResult.Value)
                    {
                        BacklogRecommendations.Add(rec);
                    }
                }
            }
            else
            {
                BacklogRecommendations.Clear();
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
