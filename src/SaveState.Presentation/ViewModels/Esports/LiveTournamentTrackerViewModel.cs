using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Esports.Models;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell.Mugen;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SaveState.Presentation.ViewModels.Esports;

/// <summary>
/// ViewModel for live tournament tracking with real-time updates.
/// </summary>
public partial class LiveTournamentTrackerViewModel : ObservableObject
{
    private readonly ILogger<LiveTournamentTrackerViewModel> _logger;
    private readonly ITournamentService _tournamentService;
    private readonly ILiveTournamentHub _liveHub;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private ObservableCollection<LiveMatch> _liveMatches = new();

    [ObservableProperty]
    private ObservableCollection<MatchResultModel> _recentResults = new();

    [ObservableProperty]
    private Tournament? _currentTournament;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Not connected";

    [ObservableProperty]
    private int _totalMatches;

    [ObservableProperty]
    private int _completedMatches;

    [ObservableProperty]
    private int _matchesInProgress;

    [ObservableProperty]
    private int _pendingMatches;

    [ObservableProperty]
    private ObservableCollection<Participant> _topParticipants = new();

    [ObservableProperty]
    private ObservableCollection<UpcomingMatch> _upcomingMatches = new();

    [ObservableProperty]
    private LiveMatch? _selectedMatch;

    [ObservableProperty]
    private bool _showBracket;

    [ObservableProperty]
    private TournamentBracketViewModel? _bracketViewModel;

    private IDisposable? _liveUpdatesSubscription;
    private System.Timers.Timer? _pollingTimer;
    private readonly Subject<LiveMatch> _matchUpdatesSubject = new();

    public LiveTournamentTrackerViewModel(
        ILogger<LiveTournamentTrackerViewModel> logger,
        ITournamentService tournamentService,
        ILiveTournamentHub liveHub,
        INotificationService notificationService,
        IDialogService dialogService,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _tournamentService = tournamentService;
        _liveHub = liveHub;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _timeProvider = timeProvider;

        // Setup throttled match updates
        _matchUpdatesSubject
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(async match => await HandleMatchUpdateAsync(match));
    }

    /// <summary>
    /// Connects to the live tournament hub for real-time updates.
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync()
    {
        IsLoading = true;
        StatusMessage = "Connecting to live updates...";

        try
        {
            await _liveHub.ConnectAsync();

            _liveUpdatesSubscription = _liveHub.OnMatchUpdated.Subscribe(match =>
            {
                var liveMatch = ConvertToLiveMatch(match);
                _matchUpdatesSubject.OnNext(liveMatch);
            });

            _liveHub.OnTournamentUpdated.Subscribe(tournament =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    CurrentTournament = tournament;
                    UpdateTournamentStats();
                });
            });

            IsConnected = true;
            ConnectionStatus = "Connected";
            StatusMessage = "Live updates active";

            // Start polling as fallback
            StartPollingTimer();

            _notificationService.ShowSuccess("Connected", "Live tournament updates are now active.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to SignalR, using polling fallback");
            ConnectionStatus = "Polling (Fallback)";
            StatusMessage = "Using polling for updates";
            StartPollingTimer();
            IsConnected = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Disconnects from the live tournament hub.
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAsync()
    {
        try
        {
            _pollingTimer?.Stop();
            _pollingTimer?.Dispose();
            _pollingTimer = null;

            _liveUpdatesSubscription?.Dispose();
            _liveUpdatesSubscription = null;

            await _liveHub.DisconnectAsync();

            IsConnected = false;
            ConnectionStatus = "Disconnected";
            StatusMessage = "Not connected";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from live hub");
        }
    }

    /// <summary>
    /// Refreshes tournament data manually.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (CurrentTournament is null) return;

        IsLoading = true;
        StatusMessage = "Refreshing...";

        try
        {
            await LoadTournamentDataAsync(CurrentTournament.Id);
            StatusMessage = $"Last updated: {_timeProvider.Now:g}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing tournament data");
            StatusMessage = "Refresh failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads a tournament for tracking.
    /// </summary>
    public async Task LoadTournamentAsync(Guid tournamentId)
    {
        IsLoading = true;
        StatusMessage = "Loading tournament...";

        try
        {
            await LoadTournamentDataAsync(tournamentId);
            await ConnectAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTournamentDataAsync(Guid tournamentId)
    {
        try
        {
            var tournamentResult = await _tournamentService.GetTournamentAsync(tournamentId);
            if (tournamentResult.IsFailure || tournamentResult.Value is null)
            {
                StatusMessage = "Failed to load tournament";
                return;
            }

            CurrentTournament = tournamentResult.Value;

            // Load all matches
            await LoadMatchesAsync(tournamentId);

            // Load bracket if available
            if (CurrentTournament.Bracket != null)
            {
                BracketViewModel = new TournamentBracketViewModel(
                    CurrentTournament.Name,
                    CurrentTournament.Participants.Count);
            }

            UpdateTournamentStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tournament {TournamentId}", tournamentId);
            StatusMessage = "Error loading tournament";
        }
    }

    private async Task LoadMatchesAsync(Guid tournamentId)
    {
        try
        {
            // This would be a real API call in production
            // var matchesResult = await _tournamentService.GetMatchesAsync(tournamentId);

            // For now, populate from tournament data
            if (CurrentTournament?.Matches != null)
            {
                LiveMatches.Clear();
                RecentResults.Clear();
                UpcomingMatches.Clear();

                foreach (var match in CurrentTournament.Matches)
                {
                    var liveMatch = ConvertToLiveMatch(match);

                    switch (match.Status)
                    {
                        case MatchStatus.InProgress:
                            LiveMatches.Add(liveMatch);
                            break;

                        case MatchStatus.Completed:
                            RecentResults.Add(new MatchResultModel
                            {
                                MatchId = match.Id,
                                Player1Name = match.Player1?.DisplayName ?? "TBD",
                                Player2Name = match.Player2?.DisplayName ?? "TBD",
                                Player1Score = match.Result?.Player1Score ?? 0,
                                Player2Score = match.Result?.Player2Score ?? 0,
                                WinnerName = match.Winner?.DisplayName ?? "TBD",
                                RoundName = GetRoundName(match.Round),
                                CompletedAt = match.CompletedTime
                            });
                            break;

                        case MatchStatus.Scheduled:
                            UpcomingMatches.Add(new UpcomingMatch
                            {
                                MatchId = match.Id,
                                Player1Name = match.Player1?.DisplayName ?? "TBD",
                                Player2Name = match.Player2?.DisplayName ?? "TBD",
                                RoundName = GetRoundName(match.Round),
                                ScheduledTime = match.ScheduledTime,
                                StreamUrl = match.StreamUrl
                            });
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading matches");
        }
    }

    private LiveMatch ConvertToLiveMatch(Match match)
    {
        return new LiveMatch
        {
            MatchId = match.Id,
            Player1Name = match.Player1?.DisplayName ?? "TBD",
            Player2Name = match.Player2?.DisplayName ?? "TBD",
            Player1Score = match.Result?.Player1Score ?? 0,
            Player2Score = match.Result?.Player2Score ?? 0,
            RoundName = GetRoundName(match.Round),
            ElapsedTime = match.StartedTime.HasValue
                ? _timeProvider.UtcNow - match.StartedTime.Value
                : null,
            StreamUrl = match.StreamUrl,
            Status = match.Status,
            IsWinnersBracket = match.IsWinnersBracket
        };
    }

    private string GetRoundName(int round)
    {
        return round switch
        {
            1 => "Round 1",
            2 => "Round 2",
            3 => "Round 3",
            4 => "Quarterfinals",
            5 => "Semifinals",
            6 => "Finals",
            7 => "Grand Finals",
            _ => $"Round {round}"
        };
    }

    private void UpdateTournamentStats()
    {
        if (CurrentTournament is null) return;

        TotalMatches = CurrentTournament.Matches.Count;
        CompletedMatches = CurrentTournament.Matches.Count(m => m.Status == MatchStatus.Completed);
        MatchesInProgress = CurrentTournament.Matches.Count(m => m.Status == MatchStatus.InProgress);
        PendingMatches = CurrentTournament.Matches.Count(m => m.Status == MatchStatus.Scheduled);

        // Update top participants (for leaderboard)
        var sortedParticipants = CurrentTournament.Participants
            .OrderByDescending(p => p.Wins)
            .ThenBy(p => p.Losses)
            .Take(8)
            .ToList();

        TopParticipants.Clear();
        foreach (var participant in sortedParticipants)
        {
            TopParticipants.Add(participant);
        }
    }

    private async Task HandleMatchUpdateAsync(LiveMatch match)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            var existingMatch = LiveMatches.FirstOrDefault(m => m.MatchId == match.MatchId);
            if (existingMatch != null)
            {
                var index = LiveMatches.IndexOf(existingMatch);
                LiveMatches[index] = match;
            }
            else if (match.Status == MatchStatus.InProgress)
            {
                LiveMatches.Add(match);
            }

            _notificationService.ShowInfo("Match Update",
                $"{match.Player1Name} vs {match.Player2Name}: {match.Player1Score} - {match.Player2Score}");
        });
    }

    private void StartPollingTimer()
    {
        _pollingTimer?.Stop();
        _pollingTimer = new System.Timers.Timer(30000); // 30 seconds
        _pollingTimer.Elapsed += async (s, e) =>
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (CurrentTournament != null)
                {
                    await LoadTournamentDataAsync(CurrentTournament.Id);
                }
            });
        };
        _pollingTimer.AutoReset = true;
        _pollingTimer.Start();
    }

    /// <summary>
    /// Selects a match to view details.
    /// </summary>
    [RelayCommand]
    private async Task ViewMatchDetailsAsync(LiveMatch? match)
    {
        if (match is null || CurrentTournament is null) return;

        SelectedMatch = match;

        // Navigate to match detail view
        // This would use a navigation service in production
        // In a real implementation, use the DI container or navigation service
        await _dialogService.ShowInfoAsync("Match Details",
            $"Opening match: {match.Player1Name} vs {match.Player2Name}\n" +
            $"Tournament: {CurrentTournament.Name}");
    }

    /// <summary>
    /// Opens the stream URL.
    /// </summary>
    [RelayCommand]
    private async Task OpenStreamAsync(string? streamUrl)
    {
        if (string.IsNullOrEmpty(streamUrl)) return;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = streamUrl,
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

    /// <summary>
    /// Toggles bracket visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleBracket()
    {
        ShowBracket = !ShowBracket;
    }

    public void Dispose()
    {
        _liveUpdatesSubscription?.Dispose();
        _pollingTimer?.Stop();
        _pollingTimer?.Dispose();
        _matchUpdatesSubject.Dispose();
    }
}

/// <summary>
/// Represents a live match with real-time updates.
/// </summary>
public class LiveMatch
{
    public Guid MatchId { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public string Player2Name { get; set; } = string.Empty;
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public string RoundName { get; set; } = string.Empty;
    public TimeSpan? ElapsedTime { get; set; }
    public string? StreamUrl { get; set; }
    public MatchStatus Status { get; set; }
    public bool IsWinnersBracket { get; set; }
}

/// <summary>
/// Represents a completed match result.
/// </summary>
public class MatchResultModel
{
    public Guid MatchId { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public string Player2Name { get; set; } = string.Empty;
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public string WinnerName { get; set; } = string.Empty;
    public string RoundName { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Represents an upcoming scheduled match.
/// </summary>
public class UpcomingMatch
{
    public Guid MatchId { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public string Player2Name { get; set; } = string.Empty;
    public string RoundName { get; set; } = string.Empty;
    public DateTime? ScheduledTime { get; set; }
    public string? StreamUrl { get; set; }
}
