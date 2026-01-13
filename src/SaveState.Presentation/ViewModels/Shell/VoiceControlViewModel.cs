using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Services.DTOs;
using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Services.DTOs;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// ViewModel for Voice &amp; AI Control panel.
/// </summary>
public partial class VoiceControlViewModel : ObservableObject
{
    private readonly IVoiceCommandService _voiceCommandService;
    private readonly ISpeechRecognitionService _speechRecognitionService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<VoiceControlViewModel> _logger;

    [ObservableProperty]
    private bool _isListening;

    [ObservableProperty]
    private bool _isContinuousMode;

    [ObservableProperty]
    private string _currentLanguage = "en-US";

    [ObservableProperty]
    private double _microphoneLevel;

    [ObservableProperty]
    private string _lastRecognizedText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Voice control ready";

    [ObservableProperty]
    private bool _isCalibrating;

    [ObservableProperty]
    private string _selectedCommandPhrase = string.Empty;

    public ObservableCollection<VoiceCommandDefinition> RegisteredCommands { get; } = new();
    public ObservableCollection<string> AvailableLanguages { get; } = new();
    public ObservableCollection<string> RecentTranscriptions { get; } = new();

    // Statistics
    [ObservableProperty]
    private int _totalCommandsRecognized;

    [ObservableProperty]
    private int _totalCommandsExecuted;

    [ObservableProperty]
    private int _failedCommands;

    public VoiceControlViewModel(
        IVoiceCommandService voiceCommandService,
        ISpeechRecognitionService speechRecognitionService,
        INotificationService notificationService,
        ILogger<VoiceControlViewModel> logger)
    {
        _voiceCommandService = voiceCommandService;
        _speechRecognitionService = speechRecognitionService;
        _notificationService = notificationService;
        _logger = logger;

        // Subscribe to events
        _voiceCommandService.VoiceCommandRecognized += OnVoiceCommandRecognized;
        _voiceCommandService.ListeningStatusChanged += OnListeningStatusChanged;
        _speechRecognitionService.SpeechRecognized += OnSpeechRecognized;
        _speechRecognitionService.SpeechRecognitionError += OnSpeechRecognitionError;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await LoadRegisteredCommandsAsync();
            await LoadAvailableLanguagesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize voice control");
            StatusMessage = "Initialization failed";
        }
    }

    [RelayCommand]
    private async Task ToggleListeningAsync()
    {
        try
        {
            if (IsListening)
            {
                var result = await _voiceCommandService.StopListeningAsync();
                if (result.IsSuccess)
                {
                    StatusMessage = "Voice control stopped";
                    _notificationService.ShowInfo("Stopped listening", "Voice Control");
                }
            }
            else
            {
                var result = await _voiceCommandService.StartListeningAsync();
                if (result.IsSuccess)
                {
                    StatusMessage = "Listening for commands...";
                    _notificationService.ShowInfo("Started listening", "Voice Control");
                }
            }

            IsListening = _voiceCommandService.IsListening;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle listening");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ToggleContinuousModeAsync()
    {
        try
        {
            if (IsContinuousMode)
            {
                var result = await _speechRecognitionService.StopContinuousRecognitionAsync();
                if (result.IsSuccess)
                {
                    StatusMessage = "Continuous mode stopped";
                    _notificationService.ShowInfo("Continuous mode disabled", "Speech Recognition");
                }
            }
            else
            {
                var result = await _speechRecognitionService.StartContinuousRecognitionAsync();
                if (result.IsSuccess)
                {
                    StatusMessage = "Continuous mode active";
                    _notificationService.ShowInfo("Continuous mode enabled", "Speech Recognition");
                }
            }

            IsContinuousMode = _speechRecognitionService.IsContinuousRecognitionActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle continuous mode");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CalibrateMicrophoneAsync()
    {
        try
        {
            IsCalibrating = true;
            StatusMessage = "Calibrating microphone...";

            var result = await _speechRecognitionService.CalibrateMicrophoneAsync();

            if (result.IsSuccess)
            {
                StatusMessage = "Microphone calibrated successfully";
                _notificationService.ShowInfo("Calibration complete", "Microphone");
                _logger.LogInformation("Microphone calibration completed");
            }
            else
            {
                StatusMessage = $"Calibration failed: {result.Error}";
                _notificationService.ShowError($"Calibration failed: {result.Error}", "Microphone");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calibrate microphone");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsCalibrating = false;
        }
    }

    [RelayCommand]
    private async Task ChangeLanguageAsync(string languageCode)
    {
        try
        {
            var result = await _speechRecognitionService.SetLanguageAsync(languageCode);

            if (result.IsSuccess)
            {
                CurrentLanguage = languageCode;
                StatusMessage = $"Language changed to {languageCode}";
                _notificationService.ShowInfo($"Changed to {languageCode}", "Language");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change language");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshCommandsAsync()
    {
        await LoadRegisteredCommandsAsync();
        _notificationService.ShowInfo("Refreshed command list", "Commands");
    }

    [RelayCommand]
    private async Task DeleteCommandAsync(VoiceCommandDefinition command)
    {
        try
        {
            var result = await _voiceCommandService.UnregisterCommandAsync(command.CommandPhrase);

            if (result.IsSuccess)
            {
                RegisteredCommands.Remove(command);
                StatusMessage = $"Deleted command: {command.CommandPhrase}";
                _notificationService.ShowInfo($"Deleted: {command.CommandPhrase}", "Command");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete command");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestCommandAsync(VoiceCommandDefinition command)
    {
        try
        {
            var result = await _voiceCommandService.ProcessVoiceCommandAsync(command.CommandPhrase);

            if (result.IsSuccess)
            {
                StatusMessage = $"Command executed: {command.CommandPhrase}";
                _notificationService.ShowSuccess($"Executed: {command.CommandPhrase}", "Test");
                TotalCommandsExecuted++;
            }
            else
            {
                FailedCommands++;
                _notificationService.ShowError(result.Error ?? "Unknown error", "Test Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test command");
            FailedCommands++;
        }
    }

    private async Task LoadRegisteredCommandsAsync()
    {
        try
        {
            var result = await _voiceCommandService.GetRegisteredCommandsAsync();

            if (result.IsSuccess && result.Value != null)
            {
                RegisteredCommands.Clear();
                foreach (var command in result.Value)
                {
                    RegisteredCommands.Add(command);
                }
                StatusMessage = $"Loaded {RegisteredCommands.Count} commands";
                _logger.LogInformation("Loaded {Count} voice commands", RegisteredCommands.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load registered commands");
        }
    }

    private async Task LoadAvailableLanguagesAsync()
    {
        try
        {
            var result = await _speechRecognitionService.GetAvailableLanguagesAsync();

            if (result.IsSuccess && result.Value != null)
            {
                AvailableLanguages.Clear();
                foreach (var lang in result.Value)
                {
                    AvailableLanguages.Add(lang.Code);
                }
                _logger.LogInformation("Loaded {Count} languages", AvailableLanguages.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load available languages");
        }
    }

    private void OnVoiceCommandRecognized(object? sender, VoiceCommandRecognizedEventArgs e)
    {
        TotalCommandsRecognized++;
        LastRecognizedText = e.Result.RecognizedText;
        StatusMessage = $"Recognized: {e.Result.RecognizedText}";

        if (RecentTranscriptions.Count >= 10)
        {
            RecentTranscriptions.RemoveAt(0);
        }
        RecentTranscriptions.Add($"{DateTime.Now:HH:mm:ss} - {e.Result.RecognizedText}");
    }

    private void OnListeningStatusChanged(object? sender, ListeningStatusChangedEventArgs e)
    {
        IsListening = e.IsListening;
        StatusMessage = e.IsListening ? "Listening..." : "Not listening";
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        LastRecognizedText = e.Result.RecognizedText;

        if (RecentTranscriptions.Count >= 10)
        {
            RecentTranscriptions.RemoveAt(0);
        }
        RecentTranscriptions.Add($"{DateTime.Now:HH:mm:ss} - {e.Result.RecognizedText}");
    }

    private void OnSpeechRecognitionError(object? sender, SpeechRecognitionErrorEventArgs e)
    {
        StatusMessage = $"Recognition error: {e.ErrorMessage}";
        _logger.LogWarning("Speech recognition error: {Error}", e.ErrorMessage);
    }
}
