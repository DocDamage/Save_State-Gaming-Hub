using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenStatsViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private int _totalMatches;

    [ObservableProperty]
    private string _mostPlayedCharacter = "None";

    [ObservableProperty]
    private string _highestWinRate = "0%";

    public MugenStatsViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "BATTLE STATISTICS";
    }

    public override async Task InitializeAsync()
    {
        // Mock data
        TotalMatches = 42;
        MostPlayedCharacter = "Ryu";
        HighestWinRate = "85%";
        await base.InitializeAsync();
    }
}

public partial class MugenCoachViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private string? _advice = "Select a character in the roster to get AI coaching.";

    [ObservableProperty]
    private string _chatInput = string.Empty;

    public ObservableCollection<ChatMessage> ChatHistory { get; } = new();

    public MugenCoachViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "AI COACH";

        ChatHistory.Add(new ChatMessage("System", "Welcome to the Dojo. I am your AI Sensei. Ask me anything about character matchups or frame data."));
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatInput)) return;

        var userMsg = ChatInput;
        ChatHistory.Add(new ChatMessage("You", userMsg));
        ChatInput = string.Empty;

        // Simulated AI response
        await Task.Delay(1000);
        ChatHistory.Add(new ChatMessage("Sensei", $"Analysis of '{userMsg}': Focusing on neutral game and spacing is key for this matchup. Your recovery frames are vulnerable."));
    }
}

public record ChatMessage(string Sender, string Message);

public partial class MugenReplayViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;

    public ObservableCollection<MugenReplayDto> Replays { get; } = new();

    [ObservableProperty]
    private MugenReplayDto? _selectedReplay;

    public MugenReplayViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "REPLAY THEATER";

        // Mock data
        Replays.Add(new MugenReplayDto { Id = Guid.NewGuid(), MatchTitle = "Ryu vs Ken - Epic Comeback", Date = DateTime.Now.AddDays(-1), Duration = "3:45" });
        Replays.Add(new MugenReplayDto { Id = Guid.NewGuid(), MatchTitle = "Chun-Li vs Akuma - Flawless Victory", Date = DateTime.Now.AddDays(-2), Duration = "1:20" });
    }

    [RelayCommand]
    private async Task PlayReplayAsync()
    {
        if (SelectedReplay == null) return;
        // Logic to launch MUGEN with replay file
    }

    [RelayCommand]
    private async Task AnalyzeReplayAsync()
    {
        if (SelectedReplay == null) return;
        // Logic to send replay data to AI for analysis
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

    [ObservableProperty]
    private MugenCharacterSummaryDto? _baseCharacter;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _fusionPartner;

    [ObservableProperty]
    private bool _isFusing;

    [ObservableProperty]
    private string _fusionResult = "The Laboratory is ready.";

    public MugenFusionViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "CHARACTER FUSION";
    }

    [RelayCommand]
    private async Task FuseCharactersAsync()
    {
        if (BaseCharacter == null || FusionPartner == null) return;

        IsFusing = true;
        FusionResult = "Initiating genetic splicing...";
        await Task.Delay(2000);
        FusionResult = $"SUCCESS! Created hybrid: {BaseCharacter.Name}_{FusionPartner.Name}";
        IsFusing = false;
    }
}

public partial class MugenEngineModsViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;

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

    public MugenEngineModsViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "ENGINE MODS";
    }

    [RelayCommand]
    private async Task ApplyModsAsync()
    {
        // Logic to update IKEMEN config.json would go here
        await Task.Delay(500);
    }
}

