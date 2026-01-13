using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.UserManagement.Services;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Overview tab.
/// </summary>
public partial class GameOverviewTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IUserContextService _userContextService;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IDialogService _dialogService;
    private readonly IUiGameContextService _gameContextService;
    private readonly ILogger<GameOverviewTabViewModel> _logger;
    private readonly INavigationService _navigationService;
    private GameId? _currentGameId;

    [ObservableProperty]
    private string _totalPlaytimeText = "0h 0m";

    [ObservableProperty]
    private int _sessionCount;

    [ObservableProperty]
    private string _lastPlayedText = "Never";

    [ObservableProperty]
    private string _firstPlayedText = "Unknown";

    [ObservableProperty]
    private string _achievementProgress = "0/0 (0%)";

    [ObservableProperty]
    private string _gameDescription = "No description available.";

    [ObservableProperty]
    private bool _hasLongDescription;

    [ObservableProperty]
    private string _hltbMainStory = "Unknown";

    [ObservableProperty]
    private string _hltbMainExtras = "Unknown";

    [ObservableProperty]
    private string _hltbCompletionist = "Unknown";

    [ObservableProperty]
    private double _completionPercentage;

    [ObservableProperty]
    private string _completionText = "0% complete";

    [ObservableProperty]
    private string _currentPrice = "Unknown";

    [ObservableProperty]
    private string _lowestPrice = "Unknown";

    [ObservableProperty]
    private string _historicalLow = "Unknown";

    [ObservableProperty]
    private bool _notifyOnSale;

    [ObservableProperty]
    private ObservableCollection<string> _gameTags = new();

    [ObservableProperty]
    private string _aiBriefingText = "No AI briefing available. Generate one to get personalized insights about this game.";

    [ObservableProperty]
    private string _aiBriefingTimestamp = string.Empty;

    public GameOverviewTabViewModel(
        IMediator mediator,
        IUserContextService userContextService,
        IAiOrchestrator aiOrchestrator,
        IDialogService dialogService,
        INavigationService navigationService,
        IUiGameContextService gameContextService,
        ILogger<GameOverviewTabViewModel> logger)
    {
        _mediator = mediator;
        _userContextService = userContextService;
        _aiOrchestrator = aiOrchestrator;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _gameContextService = gameContextService;
        _logger = logger;
    }



    [ObservableProperty]
    private string _gameTitle = string.Empty;

    // ... (keep existing properties)

    public async Task LoadDataAsync(GameId gameId)
    {
        _currentGameId = gameId; // Store for later use
        try
        {
            // Load game data
            var query = new GetGameByIdQuery(gameId);
            var game = await _mediator.Send(query).ConfigureAwait(false);

            if (game is null)
            {
                _logger.LogWarning("Game not found for overview: {GameId}", gameId);
                return;
            }

            GameTitle = game.Title;
            // Load game description
            GameDescription = game.Description ?? "No description available.";
            HasLongDescription = GameDescription.Length > 200;

            // Format total playtime
            var hours = (int)game.TotalPlayTime.TotalHours;
            var minutes = game.TotalPlayTime.Minutes;
            TotalPlaytimeText = $"{hours}h {minutes}m";

            // Format last played
            if (game.LastPlayedAt.HasValue)
            {
                var timeSince = DateTime.UtcNow - game.LastPlayedAt.Value;
                if (timeSince.TotalDays < 1)
                    LastPlayedText = "Today";
                else if (timeSince.TotalDays < 2)
                    LastPlayedText = "Yesterday";
                else if (timeSince.TotalDays < 7)
                    LastPlayedText = $"{(int)timeSince.TotalDays} days ago";
                else
                    LastPlayedText = game.LastPlayedAt.Value.ToString("MMM d, yyyy");
            }

            // Load tags
            GameTags.Clear();
            if (game.Tags.Any())
            {
                foreach (var tag in game.Tags)
                {
                    GameTags.Add(tag);
                }
            }

            // Load session count
            try
            {
                var sessionQuery = new GetGameSessionsQuery(gameId.Value);
                var sessions = await _mediator.Send(sessionQuery).ConfigureAwait(false);
                SessionCount = sessions.Count;
            }
            catch (Exception ex)
            {
                 _logger.LogWarning(ex, "Failed to load session count");
                 SessionCount = 0;
            }

            // Load achievement progress
            try
            {
                var userId = _userContextService.GetCurrentUserId();
                if (userId.HasValue)
                {
                    var achievementQuery = new GetUserAchievementsQuery(userId.Value, GameId: gameId.Value, IncludeLocked: true);
                    var achievements = await _mediator.Send(achievementQuery).ConfigureAwait(false);

                    var unlocked = achievements.Count(a => a.IsUnlocked);
                    var total = achievements.Count;
                    var percentage = total > 0 ? (double)unlocked / total * 100.0 : 0;

                    AchievementProgress = $"{unlocked}/{total} ({percentage:F0}%)";
                    CompletionPercentage = percentage;
                    CompletionText = $"{percentage:F0}% complete";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load achievement progress");
                AchievementProgress = "0/0 (0%)";
            }

            // Mock HLTB data based on title length as a silly heuristic
            var seed = GameTitle.Length;
            HltbMainStory = $"{10 + (seed % 20)}h";
            HltbMainExtras = $"{25 + (seed % 30)}h";
            HltbCompletionist = $"{60 + (seed % 100)}h";

            // Mock Price data
            CurrentPrice = "$59.99";
            LowestPrice = "$19.99";
            HistoricalLow = "$14.99";
            NotifyOnSale = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load overview data for game {GameId}", gameId);
        }
    }

    [RelayCommand]
    private async Task GenerateAiBriefing()
    {
        try
        {
            if (string.IsNullOrEmpty(GameTitle))
            {
                AiBriefingText = "Cannot generate briefing: Game title is missing.";
                return;
            }

            AiBriefingText = "Generating briefing...";

            var isRunning = _gameContextService.RunningGameId == _currentGameId;
            var statusContext = isRunning ? "Note: the user is currently playing this game." : "Note: the user is not currently playing this game.";

            var prompt = $"Write a short, engaging briefing for the game '{GameTitle}'. {statusContext} Focus on its key themes, genre, and why it's worth playing. If the user is playing, mention something relevant to an active session. Keep it under 100 words.";
            var result = await _aiOrchestrator.GenerateTextAsync(prompt);

            if (result.IsSuccess)
            {
                AiBriefingText = result.Value;
                AiBriefingTimestamp = DateTime.Now.ToString("g");
            }
            else
            {
                AiBriefingText = "Failed to generate briefing. AI Provider unavailable.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI briefing");
            AiBriefingText = "Failed to generate briefing due to an error.";
        }
    }
    [RelayCommand]
    private async Task ReadMore()
    {
        if (string.IsNullOrEmpty(GameDescription)) return;

        await _dialogService.ShowInformationAsync($"{GameTitle} - Description", GameDescription);
    }



    [RelayCommand]
    private void ViewAnalytics()
    {
        if (_currentGameId != null)
        {
             _navigationService.NavigateTo("Analytics", new { gameId = _currentGameId });
        }
    }

    [RelayCommand]
    private async Task EditTags()
    {
        if (_currentGameId == null)
        {
            _logger.LogWarning("Cannot edit tags - no game ID set");
            return;
        }

        var result = await _dialogService.ShowTagEditorAsync(GameTags.ToArray());
        if (result == null)
        {
            _logger.LogInformation("Tag editing cancelled");
            return;
        }

        try
        {
            var command = new UpdateGameTagsCommand(_currentGameId.Value, result.Tags.ToList());
            var updateResult = await _mediator.Send(command);

            if (updateResult.IsSuccess)
            {
                _logger.LogInformation("Tags updated successfully");
                // Update UI
                GameTags.Clear();
                foreach (var tag in result.Tags)
                {
                    GameTags.Add(tag);
                }
            }
            else
            {
                _logger.LogError("Failed to update tags: {Error}", updateResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tags");
        }
    }

    [RelayCommand]
    private async Task WriteReview()
    {
        if (_currentGameId == null)
        {
            _logger.LogWarning("Cannot write review - no game ID set");
            return;
        }

        var result = await _dialogService.ShowReviewEditorAsync();
        if (result == null)
        {
            _logger.LogInformation("Review creation cancelled");
            return;
        }

        try
        {
            var command = new Application.Social.Commands.CreateReviewCommand(
                _currentGameId.Value,
                result.Rating,
                result.RecommendToFriends,
                null, // title
                result.ReviewText);

            var createResult = await _mediator.Send(command);
            if (createResult.IsSuccess)
            {
                _logger.LogInformation("Review created successfully");
            }
            else
            {
                _logger.LogError("Failed to create review: {Error}", createResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
        }
    }



    [RelayCommand]
    private async Task ViewPriceHistory()
    {
        if (string.IsNullOrEmpty(GameTitle)) return;
        await _dialogService.ShowPriceHistoryChartAsync(GameTitle);
    }

    [RelayCommand]
    private async Task AddGoal()
    {
        await CreateGoal();
    }

    [RelayCommand]
    private async Task SetPriceAlert()
    {
        try
        {
            var result = await _dialogService.ShowPriceAlertDialogAsync(GameTitle, 59.99); // Hardcoded price for now until service is real

            if (result != null)
            {
                 // In real app, save alert to database
                 _logger.LogInformation("Price alert set for {Game}: {TargetPrice} at {Store}", GameTitle, result.TargetPrice, result.Store);
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to set price alert");
        }
    }

    [RelayCommand]
    private async Task CreateGoal()
    {
        if (_currentGameId == null)
        {
            _logger.LogWarning("Cannot create goal - no game ID set");
            return;
        }

        var userId = _userContextService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Cannot create goal - no current user");
            return;
        }

        var result = await _dialogService.ShowGoalCreationDialogAsync();
        if (result == null)
        {
            _logger.LogInformation("Goal creation cancelled");
            return;
        }

        try
        {
            var command = new CreateGameGoalCommand(
                _currentGameId.Value,
                userId.Value,
                result.Title,
                result.Description,
                null, // targetValue
                result.GoalType, // unit
                result.TargetDate);

            var createResult = await _mediator.Send(command);
            if (createResult.IsSuccess)
            {
                _logger.LogInformation("Goal created successfully with ID: {GoalId}", createResult.Value);
            }
            else
            {
                _logger.LogError("Failed to create goal: {Error}", createResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating goal");
        }
    }
}
