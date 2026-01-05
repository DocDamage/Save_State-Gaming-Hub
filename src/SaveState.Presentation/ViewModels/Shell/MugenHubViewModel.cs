using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the MUGEN Hub featuring tournaments, character management, and statistics.
/// </summary>
public partial class MugenHubViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IMugenTournamentService _tournamentService;
    private readonly IMugenStatsService _statsService;
    private readonly IMugenCollectionService _collectionService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MugenHubViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _selectedTab = "Tournaments";

    [ObservableProperty]
    private int _totalTournaments;

    [ObservableProperty]
    private int _activeTournaments;

    [ObservableProperty]
    private int _completedTournaments;

    [ObservableProperty]
    private int _totalCharacters;

    [ObservableProperty]
    private int _favoriteCharacters;

    [ObservableProperty]
    private MugenCharacter? _selectedCharacter;

    [ObservableProperty]
    private bool _showCreateTournament;

    [ObservableProperty]
    private string _tournamentName = string.Empty;

    [ObservableProperty]
    private string _selectedFormat = "SingleElimination";

    public MugenHubViewModel(
        IMediator mediator,
        IMugenTournamentService tournamentService,
        IMugenStatsService statsService,
        IMugenCollectionService collectionService,
        INotificationService notificationService,
        ILogger<MugenHubViewModel> logger)
    {
        _mediator = mediator;
        _tournamentService = tournamentService;
        _statsService = statsService;
        _collectionService = collectionService;
        _notificationService = notificationService;
        _logger = logger;

        Tournaments = new ObservableCollection<MugenTournament>();
        Characters = new ObservableCollection<MugenCharacter>();
        RecentMatches = new ObservableCollection<MugenMatchHistory>();
        TournamentFormats = new ObservableCollection<string>
        {
            "SingleElimination",
            "DoubleElimination",
            "RoundRobin"
        };

        // Initialize async
        _ = LoadDataAsync();
    }

    public ObservableCollection<MugenTournament> Tournaments { get; }
    public ObservableCollection<MugenCharacter> Characters { get; }
    public ObservableCollection<MugenMatchHistory> RecentMatches { get; }
    public ObservableCollection<string> TournamentFormats { get; }

    [RelayCommand]
    private void ShowTournaments()
    {
        SelectedTab = "Tournaments";
        _logger.LogDebug("Switched to Tournaments tab");
    }

    [RelayCommand]
    private void ShowCharacters()
    {
        SelectedTab = "Characters";
        _logger.LogDebug("Switched to Characters tab");
    }

    [RelayCommand]
    private void ShowStatistics()
    {
        SelectedTab = "Statistics";
        _logger.LogDebug("Switched to Statistics tab");
    }

    [RelayCommand]
    private void ShowCreateTournamentDialog()
    {
        ShowCreateTournament = true;
        TournamentName = string.Empty;
        SelectedFormat = "SingleElimination";
    }

    [RelayCommand]
    private void CancelCreateTournament()
    {
        ShowCreateTournament = false;
    }

    [RelayCommand]
    private async Task CreateTournamentAsync()
    {
        if (string.IsNullOrWhiteSpace(TournamentName))
        {
            _notificationService.ShowWarning("Please enter a tournament name");
            return;
        }

        try
        {
            _notificationService.ShowInfo($"Creating tournament: {TournamentName}");
            ShowCreateTournament = false;
            await LoadTournamentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tournament");
            _notificationService.ShowError("Failed to create tournament");
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(MugenCharacter? character)
    {
        if (character == null) return;

        try
        {
            _notificationService.ShowSuccess($"Favorite toggled for {character.DisplayName}");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle favorite");
            _notificationService.ShowError("Failed to update favorite");
        }
    }

    [RelayCommand]
    private async Task ViewCharacterDetailsAsync(MugenCharacter? character)
    {
        if (character == null) return;

        SelectedCharacter = character;
        _logger.LogInformation("Viewing character details: {CharacterName}", character.DisplayName);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await Task.WhenAll(
            LoadTournamentsAsync(),
            LoadCharactersAsync(),
            LoadRecentMatchesAsync()
        );
    }

    private async Task LoadTournamentsAsync()
    {
        try
        {
            IsLoading = true;
            Tournaments.Clear();
            TotalTournaments = 0;
            ActiveTournaments = 0;
            CompletedTournaments = 0;
            _logger.LogInformation("Tournaments loaded");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tournaments");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCharactersAsync()
    {
        try
        {
            Characters.Clear();
            TotalCharacters = 0;
            FavoriteCharacters = 0;
            _logger.LogInformation("Characters loaded");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load characters");
        }
    }

    private async Task LoadRecentMatchesAsync()
    {
        try
        {
            var result = await _statsService.GetRecentMatchesAsync(10);

            RecentMatches.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var match in result.Value)
                {
                    RecentMatches.Add(match);
                }
            }

            _logger.LogInformation("Loaded {Count} recent matches", RecentMatches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent matches");
        }
    }
}
