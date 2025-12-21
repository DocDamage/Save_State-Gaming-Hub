using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.EmulatorEnhancements;
using System.Collections.ObjectModel;

namespace SaveState.UI.ViewModels;

public partial class RetroRewindViewModel : ViewModelBase
{
    private readonly RetroRewindService _rewindService;

    [ObservableProperty]
    private bool _isSessionActive;

    [ObservableProperty]
    private int _currentFrame;

    [ObservableProperty]
    private int _totalFrames;

    [ObservableProperty]
    private ObservableCollection<FrameSnapshot> _bookmarks = new();

    [ObservableProperty]
    private string _newBookmarkName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "No active session";

    public IRelayCommand StartSessionCommand { get; }
    public IRelayCommand EndSessionCommand { get; }
    public IRelayCommand RewindCommand { get; }
    public IRelayCommand FastForwardCommand { get; }
    public IRelayCommand AddBookmarkCommand { get; }
    public IRelayCommand<string> JumpToBookmarkCommand { get; }

    public RetroRewindViewModel()
    {
        _rewindService = new RetroRewindService();

        StartSessionCommand = new RelayCommand(StartSession, () => !IsSessionActive);
        EndSessionCommand = new RelayCommand(EndSession, () => IsSessionActive);
        RewindCommand = new RelayCommand(() => Rewind(10), () => IsSessionActive);
        FastForwardCommand = new RelayCommand(() => FastForward(10), () => IsSessionActive);
        AddBookmarkCommand = new RelayCommand(AddBookmark, () => IsSessionActive && !string.IsNullOrWhiteSpace(NewBookmarkName));
        JumpToBookmarkCommand = new RelayCommand<string>(JumpToBookmark);
    }

    private void StartSession()
    {
        _rewindService.StartSession("demo-game");
        IsSessionActive = true;
        StatusMessage = "Session started - capturing frames...";
        
        // Simulate some frame captures for demo
        for (int i = 0; i < 100; i++)
        {
            _rewindService.CaptureFrame(new byte[] { (byte)i });
        }
        
        UpdateFrameInfo();
        NotifyCommands();
    }

    private void EndSession()
    {
        _rewindService.EndSession();
        IsSessionActive = false;
        CurrentFrame = 0;
        TotalFrames = 0;
        Bookmarks.Clear();
        StatusMessage = "Session ended and saved";
        NotifyCommands();
    }

    private void Rewind(int frames)
    {
        var snapshot = _rewindService.Rewind(frames);
        if (snapshot != null)
        {
            UpdateFrameInfo();
            StatusMessage = $"Rewound to frame {CurrentFrame}";
        }
    }

    private void FastForward(int frames)
    {
        var snapshot = _rewindService.FastForward(frames);
        if (snapshot != null)
        {
            UpdateFrameInfo();
            StatusMessage = $"Fast-forwarded to frame {CurrentFrame}";
        }
    }

    private void AddBookmark()
    {
        if (string.IsNullOrWhiteSpace(NewBookmarkName)) return;
        _rewindService.AddBookmark(NewBookmarkName);
        RefreshBookmarks();
        NewBookmarkName = string.Empty;
        StatusMessage = "Bookmark added";
    }

    private void JumpToBookmark(string? name)
    {
        if (string.IsNullOrEmpty(name)) return;
        var snapshot = _rewindService.JumpToBookmark(name);
        if (snapshot != null)
        {
            UpdateFrameInfo();
            StatusMessage = $"Jumped to bookmark: {name}";
        }
    }

    private void UpdateFrameInfo()
    {
        var session = _rewindService.GetCurrentSession();
        if (session != null)
        {
            CurrentFrame = session.CurrentFrame;
            TotalFrames = session.Snapshots.Count;
        }
    }

    private void RefreshBookmarks()
    {
        Bookmarks.Clear();
        foreach (var bookmark in _rewindService.GetBookmarks())
        {
            Bookmarks.Add(bookmark);
        }
    }

    private void NotifyCommands()
    {
        StartSessionCommand.NotifyCanExecuteChanged();
        EndSessionCommand.NotifyCanExecuteChanged();
        RewindCommand.NotifyCanExecuteChanged();
        FastForwardCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewBookmarkNameChanged(string value) => AddBookmarkCommand.NotifyCanExecuteChanged();
}
