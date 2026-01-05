using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class TaskCreationDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _schedule = "Daily at 12:00 PM";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private ObservableCollection<string> _scheduleOptions = new()
    {
        "Daily at 12:00 PM",
        "Weekly on Sunday",
        "Every 3 days",
        "On System Startup"
    };

    private readonly TaskCreationResult? _originalTask;

    public TaskCreationDialogViewModel(TaskCreationResult? existingTask = null)
    {
        if (existingTask != null)
        {
            Name = existingTask.Name;
            Schedule = existingTask.Schedule;
            IsEnabled = existingTask.IsEnabled;
            _originalTask = existingTask;
        }
    }

    public TaskCreationResult? Result { get; private set; }

    public void Save()
    {
        Result = new TaskCreationResult(Name, Schedule, IsEnabled);
    }
}
