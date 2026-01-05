using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Automation.Commands;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for macro recorder and playback functionality.
/// </summary>
public partial class MacroRecorderViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IMacroService _macroService;
    private readonly INotificationService _notificationService;
    private readonly ITaskRunner _taskRunner;
    private readonly ILogger<MacroRecorderViewModel> _logger;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private int _actionsRecorded;

    [ObservableProperty]
    private string _recordingDuration = "00:00:00";

    [ObservableProperty]
    private string _recordingName = string.Empty;

    [ObservableProperty]
    private string _recordingDescription = string.Empty;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private PlaybackSpeed _selectedSpeed = PlaybackSpeed.Normal;

    [ObservableProperty]
    private bool _loopPlayback;

    [ObservableProperty]
    private Macro? _selectedMacro;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private MacroStatistics? _statistics;

    private Guid? _currentRecordingSessionId;
    private Guid? _currentPlaybackSessionId;
    private System.Timers.Timer? _recordingTimer;

    public MacroRecorderViewModel(
        IMediator mediator,
        IMacroService macroService,
        INotificationService notificationService,
        ITaskRunner taskRunner,
        ILogger<MacroRecorderViewModel> logger)
    {
        _mediator = mediator;
        _macroService = macroService;
        _notificationService = notificationService;
        _taskRunner = taskRunner;
        _logger = logger;

        Macros = new ObservableCollection<Macro>();
        SpeedOptions = new ObservableCollection<PlaybackSpeed>
        {
            PlaybackSpeed.Slow,
            PlaybackSpeed.Normal,
            PlaybackSpeed.Fast,
            PlaybackSpeed.Instant
        };

        // Initialize async
        _ = LoadMacrosAsync();
    }

    /// <summary>
    /// Gets the collection of saved macros.
    /// </summary>
    public ObservableCollection<Macro> Macros { get; }

    /// <summary>
    /// Gets the collection of playback speed options.
    /// </summary>
    public ObservableCollection<PlaybackSpeed> SpeedOptions { get; }

    /// <summary>
    /// Command to start recording a new macro.
    /// </summary>
    [RelayCommand]
    private async Task StartRecordingAsync()
    {
        if (string.IsNullOrWhiteSpace(RecordingName))
        {
            _notificationService.ShowWarning("Please enter a name for the macro");
            return;
        }

        try
        {
            var command = new StartMacroRecordingCommand(
                Guid.Empty, // TODO: Get current game from context
                RecordingName,
                RecordingDescription,
                RecordingMode.Manual);

            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Value != null)
            {
                _currentRecordingSessionId = result.Value.Id;
                IsRecording = true;
                IsPaused = false;
                ActionsRecorded = 0;
                RecordingDuration = "00:00:00";

                // Start timer for duration display
                _recordingTimer = new System.Timers.Timer(1000);
                _recordingTimer.Elapsed += (s, e) => UpdateRecordingDuration();
                _recordingTimer.Start();

                _notificationService.ShowSuccess($"Recording started: {RecordingName}");
                _logger.LogInformation("Macro recording started: {MacroName}", RecordingName);
            }
            else
            {
                _notificationService.ShowError($"Failed to start recording: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start macro recording");
            _notificationService.ShowError("Failed to start recording");
        }
    }

    /// <summary>
    /// Command to stop recording and save the macro.
    /// </summary>
    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        if (!IsRecording || _currentRecordingSessionId == null)
            return;

        try
        {
            var command = new StopMacroRecordingCommand(_currentRecordingSessionId.Value);

            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Value != null)
            {
                IsRecording = false;
                IsPaused = false;
                _recordingTimer?.Stop();
                _recordingTimer?.Dispose();
                _recordingTimer = null;

                // Add new macro to list
                Macros.Insert(0, result.Value);

                _notificationService.ShowSuccess($"Macro saved: {result.Value.Name} ({ActionsRecorded} actions)");
                _logger.LogInformation("Macro recording stopped and saved: {MacroId}", result.Value.Id);

                // Reset form
                RecordingName = string.Empty;
                RecordingDescription = string.Empty;
                ActionsRecorded = 0;
                RecordingDuration = "00:00:00";
            }
            else
            {
                _notificationService.ShowError($"Failed to save recording: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop macro recording");
            _notificationService.ShowError("Failed to save recording");
        }
    }

    /// <summary>
    /// Command to pause the current recording.
    /// </summary>
    [RelayCommand]
    private async Task PauseRecordingAsync()
    {
        if (!IsRecording || _currentRecordingSessionId == null)
            return;

        try
        {
            var command = new PauseMacroRecordingCommand(_currentRecordingSessionId.Value);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                IsPaused = true;
                _recordingTimer?.Stop();
                _notificationService.ShowInfo("Recording paused");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause recording");
            _notificationService.ShowError("Failed to pause recording");
        }
    }

    /// <summary>
    /// Command to resume the current recording.
    /// </summary>
    [RelayCommand]
    private async Task ResumeRecordingAsync()
    {
        if (!IsRecording || !IsPaused || _currentRecordingSessionId == null)
            return;

        try
        {
            var command = new ResumeMacroRecordingCommand(_currentRecordingSessionId.Value);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                IsPaused = false;
                _recordingTimer?.Start();
                _notificationService.ShowInfo("Recording resumed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume recording");
            _notificationService.ShowError("Failed to resume recording");
        }
    }

    /// <summary>
    /// Command to cancel the current recording.
    /// </summary>
    [RelayCommand]
    private async Task CancelRecordingAsync()
    {
        if (!IsRecording || _currentRecordingSessionId == null)
            return;

        try
        {
            var command = new CancelMacroRecordingCommand(_currentRecordingSessionId.Value);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                IsRecording = false;
                IsPaused = false;
                _recordingTimer?.Stop();
                _recordingTimer?.Dispose();
                _recordingTimer = null;

                _notificationService.ShowInfo("Recording cancelled");

                // Reset form
                RecordingName = string.Empty;
                RecordingDescription = string.Empty;
                ActionsRecorded = 0;
                RecordingDuration = "00:00:00";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel recording");
            _notificationService.ShowError("Failed to cancel recording");
        }
    }

    /// <summary>
    /// Command to play the selected macro.
    /// </summary>
    [RelayCommand]
    private async Task PlayMacroAsync()
    {
        if (SelectedMacro == null)
        {
            _notificationService.ShowWarning("Please select a macro to play");
            return;
        }

        try
        {
            var config = new MacroPlaybackConfig
            {
                Speed = SelectedSpeed,
                Loop = LoopPlayback,
                MaxIterations = LoopPlayback ? null : 1
            };

            var command = new StartMacroPlaybackCommand(SelectedMacro.Id, config);

            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Value != null)
            {
                _currentPlaybackSessionId = result.Value.Id;
                IsPlaying = true;

                _notificationService.ShowSuccess($"Playing macro: {SelectedMacro.Name}");
                _logger.LogInformation("Macro playback started: {MacroId}", SelectedMacro.Id);

                // Poll for completion (simplified - ideally use events)
                _ = MonitorPlaybackAsync();
            }
            else
            {
                _notificationService.ShowError($"Failed to start playback: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start macro playback");
            _notificationService.ShowError("Failed to start playback");
        }
    }

    /// <summary>
    /// Command to stop the current playback.
    /// </summary>
    [RelayCommand]
    private async Task StopPlaybackAsync()
    {
        if (!IsPlaying || _currentPlaybackSessionId == null)
            return;

        try
        {
            var command = new StopMacroPlaybackCommand(_currentPlaybackSessionId.Value);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                IsPlaying = false;
                _notificationService.ShowInfo("Playback stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop playback");
            _notificationService.ShowError("Failed to stop playback");
        }
    }

    /// <summary>
    /// Command to delete the selected macro.
    /// </summary>
    [RelayCommand]
    private async Task DeleteMacroAsync()
    {
        if (SelectedMacro == null)
            return;

        try
        {
            var command = new DeleteMacroCommand(SelectedMacro.Id);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                Macros.Remove(SelectedMacro);
                _notificationService.ShowSuccess($"Macro deleted: {SelectedMacro.Name}");
                SelectedMacro = null;
            }
            else
            {
                _notificationService.ShowError($"Failed to delete macro: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete macro");
            _notificationService.ShowError("Failed to delete macro");
        }
    }

    /// <summary>
    /// Command to export the selected macro.
    /// </summary>
    [RelayCommand]
    private async Task ExportMacroAsync()
    {
        if (SelectedMacro == null)
            return;

        try
        {
            var command = new ExportMacroCommand(SelectedMacro.Id, "json");

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Macro exported: {SelectedMacro.Name}.macro.json");
            }
            else
            {
                _notificationService.ShowError($"Failed to export macro: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export macro");
            _notificationService.ShowError("Failed to export macro");
        }
    }

    /// <summary>
    /// Command to refresh the macro list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshMacrosAsync()
    {
        await LoadMacrosAsync();
    }

    private async Task LoadMacrosAsync()
    {
        try
        {
            IsLoading = true;

            // Ensure macro service is available
            if (_macroService == null)
            {
                _logger.LogWarning("Macro service is not available");
                return;
            }

            // Get all macros (simplified - no game filter for now)
            var macrosResult = await _macroService.GetMacrosAsync();

            Macros.Clear();
            if (macrosResult.IsSuccess && macrosResult.Value != null)
            {
                foreach (var macro in macrosResult.Value)
                {
                    Macros.Add(macro);
                }
            }
            else if (!macrosResult.IsSuccess)
            {
                _logger.LogWarning("Failed to get macros: {Error}", macrosResult.Error);
            }

            // Load statistics
            var statsCommand = new GetMacroStatisticsCommand();
            var statsResult = await _mediator.Send(statsCommand);
            if (statsResult.IsSuccess)
            {
                Statistics = statsResult.Value;
            }

            _logger.LogInformation("Loaded {Count} macros successfully", Macros.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load macros - critical error");
            // Don't show error notification during initialization to avoid spam
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateRecordingDuration()
    {
        if (_currentRecordingSessionId == null)
            return;

        // Get current status using centralized TaskRunner
        _taskRunner.Run(async () =>
        {
            var command = new GetMacroRecordingStatusCommand(_currentRecordingSessionId.Value);

            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Value != null)
            {
                ActionsRecorded = result.Value.ActionsRecorded;
                RecordingDuration = result.Value.Duration.ToString(@"hh\:mm\:ss");
            }
        }, "UpdateMacroRecordingStatus");
    }

    private async Task MonitorPlaybackAsync()
    {
        if (_currentPlaybackSessionId == null)
            return;

        // Poll playback status until completion
        while (IsPlaying)
        {
            await Task.Delay(500);

            try
            {
                var command = new GetMacroPlaybackStatusCommand(_currentPlaybackSessionId.Value);

                var result = await _mediator.Send(command);

                if (result.IsSuccess && result.Value != null)
                {
                    if (!result.Value.IsPlaying && !result.Value.IsPaused)
                    {
                        // Playback completed
                        IsPlaying = false;
                        _notificationService.ShowSuccess("Playback completed");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to monitor playback");
                break;
            }
        }
    }
}
