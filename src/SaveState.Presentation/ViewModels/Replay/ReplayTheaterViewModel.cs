using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.Replay;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Replay;

/// <summary>
/// View model for the Replay Theater feature, providing video playback control,
/// bookmark management, and export functionality for save state replays.
/// </summary>
public partial class ReplayTheaterViewModel : ObservableObject, IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly ILogger<ReplayTheaterViewModel> _logger;
    private readonly System.Timers.Timer _playbackTimer;

    [ObservableProperty]
    private ObservableCollection<SaveStateReplay> _replays = new();

    [ObservableProperty]
    private SaveStateReplay? _selectedReplay;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private TimeSpan _currentPosition;

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private double _playbackSpeed = 1.0;

    [ObservableProperty]
    private bool _isFullscreen;

    [ObservableProperty]
    private ObservableCollection<ReplayBookmark> _bookmarks = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showBookmarksPanel = true;

    [ObservableProperty]
    private float _volume = 1.0f;

    [ObservableProperty]
    private bool _isMuted;

    /// <summary>
    /// Gets the formatted current position string (e.g., "0:05:23").
    /// </summary>
    public string CurrentPositionText => CurrentPosition.ToString(@"h\:mm\:ss");

    /// <summary>
    /// Gets the formatted duration string.
    /// </summary>
    public string DurationText => Duration.ToString(@"h\:mm\:ss");

    /// <summary>
    /// Gets the playback progress as a percentage (0-100).
    /// </summary>
    public double ProgressPercentage => Duration.TotalSeconds > 0
        ? (CurrentPosition.TotalSeconds / Duration.TotalSeconds) * 100
        : 0;

    public ReplayTheaterViewModel(
        IDialogService dialogService,
        ILogger<ReplayTheaterViewModel> logger)
    {
        _dialogService = dialogService;
        _logger = logger;

        // Initialize playback timer for position updates
        _playbackTimer = new System.Timers.Timer(100);
        _playbackTimer.Elapsed += OnPlaybackTimerElapsed;
        _playbackTimer.AutoReset = true;

        // Load initial data
        _ = LoadReplaysAsync();
    }

    private void OnPlaybackTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (IsPlaying && CurrentPosition < Duration)
        {
            var increment = TimeSpan.FromMilliseconds(100 * PlaybackSpeed);
            CurrentPosition = CurrentPosition.Add(increment);

            if (CurrentPosition >= Duration)
            {
                CurrentPosition = Duration;
                IsPlaying = false;
                _playbackTimer.Stop();
            }

            OnPropertyChanged(nameof(CurrentPositionText));
            OnPropertyChanged(nameof(ProgressPercentage));
        }
    }

    partial void OnSelectedReplayChanged(SaveStateReplay? value)
    {
        if (value != null)
        {
            Duration = value.Duration;
            CurrentPosition = TimeSpan.Zero;
            Bookmarks = new ObservableCollection<ReplayBookmark>(value.Bookmarks);
            OnPropertyChanged(nameof(DurationText));
            OnPropertyChanged(nameof(CurrentPositionText));
            OnPropertyChanged(nameof(ProgressPercentage));
            _logger.LogInformation("Selected replay: {Title}", value.Title);
        }
    }

    partial void OnIsPlayingChanged(bool value)
    {
        if (value)
        {
            _playbackTimer.Start();
            _logger.LogDebug("Playback started");
        }
        else
        {
            _playbackTimer.Stop();
            _logger.LogDebug("Playback paused");
        }
    }

    /// <summary>
    /// Loads the list of available replays.
    /// </summary>
    [RelayCommand]
    private async Task LoadReplaysAsync()
    {
        try
        {
            // NOTE: This is a demo implementation. Replace with actual replay service call.

            // Mock data for demonstration
            Replays.Clear();
            var mockReplays = new List<SaveStateReplay>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Elden Ring - Margit Boss Fight",
                    Description = "First attempt at Margit, the Fell Omen",
                    GameName = "Elden Ring",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    Duration = TimeSpan.FromHours(2) + TimeSpan.FromMinutes(34) + TimeSpan.FromSeconds(15),
                    FileSize = 156_000_000,
                    IsFavorite = true,
                    Tags = new List<string> { "boss", "first-playthrough" },
                    Metadata = new ReplayMetadata
                    {
                        GameDate = DateTime.Now.AddDays(-2),
                        PlayTimeAtSave = TimeSpan.FromHours(45),
                        Location = "Stormveil Castle",
                        PlayerLevel = 35,
                        CompletionPercentage = 15.5f
                    },
                    Bookmarks = new List<ReplayBookmark>
                    {
                        new() { Id = Guid.NewGuid(), Title = "Phase 2 Start", Timestamp = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(23) },
                        new() { Id = Guid.NewGuid(), Title = "Near Death Experience", Timestamp = TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(45) },
                        new() { Id = Guid.NewGuid(), Title = "Victory!", Timestamp = TimeSpan.FromMinutes(23) + TimeSpan.FromSeconds(10) }
                    }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Cyberpunk 2077 - Secret Ending",
                    Description = "Discovered the secret ending path",
                    GameName = "Cyberpunk 2077",
                    CreatedAt = DateTime.Now.AddDays(-5),
                    Duration = TimeSpan.FromMinutes(45) + TimeSpan.FromSeconds(30),
                    FileSize = 89_000_000,
                    Tags = new List<string> { "secret", "ending" },
                    Metadata = new ReplayMetadata
                    {
                        GameDate = DateTime.Now.AddDays(-5),
                        PlayTimeAtSave = TimeSpan.FromHours(120),
                        Location = "Embers",
                        PlayerLevel = 50,
                        CompletionPercentage = 95.0f
                    }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Hades - 32 Heat Clear",
                    Description = "Finally cleared 32 heat with the Rail",
                    GameName = "Hades",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    Duration = TimeSpan.FromMinutes(28) + TimeSpan.FromSeconds(15),
                    FileSize = 45_000_000,
                    IsFavorite = true,
                    Tags = new List<string> { "heat-32", "rail", "victory" },
                    Metadata = new ReplayMetadata
                    {
                        GameDate = DateTime.Now.AddDays(-1),
                        PlayTimeAtSave = TimeSpan.FromHours(200),
                        Location = "Temple of Styx",
                        CompletionPercentage = 88.5f
                    }
                }
            };

            foreach (var replay in mockReplays)
            {
                Replays.Add(replay);
            }

            SelectedReplay = Replays.FirstOrDefault();
            _logger.LogInformation("Loaded {Count} replays", Replays.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load replays");
            await _dialogService.ShowErrorAsync("Failed to load replays. Please try again.");
        }
    }

    /// <summary>
    /// Starts playback of the current replay.
    /// </summary>
    [RelayCommand]
    private Task PlayAsync()
    {
        if (SelectedReplay == null)
        {
            _logger.LogWarning("Cannot play: no replay selected");
            return Task.CompletedTask;
        }

        IsPlaying = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pauses the current playback.
    /// </summary>
    [RelayCommand]
    private Task PauseAsync()
    {
        IsPlaying = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops playback and resets to the beginning.
    /// </summary>
    [RelayCommand]
    private Task StopAsync()
    {
        IsPlaying = false;
        CurrentPosition = TimeSpan.Zero;
        OnPropertyChanged(nameof(CurrentPositionText));
        OnPropertyChanged(nameof(ProgressPercentage));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeks to a specific position in the replay.
    /// </summary>
    [RelayCommand]
    private Task SeekAsync(TimeSpan position)
    {
        if (position < TimeSpan.Zero)
            position = TimeSpan.Zero;
        if (position > Duration)
            position = Duration;

        CurrentPosition = position;
        OnPropertyChanged(nameof(CurrentPositionText));
        OnPropertyChanged(nameof(ProgressPercentage));
        _logger.LogDebug("Seeked to {Position}", position);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Jumps to a bookmark position.
    /// </summary>
    [RelayCommand]
    private Task JumpToBookmarkAsync(ReplayBookmark bookmark)
    {
        if (bookmark != null)
        {
            return SeekAsync(bookmark.Timestamp);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a new bookmark at the current playback position.
    /// </summary>
    [RelayCommand]
    private async Task AddBookmarkAsync()
    {
        if (SelectedReplay == null)
        {
            await _dialogService.ShowErrorAsync("Please select a replay first.");
            return;
        }

        var title = await _dialogService.ShowInputDialogAsync(
            "Add Bookmark",
            "Enter bookmark title:",
            $"Bookmark at {CurrentPositionText}");

        if (!string.IsNullOrWhiteSpace(title))
        {
            var bookmark = new ReplayBookmark
            {
                Id = Guid.NewGuid(),
                Title = title,
                Timestamp = CurrentPosition,
                Note = ""
            };

            Bookmarks.Add(bookmark);
            SelectedReplay.Bookmarks.Add(bookmark);
            _logger.LogInformation("Added bookmark '{Title}' at {Timestamp}", title, CurrentPosition);
        }
    }

    /// <summary>
    /// Edits an existing bookmark.
    /// </summary>
    [RelayCommand]
    private async Task EditBookmarkAsync(ReplayBookmark bookmark)
    {
        if (bookmark == null) return;

        var newTitle = await _dialogService.ShowInputDialogAsync(
            "Edit Bookmark",
            "Enter new title:",
            bookmark.Title);

        if (!string.IsNullOrWhiteSpace(newTitle))
        {
            var index = Bookmarks.IndexOf(bookmark);
            if (index >= 0)
            {
                var updatedBookmark = bookmark with { Title = newTitle };
                Bookmarks[index] = updatedBookmark;
            }
        }
    }

    /// <summary>
    /// Deletes a bookmark.
    /// </summary>
    [RelayCommand]
    private void DeleteBookmark(ReplayBookmark bookmark)
    {
        if (bookmark != null && Bookmarks.Contains(bookmark))
        {
            Bookmarks.Remove(bookmark);
            SelectedReplay?.Bookmarks.Remove(bookmark);
            _logger.LogInformation("Deleted bookmark '{Title}'", bookmark.Title);
        }
    }

    /// <summary>
    /// Exports the current replay in the specified format.
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync(ReplayExportFormat format)
    {
        if (SelectedReplay == null)
        {
            await _dialogService.ShowErrorAsync("Please select a replay to export.");
            return;
        }

        try
        {
            _logger.LogInformation("Exporting replay '{Title}' as {Format}", SelectedReplay.Title, format);

            // NOTE: This is a demo implementation. Replace with actual export logic.
            await _dialogService.ShowSuccessAsync($"Replay exported as {format}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export replay");
            await _dialogService.ShowErrorAsync("Failed to export replay. Please try again.");
        }
    }

    /// <summary>
    /// Shares the current replay.
    /// </summary>
    [RelayCommand]
    private async Task ShareAsync()
    {
        if (SelectedReplay == null)
        {
            await _dialogService.ShowErrorAsync("Please select a replay to share.");
            return;
        }

        try
        {
            _logger.LogInformation("Sharing replay '{Title}'", SelectedReplay.Title);

            // NOTE: This is a demo implementation. Replace with actual sharing service.
            await _dialogService.ShowSuccessAsync("Replay shared successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share replay");
            await _dialogService.ShowErrorAsync("Failed to share replay. Please try again.");
        }
    }

    /// <summary>
    /// Deletes a replay.
    /// </summary>
    [RelayCommand]
    private async Task DeleteReplayAsync(SaveStateReplay replay)
    {
        if (replay == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Replay",
            $"Are you sure you want to delete '{replay.Title}'? This action cannot be undone.");

        if (confirmed)
        {
            Replays.Remove(replay);
            if (SelectedReplay == replay)
            {
                SelectedReplay = Replays.FirstOrDefault();
            }
            _logger.LogInformation("Deleted replay '{Title}'", replay.Title);
        }
    }

    /// <summary>
    /// Toggles fullscreen mode.
    /// </summary>
    [RelayCommand]
    private Task ToggleFullscreenAsync()
    {
        IsFullscreen = !IsFullscreen;
        _logger.LogDebug("Fullscreen mode: {IsFullscreen}", IsFullscreen);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the playback speed.
    /// </summary>
    [RelayCommand]
    private void SetPlaybackSpeed(double speed)
    {
        PlaybackSpeed = speed;
        _logger.LogDebug("Playback speed set to {Speed}x", speed);
    }

    /// <summary>
    /// Toggles the favorite status of the selected replay.
    /// </summary>
    [RelayCommand]
    private void ToggleFavorite()
    {
        if (SelectedReplay != null)
        {
            SelectedReplay.IsFavorite = !SelectedReplay.IsFavorite;
            OnPropertyChanged(nameof(SelectedReplay));
            _logger.LogInformation("Set favorite status to {IsFavorite} for '{Title}'",
                SelectedReplay.IsFavorite, SelectedReplay.Title);
        }
    }

    /// <summary>
    /// Toggles the visibility of the bookmarks panel.
    /// </summary>
    [RelayCommand]
    private void ToggleBookmarksPanel()
    {
        ShowBookmarksPanel = !ShowBookmarksPanel;
    }

    /// <summary>
    /// Toggles mute state.
    /// </summary>
    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    /// <summary>
    /// Skips forward by a fixed amount (10 seconds).
    /// </summary>
    [RelayCommand]
    private Task SkipForwardAsync()
    {
        return SeekAsync(CurrentPosition.Add(TimeSpan.FromSeconds(10)));
    }

    /// <summary>
    /// Skips backward by a fixed amount (10 seconds).
    /// </summary>
    [RelayCommand]
    private Task SkipBackwardAsync()
    {
        return SeekAsync(CurrentPosition.Subtract(TimeSpan.FromSeconds(10)));
    }

    /// <summary>
    /// Disposes resources used by this view model.
    /// </summary>
    public void Dispose()
    {
        _playbackTimer.Stop();
        _playbackTimer.Dispose();
    }
}
