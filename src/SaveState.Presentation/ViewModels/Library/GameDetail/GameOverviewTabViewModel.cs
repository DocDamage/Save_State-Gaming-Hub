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
    private readonly ILogger<GameOverviewTabViewModel> _logger;
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
        ILogger<GameOverviewTabViewModel> logger)
    {
        _mediator = mediator;
        _userContextService = userContextService;
        _aiOrchestrator = aiOrchestrator;
        _dialogService = dialogService;
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

            HltbMainStory = "Unknown"; // Requires HLTB integration
            HltbMainExtras = "Unknown";
            HltbCompletionist = "Unknown";
            CurrentPrice = "Unknown"; // Requires price tracking service
            LowestPrice = "Unknown";
            HistoricalLow = "Unknown";
            NotifyOnSale = false;
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

            var prompt = $"Write a short, engaging briefing for the game '{GameTitle}'. Focus on its key themes, genre, and why it's worth playing. Keep it under 100 words.";
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
    private void ReadMore()
    {
        // TODO: Show full description in dialog or expand view
        _logger.LogInformation("Read more requested");
    }

    [RelayCommand]
    private void AddGoal()
    {
        // TODO: Open add goal dialog
        _logger.LogInformation("Add goal requested");
    }

    [RelayCommand]
    private void ViewAnalytics()
    {
        // TODO: Navigate to analytics view
        _logger.LogInformation("View analytics requested");
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
    private void ViewPriceHistory()
    {
        // TODO: Show price history chart
        _logger.LogInformation("View price history requested");
    }

    [RelayCommand]
    private void SetPriceAlert()
    {
        // TODO: Set price alert
        _logger.LogInformation("Set price alert requested");
    }

    [RelayCommand]
    private void AddTag()
    {
        // TODO: Add tag dialog
        _logger.LogInformation("Add tag requested");
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
