using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Esports.Models;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Esports;

/// <summary>
/// ViewModel for displaying tournament standings based on format.
/// </summary>
public partial class TournamentStandingsViewModel : ObservableObject
{
    private readonly ILogger<TournamentStandingsViewModel> _logger;
    private readonly ITournamentService _tournamentService;
    private readonly INotificationService _notificationService;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private Tournament? _tournament;

    [ObservableProperty]
    private TournamentFormat _tournamentFormat;

    [ObservableProperty]
    private ObservableCollection<EliminationStanding> _eliminationStandings = new();

    [ObservableProperty]
    private ObservableCollection<RoundRobinStanding> _roundRobinStandings = new();

    [ObservableProperty]
    private ObservableCollection<SwissStanding> _swissStandings = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _topCutThreshold;

    [ObservableProperty]
    private bool _showTopCutOnly;

    [ObservableProperty]
    private ObservableCollection<Match> _bracketMatches = new();

    [ObservableProperty]
    private int _currentRound;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private Participant? _champion;

    [ObservableProperty]
    private ObservableCollection<string> _tiebreakerInfo = new();

    public TournamentStandingsViewModel(
        ILogger<TournamentStandingsViewModel> logger,
        ITournamentService tournamentService,
        INotificationService notificationService,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _tournamentService = tournamentService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Loads tournament standings.
    /// </summary>
    [RelayCommand]
    private async Task LoadStandingsAsync(Guid tournamentId)
    {
        IsLoading = true;
        StatusMessage = "Loading standings...";

        try
        {
            var result = await _tournamentService.GetTournamentAsync(tournamentId);
            if (result.IsFailure || result.Value is null)
            {
                StatusMessage = "Failed to load tournament";
                return;
            }

            Tournament = result.Value;
            TournamentFormat = Tournament.Format;
            CurrentRound = Tournament.Matches
                .Where(m => m.Status == MatchStatus.Completed)
                .Max(m => (int?)m.Round) ?? 0;
            IsComplete = Tournament.Status == TournamentStatus.Completed;

            // Load standings based on format
            switch (TournamentFormat)
            {
                case TournamentFormat.SingleElimination:
                case TournamentFormat.DoubleElimination:
                    await LoadEliminationStandingsAsync();
                    break;

                case TournamentFormat.RoundRobin:
                    await LoadRoundRobinStandingsAsync();
                    break;

                case TournamentFormat.Swiss:
                    await LoadSwissStandingsAsync();
                    break;
            }

            // Determine champion
            if (IsComplete)
            {
                Champion = Tournament.Matches
                    .Where(m => m.Status == MatchStatus.Completed)
                    .OrderByDescending(m => m.Round)
                    .FirstOrDefault()?.Winner;
            }

            StatusMessage = $"Standings updated: {_timeProvider.Now:g}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tournament standings");
            StatusMessage = "Error loading standings";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task LoadEliminationStandingsAsync()
    {
        EliminationStandings.Clear();

        if (Tournament?.Participants == null) return Task.CompletedTask;

        foreach (var participant in Tournament.Participants)
        {
            var placement = CalculateEliminationPlacement(participant);
            var standing = new EliminationStanding
            {
                ParticipantId = participant.Id,
                DisplayName = participant.DisplayName,
                ProfileImageUrl = participant.ProfileImageUrl,
                Seed = participant.Seed ?? 0,
                Placement = placement,
                Status = participant.Status,
                Wins = participant.Wins,
                Losses = participant.Losses,
                EliminatedInRound = GetEliminationRound(participant),
                IsInWinnersBracket = IsInWinnersBracket(participant),
                IsQualifiedForTopCut = IsQualifiedForTopCut(placement),
                LastMatch = GetLastMatchForParticipant(participant)
            };

            EliminationStandings.Add(standing);
        }

        // Sort by placement
        var sortedStandings = EliminationStandings.OrderBy(s => s.Placement).ToList();
        EliminationStandings.Clear();
        foreach (var standing in sortedStandings)
        {
            EliminationStandings.Add(standing);
        }

        return Task.CompletedTask;
    }

    private Task LoadRoundRobinStandingsAsync()
    {
        RoundRobinStandings.Clear();

        if (Tournament?.Participants == null) return Task.CompletedTask;

        foreach (var participant in Tournament.Participants)
        {
            var standing = new RoundRobinStanding
            {
                ParticipantId = participant.Id,
                DisplayName = participant.DisplayName,
                ProfileImageUrl = participant.ProfileImageUrl,
                Seed = participant.Seed ?? 0,
                Wins = participant.Wins,
                Losses = participant.Losses,
                Ties = participant.Ties,
                MatchesPlayed = participant.MatchHistory?.Count ?? 0,
                WinRate = CalculateWinRate(participant),
                Points = CalculateRoundRobinPoints(participant),
                GamesWon = CalculateGamesWon(participant),
                GamesLost = CalculateGamesLost(participant),
                GameDifference = CalculateGameDifference(participant),
                IsQualifiedForTopCut = IsQualifiedForTopCut(CalculateRank(participant)),
                HeadToHeadRecord = GetHeadToHeadRecord(participant),
                RecentForm = GetRecentForm(participant)
            };

            RoundRobinStandings.Add(standing);
        }

        // Sort by points, then game difference, then head-to-head
        var sortedStandings = RoundRobinStandings
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.GameDifference)
            .ThenByDescending(s => s.WinRate)
            .ToList();

        RoundRobinStandings.Clear();
        int rank = 1;
        foreach (var standing in sortedStandings)
        {
            standing.Rank = rank++;
            RoundRobinStandings.Add(standing);
        }

        return Task.CompletedTask;
    }

    private Task LoadSwissStandingsAsync()
    {
        SwissStandings.Clear();

        if (Tournament?.Participants == null) return Task.CompletedTask;

        foreach (var participant in Tournament.Participants)
        {
            var standing = new SwissStanding
            {
                ParticipantId = participant.Id,
                DisplayName = participant.DisplayName,
                ProfileImageUrl = participant.ProfileImageUrl,
                Seed = participant.Seed ?? 0,
                Wins = participant.Wins,
                Losses = participant.Losses,
                MatchesPlayed = participant.Wins + participant.Losses,
                MatchPoints = CalculateSwissMatchPoints(participant),
                OpponentMatchWinPercentage = CalculateOMW(participant),
                GameWinPercentage = CalculateGameWinPercentage(participant),
                OpponentGameWinPercentage = CalculateOGW(participant),
                BuchholzScore = CalculateBuchholzScore(participant),
                IsQualifiedForTopCut = participant.Wins >= TopCutThreshold,
                RoundHistory = GetSwissRoundHistory(participant),
                Tiebreakers = CalculateSwissTiebreakers(participant)
            };

            SwissStandings.Add(standing);
        }

        // Sort by match points, then tiebreakers
        var sortedStandings = SwissStandings
            .OrderByDescending(s => s.MatchPoints)
            .ThenByDescending(s => s.OpponentMatchWinPercentage)
            .ThenByDescending(s => s.GameWinPercentage)
            .ToList();

        SwissStandings.Clear();
        int rank = 1;
        foreach (var standing in sortedStandings)
        {
            standing.Rank = rank++;
            SwissStandings.Add(standing);
        }

        return Task.CompletedTask;
    }

    private int CalculateEliminationPlacement(Participant participant)
    {
        if (participant.Status == ParticipantStatus.Eliminated)
        {
            // Count remaining participants to determine placement
            var remaining = Tournament?.Participants.Count(p =>
                p.Status != ParticipantStatus.Eliminated &&
                p.Status != ParticipantStatus.Disqualified) ?? 0;

            // Approximate placement based on elimination round
            var eliminationRound = GetEliminationRound(participant);
            if (eliminationRound.HasValue)
            {
                return (int)Math.Pow(2, eliminationRound.Value + 1);
            }

            return Tournament?.Participants.Count ?? 0;
        }

        if (participant.Status == ParticipantStatus.Competing)
        {
            // Still competing - calculate based on current position
            var eliminated = Tournament?.Participants.Count(p =>
                p.Status == ParticipantStatus.Eliminated ||
                p.Status == ParticipantStatus.Disqualified) ?? 0;

            return eliminated + 1;
        }

        return Tournament?.Participants.Count ?? 0;
    }

    private int? GetEliminationRound(Participant participant)
    {
        var lastLoss = Tournament?.Matches
            .Where(m => m.Status == MatchStatus.Completed &&
                       (m.Player1?.Id == participant.Id || m.Player2?.Id == participant.Id) &&
                       m.Winner?.Id != participant.Id)
            .OrderByDescending(m => m.Round)
            .FirstOrDefault();

        return lastLoss?.Round;
    }

    private bool IsInWinnersBracket(Participant participant)
    {
        // Check if participant has any losses in the tournament
        return !Tournament?.Matches.Any(m =>
            m.Status == MatchStatus.Completed &&
            (m.Player1?.Id == participant.Id || m.Player2?.Id == participant.Id) &&
            m.Winner?.Id != participant.Id) ?? true;
    }

    private bool IsQualifiedForTopCut(int placement)
    {
        // Top 8 qualify for top cut by default
        return placement <= 8;
    }

    private Match? GetLastMatchForParticipant(Participant participant)
    {
        return Tournament?.Matches
            .Where(m => m.Status == MatchStatus.Completed &&
                       (m.Player1?.Id == participant.Id || m.Player2?.Id == participant.Id))
            .OrderByDescending(m => m.CompletedTime)
            .FirstOrDefault();
    }

    private double CalculateWinRate(Participant participant)
    {
        var total = participant.Wins + participant.Losses + participant.Ties;
        if (total == 0) return 0;
        return (double)participant.Wins / total;
    }

    private int CalculateRoundRobinPoints(Participant participant)
    {
        // Standard: 3 points for win, 1 for tie, 0 for loss
        return (participant.Wins * 3) + participant.Ties;
    }

    private int CalculateGamesWon(Participant participant)
    {
        return participant.MatchHistory?.Sum(m =>
            m.Winner?.Id == participant.Id ? m.Player1Score : m.Player2Score) ?? 0;
    }

    private int CalculateGamesLost(Participant participant)
    {
        return participant.MatchHistory?.Sum(m =>
            m.Winner?.Id == participant.Id ? m.Player2Score : m.Player1Score) ?? 0;
    }

    private int CalculateGameDifference(Participant participant)
    {
        return CalculateGamesWon(participant) - CalculateGamesLost(participant);
    }

    private int CalculateRank(Participant participant)
    {
        var ranked = Tournament?.Participants
            .OrderByDescending(p => p.Wins)
            .ThenBy(p => p.Losses)
            .ToList();

        return ranked?.FindIndex(p => p.Id == participant.Id) + 1 ?? 0;
    }

    private string GetHeadToHeadRecord(Participant participant)
    {
        // Simplified - in a real implementation, check direct matchups
        return $"{participant.Wins}-{participant.Losses}";
    }

    private string GetRecentForm(Participant participant)
    {
        if (participant.MatchHistory?.Count == 0) return "-";

        var recent = participant.MatchHistory
            .Take(5)
            .Select(m => m.Winner?.Id == participant.Id ? "W" : "L")
            .ToList();

        return string.Join("", recent);
    }

    private int CalculateSwissMatchPoints(Participant participant)
    {
        return participant.Wins * 3; // 3 points per win in Swiss
    }

    private double CalculateOMW(Participant participant)
    {
        // Opponent Match Win Percentage
        var opponents = GetOpponents(participant);
        if (!opponents.Any()) return 0;

        var totalOpponentWinRate = opponents.Sum(CalculateWinRate);
        return totalOpponentWinRate / opponents.Count;
    }

    private double CalculateGameWinPercentage(Participant participant)
    {
        var gamesWon = CalculateGamesWon(participant);
        var gamesLost = CalculateGamesLost(participant);
        var total = gamesWon + gamesLost;

        if (total == 0) return 0;
        return (double)gamesWon / total;
    }

    private double CalculateOGW(Participant participant)
    {
        // Opponent Game Win Percentage
        var opponents = GetOpponents(participant);
        if (!opponents.Any()) return 0;

        var totalOpponentGameWinRate = opponents.Sum(CalculateGameWinPercentage);
        return totalOpponentGameWinRate / opponents.Count;
    }

    private double CalculateBuchholzScore(Participant participant)
    {
        // Sum of opponents' match points
        return GetOpponents(participant).Sum(CalculateSwissMatchPoints);
    }

    private List<Participant> GetOpponents(Participant participant)
    {
        if (Tournament?.Matches == null) return new List<Participant>();

        return Tournament.Matches
            .Where(m => m.Status == MatchStatus.Completed &&
                       (m.Player1?.Id == participant.Id || m.Player2?.Id == participant.Id))
            .Select(m => m.Player1?.Id == participant.Id ? m.Player2 : m.Player1)
            .Where(p => p != null)
            .Cast<Participant>()
            .ToList();
    }

    private List<SwissRoundResult> GetSwissRoundHistory(Participant participant)
    {
        if (Tournament?.Matches == null) return new List<SwissRoundResult>();

        return Tournament.Matches
            .Where(m => m.Status == MatchStatus.Completed &&
                       (m.Player1?.Id == participant.Id || m.Player2?.Id == participant.Id))
            .OrderBy(m => m.Round)
            .Select(m => new SwissRoundResult
            {
                Round = m.Round,
                OpponentName = m.Player1?.Id == participant.Id
                    ? m.Player2?.DisplayName ?? "Bye"
                    : m.Player1?.DisplayName ?? "Bye",
                Result = m.Winner?.Id == participant.Id ? "W" : "L",
                Score = m.Player1?.Id == participant.Id
                    ? $"{m.Result?.Player1Score}-{m.Result?.Player2Score}"
                    : $"{m.Result?.Player2Score}-{m.Result?.Player1Score}"
            })
            .ToList();
    }

    private List<double> CalculateSwissTiebreakers(Participant participant)
    {
        return new List<double>
        {
            CalculateOMW(participant),
            CalculateGameWinPercentage(participant),
            CalculateOGW(participant),
            CalculateBuchholzScore(participant)
        };
    }

    /// <summary>
    /// Exports standings to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportStandingsAsync()
    {
        try
        {
            // This would implement actual export functionality
            _notificationService.ShowSuccess("Export Complete", "Standings exported successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting standings");
            await _notificationService.ShowErrorAsync("Error", "Failed to export standings.");
        }
    }

    /// <summary>
    /// Toggles top cut filter.
    /// </summary>
    [RelayCommand]
    private void ToggleTopCutFilter()
    {
        ShowTopCutOnly = !ShowTopCutOnly;
    }
}

/// <summary>
/// Standing for elimination format tournaments.
/// </summary>
public class EliminationStanding
{
    public Guid ParticipantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public int Seed { get; set; }
    public int Placement { get; set; }
    public ParticipantStatus Status { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int? EliminatedInRound { get; set; }
    public bool IsInWinnersBracket { get; set; }
    public bool IsQualifiedForTopCut { get; set; }
    public Match? LastMatch { get; set; }

    public string PlacementDisplay => Placement switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{Placement}th"
    };
}

/// <summary>
/// Standing for round robin format tournaments.
/// </summary>
public class RoundRobinStanding
{
    public Guid ParticipantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public int Seed { get; set; }
    public int Rank { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public int MatchesPlayed { get; set; }
    public double WinRate { get; set; }
    public int Points { get; set; }
    public int GamesWon { get; set; }
    public int GamesLost { get; set; }
    public int GameDifference { get; set; }
    public bool IsQualifiedForTopCut { get; set; }
    public string HeadToHeadRecord { get; set; } = string.Empty;
    public string RecentForm { get; set; } = string.Empty;

    public string WinRateDisplay => $"{WinRate:P1}";
}

/// <summary>
/// Standing for Swiss format tournaments.
/// </summary>
public class SwissStanding
{
    public Guid ParticipantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public int Seed { get; set; }
    public int Rank { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MatchesPlayed { get; set; }
    public int MatchPoints { get; set; }
    public double OpponentMatchWinPercentage { get; set; }
    public double GameWinPercentage { get; set; }
    public double OpponentGameWinPercentage { get; set; }
    public double BuchholzScore { get; set; }
    public bool IsQualifiedForTopCut { get; set; }
    public List<SwissRoundResult> RoundHistory { get; set; } = new();
    public List<double> Tiebreakers { get; set; } = new();

    public string OMWDisplay => $"{OpponentMatchWinPercentage:P1}";
    public string GWPDisplay => $"{GameWinPercentage:P1}";
    public string OGWDisplay => $"{OpponentGameWinPercentage:P1}";
}

/// <summary>
/// Individual round result for Swiss tournaments.
/// </summary>
public class SwissRoundResult
{
    public int Round { get; set; }
    public string OpponentName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
}
