using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Services.DTOs;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// ViewModel for voice command control and monitoring.
/// </summary>
public partial class VoiceCommandViewModel : ObservableObject
{
    private readonly IVoiceCommandService _voiceCommandService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool isListening;

    [ObservableProperty]
    private string? lastRecognizedCommand;

    [ObservableProperty]
    private float lastConfidenceLevel;

    [ObservableProperty]
    private bool isAvailable = true;

    [ObservableProperty]
    private ObservableCollection<VoiceCommandDefinition> registeredCommands = new();

    [ObservableProperty]
    private ObservableCollection<VoiceCommandHistory> commandHistory = new();

    public VoiceCommandViewModel(
        IVoiceCommandService voiceCommandService,
        INotificationService notificationService)
    {
        _voiceCommandService = voiceCommandService;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        await LoadRegisteredCommandsAsync();
    }

    [RelayCommand]
    public async Task StartListening()
    {
        try
        {
            if (IsListening)
                return;

            var result = await _voiceCommandService.StartListeningAsync();
            if (result.IsSuccess)
            {
                IsListening = true;
                await _notificationService.ShowNotificationAsync("Voice command listening started", "Success");
            }
            else
            {
                await _notificationService.ShowErrorAsync($"Failed to start listening: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task StopListening()
    {
        try
        {
            if (!IsListening)
                return;

            var result = await _voiceCommandService.StopListeningAsync();
            if (result.IsSuccess)
            {
                IsListening = false;
                await _notificationService.ShowNotificationAsync("Voice command listening stopped", "Success");
            }
            else
            {
                await _notificationService.ShowErrorAsync($"Failed to stop listening: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task RegisterCommand(VoiceCommandDefinition command)
    {
        try
        {
            var result = await _voiceCommandService.RegisterCommandAsync(command);
            if (result.IsSuccess)
            {
                await _notificationService.ShowNotificationAsync($"Command '{command.CommandPhrase}' registered", "Success");
                await LoadRegisteredCommandsAsync();
            }
            else
            {
                await _notificationService.ShowErrorAsync($"Failed to register command: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task UnregisterCommand(string commandPhrase)
    {
        try
        {
            var result = await _voiceCommandService.UnregisterCommandAsync(commandPhrase);
            if (result.IsSuccess)
            {
                await _notificationService.ShowNotificationAsync($"Command '{commandPhrase}' unregistered", "Success");
                await LoadRegisteredCommandsAsync();
            }
            else
            {
                await _notificationService.ShowErrorAsync($"Failed to unregister command: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ClearHistory()
    {
        CommandHistory.Clear();
        await _notificationService.ShowNotificationAsync("Command history cleared", "Success");
    }

    private async Task LoadRegisteredCommandsAsync()
    {
        try
        {
            var result = await _voiceCommandService.GetRegisteredCommandsAsync();
            if (result.IsSuccess)
            {
                RegisteredCommands.Clear();
                foreach (var command in result.Value)
                {
                    RegisteredCommands.Add(command);
                }
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to load commands: {ex.Message}");
        }
    }

    public void RecordCommandRecognized(string recognizedText, float confidence)
    {
        LastRecognizedCommand = recognizedText;
        LastConfidenceLevel = confidence;

        var historyEntry = new VoiceCommandHistory(
            RecognizedText: recognizedText,
            Confidence: confidence,
            Timestamp: DateTime.Now,
            WasSuccessful: confidence > 0.7f);

        CommandHistory.Insert(0, historyEntry);

        // Keep history size reasonable
        while (CommandHistory.Count > 100)
        {
            CommandHistory.RemoveAt(CommandHistory.Count - 1);
        }
    }
}

/// <summary>
/// Represents a voice command in the history.
/// </summary>
public record VoiceCommandHistory(
    string RecognizedText,
    float Confidence,
    DateTime Timestamp,
    bool WasSuccessful);
