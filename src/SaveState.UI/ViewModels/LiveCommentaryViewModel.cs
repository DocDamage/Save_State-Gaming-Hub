using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.EmulatorEnhancements;
using System;
using System.Collections.ObjectModel;

namespace SaveState.UI.ViewModels;

public partial class LiveCommentaryViewModel : ViewModelBase
{
    private readonly LiveCommentaryService _commentaryService;

    [ObservableProperty]
    private CommentatorPersonality _selectedPersonality = CommentatorPersonality.HypeCaster;

    [ObservableProperty]
    private ObservableCollection<CommentaryLine> _commentaryHistory = new();

    [ObservableProperty]
    private string? _latestComment;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public CommentatorPersonality[] AvailablePersonalities { get; } = Enum.GetValues<CommentatorPersonality>();
    public GameEventType[] AvailableEvents { get; } = Enum.GetValues<GameEventType>();

    public IRelayCommand<CommentatorPersonality> SetPersonalityCommand { get; }
    public IRelayCommand<GameEventType> SimulateEventCommand { get; }
    public IRelayCommand ClearHistoryCommand { get; }

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public LiveCommentaryViewModel(LiveCommentaryService commentaryService)
    {
        _commentaryService = commentaryService ?? throw new ArgumentNullException(nameof(commentaryService));

        SetPersonalityCommand = new RelayCommand<CommentatorPersonality>(SetPersonality);
        SimulateEventCommand = new RelayCommand<GameEventType>(SimulateEvent);
        ClearHistoryCommand = new RelayCommand(ClearHistory);

        SelectedPersonality = _commentaryService.GetPersonality();
    }

    /// <summary>
    /// Design-time/fallback constructor.
    /// </summary>
    public LiveCommentaryViewModel() : this(new LiveCommentaryService())
    {
    }

    private void SetPersonality(CommentatorPersonality personality)
    {
        _commentaryService.SetPersonality(personality);
        SelectedPersonality = personality;
        StatusMessage = $"Personality set to: {personality}";
    }

    private void SimulateEvent(GameEventType eventType)
    {
        var line = _commentaryService.OnEvent(eventType);
        if (line != null)
        {
            CommentaryHistory.Insert(0, line);
            LatestComment = line.Text;
            StatusMessage = $"Event: {eventType}";
        }
        else
        {
            StatusMessage = "Rate limited - wait a moment";
        }
    }

    private void ClearHistory()
    {
        _commentaryService.ClearHistory();
        CommentaryHistory.Clear();
        LatestComment = null;
        StatusMessage = "History cleared";
    }
}
