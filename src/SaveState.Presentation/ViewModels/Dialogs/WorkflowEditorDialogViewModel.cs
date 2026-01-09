using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class WorkflowEditorDialogViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _workflowName = "New Workflow";

    [ObservableProperty]
    private ObservableCollection<WorkflowNodeViewModel> _nodes = new();

    public WorkflowEditorDialogViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;

        // Add default start node
        Nodes.Add(new WorkflowNodeViewModel("Start", "Trigger", 50, 50));
    }

    [RelayCommand]
    private void AddNode(string type)
    {
        // Simple positioning logic
        double x = 50 + (Nodes.Count * 20);
        double y = 50 + (Nodes.Count * 50);
        Nodes.Add(new WorkflowNodeViewModel(type, type, x, y));
    }

    [RelayCommand]
    private void Save()
    {
        // Save logic would go here
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(new WorkflowEditorResult(System.Guid.NewGuid(), WorkflowName, "Description", new System.Collections.Generic.List<WorkflowStepViewModel>()));
        }
    }

    [RelayCommand]
    private void Close()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(null);
        }
    }
}

public partial class WorkflowNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _type;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    public WorkflowNodeViewModel(string title, string type, double x, double y)
    {
        Title = title;
        Type = type;
        X = x;
        Y = y;
    }
}
