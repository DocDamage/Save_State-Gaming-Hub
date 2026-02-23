using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Esports.Models;
using SaveState.Core.UserManagement.Services;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Esports;

/// <summary>
/// ViewModel for match detail view with result reporting and real-time updates.
/// </summary>
public partial class MatchDetailViewModel : ObservableObject
{
    private readonly ILogger<MatchDetailViewModel> _logger;
    private readonly ITournamentService _tournamentService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly IUserContextService _userContextService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILiveTournamentHub _liveHub;

    [ObservableProperty]
    private Match? _match;

    [ObservableProperty]
    private Tournament? _tournament;

    [ObservableProperty]
    private bool _isPlayer1;

    [ObservableProperty]
    private bool _isPlayer2;

    [ObservableProperty]
    private bool _isOrganizer;

    [ObservableProperty]
    private bool _canReportResult;

    [ObservableProperty]
    private ObservableCollection<MatchGame> _games = new();

    [ObservableProperty]
    private int _player1Score;

    [ObservableProperty]
    private int _player2Score;

    [ObservableProperty]
    private string _matchNotes = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _player1Character = string.Empty;

    [ObservableProperty]
    private string _player2Character = string.Empty;

    [ObservableProperty]
    private string _selectedStage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _availableStages = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableCharacters = new();

    [ObservableProperty]
    private bool _isDisputeMode;

    [ObservableProperty]
    private string _disputeReason = string.Empty;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private string? _streamUrl;

    [ObservableProperty]
    private TimeSpan? _elapsedTime;

    private Guid _currentTournamentId;
    private Guid _currentMatchId;
    private IDisposable? _liveUpdatesSubscription;
    private System.Timers.Timer? _refreshTimer;

    public MatchDetailViewModel(
        ILogger<MatchDetailViewModel> logger,
        ITournamentService tournamentService,
        IDialogService dialogService,
        INotificationService notificationService,
        IUserContextService userContextService,
        ITimeProvider timeProvider,
        ILiveTournamentHub liveHub)
    {
        _logger = logger;
        _tournamentService = tournamentService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _userContextService = userContextService;
        _timeProvider = timeProvider;
        _liveHub = liveHub;

        // Initialize available stages and characters
        InitializeAvailableOptions();
    }

    private void InitializeAvailableOptions()
    {
        AvailableStages = new ObservableCollection<string>
        {
            "Battlefield",
            "Final Destination",
            "Smashville",
            "Town & City",
            "Pokemon Stadium 2",
            "Unova Pokemon League",
            "Kalos Pokemon League",
            "Hollow Bastion",
            "Small Battlefield",
            "Yoshi's Story"
        };

        AvailableCharacters = new ObservableCollection<string>
        {
            "Mario", "Donkey Kong", "Link", "Samus", "Dark Samus", "Yoshi",
            "Kirby", "Fox", "Pikachu", "Luigi", "Ness", "Captain Falcon",
            "Jigglypuff", "Peach", "Daisy", "Bowser", "Ice Climbers", "Sheik",
            "Zelda", "Dr. Mario", "Pichu", "Falco", "Marth", "Lucina",
            "Young Link", "Ganondorf", "Mewtwo", "Roy", "Chrom", "Mr. Game & Watch"
        };
    }

    /// <summary>
    /// Loads match details for the specified tournament and match.
    /// </summary>
    public async Task LoadMatchAsync(Guid tournamentId, Guid matchId)
    {
        IsLoading = true;
        StatusMessage = "Loading match details...";

        try
        {
            _currentTournamentId = tournamentId;
            _currentMatchId = matchId;

            // Load tournament details
            var tournamentResult = await _tournamentService.GetTournamentAsync(tournamentId);
            if (tournamentResult.IsSuccess)
            {
                Tournament = tournamentResult.Value;
            }

            // Load match details
            var matchResult = await _tournamentService.GetMatchAsync(tournamentId, matchId);
            if (matchResult.IsFailure || matchResult.Value is null)
            {
                StatusMessage = "Failed to load match details.";
                await _dialogService.ShowErrorAsync("Error", "Failed to load match details.");
                return;
            }

            Match = matchResult.Value;
            UpdateMatchState();

            // Setup live updates
            await SetupLiveUpdatesAsync();

            // Start refresh timer for elapsed time
            StartRefreshTimer();

            StatusMessage = "Match loaded successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading match {MatchId}", matchId);
            StatusMessage = "Error loading match.";
            await _dialogService.ShowErrorAsync("Error", "An error occurred while loading the match.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateMatchState()
    {
        if (Match is null) return;

        var currentUserId = _userContextService.CurrentUserId?.ToString();

        // Determine user role
        IsPlayer1 = Match.Player1?.UserId == currentUserId;
        IsPlayer2 = Match.Player2?.UserId == currentUserId;
        IsOrganizer = Tournament?.CreatedBy == currentUserId;

        // Determine if user can report result
        CanReportResult = (IsPlayer1 || IsPlayer2 || IsOrganizer) &&
                          Match.Status != MatchStatus.Completed &&
                          Match.Status != MatchStatus.Cancelled;

        // Load existing games
        Games.Clear();
        if (Match.Games?.Count > 0)
        {
            foreach (var game in Match.Games)
            {
                Games.Add(game);
            }

            // Calculate scores from games
            CalculateScoresFromGames();
        }
        else
        {
            // Add initial game for best-of series
            AddNewGame();
        }

        // Load stream info
        IsStreaming = !string.IsNullOrEmpty(Match.StreamUrl);
        StreamUrl = Match.StreamUrl;

        // Calculate elapsed time if match is in progress
        if (Match.StartedTime.HasValue && Match.Status == MatchStatus.InProgress)
        {
            ElapsedTime = _timeProvider.UtcNow - Match.StartedTime.Value;
        }
    }

    private void CalculateScoresFromGames()
    {
        Player1Score = 0;
        Player2Score = 0;

        foreach (var game in Games)
        {
            if (game.Winner?.Id == Match?.Player1?.Id)
            {
                Player1Score++;
            }
            else if (game.Winner?.Id == Match?.Player2?.Id)
            {
                Player2Score++;
            }
        }
    }

    private void AddNewGame()
    {
        var gameNumber = Games.Count + 1;
        Games.Add(new MatchGame
        {
            GameNumber = gameNumber,
            Player1Score = 0,
            Player2Score = 0
        });
    }

    private async Task SetupLiveUpdatesAsync()
    {
        try
        {
            await _liveHub.ConnectAsync();

            _liveUpdatesSubscription = _liveHub.OnMatchUpdated.Subscribe(updatedMatch =>
            {
                if (updatedMatch.Id == _currentMatchId)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        Match = updatedMatch;
                        UpdateMatchState();
                        _notificationService.ShowInfo("Match Updated", "The match has been updated.");
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to setup live updates, falling back to polling");
            // Fallback to polling handled by refresh timer
        }
    }

    private void StartRefreshTimer()
    {
        _refreshTimer?.Stop();
        _refreshTimer = new System.Timers.Timer(10000); // 10 seconds
        _refreshTimer.Elapsed += async (s, e) =>
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await RefreshMatchAsync();
            });
        };
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
    }

    private async Task RefreshMatchAsync()
    {
        if (_currentTournamentId == Guid.Empty || _currentMatchId == Guid.Empty) return;

        try
        {
            var result = await _tournamentService.GetMatchAsync(_currentTournamentId, _currentMatchId);
            if (result.IsSuccess && result.Value is not null)
            {
                var updatedMatch = result.Value;

                // Check for score changes
                if (updatedMatch.Result?.Player1Score != Match?.Result?.Player1Score ||
                    updatedMatch.Result?.Player2Score != Match?.Result?.Player2Score)
                {
                    Match = updatedMatch;
                    UpdateMatchState();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error refreshing match");
        }
    }

    /// <summary>
    /// Reports the match result.
    /// </summary>
    [RelayCommand]
    private async Task ReportResultAsync()
    {
        if (Match is null || Tournament is null) return;

        try
        {
            // Validate games
            var incompleteGames = Games.Where(g => g.Winner is null).ToList();
            if (incompleteGames.Any())
            {
                await _dialogService.ShowErrorAsync("Incomplete Games", "Please set winners for all games.");
                return;
            }

            // Determine winner based on best-of
            var bestOf = Tournament.Rules.BestOf;
            var winsNeeded = (bestOf / 2) + 1;

            if (Player1Score < winsNeeded && Player2Score < winsNeeded)
            {
                await _dialogService.ShowErrorAsync("Invalid Score", $"Need {winsNeeded} wins to complete match.");
                return;
            }

            var winner = Player1Score > Player2Score ? Match.Player1 : Match.Player2;
            if (winner is null)
            {
                await _dialogService.ShowErrorAsync("Error", "Could not determine winner.");
                return;
            }

            // Confirm with user
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Confirm Result",
                $"Report result: {Match.Player1?.DisplayName} {Player1Score} - {Player2Score} {Match.Player2?.DisplayName}\n\nWinner: {winner.DisplayName}");

            if (!confirmed) return;

            // Submit result
            var request = new ReportMatchResultRequest(
                Player1Score,
                Player2Score,
                MatchNotes,
                Games.ToList()
            );

            var result = await _tournamentService.ReportMatchResultAsync(
                Tournament.Id,
                Match.Id,
                request);

            if (result.IsSuccess)
            {
                StatusMessage = "Result reported successfully.";
                _notificationService.ShowSuccess("Result Submitted", "Match result has been reported.");

                // Refresh match
                await LoadMatchAsync(Tournament.Id, Match.Id);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to report result: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting match result");
            await _dialogService.ShowErrorAsync("Error", "An error occurred while reporting the result.");
        }
    }

    /// <summary>
    /// Confirms a reported result.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmResultAsync()
    {
        if (Match is null || Tournament is null) return;

        try
        {
            var result = await _tournamentService.ConfirmMatchResultAsync(Tournament.Id, Match.Id);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess("Result Confirmed", "Match result has been confirmed.");
                await LoadMatchAsync(Tournament.Id, Match.Id);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to confirm result: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming match result");
            await _dialogService.ShowErrorAsync("Error", "An error occurred while confirming the result.");
        }
    }

    /// <summary>
    /// Disputes a reported result.
    /// </summary>
    [RelayCommand]
    private async Task DisputeResultAsync()
    {
        if (Match is null || Tournament is null) return;

        try
        {
            IsDisputeMode = true;

            var reason = await _dialogService.ShowTextInputAsync(
                "Dispute Result",
                "Please provide a reason for disputing this result:",
                DisputeReason);

            IsDisputeMode = false;

            if (string.IsNullOrWhiteSpace(reason)) return;

            DisputeReason = reason;

            var result = await _tournamentService.DisputeMatchResultAsync(
                Tournament.Id,
                Match.Id,
                reason);

            if (result.IsSuccess)
            {
                _notificationService.ShowWarning("Result Disputed", "Match result has been disputed and will be reviewed.");
                await LoadMatchAsync(Tournament.Id, Match.Id);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to dispute result: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disputing match result");
            await _dialogService.ShowErrorAsync("Error", "An error occurred while disputing the result.");
        }
    }

    /// <summary>
    /// Starts the match.
    /// </summary>
    [RelayCommand]
    private async Task StartMatchAsync()
    {
        if (Match is null || Tournament is null) return;

        try
        {
            var result = await _tournamentService.StartMatchAsync(Tournament.Id, Match.Id);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess("Match Started", "Good luck and have fun!");
                await LoadMatchAsync(Tournament.Id, Match.Id);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to start match: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting match");
            await _dialogService.ShowErrorAsync("Error", "An error occurred while starting the match.");
        }
    }

    /// <summary>
    /// Contacts the opponent via chat or messaging.
    /// </summary>
    [RelayCommand]
    private async Task ContactOpponentAsync()
    {
        if (Match is null) return;

        var opponent = IsPlayer1 ? Match.Player2 : Match.Player1;
        if (opponent is null)
        {
            await _dialogService.ShowErrorAsync("Error", "Opponent not found.");
            return;
        }

        // Open chat dialog or messaging interface
        await _dialogService.ShowInfoAsync("Contact Opponent",
            $"Contact {opponent.DisplayName}\n\nDiscord: {opponent.UserId}\n\nRemember to be respectful and follow the tournament rules.");
    }

    /// <summary>
    /// Views replay for a specific game.
    /// </summary>
    [RelayCommand]
    private async Task ViewReplayAsync(MatchGame? game)
    {
        if (game is null || string.IsNullOrEmpty(game.ReplayUrl))
        {
            await _dialogService.ShowInfoAsync("No Replay", "No replay available for this game.");
            return;
        }

        try
        {
            // Open replay URL in default browser or media player
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = game.ReplayUrl,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening replay");
            await _dialogService.ShowErrorAsync("Error", "Could not open replay.");
        }
    }

    /// <summary>
    /// Adds a new game to the match.
    /// </summary>
    [RelayCommand]
    private void AddGame()
    {
        var bestOf = Tournament?.Rules.BestOf ?? 3;
        if (Games.Count >= bestOf)
        {
            _notificationService.ShowWarning("Maximum Games", $"Best of {bestOf} series cannot have more than {bestOf} games.");
            return;
        }

        AddNewGame();
    }

    /// <summary>
    /// Removes a game from the match.
    /// </summary>
    [RelayCommand]
    private void RemoveGame(MatchGame game)
    {
        Games.Remove(game);

        // Renumber games
        for (int i = 0; i < Games.Count; i++)
        {
            Games[i] = Games[i] with { GameNumber = i + 1 };
        }

        CalculateScoresFromGames();
    }

    /// <summary>
    /// Updates the winner for a game.
    /// </summary>
    [RelayCommand]
    private void SetGameWinner(MatchGame game)
    {
        if (Match is null) return;

        // Toggle between player 1, player 2, and no winner
        if (game.Winner?.Id == Match.Player1?.Id)
        {
            game.Winner = Match.Player2;
            game.Player1Score = 0;
            game.Player2Score = 1;
        }
        else if (game.Winner?.Id == Match.Player2?.Id)
        {
            game.Winner = null;
            game.Player1Score = 0;
            game.Player2Score = 0;
        }
        else
        {
            game.Winner = Match.Player1;
            game.Player1Score = 1;
            game.Player2Score = 0;
        }

        CalculateScoresFromGames();
    }

    /// <summary>
    /// Opens the stream URL.
    /// </summary>
    [RelayCommand]
    private async Task OpenStreamAsync()
    {
        if (string.IsNullOrEmpty(StreamUrl)) return;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = StreamUrl,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening stream");
            await _dialogService.ShowErrorAsync("Error", "Could not open stream.");
        }
    }

    public void Dispose()
    {
        _liveUpdatesSubscription?.Dispose();
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
    }
}

/// <summary>
/// Service interface for tournament operations.
/// </summary>
public interface ITournamentService
{
    Task<Result<Tournament>> GetTournamentAsync(Guid tournamentId);
    Task<Result<Match>> GetMatchAsync(Guid tournamentId, Guid matchId);
    Task<Result> ReportMatchResultAsync(Guid tournamentId, Guid matchId, ReportMatchResultRequest request);
    Task<Result> ConfirmMatchResultAsync(Guid tournamentId, Guid matchId);
    Task<Result> DisputeMatchResultAsync(Guid tournamentId, Guid matchId, string reason);
    Task<Result> StartMatchAsync(Guid tournamentId, Guid matchId);
}

/// <summary>
/// Service interface for live tournament updates via SignalR or polling.
/// </summary>
public interface ILiveTournamentHub
{
    Task ConnectAsync();
    Task DisconnectAsync();
    IObservable<Match> OnMatchUpdated { get; }
    IObservable<Tournament> OnTournamentUpdated { get; }
}
