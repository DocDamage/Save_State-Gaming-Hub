using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class WorkflowCreationDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _icon = "🔄";

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

    public void Save()
    {
        Result = new WorkflowCreationResult(Name, Description, Icon);
    }
}
