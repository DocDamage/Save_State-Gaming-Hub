using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.Mugen;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.UI.ViewModels;

public partial class CrossGameBattleViewModel : ViewModelBase
{
    private readonly CrossGameBattleService _battleService;
    private readonly MugenService _mugenService;

    [ObservableProperty]
    private ObservableCollection<MugenFighter> _availableFighters = new();

    [ObservableProperty]
    private ObservableCollection<BattleCharacter> _battleRoster = new();

    [ObservableProperty]
    private ObservableCollection<BattleMatch> _matchHistory = new();

    [ObservableProperty]
    private BattleCharacter? _selectedPlayer1;

    [ObservableProperty]
    private BattleCharacter? _selectedPlayer2;

    [ObservableProperty]
    private string _selectedStage = "Training Stage";

    [ObservableProperty]
    private BattleMode _selectedMode = BattleMode.Versus;

    [ObservableProperty]
    private BattleMatch? _lastMatch;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public string[] Stages { get; } = { "Training Stage", "City Arena", "Forest Clearing", "Space Station" };
    public BattleMode[] BattleModes { get; } = Enum.GetValues<BattleMode>();

    public IRelayCommand<MugenFighter> AddToRosterCommand { get; }
    public IRelayCommand FightCommand { get; }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand ClearHistoryCommand { get; }

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public CrossGameBattleViewModel(CrossGameBattleService battleService, MugenService mugenService)
    {
        _battleService = battleService ?? throw new ArgumentNullException(nameof(battleService));
        _mugenService = mugenService ?? throw new ArgumentNullException(nameof(mugenService));

        AddToRosterCommand = new RelayCommand<MugenFighter>(AddToRoster);
        FightCommand = new RelayCommand(StartFight, CanFight);
        RefreshCommand = new RelayCommand(Refresh);
        ClearHistoryCommand = new RelayCommand(ClearHistory);

        Refresh();
    }

    /// <summary>
    /// Design-time/fallback constructor.
    /// </summary>
    public CrossGameBattleViewModel() : this(new CrossGameBattleService(), new MugenService())
    {
    }

    private bool CanFight() => SelectedPlayer1 != null && SelectedPlayer2 != null && SelectedPlayer1 != SelectedPlayer2;

    private void AddToRoster(MugenFighter? fighter)
    {
        if (fighter == null) return;
        _battleService.AddToBattleRoster(fighter);
        RefreshRoster();
        StatusMessage = $"Added {fighter.Name} to battle roster!";
    }

    private void StartFight()
    {
        if (SelectedPlayer1 == null || SelectedPlayer2 == null) return;

        try
        {
            var match = _battleService.SimulateMatch(SelectedPlayer1, SelectedPlayer2, SelectedStage, SelectedMode);
            LastMatch = match;
            MatchHistory.Insert(0, match);
            StatusMessage = $"🏆 {match.Winner} wins!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Battle error: {ex.Message}";
        }
    }

    private void Refresh()
    {
        AvailableFighters.Clear();
        foreach (var fighter in _mugenService.GetFighters())
        {
            AvailableFighters.Add(fighter);
        }
        RefreshRoster();
        RefreshHistory();
    }

    private void RefreshRoster()
    {
        BattleRoster.Clear();
        foreach (var character in _battleService.GetRoster())
        {
            BattleRoster.Add(character);
        }
    }

    private void RefreshHistory()
    {
        MatchHistory.Clear();
        foreach (var match in _battleService.GetHistory().OrderByDescending(m => m.Timestamp).Take(20))
        {
            MatchHistory.Add(match);
        }
    }

    private void ClearHistory()
    {
        _battleService.ClearHistory();
        MatchHistory.Clear();
        StatusMessage = "History cleared";
    }

    partial void OnSelectedPlayer1Changed(BattleCharacter? value) => FightCommand.NotifyCanExecuteChanged();
    partial void OnSelectedPlayer2Changed(BattleCharacter? value) => FightCommand.NotifyCanExecuteChanged();
}
