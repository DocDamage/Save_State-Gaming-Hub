using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Common.Services;
using SaveState.Core.Configuration;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Services;
using SaveState.Presentation.ViewModels.Shell.Mugen;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the MUGEN tab (Shell).
/// Coordinates various MUGEN sections and shared state.
/// </summary>
public partial class MugenViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly MugenOptions _mugenOptions;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _player1;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _player2;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private MugenSectionViewModelBase? _selectedSection;

    public ObservableCollection<MugenSectionViewModelBase> MugenSections { get; } = new();

    private readonly Dictionary<string, MugenSectionViewModelBase> _sections = new();

    public MugenViewModel(
        IMediator mediator,
        IOptions<MugenOptions> mugenOptions,
        MugenHubViewModel hubViewModel,
        IMugenStatsService statsService,
        IMugenFusionService fusionService,
        IMugenRosterService rosterService,
        IMugenTournamentService tournamentService,
        IMugenCoachService coachService,
        IMugenTrainingService trainingService,
        IMugenMatchHistoryRepository matchHistoryRepository,
        IMugenCollectionService collectionService,
        IMugenDiscoveryService discoveryService,
        IMugenEloService eloService,
        IMugenMoveListService moveListService,
        IMatchPredictionEngine predictionEngine,
        IDeathMatchSimulator matchSimulator,
        IMugenLauncher launcher,
        IMugenConfigService configService,
        MoveCreationViewModel moveCreationViewModel,
        MachineLearningViewModel machineLearningViewModel,
        ILoggerFactory loggerFactory,
        ITimeProvider timeProvider)
    {
        _mediator = mediator;
        _mugenOptions = mugenOptions.Value;

        // Initialize sections
        var hub = new MugenHubSectionAdapter(hubViewModel) { Id = "Hub", Name = "Hub", Icon = "🏠", Title = "MUGEN Hub" };
        var roster = new MugenRosterViewModel(mediator, mugenOptions, rosterService) { Id = "Roster", Name = "Roster", Icon = "👥", Title = "Character Roster" };
        var deathBattle = new MugenDeathBattleViewModel(mediator) { Id = "DeathBattle", Name = "Death Battle", Icon = "💀", Title = "Death Battle Simulator" };
        var tournament = new MugenTournamentViewModel(mediator, tournamentService, collectionService, predictionEngine, matchSimulator, timeProvider) { Id = "Tournament", Name = "Tournament", Icon = "🏆", Title = "Tournament Mode" };
        var training = new MugenTrainingViewModel(mediator, trainingService) { Id = "Training", Name = "Training", Icon = "🥋", Title = "Training Mode" };
        var replay = new MugenReplayViewModel(mediator, matchHistoryRepository, launcher, coachService, mugenOptions) { Id = "Replays", Name = "Replays", Icon = "🎬", Title = "Replay Theater" };
        var coach = new MugenCoachViewModel(mediator, coachService, moveListService, collectionService) { Id = "Coach", Name = "Coach", Icon = "🎓", Title = "AI Dojo" };
        var fusion = new MugenFusionViewModel(mediator, fusionService) { Id = "Fusion", Name = "Fusion", Icon = "🧬", Title = "Character Fusion" };
        var engineMods = new MugenEngineModsViewModel(mediator, configService) { Id = "EngineMods", Name = "Engine Mods", Icon = "🛠️", Title = "Engine Modifications" };
        var downloads = new MugenDownloadsViewModel(discoveryService) { Id = "Downloads", Name = "Downloads", Icon = "📥", Title = "Asset Downloader" };
        var stats = new MugenStatsViewModel(mediator, statsService, eloService, collectionService, matchHistoryRepository, loggerFactory.CreateLogger<MugenStatsViewModel>()) { Id = "Stats", Name = "Stats", Icon = "📊", Title = "Statistics" };
        var moveCreation = moveCreationViewModel;
        moveCreation.Id = "MoveCreation";
        moveCreation.Name = "Move Creation";
        moveCreation.Icon = "🎨";
        moveCreation.Title = "Professional Move Creation Engine";
        var machineLearning = machineLearningViewModel;
        machineLearning.Id = "MachineLearning";
        machineLearning.Name = "AI & Analytics";
        machineLearning.Icon = "🤖";
        machineLearning.Title = "Machine Learning & Predictive Analytics";

        _sections["Hub"] = hub;
        _sections["Roster"] = roster;
        _sections["DeathBattle"] = deathBattle;
        _sections["Tournament"] = tournament;
        _sections["Training"] = training;
        _sections["Replays"] = replay;
        _sections["Coach"] = coach;
        _sections["Fusion"] = fusion;
        _sections["EngineMods"] = engineMods;
        _sections["Downloads"] = downloads;
        _sections["Stats"] = stats;
        _sections["MoveCreation"] = moveCreation;
        _sections["MachineLearning"] = machineLearning;

        MugenSections.Add(hub);
        MugenSections.Add(roster);
        MugenSections.Add(deathBattle);
        MugenSections.Add(tournament);
        MugenSections.Add(training);
        MugenSections.Add(replay);
        MugenSections.Add(coach);
        MugenSections.Add(fusion);
        MugenSections.Add(engineMods);
        MugenSections.Add(downloads);
        MugenSections.Add(stats);
        MugenSections.Add(moveCreation);
        MugenSections.Add(machineLearning);

        SelectedSection = hub;

        SelectPlayerCommand = new RelayCommand<MugenCharacterSummaryDto>(SelectPlayer);
        SelectSectionCommand = new AsyncRelayCommand<MugenSectionViewModelBase>(SelectSectionAsync);
        LoadCharactersCommand = new AsyncRelayCommand(LoadCharactersAsync);

        // Initial init
        if (SelectedSection != null)
        {
            SelectedSection.IsActive = true;
            _ = SelectedSection.InitializeAsync();
        }
    }

    public MugenHubSectionAdapter Hub => (MugenHubSectionAdapter)_sections["Hub"];
    public MugenRosterViewModel Roster => (MugenRosterViewModel)_sections["Roster"];
    public MugenDeathBattleViewModel DeathBattle => (MugenDeathBattleViewModel)_sections["DeathBattle"];
    public MugenFusionViewModel Fusion => (MugenFusionViewModel)_sections["Fusion"];
    public MugenDownloadsViewModel Downloads => (MugenDownloadsViewModel)_sections["Downloads"];
    public MugenStatsViewModel Stats => (MugenStatsViewModel)_sections["Stats"];
    public MoveCreationViewModel MoveCreation => (MoveCreationViewModel)_sections["MoveCreation"];
    public MachineLearningViewModel MachineLearning => (MachineLearningViewModel)_sections["MachineLearning"];

    private MainViewModel? _parent;

    public void SetParent(MainViewModel parent)
    {
        _parent = parent;
    }

    [RelayCommand]
    private void GoBack()
    {
        _parent?.NavigateToGameLibrary();
    }

    public string Title => "MUGEN";

    public MugenSectionViewModelBase? CurrentSection => SelectedSection;

    public IRelayCommand<MugenCharacterSummaryDto> SelectPlayerCommand { get; }
    public IAsyncRelayCommand<MugenSectionViewModelBase> SelectSectionCommand { get; }

    [RelayCommand]
    private async Task SetSection(string sectionId)
    {
        if (_sections.TryGetValue(sectionId, out var section))
        {
            await SelectSectionAsync(section);
            OnPropertyChanged(nameof(CurrentSection));
        }
    }

    public IAsyncRelayCommand LoadCharactersCommand { get; }

    private void SelectPlayer(MugenCharacterSummaryDto? character)
    {
        if (character == null) return;

        if (Player1 == null) Player1 = character;
        else if (Player2 == null && character != Player1) Player2 = character;
        else if (character == Player1) Player1 = null;
        else if (character == Player2) Player2 = null;
        else Player1 = character;

        // Sync players to sections that need them
        if (_sections.TryGetValue("DeathBattle", out var dbBase) && dbBase is MugenDeathBattleViewModel db)
        {
            db.Player1 = Player1;
            db.Player2 = Player2;
        }

        if (_sections.TryGetValue("Fusion", out var fusionBase) && fusionBase is MugenFusionViewModel fusion)
        {
            fusion.BaseCharacter = Player1;
            fusion.FusionPartner = Player2;
        }
    }

    private async Task SelectSectionAsync(MugenSectionViewModelBase? section)
    {
        if (section == null) return;

        if (SelectedSection != null) SelectedSection.IsActive = false;

        SelectedSection = section;
        SelectedSection.IsActive = true;

        await SelectedSection.InitializeAsync();
    }

    private async Task LoadCharactersAsync()
    {
         if (_sections.TryGetValue("Roster", out var rosterBase) && rosterBase is MugenRosterViewModel roster)
        {
            await roster.ScanCharactersCommand.ExecuteAsync(null);
            StatusMessage = roster.StatusMessage;
        }
    }
}
