using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.TournamentManagement.Models;
using SaveState.Core.TournamentManagement.Services;
using SaveState.Core.UserManagement.Services;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Esports;

/// <summary>
/// ViewModel for the tournament list view.
/// </summary>
public partial class TournamentListViewModel : ObservableObject
{
    private readonly ITournamentManagementService _tournamentService;
    private readonly IDialogService _dialogService;
    private readonly IUserContextService _userContext;

    [ObservableProperty]
    private ObservableCollection<TournamentListItem> _tournaments = new();

    [ObservableProperty]
    private TournamentListItem? _selectedTournament;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private TournamentStatus? _statusFilter;

    [ObservableProperty]
    private TournamentFormat? _formatFilter;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isOrganizer;

    /// <summary>
    /// Available tournament statuses for filtering.
    /// </summary>
    public IEnumerable<TournamentStatus> AvailableStatuses => Enum.GetValues<TournamentStatus>();

    /// <summary>
    /// Available tournament formats for filtering.
    /// </summary>
    public IEnumerable<TournamentFormat> AvailableFormats => Enum.GetValues<TournamentFormat>();

    public TournamentListViewModel(
        ITournamentManagementService tournamentService,
        IDialogService dialogService,
        IUserContextService userContext)
    {
        _tournamentService = tournamentService;
        _dialogService = dialogService;
        _userContext = userContext;
    }

    [RelayCommand]
    private async Task LoadTournamentsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        Tournaments.Clear();

        try
        {
            var result = await _tournamentService.ListTournamentsAsync(
                status: StatusFilter,
                ct: CancellationToken.None);

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var tournament in result.Value)
                {
                    var item = MapToListItem(tournament);
                    if (string.IsNullOrEmpty(SearchQuery) || 
                        item.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                        item.GameName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!FormatFilter.HasValue || item.Format == FormatFilter.Value)
                        {
                            Tournaments.Add(item);
                        }
                    }
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateTournamentAsync()
    {
        var result = await _dialogService.ShowCreateTournamentDialogAsync();
        if (result != null)
        {
            var createResult = await _tournamentService.CreateTournamentAsync(
                new CreateTournamentRequest
                {
                    Name = result.Name,
                    Description = result.Description,
                    GameId = result.GameId,
                    Format = result.Format,
                    RegistrationStart = result.RegistrationStart,
                    RegistrationEnd = result.RegistrationEnd,
                    TournamentStart = result.TournamentStart,
                    MaxParticipants = result.MaxParticipants,
                    Rules = result.Rules,
                    InitialPrizePool = result.PrizePool
                },
                _userContext.CurrentUserId?.ToString() ?? string.Empty,
                CancellationToken.None);

            if (createResult.IsSuccess)
            {
                await LoadTournamentsAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to create tournament: {createResult.Error}");
            }
        }
    }

    [RelayCommand]
    private async Task EditTournamentAsync(TournamentListItem? tournament)
    {
        if (tournament == null) return;
        // Navigate to edit view or show edit dialog
        await _dialogService.ShowInformationAsync("Edit Tournament", $"Editing {tournament.Name}");
    }

    [RelayCommand]
    private async Task ViewTournamentAsync(TournamentListItem? tournament)
    {
        if (tournament == null) return;
        // Navigate to detail view
        SelectedTournament = tournament;
    }

    [RelayCommand]
    private async Task RegisterAsync(TournamentListItem? tournament)
    {
        if (tournament == null) return;

        var result = await _tournamentService.RegisterParticipantAsync(
            tournament.Id.ToString(),
            _userContext.CurrentUserId?.ToString() ?? string.Empty,
            _userContext.CurrentUsername ?? "Player",
            CancellationToken.None);

        if (result.IsSuccess)
        {
            tournament.RegisteredCount++;
            tournament.IsRegistered = true;
            OnPropertyChanged(nameof(Tournaments));
        }
        else
        {
            await _dialogService.ShowErrorAsync("Registration Failed", result.Error ?? "Unknown error");
        }
    }

    [RelayCommand]
    private async Task UnregisterAsync(TournamentListItem? tournament)
    {
        if (tournament == null) return;

        // Find participant ID
        var tourneyResult = await _tournamentService.GetTournamentAsync(tournament.Id.ToString());
        if (tourneyResult.IsSuccess && tourneyResult.Value != null)
        {
            var participant = tourneyResult.Value.Participants
                .FirstOrDefault(p => p.UserId.Equals(_userContext.CurrentUserId?.ToString(), StringComparison.OrdinalIgnoreCase));

            if (participant != null)
            {
                var result = await _tournamentService.UnregisterParticipantAsync(
                    tournament.Id.ToString(),
                    participant.Id,
                    CancellationToken.None);

                if (result.IsSuccess)
                {
                    tournament.RegisteredCount--;
                    tournament.IsRegistered = false;
                    OnPropertyChanged(nameof(Tournaments));
                }
            }
        }
    }

    [RelayCommand]
    private async Task DeleteTournamentAsync(TournamentListItem? tournament)
    {
        if (tournament == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Tournament",
            $"Are you sure you want to delete '{tournament.Name}'? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (confirmed)
        {
            var result = await _tournamentService.DeleteTournamentAsync(
                tournament.Id.ToString(),
                CancellationToken.None);

            if (result.IsSuccess)
            {
                Tournaments.Remove(tournament);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to delete tournament: {result.Error}");
            }
        }
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        _ = LoadTournamentsAsync();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchQuery = string.Empty;
        StatusFilter = null;
        FormatFilter = null;
        _ = LoadTournamentsAsync();
    }

    private TournamentListItem MapToListItem(Tournament tournament)
    {
        var isRegistered = tournament.Participants.Any(p => p.UserId.Equals(_userContext.CurrentUserId?.ToString(), StringComparison.OrdinalIgnoreCase));
        var isOrganizer = tournament.OrganizerId.Equals(_userContext.CurrentUserId?.ToString(), StringComparison.OrdinalIgnoreCase);

        return new TournamentListItem
        {
            Id = Guid.Parse(tournament.Id),
            Name = tournament.Name,
            GameName = tournament.GameName,
            GameCover = null, // Would be populated from game service
            Format = tournament.Format,
            Status = tournament.Status,
            StartDate = tournament.TournamentStart,
            RegisteredCount = tournament.CurrentParticipants,
            MaxParticipants = tournament.MaxParticipants,
            IsRegistered = isRegistered,
            IsOrganizer = isOrganizer,
            StatusColor = GetStatusColor(tournament.Status)
        };
    }

    private static string GetStatusColor(TournamentStatus status) => status switch
    {
        TournamentStatus.RegistrationOpen => "Green",
        TournamentStatus.InProgress => "Blue",
        TournamentStatus.Completed => "Gray",
        TournamentStatus.Cancelled => "Red",
        TournamentStatus.Paused => "Orange",
        TournamentStatus.RegistrationClosed => "Yellow",
        TournamentStatus.Draft => "Purple",
        _ => "Gray"
    };
}

/// <summary>
/// List item display model for tournaments.
/// </summary>
public class TournamentListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string? GameCover { get; set; }
    public TournamentFormat Format { get; set; }
    public TournamentStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public int RegisteredCount { get; set; }
    public int MaxParticipants { get; set; }
    public bool IsRegistered { get; set; }
    public bool IsOrganizer { get; set; }
    public string StatusColor { get; set; } = "Green";

    /// <summary>
    /// Gets the participant count display text.
    /// </summary>
    public string ParticipantDisplay => $"{RegisteredCount}/{MaxParticipants}";

    /// <summary>
    /// Gets the formatted start date display.
    /// </summary>
    public string StartDateDisplay => StartDate.ToString("MMM d, yyyy");

    /// <summary>
    /// Gets the format display name.
    /// </summary>
    public string FormatDisplay => Format.ToString().Replace("Elimination", " Elimination");
}
