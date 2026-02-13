using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Common.Services;
using SaveState.Presentation.ViewModels.Automation;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class MacroRecorderDialogViewModel : ObservableObject
{
    private readonly IMacroService _macroService;
    private readonly ILogger<MacroRecorderDialogViewModel>? _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private string _status = "Ready to record";

    [ObservableProperty]
    private string _macroName = "New Macro";

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private string _elapsedTime = "00:00";

    [ObservableProperty]
    private string _actionsCountText = "0 actions recorded";

    private Stopwatch _stopwatch = new();
    private int _actionsCount;

    public MacroViewModel? Result { get; private set; }

    public MacroRecorderDialogViewModel(
        IMacroService macroService,
        ITimeProvider timeProvider,
        ILogger<MacroRecorderDialogViewModel>? logger = null)
    {
        _macroService = macroService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    [RelayCommand]
    private async Task ToggleRecording()
    {
        IsRecording = !IsRecording;
        if (IsRecording)
        {
            Status = "Recording...";
            _stopwatch.Start();

            // Start actual recording service
            var result = await _macroService.StartRecordingAsync(
                MacroName,
                $"Recorded at {_timeProvider.Now:t}");

            if (result.IsFailure)
            {
                _logger?.LogError("Failed to start macro recording: {Error}", result.Error);
                Status = $"Error: {result.Error}";
                IsRecording = false;
                _stopwatch.Stop();
            }
            else
            {
                _logger?.LogInformation("Started recording macro: {Name}", MacroName);
                _actionsCount = 0;
                ActionsCountText = "0 actions recorded";
            }
        }
        else
        {
            Status = "Saving...";
            _stopwatch.Stop();

            // Stop actual recording service
            var result = await _macroService.StopRecordingAsync();

            if (result.IsSuccess)
            {
                _logger?.LogInformation("Stopped recording macro: {Name}", result.Value.Name);
                _actionsCount = result.Value.Actions.Count;
                ActionsCountText = $"{_actionsCount} actions recorded";
                Status = "Recording saved";
            }
            else
            {
                _logger?.LogError("Failed to stop macro recording: {Error}", result.Error);
                Status = $"Error: {result.Error}";
            }
        }
    }

    public void Save()
    {
        Result = new MacroViewModel
        {
            Name = MacroName,
            Description = $"Recorded at {_timeProvider.Now:t}",
            Duration = $"{_stopwatch.Elapsed.TotalSeconds:F1}s",
            ActionsText = $"{_actionsCount} actions"
        };
    }
}
