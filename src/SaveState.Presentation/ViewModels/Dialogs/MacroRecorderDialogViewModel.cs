using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.ViewModels.Automation;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class MacroRecorderDialogViewModel : ObservableObject
{
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

    [RelayCommand]
    private void ToggleRecording()
    {
        IsRecording = !IsRecording;
        if (IsRecording)
        {
            Status = "Recording...";
            _stopwatch.Start();
            // TODO: Start actual recording service
        }
        else
        {
            Status = "Paused";
            _stopwatch.Stop();
            // TODO: Stop actual recording service
        }
    }

    public void Save()
    {
        Result = new MacroViewModel
        {
            Name = MacroName,
            Description = $"Recorded at {DateTime.Now:t}",
            Duration = $"{_stopwatch.Elapsed.TotalSeconds:F1}s",
            ActionsText = $"{_actionsCount} actions"
        };
    }
}
