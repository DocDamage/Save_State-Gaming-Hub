using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class TaskCreationDialogViewModel : ObservableObject
{
    // Validation constants
    private const int MaxNameLength = 100;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _schedule = "Daily at 12:00 PM";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private string _validationError = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _scheduleOptions = new()
    {
        "Daily at 12:00 PM",
        "Weekly on Sunday",
        "Every 3 days",
        "On System Startup"
    };

    private readonly TaskCreationResult? _originalTask;

    /// <summary>
    /// Gets whether the name is valid.
    /// </summary>
    public bool IsNameValid => 
        !string.IsNullOrWhiteSpace(Name) && 
        Name.Length <= MaxNameLength &&
        !InvalidCharsPattern.IsMatch(Name);

    /// <summary>
    /// Gets whether the save button should be enabled.
    /// </summary>
    public bool CanSave => IsNameValid;

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

    partial void OnNameChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxNameLength)
        {
            Name = value[..MaxNameLength];
            return;
        }

        UpdateValidationError();
    }

    private void UpdateValidationError()
    {
        if (!IsNameValid)
        {
            if (string.IsNullOrWhiteSpace(Name))
                ValidationError = "Task name is required.";
            else if (Name.Length > MaxNameLength)
                ValidationError = $"Name must not exceed {MaxNameLength} characters.";
            else
                ValidationError = "Name contains invalid characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }
    }

    public void Save()
    {
        if (!CanSave) return;

        Result = new TaskCreationResult(Name.Trim(), Schedule, IsEnabled);
    }
}
