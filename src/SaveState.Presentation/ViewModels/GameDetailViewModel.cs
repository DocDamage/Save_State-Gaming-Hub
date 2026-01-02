using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Application.GameLibrary.ReadModels;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Presentation.ViewModels;

public partial class GameDetailViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    public GameDetailViewModel(IMediator mediator, GameId gameId)
    {
        _mediator = mediator;
        GameId = gameId;

        // Initialize commands
        CloseCommand = new RelayCommand(Close);
        LaunchGameCommand = new AsyncRelayCommand(LaunchGameAsync);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenMoreMenuCommand = new RelayCommand(OpenMoreMenu);
        AddToFavoritesCommand = new AsyncRelayCommand(AddToFavoritesAsync);
        AddToBacklogCommand = new AsyncRelayCommand(AddToBacklogAsync);
        ExportGameDataCommand = new AsyncRelayCommand(ExportGameDataAsync);
        UninstallCommand = new AsyncRelayCommand(UninstallAsync);
        GenerateAiBriefingCommand = new AsyncRelayCommand(GenerateAiBriefingAsync);

        // Initialize tab view models
        OverviewTab = new GameOverviewTabViewModel(mediator, gameId);
        SaveStatesTab = new GameSaveStatesTabViewModel(mediator, gameId);
        AchievementsTab = new GameAchievementsTabViewModel(mediator, gameId);
        SessionsTab = new GameSessionsTabViewModel(mediator, gameId);
        NotesTab = new GameNotesTabViewModel(mediator, gameId);
        ModsTab = new GameModsTabViewModel(mediator, gameId);
        ScreenshotsTab = new GameScreenshotsTabViewModel(mediator, gameId);
        PerformanceTab = new GamePerformanceTabViewModel(mediator, gameId);

        // Load game data
        _ = LoadGameAsync();
    }

    public GameId GameId { get; }

    [ObservableProperty]
    private GameDetail? game;

    [ObservableProperty]
    private string? aiBriefing;

    [ObservableProperty]
    private int selectedTabIndex;

    // Tab View Models
    public GameOverviewTabViewModel OverviewTab { get; }
    public GameSaveStatesTabViewModel SaveStatesTab { get; }
    public GameAchievementsTabViewModel AchievementsTab { get; }
    public GameSessionsTabViewModel SessionsTab { get; }
    public GameNotesTabViewModel NotesTab { get; }
    public GameModsTabViewModel ModsTab { get; }
    public GameScreenshotsTabViewModel ScreenshotsTab { get; }
    public GamePerformanceTabViewModel PerformanceTab { get; }

    // Commands
    public IRelayCommand CloseCommand { get; }
    public IAsyncRelayCommand LaunchGameCommand { get; }
    public IRelayCommand OpenSettingsCommand { get; }
    public IRelayCommand OpenMoreMenuCommand { get; }
    public IAsyncRelayCommand AddToFavoritesCommand { get; }
    public IAsyncRelayCommand AddToBacklogCommand { get; }
    public IAsyncRelayCommand ExportGameDataCommand { get; }
    public IAsyncRelayCommand UninstallCommand { get; }
    public IAsyncRelayCommand GenerateAiBriefingCommand { get; }

    // Computed Properties
    public bool CanGenerateAiBriefing => string.IsNullOrEmpty(AiBriefing);

    private async Task LoadGameAsync()
    {
        var query = new GetGameDetailsQuery { GameId = GameId };
        var result = await _mediator.Send(query);

        if (result.IsSuccess)
        {
            Game = result.Value;
            // TODO: Load AI briefing from cache/service
            AiBriefing = null; // Will be loaded separately
        }
    }

    private void Close()
    {
        // TODO: Navigate back to library
    }

    private async Task LaunchGameAsync()
    {
        // TODO: Implement game launching
        await Task.CompletedTask;
    }

    private void OpenSettings()
    {
        // TODO: Open game settings dialog
    }

    private void OpenMoreMenu()
    {
        // TODO: Open context menu with more options
    }

    private async Task AddToFavoritesAsync()
    {
        // TODO: Add to favorites collection
        await Task.CompletedTask;
    }

    private async Task AddToBacklogAsync()
    {
        // TODO: Add to backlog
        await Task.CompletedTask;
    }

    private async Task ExportGameDataAsync()
    {
        // TODO: Export game data
        await Task.CompletedTask;
    }

    private async Task UninstallAsync()
    {
        // TODO: Uninstall game
        await Task.CompletedTask;
    }

    private async Task GenerateAiBriefingAsync()
    {
        // TODO: Generate AI briefing using orchestrator
        await Task.CompletedTask;
    }
}

// Tab View Models
public partial class GameOverviewTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly GameId _gameId;

    public GameOverviewTabViewModel(IMediator mediator, GameId gameId)
    {
        _mediator = mediator;
        _gameId = gameId;

        // TODO: Load data from services
        RecentSessions = new ObservableCollection<GameSessionSummary>();
        GameFiles = new ObservableCollection<GameFileInfo>();
    }

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private TimeSpan? hltbMainStory;

    [ObservableProperty]
    private TimeSpan? hltbMainPlusExtras;

    [ObservableProperty]
    private TimeSpan? hltbCompletionist;

    [ObservableProperty]
    private TimeSpan? hltbAllStyles;

    [ObservableProperty]
    private string? systemRequirements;

    public ObservableCollection<GameSessionSummary> RecentSessions { get; }
    public ObservableCollection<GameFileInfo> GameFiles { get; }
}

// Supporting classes
public class GameSessionSummary
{
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int AchievementsUnlocked { get; set; }
}

public class GameFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

public class GameSaveStatesTabViewModel : ObservableObject
{
    public GameSaveStatesTabViewModel(IMediator mediator, GameId gameId)
    {
        // TODO: Implement save states tab logic
    }
}

public class GameAchievementsTabViewModel : ObservableObject
{
    public GameAchievementsTabViewModel(IMediator mediator, GameId gameId)
    {
        // TODO: Implement achievements tab logic
    }
}

public class GameSessionsTabViewModel : ObservableObject
{
    public GameSessionsTabViewModel(IMediator mediator, GameId gameId)
    {
        // TODO: Implement sessions tab logic
    }
}

public class GameNotesTabViewModel : ObservableObject
{
    public GameNotesTabViewModel(IMediator mediator, GameId gameId)
    {
        // TODO: Implement notes tab logic
    }
}

public class GameModsTabViewModel : ObservableObject
{
    public GameModsTabViewModel(IMediator mediator, GameId gameId)
    {
        // TODO: Implement mods tab logic
    }
}

public class GameScreenshotsTabViewModel : ObservableObject
{
    public GameScreenshotsTabViewModel(IMediator mediator, GameId gameId)
    {
        // TODO: Implement screenshots tab logic
    }
}

public class GamePerformanceTabViewModel : ObservableObject
{
    public GamePerformanceTabViewModel(IMediator mediator, GameId gameId)
    {
        // TODO: Implement performance tab logic
    }
}
