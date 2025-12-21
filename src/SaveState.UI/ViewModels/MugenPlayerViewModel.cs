using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.Mugen;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.UI.ViewModels;

public partial class MugenPlayerViewModel : ViewModelBase
{
    private readonly MugenService _mugenService;
    private readonly MugenTournamentService _tournamentService;

    [ObservableProperty]
    private ObservableCollection<MugenFighter> _fighters = new();

    [ObservableProperty]
    private ObservableCollection<MugenStage> _stages = new();

    [ObservableProperty]
    private ObservableCollection<string> _roster = new();

    [ObservableProperty]
    private ObservableCollection<MugenTournament> _tournaments = new();

    [ObservableProperty]
    private MugenFighter? _selectedFighter;

    [ObservableProperty]
    private MugenStage? _selectedStage;

    [ObservableProperty]
    private MugenTournament? _selectedTournament;

    [ObservableProperty]
    private string _newTournamentName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isEngineInstalled;

    public IRelayCommand LaunchEngineCommand { get; }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand CreateTournamentCommand { get; }
    public IRelayCommand<string> StartTournamentCommand { get; }

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public MugenPlayerViewModel(MugenService mugenService, MugenTournamentService tournamentService)
    {
        _mugenService = mugenService ?? throw new ArgumentNullException(nameof(mugenService));
        _tournamentService = tournamentService ?? throw new ArgumentNullException(nameof(tournamentService));

        LaunchEngineCommand = new RelayCommand(LaunchEngine);
        RefreshCommand = new RelayCommand(Refresh);
        CreateTournamentCommand = new RelayCommand(CreateTournament, CanCreateTournament);
        StartTournamentCommand = new RelayCommand<string>(StartTournament);

        Refresh();
    }

    /// <summary>
    /// Design-time/fallback constructor.
    /// </summary>
    public MugenPlayerViewModel() : this(new MugenService(), new MugenTournamentService())
    {
    }

    private bool CanCreateTournament() => !string.IsNullOrWhiteSpace(NewTournamentName) && Roster.Count >= 2;

    private void LaunchEngine()
    {
        try
        {
            _mugenService.LaunchEngine();
            StatusMessage = "Launching Ikemen GO...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Launch failed: {ex.Message}";
        }
    }

    private void Refresh()
    {
        Fighters.Clear();
        foreach (var fighter in _mugenService.GetFighters())
        {
            Fighters.Add(fighter);
        }

        Stages.Clear();
        foreach (var stage in _mugenService.GetStages())
        {
            Stages.Add(stage);
        }

        Roster.Clear();
        foreach (var name in _mugenService.GetRoster())
        {
            Roster.Add(name);
        }

        Tournaments.Clear();
        foreach (var t in _tournamentService.GetAllTournaments())
        {
            Tournaments.Add(t);
        }

        IsEngineInstalled = Fighters.Count > 0 || Stages.Count > 0;
        StatusMessage = $"{Fighters.Count} fighters, {Stages.Count} stages loaded";
    }

    private void CreateTournament()
    {
        if (string.IsNullOrWhiteSpace(NewTournamentName)) return;
        
        var participants = Roster.Take(16).ToList();
        if (participants.Count < 2)
        {
            StatusMessage = "Need at least 2 fighters in roster";
            return;
        }

        var tournament = _tournamentService.CreateTournament(NewTournamentName, participants);
        Tournaments.Add(tournament);
        NewTournamentName = string.Empty;
        StatusMessage = $"Created tournament: {tournament.Name}";
    }

    private void StartTournament(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _tournamentService.StartTournament(id);
        Refresh();
        StatusMessage = "Tournament started!";
    }

    partial void OnNewTournamentNameChanged(string value) => CreateTournamentCommand.NotifyCanExecuteChanged();
}
