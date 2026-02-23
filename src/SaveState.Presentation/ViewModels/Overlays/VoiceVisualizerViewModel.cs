using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Services.DTOs;
using SaveState.Presentation.Models.Voice;
using VoiceCommandResult = SaveState.Presentation.Models.Voice.VoiceCommandResult;
using SaveState.Presentation.Services;
using SaveState.Presentation.Services.Voice;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the voice visualizer overlay.
/// </summary>
public sealed partial class VoiceVisualizerViewModel : OverlayViewModelBase
{
    private readonly IVoiceCommandService _voiceCommandService;
    private readonly IVoiceVisualizerService _visualizerService;
    private readonly INotificationService _notificationService;
    private readonly IOverlayService _overlayService;
    private readonly ILogger<VoiceVisualizerViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    private DateTime _listeningStartTime;
    private CancellationTokenSource? _autoHideCts;

    /// <summary>
    /// Gets or sets the current visualizer state.
    /// </summary>
    [ObservableProperty]
    private VoiceVisualizerState _currentState = VoiceVisualizerState.Idle;

    /// <summary>
    /// Gets or sets the current audio level (0-100).
    /// </summary>
    [ObservableProperty]
    private float _audioLevel;

    /// <summary>
    /// Gets the frequency data for visualization.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<float> _frequencyData = new();

    /// <summary>
    /// Gets or sets the recognized text.
    /// </summary>
    [ObservableProperty]
    private string _recognizedText = string.Empty;

    /// <summary>
    /// Gets or sets the command text.
    /// </summary>
    [ObservableProperty]
    private string _commandText = string.Empty;

    /// <summary>
    /// Gets or sets the confidence level.
    /// </summary>
    [ObservableProperty]
    private float _confidence;

    /// <summary>
    /// Gets or sets the wake word phrase.
    /// </summary>
    [ObservableProperty]
    private string _wakeWord = "Hey SaveState";

    /// <summary>
    /// Gets or sets the listening duration.
    /// </summary>
    [ObservableProperty]
    private TimeSpan _listeningDuration;

    /// <summary>
    /// Gets or sets whether the visualizer is in compact mode.
    /// </summary>
    [ObservableProperty]
    private bool _isCompactMode;

    /// <summary>
    /// Gets or sets whether to show frequency bands.
    /// </summary>
    [ObservableProperty]
    private bool _showFrequencyBands = true;

    /// <summary>
    /// Gets or sets whether the microphone is muted.
    /// </summary>
    [ObservableProperty]
    private bool _isMuted;

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets or sets whether to auto-hide after command completion.
    /// </summary>
    [ObservableProperty]
    private bool _autoHideEnabled = true;

    /// <summary>
    /// Creates a new voice visualizer view model.
    /// </summary>
    public VoiceVisualizerViewModel(
        IVoiceCommandService voiceCommandService,
        IVoiceVisualizerService visualizerService,
        INotificationService notificationService,
        IOverlayService overlayService,
        ILogger<VoiceVisualizerViewModel> logger,
        ITimeProvider timeProvider)
    {
        _voiceCommandService = voiceCommandService;
        _visualizerService = visualizerService;
        _notificationService = notificationService;
        _overlayService = overlayService;
        _logger = logger;
        _timeProvider = timeProvider;

        // Initialize frequency data
        for (int i = 0; i < 16; i++)
        {
            FrequencyData.Add(0.1f);
        }

        // Subscribe to visualizer service events
        _visualizerService.StateChanged += OnVisualizerStateChanged;
        _visualizerService.AudioLevelUpdated += OnAudioLevelUpdated;
        _visualizerService.RecognizedTextChanged += OnRecognizedTextChanged;
        _visualizerService.CommandResultReceived += OnCommandResultReceived;
        _visualizerService.VisibilityChanged += OnVisibilityChanged;

        // Subscribe to voice command service events
        _voiceCommandService.ListeningStatusChanged += OnListeningStatusChanged;
        _voiceCommandService.VoiceCommandRecognized += OnVoiceCommandRecognized;
    }

    /// <inheritdoc />
    public override Task ShowAsync()
    {
        _visualizerService.StartVisualization();
        return base.ShowAsync();
    }

    /// <inheritdoc />
    public override Task HideAsync()
    {
        _visualizerService.StopVisualization();
        return base.HideAsync();
    }

    /// <inheritdoc />
    protected override void Close()
    {
        StopListening();
        base.Close();
    }

    /// <summary>
    /// Starts listening for voice commands.
    /// </summary>
    [RelayCommand]
    private async Task StartListeningAsync()
    {
        try
        {
            if (IsMuted)
            {
                await _notificationService.ShowNotificationAsync("Microphone is muted. Unmute first.", "Warning");
                return;
            }

            var result = await _voiceCommandService.StartListeningAsync();
            if (result.IsSuccess)
            {
                _listeningStartTime = _timeProvider.Now;
                CurrentState = VoiceVisualizerState.Listening;
                IsVisible = true;
                _logger.LogDebug("Voice listening started");
            }
            else
            {
                ErrorMessage = result.Error;
                CurrentState = VoiceVisualizerState.Error;
                await _notificationService.ShowErrorAsync($"Failed to start listening: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting voice listening");
            ErrorMessage = ex.Message;
            CurrentState = VoiceVisualizerState.Error;
        }
    }

    /// <summary>
    /// Stops listening for voice commands.
    /// </summary>
    [RelayCommand]
    private async Task StopListeningAsync()
    {
        try
        {
            var result = await _voiceCommandService.StopListeningAsync();
            if (result.IsSuccess)
            {
                StopListening();
                _logger.LogDebug("Voice listening stopped");
            }
            else
            {
                _logger.LogWarning("Failed to stop listening: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping voice listening");
        }
    }

    /// <summary>
    /// Toggles the mute state.
    /// </summary>
    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        CurrentState = IsMuted ? VoiceVisualizerState.Muted : VoiceVisualizerState.Idle;

        if (IsMuted && _voiceCommandService.IsListening)
        {
            _ = StopListeningAsync();
        }

        _notificationService.ShowNotificationAsync(
            IsMuted ? "Microphone muted" : "Microphone unmuted",
            "Info");

        _logger.LogDebug("Microphone mute state changed to {IsMuted}", IsMuted);
    }

    /// <summary>
    /// Shows the voice command help overlay.
    /// </summary>
    [RelayCommand]
    private void ShowHelp()
    {
        // Show help overlay - this will be implemented in the view
        _overlayService.ShowAiAssistantOverlay();
        _logger.LogDebug("Voice command help requested");
    }

    /// <summary>
    /// Cancels the current voice operation.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        StopListening();
        IsVisible = false;
        _logger.LogDebug("Voice visualizer cancelled");
    }

    /// <summary>
    /// Toggles compact mode.
    /// </summary>
    [RelayCommand]
    private void ToggleCompactMode()
    {
        IsCompactMode = !IsCompactMode;
        _logger.LogDebug("Compact mode toggled to {IsCompactMode}", IsCompactMode);
    }

    /// <summary>
    /// Updates the audio visualization data.
    /// </summary>
    public void UpdateAudioVisualization(float[] frequencyBands, float overallLevel)
    {
        AudioLevel = overallLevel * 100;

        if (frequencyBands.Length > 0)
        {
            FrequencyData.Clear();
            foreach (var band in frequencyBands)
            {
                FrequencyData.Add(band);
            }
        }
    }

    private void OnVisualizerStateChanged(object? sender, VoiceVisualizerStateChangedEventArgs e)
    {
        CurrentState = e.NewState;

        if (e.NewState == VoiceVisualizerState.Listening)
        {
            _listeningStartTime = e.Timestamp;
            StartDurationTracking();
        }
        else if (e.NewState is VoiceVisualizerState.Success or VoiceVisualizerState.Error)
        {
            StopDurationTracking();

            if (AutoHideEnabled)
            {
                ScheduleAutoHide();
            }
        }
    }

    private void OnAudioLevelUpdated(object? sender, AudioLevelUpdatedEventArgs e)
    {
        UpdateAudioVisualization(e.Data.FrequencyBands, e.Data.OverallLevel);
    }

    private void OnRecognizedTextChanged(object? sender, string text)
    {
        RecognizedText = text;
        CurrentState = VoiceVisualizerState.Processing;
    }

    private void OnCommandResultReceived(object? sender, VoiceCommandResult result)
    {
        CommandText = result.MatchedCommand ?? "Unknown command";
        Confidence = result.Confidence;
        CurrentState = result.IsSuccess ? VoiceVisualizerState.Success : VoiceVisualizerState.Error;

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
        }
    }

    private void OnVisibilityChanged(object? sender, bool isVisible)
    {
        IsVisible = isVisible;
    }

    private void OnListeningStatusChanged(object? sender, ListeningStatusChangedEventArgs e)
    {
        if (!e.IsListening && CurrentState == VoiceVisualizerState.Listening)
        {
            CurrentState = VoiceVisualizerState.Idle;
        }
        else if (e.IsListening)
        {
            CurrentState = VoiceVisualizerState.Listening;
        }
    }

    private void OnVoiceCommandRecognized(object? sender, VoiceCommandRecognizedEventArgs e)
    {
        RecognizedText = e.Result.RecognizedText;
        CommandText = e.Result.MatchedCommand?.CommandPhrase ?? "Unknown command";
        Confidence = e.Result.Confidence;
        CurrentState = e.Result.Success ? VoiceVisualizerState.Success : VoiceVisualizerState.Error;

        // Create result for visualizer service
        var result = new VoiceCommandResult(_timeProvider)
        {
            RawText = e.Result.RecognizedText,
            MatchedCommand = e.Result.MatchedCommand?.CommandPhrase,
            Confidence = e.Result.Confidence,
            IsSuccess = e.Result.Success,
            ErrorMessage = e.Result.ErrorMessage
        };

        _visualizerService.ShowCommandResult(result);
    }

    private void StopListening()
    {
        CurrentState = VoiceVisualizerState.Idle;
        AudioLevel = 0;
        RecognizedText = string.Empty;
        CommandText = string.Empty;
        Confidence = 0;
        StopDurationTracking();
        _autoHideCts?.Cancel();
    }

    private void StartDurationTracking()
    {
        _ = Task.Run(async () =>
        {
            while (CurrentState == VoiceVisualizerState.Listening)
            {
                ListeningDuration = _timeProvider.Now - _listeningStartTime;
                await Task.Delay(100);
            }
        });
    }

    private void StopDurationTracking()
    {
        ListeningDuration = TimeSpan.Zero;
    }

    private void ScheduleAutoHide()
    {
        _autoHideCts?.Cancel();
        _autoHideCts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(3000, _autoHideCts.Token);
                if (!_autoHideCts.Token.IsCancellationRequested)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsVisible = false;
                        CurrentState = VoiceVisualizerState.Idle;
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when cancelled
            }
        });
    }
}
