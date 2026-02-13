using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen.DTOs;
using SaveState.Core.Configuration;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell.Mugen;
// Use ValueObjects as canonical types for ambiguous names
using MugenNetplayLobby = SaveState.Core.Mugen.ValueObjects.MugenNetplayLobby;
using MugenAssetEntry = SaveState.Core.Mugen.ValueObjects.MugenAssetEntry;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the MUGEN Hub featuring tournaments, character management, and statistics.
/// </summary>
public partial class MugenHubViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    private readonly IMugenStatsService _statsService;
    private readonly IMugenCollectionService _collectionService;
    private readonly IMugenMatchHistoryRepository _matchHistoryRepository;

    private readonly IMugenMoveListService _moveListService;
    private readonly IMugenNetplayService _netplayService;

    private readonly IMugenAssetPreviewService _assetPreviewService;
    private readonly IMugenCompatibilityService _compatibilityService;
    private readonly IMugenEloService _eloService;
    private readonly IDeathMatchSimulator _deathMatchSimulator;
    private readonly MugenOptions _mugenOptions;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MugenHubViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _selectedTab = "Statistics";

    [ObservableProperty]
    private int _totalCharacters;

    [ObservableProperty]
    private int _favoriteCharacters;

    [ObservableProperty]
    private MugenCharacter? _selectedCharacter;

    [ObservableProperty]
    private string? _selectedCharacterPreviewPath;

    [ObservableProperty]
    private string? _coachingAdvice;

    [ObservableProperty]
    private bool _showCoachingOverlay;

    [ObservableProperty]
    private MugenStageSummary? _selectedStage;

    [ObservableProperty]
    private MugenReplaySummary? _selectedReplay;

    [ObservableProperty]
    private MugenTierEntry? _selectedTierEntry;

    [ObservableProperty]
    private bool _isMoveListLoading;

    [ObservableProperty]
    private string _moveListStatus = "Select a character to load moves.";

    [ObservableProperty]
    private bool _isAssetLoading;

    [ObservableProperty]
    private string _assetStatus = "Select a character to preview assets.";

    [ObservableProperty]
    private bool _isCompatibilityLoading;

    [ObservableProperty]
    private string _compatibilityStatus = "Run analysis to check compatibility.";

    [ObservableProperty]
    private bool _isNetplayLoading;

    [ObservableProperty]
    private string _netplayStatus = "Ready";

    [ObservableProperty]
    private bool _isSimulationLoading;

    [ObservableProperty]
    private string _simulationStatus = "Ready";

    [ObservableProperty]
    private int _simulationParticipants = 8;

    [ObservableProperty]
    private int _simulationsPerMatch = 200;

    [ObservableProperty]
    private string? _simulationWinnerName;

    [ObservableProperty]
    private double _simulationWinnerConfidence;

    [ObservableProperty]
    private int _spectatorCredits = 1000;

    [ObservableProperty]
    private int _betAmount = 50;

    [ObservableProperty]
    private MugenCharacter? _selectedBetCharacter;

    [ObservableProperty]
    private string _betStatus = "No bet placed.";

    public MugenHubViewModel(
        IMediator mediator,
        IMugenStatsService statsService,
        IMugenCollectionService collectionService,
        IMugenMatchHistoryRepository matchHistoryRepository,
        IMugenRosterService rosterService,
        IMugenMoveListService moveListService,
        IMugenNetplayService netplayService,
        IMugenAssetPreviewService assetPreviewService,
        IMugenCompatibilityService compatibilityService,
        IMugenEloService eloService,
        IDeathMatchSimulator deathMatchSimulator,
        IOptions<MugenOptions> mugenOptions,
        INotificationService notificationService,
        ILogger<MugenHubViewModel> logger)
    {
        _mediator = mediator;
        _statsService = statsService;
        _collectionService = collectionService;
        _matchHistoryRepository = matchHistoryRepository;

        _moveListService = moveListService;
        _netplayService = netplayService;

        _assetPreviewService = assetPreviewService;
        _compatibilityService = compatibilityService;
        _eloService = eloService;
        _deathMatchSimulator = deathMatchSimulator;
        _mugenOptions = mugenOptions.Value;
        _notificationService = notificationService;
        _logger = logger;

        Characters = new ObservableCollection<MugenCharacter>();
        RecentMatches = new ObservableCollection<MugenMatchSummary>();
        Stages = new ObservableCollection<MugenStageSummary>();
        Replays = new ObservableCollection<MugenReplaySummary>();
        TierList = new ObservableCollection<MugenTierEntry>();

        MoveList = new ObservableCollection<MugenMoveEntryDto>();
        NetplayLobbies = new ObservableCollection<MugenNetplayLobby>();
        AssetEntries = new ObservableCollection<MugenAssetEntry>();
        CompatibilityIssues = new ObservableCollection<MugenCompatibilityIssue>();
        CompatibilityFixes = new ObservableCollection<MugenCompatibilityFix>();
        EloRatings = new ObservableCollection<MugenEloRating>();
        SimulationMatches = new ObservableCollection<SimulatedMatchSummary>();
        BetHistory = new ObservableCollection<BetRecord>();
        BetLeaderboard = new ObservableCollection<BetLeaderboardEntry>();
        TournamentFormats = new ObservableCollection<string>
        {
            "SingleElimination",
            "DoubleElimination",
            "RoundRobin"
        };

        // Initialize async
        _ = LoadDataAsync();
    }

    public ObservableCollection<MugenCharacter> Characters { get; }
    public ObservableCollection<MugenMatchSummary> RecentMatches { get; }
    public ObservableCollection<MugenStageSummary> Stages { get; }
    public ObservableCollection<MugenReplaySummary> Replays { get; }
    public ObservableCollection<MugenTierEntry> TierList { get; }

    public ObservableCollection<MugenMoveEntryDto> MoveList { get; }
    public ObservableCollection<MugenNetplayLobby> NetplayLobbies { get; }

    public ObservableCollection<MugenAssetEntry> AssetEntries { get; }
    public ObservableCollection<MugenCompatibilityIssue> CompatibilityIssues { get; }
    public ObservableCollection<MugenCompatibilityFix> CompatibilityFixes { get; }
    public ObservableCollection<MugenEloRating> EloRatings { get; }
    public ObservableCollection<SimulatedMatchSummary> SimulationMatches { get; }
    public ObservableCollection<BetRecord> BetHistory { get; }
    public ObservableCollection<BetLeaderboardEntry> BetLeaderboard { get; }
    public ObservableCollection<string> TournamentFormats { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<MugenCharacter> FilteredCharacters { get; } = new();

    public bool HasSelectedCharacter => SelectedCharacter != null;

    // Fight Settings
    [ObservableProperty]
    private int _roundsToWin = 2;

    [ObservableProperty]
    private int _timeLimit = 99;

    [ObservableProperty]
    private int _aiDifficulty = 3; // 1-8

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
    private void ShowNetplay()
    {
        SelectedTab = "Netplay";
        _logger.LogDebug("Switched to Netplay tab");
    }

    [RelayCommand]
    private void ShowStatistics()
    {
        SelectedTab = "Statistics";
        _logger.LogDebug("Switched to Statistics tab");
    }

    [RelayCommand]
    private void ShowSettings()
    {
        SelectedTab = "Settings";
        _logger.LogDebug("Switched to Settings tab");
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterCharacters();
    }

    partial void OnSelectedCharacterChanged(MugenCharacter? value)
    {
        SelectedCharacterPreviewPath = ResolvePreviewPath(value);
        OnPropertyChanged(nameof(HasSelectedCharacter));
        CompatibilityIssues.Clear();
        CompatibilityFixes.Clear();
        CompatibilityStatus = value == null ? "Select a character to analyze." : "Run analysis to check compatibility.";
        _ = LoadMoveListForSelectionAsync(value);
        _ = LoadAssetPreviewForSelectionAsync(value);
    }

    [RelayCommand]
    private async Task OpenReplayAsync(MugenReplaySummary? replay)
    {
        if (replay == null || string.IsNullOrWhiteSpace(replay.Path)) return;

        try
        {
            if (!File.Exists(replay.Path))
            {
                _notificationService.ShowWarning("Replay file not found.");
                return;
            }

            await Task.Run(() =>
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = replay.Path,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            });

            _notificationService.ShowInfo($"Opened replay: {replay.Title}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open replay");
            _notificationService.ShowError("Failed to open replay.");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
        _notificationService.ShowInfo("MUGEN Hub data refreshed");
    }

    private static string? ResolvePreviewPath(MugenCharacter? character)
    {
        if (character == null)
            return null;

        var dir = character.CharacterDirectory;
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return null;

        var candidates = new[]
        {
            "portrait.png",
            "portrait.jpg",
            "portrait.jpeg",
            "portrait.bmp",
            "preview.png",
            "preview.jpg",
            "icon.png",
            "select.png"
        };

        foreach (var file in candidates)
        {
            var path = Path.Combine(dir, file);
            if (File.Exists(path))
                return path;
        }

        return null;
    }
}

public sealed record MugenStageSummary(string Name, string Path);

public sealed record MugenReplaySummary(string Title, string Path, DateTime PlayedAt, TimeSpan Duration);

public sealed record MugenMatchSummary(string Player1Name, string Player2Name, MatchResult Result, TimeSpan Duration, DateTime PlayedAt);

public sealed record MugenTierEntry(Guid CharacterId, string Name, int Wins, int Losses, double WinRate, string Tier)
{
    public int Matches => Wins + Losses;
}

public sealed record SimulatedMatchSummary(
    string RoundName,
    string Player1Name,
    string Player2Name,
    string WinnerName,
    float Confidence,
    int SimulatedPlayer1Wins,
    int SimulatedPlayer2Wins);

public sealed record BetRecord(
    Guid CharacterId,
    string CharacterName,
    int Amount,
    bool Won,
    int CreditsAfter,
    DateTime PlacedAt)
{
    public string ResultLabel => Won ? "Won" : "Lost";
}

public sealed record BetLeaderboardEntry(
    Guid CharacterId,
    string CharacterName,
    int Bets,
    int Wins,
    int Losses)
{
    public double WinRate => Bets == 0 ? 0 : (double)Wins / Bets;
}

internal sealed class CharacterRecord
{
    public int Wins { get; set; }
    public int Losses { get; set; }
}

/// <summary>
/// Represents a compatibility issue detected in a MUGEN character.
/// </summary>
public sealed record MugenCompatibilityIssue(
    string IssueType,
    string Description,
    string Severity,
    string? SuggestedFix = null
);

/// <summary>
/// Represents a compatibility fix applied to a MUGEN character.
/// </summary>
public sealed record MugenCompatibilityFix(
    string FixType,
    string Description,
    bool Success,
    string? Details = null
);

/// <summary>
/// Represents an ELO rating for a MUGEN character.
/// </summary>
public sealed class MugenEloRating
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public int Rank { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate => (Wins + Losses) > 0 ? (double)Wins / (Wins + Losses) : 0;
}
