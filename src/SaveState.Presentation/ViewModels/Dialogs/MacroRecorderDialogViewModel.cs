using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Common.Services;
using SaveState.Presentation.ViewModels.Automation;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class MacroRecorderDialogViewModel : ObservableObject
{
    private readonly IMacroService _macroService;
    private readonly ILogger<MacroRecorderDialogViewModel>? _logger;
    private readonly ITimeProvider _timeProvider;

    // Validation constants
    private const int MaxMacroNameLength = 100;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    private string _status = "Ready to record";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMacroNameValid))]
    private string _macroName = "New Macro";

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private string _elapsedTime = "00:00";

    [ObservableProperty]
    private string _actionsCountText = "0 actions recorded";

    [ObservableProperty]
    private string _validationError = string.Empty;

    private Stopwatch _stopwatch = new();
    private int _actionsCount;

    public MacroViewModel? Result { get; private set; }

    /// <summary>
    /// Gets whether the macro name is valid.
    /// </summary>
    public bool IsMacroNameValid => 
        !string.IsNullOrWhiteSpace(MacroName) && 
        MacroName.Length <= MaxMacroNameLength &&
        !InvalidCharsPattern.IsMatch(MacroName);

    public MacroRecorderDialogViewModel(
        IMacroService macroService,
        ITimeProvider timeProvider,
        ILogger<MacroRecorderDialogViewModel>? logger = null)
    {
        _macroService = macroService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    partial void OnMacroNameChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxMacroNameLength)
        {
            MacroName = value[..MaxMacroNameLength];
            return;
        }

        // Update validation error
        if (!IsMacroNameValid)
        {
            if (string.IsNullOrWhiteSpace(value))
                ValidationError = "Macro name is required.";
            else if (value?.Length > MaxMacroNameLength)
                ValidationError = $"Name must not exceed {MaxMacroNameLength} characters.";
            else
                ValidationError = "Name contains invalid characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ToggleRecording()
    {
        // Validate macro name before starting
        if (!IsRecording && !IsMacroNameValid)
        {
            Status = "Error: Please enter a valid macro name";
            return;
        }

        IsRecording = !IsRecording;
        if (IsRecording)
        {
            Status = "Recording...";
            _stopwatch.Start();

            // Start actual recording service
            var result = await _macroService.StartRecordingAsync(
                MacroName.Trim(),
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
        if (!IsMacroNameValid) return;

        Result = new MacroViewModel
        {
            Name = MacroName.Trim(),
            Description = $"Recorded at {_timeProvider.Now:t}",
            Duration = $"{_stopwatch.Elapsed.TotalSeconds:F1}s",
            ActionsText = $"{_actionsCount} actions"
        };
    }
}
