using CommunityToolkit.Mvvm.ComponentModel;
using SaveState.Core.Mugen.Entities;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen;
using SaveState.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
// Use ValueObjects as canonical type for ambiguous MugenMoveEntry
using MugenMoveEntry = SaveState.Core.Mugen.ValueObjects.MugenMoveEntry;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenStatsViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMugenStatsService _statsService;
    private readonly IMugenEloService _eloService;
    private readonly IMugenCollectionService _collectionService;
    private readonly IMugenMatchHistoryRepository _historyRepository;
    private readonly ILogger<MugenStatsViewModel> _logger;

    [ObservableProperty]
    private int _totalMatches;

    [ObservableProperty]
    private string _mostPlayedCharacter = "None";

    [ObservableProperty]
    private string _highestWinRate = "0%";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<MugenMatchSummary> RecentMatches { get; } = new();
    public ObservableCollection<MugenTierEntry> TierList { get; } = new();
    public ObservableCollection<SaveState.Core.Mugen.ValueObjects.MugenEloRating> EloRatings { get; } = new();

    public MugenStatsViewModel(
        IMediator mediator,
        IMugenStatsService statsService,
        IMugenEloService eloService,
        IMugenCollectionService collectionService,
        IMugenMatchHistoryRepository historyRepository,
        ILogger<MugenStatsViewModel> logger)
    {
        _mediator = mediator;
        _statsService = statsService;
        _eloService = eloService;
        _collectionService = collectionService;
        _historyRepository = historyRepository;
        _logger = logger;
        Title = "BATTLE STATISTICS";
    }

    public override async Task InitializeAsync()
    {
        await LoadStatsAsync();
    }

    [RelayCommand]
    private async Task LoadStatsAsync()
    {
        IsLoading = true;
        try
        {
            await Task.WhenAll(
                LoadGlobalStatsAsync(),
                LoadRecentMatchesAsync(),
                LoadTierListAsync(),
                LoadEloRatingsAsync()
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadGlobalStatsAsync()
    {
        try
        {
            var result = await _statsService.GetGlobalStatsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                var stats = result.Value;
                TotalMatches = stats.TotalMatches;
                MostPlayedCharacter = stats.MostPlayedCharacterName ?? "None";
                HighestWinRate = $"{stats.HighestWinRate:P0}";
            }
        }
        catch (Exception)
        {
            TotalMatches = 0;
            MostPlayedCharacter = "N/A";
            HighestWinRate = "0%";
        }
    }

    private async Task LoadRecentMatchesAsync()
    {
        try
        {
            var result = await _statsService.GetRecentMatchesAsync(20);
            RecentMatches.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                var roster = await _collectionService.GetRosterAsync();
                var nameMap = roster.IsSuccess ? roster.Value.ToDictionary(c => c.Id, c => c.DisplayName) : new Dictionary<Guid, string>();

                foreach (var match in result.Value)
                {
                    RecentMatches.Add(new MugenMatchSummary(
                        nameMap.GetValueOrDefault(match.Player1CharacterId, match.Player1CharacterId.ToString()),
                        nameMap.GetValueOrDefault(match.Player2CharacterId, match.Player2CharacterId.ToString()),
                        match.Result,
                        match.MatchDuration,
                        match.PlayedAt));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load recent matches");
        }
    }

    private async Task LoadTierListAsync()
    {
        try
        {
            var matches = await _historyRepository.GetAllAsync();
            if (!matches.Any()) return;

            var stats = new Dictionary<Guid, (int Wins, int Losses)>();
            foreach (var match in matches)
            {
                UpdateLocalRecord(stats, match.Player1CharacterId, match.Result == MatchResult.Player1Win);
                UpdateLocalRecord(stats, match.Player2CharacterId, match.Result == MatchResult.Player2Win);
            }

            var rosterResult = await _collectionService.GetRosterAsync();
            var nameMap = rosterResult.IsSuccess ? rosterResult.Value.ToDictionary(c => c.Id, c => c.DisplayName) : new Dictionary<Guid, string>();

            var tiers = stats
                .Select(kvp =>
                {
                    var total = kvp.Value.Wins + kvp.Value.Losses;
                    var winRate = total > 0 ? (double)kvp.Value.Wins / total : 0;
                    return new MugenTierEntry(
                        kvp.Key,
                        nameMap.GetValueOrDefault(kvp.Key, kvp.Key.ToString()),
                        kvp.Value.Wins,
                        kvp.Value.Losses,
                        winRate,
                        GetTierLabel(winRate, total));
                })
                .OrderByDescending(t => t.WinRate)
                .ThenByDescending(t => t.Matches)
                .Take(20);

            TierList.Clear();
            foreach (var entry in tiers) TierList.Add(entry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load tier list");
        }
    }

    private static void UpdateLocalRecord(Dictionary<Guid, (int Wins, int Losses)> stats, Guid id, bool won)
    {
        if (!stats.TryGetValue(id, out var record)) record = (0, 0);
        if (won) record.Wins++; else record.Losses++;
        stats[id] = record;
    }

    private async Task LoadEloRatingsAsync()
    {
        try
        {
            var result = await _eloService.GetRatingsAsync();
            EloRatings.Clear();
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var entry in result.Value) EloRatings.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load ELO ratings");
        }
    }

    private static string GetTierLabel(double winRate, int matches)
    {
        if (matches < 5) return "C";
        if (winRate >= 0.7) return "S";
        if (winRate >= 0.6) return "A";
        if (winRate >= 0.5) return "B";
        return "C";
    }
}

public partial class MugenCoachViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMugenCoachService _coachService;
    private readonly IMugenMoveListService _moveListService;
    private readonly IMugenCollectionService _collectionService;

    [ObservableProperty]
    private string? _advice = "Select a character in the roster to get AI coaching.";

    [ObservableProperty]
    private string _chatInput = string.Empty;

    [ObservableProperty]
    private bool _isThinking;

    [ObservableProperty]
    private bool _isMoveListLoading;

    public ObservableCollection<ChatMessage> ChatHistory { get; } = new();
    public ObservableCollection<MugenMoveEntry> MoveList { get; } = new();

    public MugenCoachViewModel(
        IMediator mediator,
        IMugenCoachService coachService,
        IMugenMoveListService moveListService,
        IMugenCollectionService collectionService)
    {
        _mediator = mediator;
        _coachService = coachService;
        _moveListService = moveListService;
        _collectionService = collectionService;
        Title = "AI COACH";

        ChatHistory.Add(new ChatMessage("Sensei", "Welcome to the Dojo. I am your AI Sensei. Ask me anything about character matchups or frame data."));
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatInput)) return;

        var userMsg = ChatInput;
        ChatHistory.Add(new ChatMessage("You", userMsg));
        ChatInput = string.Empty;
        IsThinking = true;

        try
        {
             // Use the real AI coach service
             var result = await _coachService.SendChatMessageAsync(userMsg);

             if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
             {
                 ChatHistory.Add(new ChatMessage("Sensei", result.Value));
             }
             else
             {
                 ChatHistory.Add(new ChatMessage("Sensei", "I'm having trouble processing that right now. Could you rephrase your question?"));
             }
        }
        catch (Exception)
        {
            ChatHistory.Add(new ChatMessage("Sensei", "An error occurred. Please try again."));
        }
        finally
        {
            IsThinking = false;
        }
    }

    public async Task UpdateContextAsync(Guid characterId)
    {
         IsThinking = true;
         IsMoveListLoading = true;
         MoveList.Clear();

         var adviceResult = await _coachService.GetCoachingAdviceAsync(characterId);
         if (adviceResult.IsSuccess && !string.IsNullOrEmpty(adviceResult.Value))
         {
             ChatHistory.Add(new ChatMessage("Sensei", adviceResult.Value));
         }

         var charResult = await _collectionService.GetRosterAsync();
         if (charResult.IsSuccess && charResult.Value != null)
         {
             var character = charResult.Value.FirstOrDefault(c => c.Id == characterId);
             if (character != null)
             {
                 var movesResult = await _moveListService.GetMoveListAsync(character);
                 if (movesResult.IsSuccess && movesResult.Value != null)
                 {
                     foreach (var move in movesResult.Value) MoveList.Add(move);
                 }
             }
         }

         IsThinking = false;
         IsMoveListLoading = false;
    }
}

public record ChatMessage(string Sender, string Message);

public partial class MugenReplayViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMugenMatchHistoryRepository _historyRepository;
    private readonly IMugenLauncher _launcher;
    private readonly IMugenCoachService _coachService;
    private readonly MugenOptions _options;

    public ObservableCollection<MugenReplayDto> Replays { get; } = new();

    [ObservableProperty]
    private MugenReplayDto? _selectedReplay;

    [ObservableProperty]
    private string? _analysisResult;

    [ObservableProperty]
    private bool _isLoading;

    public MugenReplayViewModel(
        IMediator mediator,
        IMugenMatchHistoryRepository historyRepository,
        IMugenLauncher launcher,
        IMugenCoachService coachService,
        IOptions<MugenOptions> options)
    {
        _mediator = mediator;
        _historyRepository = historyRepository;
        _launcher = launcher;
        _coachService = coachService;
        _options = options.Value;
        Title = "REPLAY THEATER";
    }

    public override async Task InitializeAsync()
    {
        await LoadReplaysAsync();
        await base.InitializeAsync();
    }

    [RelayCommand]
    private async Task LoadReplaysAsync()
    {
        Replays.Clear();
        // Here we would fetch from repository
        var matches = (await _historyRepository.GetAllAsync()).Take(20);
        foreach (var match in matches)
        {
             Replays.Add(new MugenReplayDto
             {
                 Id = match.Id,
                 MatchTitle = $"P1 vs P2 - {match.Result}",
                 Date = match.PlayedAt,
                 Duration = $"{match.MatchDuration.TotalMinutes:0}:{match.MatchDuration.Seconds:00}"
             });
        }
    }

    [RelayCommand]
    private async Task PlayReplayAsync()
    {
        if (SelectedReplay == null) return;

        try
        {
            // Construct replay file path (assuming replays are stored in a replays folder)
            var replayPath = Path.Combine(_options.ReplaysFolder ?? "replays", $"{SelectedReplay.Id}.replay");

            if (!File.Exists(replayPath))
            {
                // Try alternative extensions
                var alternativePaths = new[] { ".json", ".log", ".txt" }
                    .Select(ext => Path.Combine(_options.ReplaysFolder ?? "replays", $"{SelectedReplay.Id}{ext}"))
                    .FirstOrDefault(File.Exists);

                if (alternativePaths == null)
                {
                    AnalysisResult = "Replay file not found.";
                    return;
                }

                replayPath = alternativePaths;
            }

            await _launcher.LaunchReplayAsync(replayPath);
            AnalysisResult = $"Playing replay: {SelectedReplay.MatchTitle}";
        }
        catch (Exception ex)
        {
            AnalysisResult = $"Failed to play replay: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AnalyzeReplayAsync()
    {
        if (SelectedReplay == null) return;

        try
        {
            IsLoading = true;
            AnalysisResult = "Analyzing replay...";

            // Construct replay file path
            var replayPath = Path.Combine(_options.ReplaysFolder ?? "replays", $"{SelectedReplay.Id}.replay");

            if (!File.Exists(replayPath))
            {
                // Try alternative extensions
                var alternativePaths = new[] { ".json", ".log", ".txt" }
                    .Select(ext => Path.Combine(_options.ReplaysFolder ?? "replays", $"{SelectedReplay.Id}{ext}"))
                    .FirstOrDefault(File.Exists);

                if (alternativePaths == null)
                {
                    AnalysisResult = "Replay file not found for analysis.";
                    return;
                }

                replayPath = alternativePaths;
            }

            var result = await _coachService.AnalyzeReplayAsync(replayPath);

            if (result.IsSuccess && result.Value != null && result.Value.Any())
            {
                AnalysisResult = string.Join("\n• ", new[] { "AI Coach Analysis:" }.Concat(result.Value));
            }
            else
            {
                AnalysisResult = "No analysis available for this replay.";
            }
        }
        catch (Exception ex)
        {
            AnalysisResult = $"Failed to analyze replay: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class MugenReplayDto
{
    public Guid Id { get; set; }
    public string MatchTitle { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Duration { get; set; } = string.Empty;
}

public partial class MugenFusionViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMugenFusionService _fusionService;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _baseCharacter;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _fusionPartner;

    [ObservableProperty]
    private bool _isFusing;

    [ObservableProperty]
    private string _fusionResult = "The Laboratory is ready.";

    public MugenFusionViewModel(IMediator mediator, IMugenFusionService fusionService)
    {
        _mediator = mediator;
        _fusionService = fusionService;
        Title = "CHARACTER FUSION";
    }

    [RelayCommand]
    private async Task FuseCharactersAsync()
    {
        if (BaseCharacter == null || FusionPartner == null) return;

        IsFusing = true;
        FusionResult = "Initiating genetic splicing...";

        var ids = new[] { BaseCharacter.Id, FusionPartner.Id };
        var newName = $"{BaseCharacter.Name}_{FusionPartner.Name}";

        var result = await _fusionService.FuseCharactersAsync(
            ids,
            newName,
            FusionType.Balanced,
            new FusionOptions());

        if (result.IsSuccess)
        {
            FusionResult = $"SUCCESS! Created hybrid: {result.Value.Name}. Character files generated permanently.";
        }
        else
        {
            FusionResult = $"FAILURE: {result.Error}";
        }

        IsFusing = false;
    }
}

public partial class MugenEngineModsViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMugenConfigService _configService;

    [ObservableProperty]
    private bool _activeTagEnabled = true;

    [ObservableProperty]
    private bool _dashCancelEnabled = false;

    [ObservableProperty]
    private bool _dramaticZoomEnabled = false;

    [ObservableProperty]
    private bool _guardBreakEnabled = true;

    [ObservableProperty]
    private bool _clashingEnabled = false;

    [ObservableProperty]
    private bool _shadowAssistEnabled = false;

    [ObservableProperty]
    private bool _rainbowEditionEnabled = false;

    [ObservableProperty]
    private bool _autoCameraEnabled = true;

    [ObservableProperty]
    private bool _attackDataDisplayEnabled = false;

    [ObservableProperty]
    private string _statusMessage = "Ready to apply modifications.";

    public MugenEngineModsViewModel(IMediator mediator, IMugenConfigService configService)
    {
        _mediator = mediator;
        _configService = configService;
        Title = "ENGINE MODS";
    }

    public override async Task InitializeAsync()
    {
        await LoadCurrentSettingsAsync();
        await base.InitializeAsync();
    }

    private async Task LoadCurrentSettingsAsync()
    {
        try
        {
            StatusMessage = "Loading current engine settings...";

            var activeTagResult = await _configService.GetConfigValueAsync("GameMode.ActiveTag");
            if (activeTagResult.IsSuccess && bool.TryParse(activeTagResult.Value, out var activeTag))
                ActiveTagEnabled = activeTag;

            var dashCancelResult = await _configService.GetConfigValueAsync("GameMode.DashCancel");
            if (dashCancelResult.IsSuccess && bool.TryParse(dashCancelResult.Value, out var dashCancel))
                DashCancelEnabled = dashCancel;

            var guardBreakResult = await _configService.GetConfigValueAsync("GameMode.GuardBreak");
            if (guardBreakResult.IsSuccess && bool.TryParse(guardBreakResult.Value, out var guardBreak))
                GuardBreakEnabled = guardBreak;

            var clashingResult = await _configService.GetConfigValueAsync("GameMode.Clashing");
            if (clashingResult.IsSuccess && bool.TryParse(clashingResult.Value, out var clashing))
                ClashingEnabled = clashing;

            var shadowAssistResult = await _configService.GetConfigValueAsync("GameMode.ShadowAssist");
            if (shadowAssistResult.IsSuccess && bool.TryParse(shadowAssistResult.Value, out var shadowAssist))
                ShadowAssistEnabled = shadowAssist;

            var dramaticZoomResult = await _configService.GetConfigValueAsync("Video.DramaticZoom");
            if (dramaticZoomResult.IsSuccess && bool.TryParse(dramaticZoomResult.Value, out var dramaticZoom))
                DramaticZoomEnabled = dramaticZoom;

            var rainbowResult = await _configService.GetConfigValueAsync("Video.RainbowEdition");
            if (rainbowResult.IsSuccess && bool.TryParse(rainbowResult.Value, out var rainbow))
                RainbowEditionEnabled = rainbow;

            var autoCameraResult = await _configService.GetConfigValueAsync("Video.AutoCamera");
            if (autoCameraResult.IsSuccess && bool.TryParse(autoCameraResult.Value, out var autoCamera))
                AutoCameraEnabled = autoCamera;

            var attackDisplayResult = await _configService.GetConfigValueAsync("Video.AttackDataDisplay");
            if (attackDisplayResult.IsSuccess && bool.TryParse(attackDisplayResult.Value, out var attackDisplay))
                AttackDataDisplayEnabled = attackDisplay;

            StatusMessage = "Ready to apply modifications.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ApplyModsAsync()
    {
        try
        {
            StatusMessage = "Applying engine modifications...";

            var settings = new Dictionary<string, object>
            {
                { "GameMode.ActiveTag", ActiveTagEnabled },
                { "GameMode.DashCancel", DashCancelEnabled },
                { "GameMode.GuardBreak", GuardBreakEnabled },
                { "GameMode.Clashing", ClashingEnabled },
                { "GameMode.ShadowAssist", ShadowAssistEnabled },
                { "Video.DramaticZoom", DramaticZoomEnabled },
                { "Video.RainbowEdition", RainbowEditionEnabled },
                { "Video.AutoCamera", AutoCameraEnabled },
                { "Video.AttackDataDisplay", AttackDataDisplayEnabled }
            };

            var result = await _configService.SetConfigValuesAsync(settings);

            if (result.IsSuccess)
            {
                StatusMessage = "✓ Modifications applied successfully! Restart IKEMEN to see changes.";
            }
            else
            {
                StatusMessage = $"✗ Failed to apply modifications: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
        }
    }
}
