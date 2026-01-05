using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Options;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Configuration;
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
        MugenHubViewModel hubViewModel)
    {
        _mediator = mediator;
        _mugenOptions = mugenOptions.Value;

        // Initialize sections
        var hub = new MugenHubSectionAdapter(hubViewModel) { Id = "Hub", Name = "Hub", Icon = "🏠", Title = "MUGEN Hub" };
        var roster = new MugenRosterViewModel(mediator, mugenOptions) { Id = "Roster", Name = "Roster", Icon = "👥", Title = "Character Roster" };
        var deathBattle = new MugenDeathBattleViewModel(mediator) { Id = "DeathBattle", Name = "Death Battle", Icon = "💀", Title = "Death Battle Simulator" };
        var downloads = new MugenDownloadsViewModel() { Id = "Downloads", Name = "Downloads", Icon = "📥", Title = "Asset Downloader" };
        var stats = new MugenStatsViewModel(mediator) { Id = "Stats", Name = "Stats", Icon = "📊", Title = "Statistics" };

        _sections["Hub"] = hub;
        _sections["Roster"] = roster;
        _sections["DeathBattle"] = deathBattle;
        _sections["Downloads"] = downloads;
        _sections["Stats"] = stats;

        MugenSections.Add(hub);
        MugenSections.Add(roster);
        MugenSections.Add(deathBattle);
        MugenSections.Add(downloads);
        MugenSections.Add(stats);

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
    public MugenDownloadsViewModel Downloads => (MugenDownloadsViewModel)_sections["Downloads"];
    public MugenStatsViewModel Stats => (MugenStatsViewModel)_sections["Stats"];

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

    public IRelayCommand<MugenCharacterSummaryDto> SelectPlayerCommand { get; }
    public IAsyncRelayCommand<MugenSectionViewModelBase> SelectSectionCommand { get; }
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
