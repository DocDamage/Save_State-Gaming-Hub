using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class WorkflowCreationDialogViewModel : ObservableObject
{
    // Validation constants
    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 500;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _icon = "🔄";

    [ObservableProperty]
    private string _validationError = string.Empty;

    /// <summary>
    /// Gets whether the name is valid.
    /// </summary>
    public bool IsNameValid => 
        !string.IsNullOrWhiteSpace(Name) && 
        Name.Length <= MaxNameLength &&
        !InvalidCharsPattern.IsMatch(Name);

    /// <summary>
    /// Gets whether the description is valid.
    /// </summary>
    public bool IsDescriptionValid => 
        Description.Length <= MaxDescriptionLength &&
        !InvalidCharsPattern.IsMatch(Description);

    /// <summary>
    /// Gets whether there are any validation errors.
    /// </summary>
    public bool HasValidationErrors => !IsNameValid || !IsDescriptionValid;

    /// <summary>
    /// Gets whether the save button should be enabled.
    /// </summary>
    public bool CanSave => IsNameValid;

    public WorkflowCreationDialogViewModel(WorkflowCreationResult? existingWorkflow = null)
    {
        if (existingWorkflow != null)
        {
            Name = existingWorkflow.Name;
            Description = existingWorkflow.Description;
            Icon = existingWorkflow.Icon;
        }
    }

    public WorkflowCreationResult? Result { get; private set; }

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

    partial void OnDescriptionChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxDescriptionLength)
        {
            Description = value[..MaxDescriptionLength];
            return;
        }
        UpdateValidationError();
    }

    private void UpdateValidationError()
    {
        if (!IsNameValid)
        {
            if (string.IsNullOrWhiteSpace(Name))
                ValidationError = "Workflow name is required.";
            else if (Name.Length > MaxNameLength)
                ValidationError = $"Name must not exceed {MaxNameLength} characters.";
            else
                ValidationError = "Name contains invalid characters.";
        }
        else if (!IsDescriptionValid)
        {
            ValidationError = $"Description must not exceed {MaxDescriptionLength} characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }
    }

    public void Save()
    {
        if (!CanSave) return;

        Result = new WorkflowCreationResult(Name.Trim(), Description.Trim(), Icon);
    }
}
