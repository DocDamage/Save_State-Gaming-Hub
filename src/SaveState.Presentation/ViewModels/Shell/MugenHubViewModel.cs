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
using SaveState.Core.Configuration;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell.Mugen;
using SaveState.Core.Mugen.DTOs;

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



    private void FilterCharacters()
    {
        if (FilteredCharacters == null) return;

        FilteredCharacters.Clear();
        foreach(var character in Characters)
        {
            if(string.IsNullOrWhiteSpace(SearchText) ||
               character.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                FilteredCharacters.Add(character);
            }
        }
    }



    [RelayCommand]
    private async Task ToggleFavoriteAsync(MugenCharacter? character)
    {
        if (character == null) return;

        try
        {
            var newState = !character.IsFavorite;
            var result = await _collectionService.SetFavoriteAsync(character.Id, newState);

            if (result.IsSuccess)
            {
                character.SetFavorite(newState);
                _notificationService.ShowSuccess($"{character.DisplayName} {(newState ? "added to" : "removed from")} favorites");
                await LoadDataAsync();
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to update favorite");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle favorite");
            _notificationService.ShowError("Failed to update favorite");
        }
    }



    [RelayCommand]
    private void SelectCharacter(MugenCharacter? character)
    {
        SelectedCharacter = character;
    }

    [RelayCommand]
    private void ClearSelectedCharacter()
    {
        SelectedCharacter = null;
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



    [RelayCommand]
    private async Task LoadMoveListAsync()
    {
        await LoadMoveListForSelectionAsync(SelectedCharacter);
    }

    [RelayCommand]
    private async Task LoadAssetsAsync()
    {
        await LoadAssetPreviewForSelectionAsync(SelectedCharacter);
    }

    [RelayCommand]
    private async Task OpenAssetAsync(MugenAssetEntry? asset)
    {
        if (asset == null || string.IsNullOrWhiteSpace(asset.FullPath))
            return;

        try
        {
            if (!File.Exists(asset.FullPath))
            {
                _notificationService.ShowWarning("Asset file not found.");
                return;
            }

            await Task.Run(() =>
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = asset.FullPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open asset");
            _notificationService.ShowError("Failed to open asset.");
        }
    }

    [RelayCommand]
    private async Task AnalyzeCompatibilityAsync()
    {
        if (SelectedCharacter == null)
        {
            _notificationService.ShowWarning("Select a character to analyze.");
            return;
        }

        try
        {
            IsCompatibilityLoading = true;
            CompatibilityStatus = "Analyzing compatibility...";

            var result = await _compatibilityService.AnalyzeAsync(SelectedCharacter);
            CompatibilityIssues.Clear();
            CompatibilityFixes.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var issue in result.Value.Issues)
                    CompatibilityIssues.Add(new MugenCompatibilityIssue(
                        issue.IssueType,
                        issue.Description,
                        issue.Severity,
                        issue.SuggestedFix));

                CompatibilityStatus = CompatibilityIssues.Count == 0
                    ? "No issues detected."
                    : $"{CompatibilityIssues.Count} issues found.";
            }
            else
            {
                CompatibilityStatus = result.Error ?? "Compatibility analysis failed.";
                _notificationService.ShowError(CompatibilityStatus);
            }
        }
        catch (Exception ex)
        {
            CompatibilityStatus = $"Compatibility analysis failed: {ex.Message}";
            _notificationService.ShowError(CompatibilityStatus);
        }
        finally
        {
            IsCompatibilityLoading = false;
        }
    }

    [RelayCommand]
    private async Task FixCompatibilityAsync()
    {
        if (SelectedCharacter == null)
        {
            _notificationService.ShowWarning("Select a character to fix.");
            return;
        }

        try
        {
            IsCompatibilityLoading = true;
            CompatibilityStatus = "Applying fixes...";

            var result = await _compatibilityService.FixAsync(SelectedCharacter);
            CompatibilityIssues.Clear();
            CompatibilityFixes.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var fix in result.Value.Fixes)
                    CompatibilityFixes.Add(new MugenCompatibilityFix(
                        fix.FixType,
                        fix.Description,
                        fix.Success,
                        fix.Details));

                foreach (var issue in result.Value.Issues)
                     CompatibilityIssues.Add(new MugenCompatibilityIssue(
                        issue.IssueType,
                        issue.Description,
                        issue.Severity,
                        issue.SuggestedFix));

                CompatibilityStatus = CompatibilityFixes.Count == 0
                    ? "No fixes applied."
                    : $"{CompatibilityFixes.Count} fixes applied.";
            }
            else
            {
                CompatibilityStatus = result.Error ?? "Compatibility fixes failed.";
                _notificationService.ShowError(CompatibilityStatus);
            }
        }
        catch (Exception ex)
        {
            CompatibilityStatus = $"Compatibility fixes failed: {ex.Message}";
            _notificationService.ShowError(CompatibilityStatus);
        }
        finally
        {
            IsCompatibilityLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadNetplayAsync()
    {
        try
        {
            IsNetplayLoading = true;
            NetplayStatus = "Loading lobbies...";

            var result = await _netplayService.GetLobbiesAsync();
            NetplayLobbies.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var lobby in result.Value)
                    NetplayLobbies.Add(lobby);

                NetplayStatus = $"{NetplayLobbies.Count} lobbies found.";
            }
            else
            {
                NetplayStatus = result.Error ?? "Failed to load lobbies.";
            }
        }
        catch (Exception ex)
        {
            NetplayStatus = $"Lobby load failed: {ex.Message}";
        }
        finally
        {
            IsNetplayLoading = false;
        }
    }

    [RelayCommand]
    private async Task JoinLobbyAsync(MugenNetplayLobby? lobby)
    {
        if (lobby == null)
            return;

        var result = await _netplayService.JoinLobbyAsync(lobby);
        if (result.IsSuccess)
            _notificationService.ShowSuccess($"Joining lobby: {lobby.Name}");
        else
            _notificationService.ShowError(result.Error ?? "Failed to join lobby.");
    }



    [RelayCommand]
    private async Task SimulateTournamentAsync()
    {
        try
        {
            IsSimulationLoading = true;
            SimulationStatus = "Simulating tournament...";
            SimulationWinnerName = null;
            SimulationWinnerConfidence = 0;
            SimulationMatches.Clear();

            if (Characters.Count == 0)
                await LoadCharactersAsync();

            var participants = Characters
                .Where(c => c.IsFavorite)
                .Take(Math.Max(2, SimulationParticipants))
                .Select(c => c.Id)
                .ToList();

            if (participants.Count < 2)
            {
                participants = Characters.Take(Math.Max(2, SimulationParticipants))
                    .Select(c => c.Id)
                    .ToList();
            }

            if (participants.Count < 2)
            {
                SimulationStatus = "Not enough characters to simulate.";
                return;
            }

            var betCharacter = SelectedBetCharacter;
            var betInvalid = false;
            if (betCharacter != null && !participants.Contains(betCharacter.Id))
            {
                BetStatus = "Bet character not in simulation.";
                betCharacter = null;
                betInvalid = true;
            }

            var result = await _deathMatchSimulator.SimulateTournamentAsync(
                participants,
                TournamentFormat.SingleElimination,
                Math.Max(10, SimulationsPerMatch));

            if (!result.IsSuccess || result.Value == null)
            {
                SimulationStatus = result.Error ?? "Simulation failed.";
                return;
            }

            var simulation = result.Value;
            var nameMap = Characters.ToDictionary(c => c.Id, c => c.DisplayName);

            SimulationWinnerName = nameMap.TryGetValue(simulation.PredictedWinnerId, out var winnerName)
                ? winnerName
                : simulation.PredictedWinnerName;
            SimulationWinnerConfidence = simulation.WinnerConfidence;

            var betAmount = Math.Clamp(BetAmount, 0, SpectatorCredits);
            if (betCharacter != null && betAmount > 0)
            {
                var betName = nameMap.TryGetValue(betCharacter.Id, out var bn) ? bn : betCharacter.DisplayName;
                var betWon = betCharacter.Id == simulation.PredictedWinnerId;

                if (betCharacter.Id == simulation.PredictedWinnerId)
                {
                    SpectatorCredits += betAmount;
                    BetStatus = $"Bet won. +{betAmount} credits.";
                }
                else
                {
                    SpectatorCredits = Math.Max(0, SpectatorCredits - betAmount);
                    BetStatus = $"Bet lost. -{betAmount} credits.";
                }

                BetHistory.Insert(0, new BetRecord(betCharacter.Id, betName, betAmount, betWon, SpectatorCredits, DateTime.UtcNow));
                if (BetHistory.Count > 20)
                    BetHistory.RemoveAt(BetHistory.Count - 1);
                UpdateBetLeaderboard();
            }
            else if (!betInvalid)
            {
                BetStatus = "No bet placed.";
            }

            foreach (var bracket in simulation.Brackets)
            {
                foreach (var match in bracket.Matches)
                {
                    var p1Name = nameMap.TryGetValue(match.Participant1Id, out var n1) ? n1 : match.Participant1Id.ToString();
                    var p2Name = nameMap.TryGetValue(match.Participant2Id, out var n2) ? n2 : match.Participant2Id.ToString();
                    var winner = nameMap.TryGetValue(match.PredictedWinnerId, out var wn) ? wn : match.PredictedWinnerId.ToString();

                    SimulationMatches.Add(new SimulatedMatchSummary(
                        bracket.RoundName,
                        p1Name,
                        p2Name,
                        winner,
                        match.WinConfidence,
                        match.SimulatedP1Wins,
                        match.SimulatedP2Wins));
                }
            }

            SimulationStatus = "Simulation complete.";
        }
        catch (Exception ex)
        {
            SimulationStatus = $"Simulation failed: {ex.Message}";
        }
        finally
        {
            IsSimulationLoading = false;
        }
    }



    private async Task LoadMoveListForSelectionAsync(MugenCharacter? character)
    {
        if (character == null)
        {
            MoveList.Clear();
            MoveListStatus = "Select a character to load moves.";
            return;
        }

        try
        {
            IsMoveListLoading = true;
            MoveListStatus = "Loading move list...";

            var result = await _moveListService.GetMoveListAsync(character);
            MoveList.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var entry in result.Value)
                    // Ensure DTO is compatible or map it if needed. MoveList is ObservableCollection<MugenMoveEntryDto>
                    MoveList.Add(entry);

                MoveListStatus = MoveList.Count == 0 ? "No moves found." : $"{MoveList.Count} moves loaded.";
            }
            else
            {
                MoveListStatus = result.Error ?? "Move list load failed.";
            }
        }
        catch (Exception ex)
        {
            MoveListStatus = $"Move list load failed: {ex.Message}";
        }
        finally
        {
            IsMoveListLoading = false;
        }
    }

    private async Task LoadAssetPreviewForSelectionAsync(MugenCharacter? character)
    {
        if (character == null)
        {
            AssetEntries.Clear();
            AssetStatus = "Select a character to preview assets.";
            return;
        }

        try
        {
            IsAssetLoading = true;
            AssetStatus = "Loading assets...";

            var result = await _assetPreviewService.GetAssetsAsync(character);
            AssetEntries.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var entry in result.Value)
                    AssetEntries.Add(entry);

                AssetStatus = AssetEntries.Count == 0 ? "No assets found." : $"{AssetEntries.Count} assets found.";
            }
            else
            {
                AssetStatus = result.Error ?? "Asset load failed.";
            }
        }
        catch (Exception ex)
        {
            AssetStatus = $"Asset load failed: {ex.Message}";
        }
        finally
        {
            IsAssetLoading = false;
        }
    }

    private async Task LoadEloRatingsAsync()
    {
        try
        {
            var result = await _eloService.GetRatingsAsync();
            EloRatings.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var entry in result.Value)
                    EloRatings.Add(new MugenEloRating
                    {
                        CharacterName = entry.CharacterName,
                        Rating = entry.Rating,
                        Wins = entry.Wins,
                        Losses = entry.Losses
                        // Map other fields
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ELO ratings");
        }
    }

    private void UpdateBetLeaderboard()
    {
        var leaderboard = BetHistory
            .GroupBy(bet => bet.CharacterId)
            .Select(group => new BetLeaderboardEntry(
                group.Key,
                group.First().CharacterName,
                group.Count(),
                group.Count(bet => bet.Won),
                group.Count(bet => !bet.Won)))
            .OrderByDescending(entry => entry.Wins)
            .ThenByDescending(entry => entry.Bets)
            .ToList();

        BetLeaderboard.Clear();
        foreach (var entry in leaderboard)
            BetLeaderboard.Add(entry);
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await Task.WhenAll(
                LoadCharactersAsync(),
                LoadRecentMatchesAsync(),
                LoadStagesAsync(),
                LoadReplaysAsync(),

                LoadNetplayAsync()
            );

            await Task.WhenAll(
                LoadTierListAsync(),
                LoadEloRatingsAsync()
            );
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
            var result = await _collectionService.GetRosterAsync();

            Characters.Clear();
            if (result.IsSuccess && result.Value != null)
            {
               foreach(var c in result.Value) Characters.Add(c);
            }

            FilterCharacters();

            TotalCharacters = Characters.Count;
            FavoriteCharacters = Characters.Count(c => c.IsFavorite);

            if (SelectedBetCharacter != null && Characters.All(c => c.Id != SelectedBetCharacter.Id))
                SelectedBetCharacter = null;

            _logger.LogInformation("Loaded {Count} characters", Characters.Count);
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
                var nameMap = await BuildRosterNameMapAsync();
                foreach (var match in result.Value)
                {
                    var p1Name = nameMap.TryGetValue(match.Player1CharacterId, out var n1) ? n1 : match.Player1CharacterId.ToString();
                    var p2Name = nameMap.TryGetValue(match.Player2CharacterId, out var n2) ? n2 : match.Player2CharacterId.ToString();

                    RecentMatches.Add(new MugenMatchSummary(
                        p1Name,
                        p2Name,
                        match.Result,
                        match.MatchDuration,
                        match.PlayedAt));
                }
            }

            _logger.LogInformation("Loaded {Count} recent matches", RecentMatches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent matches");
        }
    }

    private async Task<Dictionary<Guid, string>> BuildRosterNameMapAsync()
    {
        var map = new Dictionary<Guid, string>();

        foreach (var character in Characters)
        {
            map[character.Id] = character.DisplayName;
        }

        if (map.Count > 0)
            return map;

        var roster = await _collectionService.GetRosterAsync();
        if (roster.IsSuccess && roster.Value != null)
        {
            foreach (var character in roster.Value)
            {
                map[character.Id] = character.DisplayName;
            }
        }

        return map;
    }

    private async Task LoadStagesAsync()
    {
        try
        {
            var stageFiles = FindStageDefinitionFiles(_mugenOptions.StageDirectories)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var stageItems = new List<MugenStageSummary>();

            foreach (var stagePath in stageFiles)
            {
                var name = await ExtractStageNameAsync(stagePath);
                stageItems.Add(new MugenStageSummary(name, stagePath));
            }

            Stages.Clear();
            foreach (var stage in stageItems.OrderBy(s => s.Name))
            {
                Stages.Add(stage);
            }

            SelectedStage ??= Stages.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load stages");
        }
    }

    private async Task LoadReplaysAsync()
    {
        try
        {
            var replays = new List<MugenReplaySummary>();
            var nameMap = await BuildRosterNameMapAsync();

            var histories = await _matchHistoryRepository.GetMatchHistoriesAsync(pageNumber: 1, pageSize: 100, ct: default);
            foreach (var match in histories.Items)
            {
                if (string.IsNullOrWhiteSpace(match.ReplayPath))
                    continue;

                var p1Name = nameMap.TryGetValue(match.Player1CharacterId, out var n1) ? n1 : match.Player1CharacterId.ToString();
                var p2Name = nameMap.TryGetValue(match.Player2CharacterId, out var n2) ? n2 : match.Player2CharacterId.ToString();

                replays.Add(new MugenReplaySummary(
                    $"{p1Name} vs {p2Name}",
                    match.ReplayPath,
                    match.PlayedAt,
                    match.MatchDuration));
            }

            var replayFiles = FindReplayFiles(_mugenOptions.SaveDirectory);
            foreach (var replay in replayFiles)
            {
                replays.Add(replay);
            }

            Replays.Clear();
            foreach (var replay in replays
                .GroupBy(r => r.Path, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.PlayedAt).First())
                .OrderByDescending(r => r.PlayedAt)
                .Take(50))
            {
                Replays.Add(replay);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load replays");
        }
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
    private async Task LoadTierListAsync()
    {
        try
        {
            var matches = await _matchHistoryRepository.GetMatchHistoriesAsync(pageNumber: 1, pageSize: 1000, ct: default);
            var stats = new Dictionary<Guid, CharacterRecord>();

            foreach (var match in matches.Items)
            {
                UpdateRecord(stats, match.Player1CharacterId, match.Result == MatchResult.Player1Win);
                UpdateRecord(stats, match.Player2CharacterId, match.Result == MatchResult.Player2Win);
            }

            if (stats.Count == 0)
            {
                TierList.Clear();
                return;
            }

            if (Characters.Count == 0)
            {
                var roster = await _collectionService.GetRosterAsync();
                if (roster.IsSuccess && roster.Value != null)
                {
                    Characters.Clear();
                    foreach (var character in roster.Value)
                    {
                        Characters.Add(character);
                    }
                }
            }

            var nameMap = Characters.ToDictionary(c => c.Id, c => c.DisplayName);
            var tiers = stats
                .Select(kvp =>
                {
                    var total = kvp.Value.Wins + kvp.Value.Losses;
                    var winRate = total > 0 ? (double)kvp.Value.Wins / total : 0;
                    return new MugenTierEntry(
                        kvp.Key,
                        nameMap.TryGetValue(kvp.Key, out var name) ? name : kvp.Key.ToString(),
                        kvp.Value.Wins,
                        kvp.Value.Losses,
                        winRate,
                        GetTierLabel(winRate, total));
                })
                .OrderByDescending(t => t.WinRate)
                .ThenByDescending(t => t.Matches)
                .Take(20)
                .ToList();

            TierList.Clear();
            foreach (var entry in tiers)
            {
                TierList.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tier list");
        }
    }

    private static IReadOnlyDictionary<Guid, string> BuildCharacterNameMap(MugenTournament tournament)
    {
        var map = new Dictionary<Guid, string>();

        foreach (var participant in tournament.Participants)
        {
            var name = participant.Character?.DisplayName;
            if (string.IsNullOrWhiteSpace(name))
                name = participant.CharacterId.ToString();

            map[participant.CharacterId] = name;
        }

        return map;
    }

    private static void UpdateRecord(Dictionary<Guid, CharacterRecord> stats, Guid characterId, bool isWin)
    {
        if (!stats.TryGetValue(characterId, out var record))
        {
            record = new CharacterRecord();
            stats[characterId] = record;
        }

        if (isWin)
            record.Wins++;
        else
            record.Losses++;
    }

    private static string GetTierLabel(double winRate, int matches)
    {
        if (matches < 5) return "C";
        if (winRate >= 0.7) return "S";
        if (winRate >= 0.6) return "A";
        if (winRate >= 0.5) return "B";
        return "C";
    }

    private static IEnumerable<string> FindStageDefinitionFiles(IEnumerable<string> directories)
    {
        var results = new List<string>();

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                results.AddRange(Directory.EnumerateFiles(dir, "*.def", SearchOption.AllDirectories));
            }
            catch
            {
                // Ignore invalid paths
            }
        }

        return results;
    }

    private static async Task<string> ExtractStageNameAsync(string stagePath)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(stagePath);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("name", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                        return parts[1].Trim().Trim('"');
                }
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return Path.GetFileNameWithoutExtension(stagePath);
    }

    private static IEnumerable<MugenReplaySummary> FindReplayFiles(string baseDirectory)
    {
        var results = new List<MugenReplaySummary>();

        if (string.IsNullOrWhiteSpace(baseDirectory))
            return results;

        var root = Path.GetFullPath(baseDirectory);
        if (!Directory.Exists(root))
            return results;

        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".rep",
            ".replay",
            ".rpl"
        };

        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (!extensions.Contains(Path.GetExtension(file)))
                    continue;

                var info = new FileInfo(file);
                results.Add(new MugenReplaySummary(
                    Path.GetFileNameWithoutExtension(file),
                    file,
                    info.LastWriteTimeUtc,
                    TimeSpan.Zero));
            }
        }
        catch
        {
            return results;
        }

        return results;
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

