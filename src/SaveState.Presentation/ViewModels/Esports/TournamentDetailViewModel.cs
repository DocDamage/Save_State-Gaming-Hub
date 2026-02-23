using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.TournamentManagement.Models;
using SaveState.Core.TournamentManagement.Services;
using SaveState.Core.UserManagement.Services;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Esports;

/// <summary>
/// ViewModel for the tournament detail view.
/// </summary>
public partial class TournamentDetailViewModel : ObservableObject
{
    private readonly ITournamentManagementService _tournamentService;
    private readonly IDialogService _dialogService;
    private readonly IUserContextService _userContext;

    [ObservableProperty]
    private Tournament? _tournament;

    [ObservableProperty]
    private TournamentBracket? _bracket;

    [ObservableProperty]
    private ObservableCollection<TournamentParticipant> _participants = new();

    [ObservableProperty]
    private ObservableCollection<TournamentMatch> _matches = new();

    [ObservableProperty]
    private TournamentParticipant? _currentUserParticipant;

    [ObservableProperty]
    private bool _isOrganizer;

    [ObservableProperty]
    private bool _isRegistered;

    [ObservableProperty]
    private bool _canCheckIn;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private TournamentStandings? _standings;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Tab names for the detail view.
    /// </summary>
    public string[] Tabs => new[] { "Overview", "Bracket", "Participants", "Matches", "Rules" };

    public TournamentDetailViewModel(
        ITournamentManagementService tournamentService,
        IDialogService dialogService,
        IUserContextService userContext)
    {
        _tournamentService = tournamentService;
        _dialogService = dialogService;
        _userContext = userContext;
    }

    [RelayCommand]
    private async Task LoadTournamentAsync(Guid tournamentId)
    {
        StatusMessage = "Loading tournament...";

        var result = await _tournamentService.GetTournamentAsync(
            tournamentId.ToString(),
            CancellationToken.None);

        if (result.IsSuccess && result.Value != null)
        {
            Tournament = result.Value;
            IsOrganizer = Tournament.OrganizerId == _userContext.CurrentUserId?.ToString();
            CurrentUserParticipant = Tournament.Participants
                .FirstOrDefault(p => p.UserId == _userContext.CurrentUserId?.ToString());
            IsRegistered = CurrentUserParticipant != null;
            CanCheckIn = IsRegistered &&
                        CurrentUserParticipant?.Status == ParticipantStatus.Registered &&
                        Tournament.Status == TournamentStatus.RegistrationClosed;

            // Load participants
            Participants.Clear();
            foreach (var participant in Tournament.Participants.OrderBy(p => p.Seed))
            {
                Participants.Add(participant);
            }

            // Load matches
            Matches.Clear();
            foreach (var match in Tournament.Matches.OrderBy(m => m.Round).ThenBy(m => m.MatchNumber))
            {
                Matches.Add(match);
            }

            // Load bracket
            await LoadBracketAsync(tournamentId.ToString());

            // Load standings
            await LoadStandingsAsync(tournamentId.ToString());

            StatusMessage = string.Empty;
        }
        else
        {
            StatusMessage = $"Failed to load tournament: {result.Error}";
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (Tournament == null) return;

        var result = await _tournamentService.RegisterParticipantAsync(
            Tournament.Id,
            _userContext.CurrentUserId?.ToString() ?? string.Empty,
            _userContext.CurrentUsername ?? "Unknown",
            CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadTournamentAsync(Guid.Parse(Tournament.Id));
        }
        else
        {
            await _dialogService.ShowErrorAsync("Registration Failed", result.Error ?? "Unknown error");
        }
    }

    [RelayCommand]
    private async Task UnregisterAsync()
    {
        if (Tournament == null || CurrentUserParticipant == null) return;

        var result = await _tournamentService.UnregisterParticipantAsync(
            Tournament.Id,
            CurrentUserParticipant.Id,
            CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadTournamentAsync(Guid.Parse(Tournament.Id));
        }
        else
        {
            await _dialogService.ShowErrorAsync("Error", result.Error ?? "Unknown error");
        }
    }

    [RelayCommand]
    private async Task CheckInAsync()
    {
        if (Tournament == null || CurrentUserParticipant == null) return;

        var result = await _tournamentService.CheckInParticipantAsync(
            Tournament.Id,
            CurrentUserParticipant.Id,
            CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadTournamentAsync(Guid.Parse(Tournament.Id));
        }
        else
        {
            await _dialogService.ShowErrorAsync("Check-in Failed", result.Error ?? "Unknown error");
        }
    }

    [RelayCommand]
    private async Task StartTournamentAsync()
    {
        if (Tournament == null) return;

        if (!IsOrganizer)
        {
            await _dialogService.ShowErrorAsync("Unauthorized", "Only the organizer can start the tournament.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Start Tournament",
            "Are you sure you want to start the tournament? Registration will close.",
            "Start",
            "Cancel");

        if (!confirmed) return;

        var result = await _tournamentService.StartTournamentAsync(
            Tournament.Id,
            CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadTournamentAsync(Guid.Parse(Tournament.Id));
        }
        else
        {
            await _dialogService.ShowErrorAsync("Error", result.Error ?? "Unknown error");
        }
    }

    [RelayCommand]
    private async Task StartMatchAsync(TournamentMatch? match)
    {
        if (match == null) return;
        // Launch game with match configuration
        StatusMessage = $"Starting match {match.MatchNumber}...";
        await Task.Delay(500); // Placeholder for actual implementation
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task ReportResultAsync(TournamentMatch? match)
    {
        if (match == null || Tournament == null) return;

        var result = await _dialogService.ShowMatchResultDialogAsync(match);
        if (result != null)
        {
            var reportResult = await _tournamentService.ReportMatchResultAsync(
                Tournament.Id,
                match.Id,
                result.Player1Score,
                result.Player2Score,
                _userContext.CurrentUserId?.ToString() ?? string.Empty,
                CancellationToken.None);

            if (reportResult.IsSuccess)
            {
                await LoadTournamentAsync(Guid.Parse(Tournament.Id));
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", reportResult.Error ?? "Failed to report result");
            }
        }
    }

    [RelayCommand]
    private async Task EditTournamentAsync()
    {
        if (Tournament == null) return;
        await _dialogService.ShowInformationAsync("Edit Tournament", "Edit functionality coming soon!");
    }

    [RelayCommand]
    private async Task DeleteTournamentAsync()
    {
        if (Tournament == null) return;

        if (!IsOrganizer)
        {
            await _dialogService.ShowErrorAsync("Unauthorized", "Only the organizer can delete the tournament.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Tournament",
            $"Are you sure you want to delete '{Tournament.Name}'? This cannot be undone.",
            "Delete",
            "Cancel");

        if (confirmed)
        {
            var result = await _tournamentService.DeleteTournamentAsync(
                Tournament.Id,
                CancellationToken.None);

            if (result.IsFailure)
            {
                await _dialogService.ShowErrorAsync("Error", result.Error ?? "Failed to delete tournament");
            }
        }
    }

    [RelayCommand]
    private async Task ShareTournamentAsync()
    {
        if (Tournament == null) return;

        var shareText = $"Join my tournament: {Tournament.Name} - {Tournament.GameName}";
        // Copy to clipboard or open share dialog
        await _dialogService.ShowInformationAsync("Share Tournament", $"Share link copied: {shareText}");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (Tournament != null)
        {
            await LoadTournamentAsync(Guid.Parse(Tournament.Id));
        }
    }

    private async Task LoadBracketAsync(string tournamentId)
    {
        var result = await _tournamentService.GetBracketAsync(tournamentId, CancellationToken.None);
        if (result.IsSuccess)
        {
            Bracket = result.Value;
        }
    }

    private async Task LoadStandingsAsync(string tournamentId)
    {
        var result = await _tournamentService.GetStandingsAsync(tournamentId, CancellationToken.None);
        if (result.IsSuccess)
        {
            Standings = result.Value;
        }
    }
}

/// <summary>
/// ViewModel for participant display in the tournament detail view.
/// </summary>
public class ParticipantViewModel : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Seed { get; set; }
    public ParticipantStatus Status { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }

    public string StatusDisplay => Status.ToString().Replace("Registered", "Registered").Replace("CheckedIn", "Checked In");
    public string RecordDisplay => $"{Wins}-{Losses}";
}

/// <summary>
/// ViewModel for match display in the tournament detail view.
/// </summary>
public class MatchViewModel : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public int Round { get; set; }
    public int MatchNumber { get; set; }
    public string? Participant1Name { get; set; }
    public string? Participant2Name { get; set; }
    public int? Participant1Score { get; set; }
    public int? Participant2Score { get; set; }
    public MatchStatus Status { get; set; }
    public string? WinnerName { get; set; }

    public string MatchDisplay => $"Match {MatchNumber}";
    public string ScoreDisplay => Participant1Score.HasValue && Participant2Score.HasValue
        ? $"{Participant1Score} - {Participant2Score}"
        : "vs";
}
