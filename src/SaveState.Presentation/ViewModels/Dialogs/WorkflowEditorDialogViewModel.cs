using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class WorkflowEditorDialogViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IWorkflowAutomationService _workflowService;
    private Guid? _workflowId;

    [ObservableProperty]
    private string _workflowName = "New Workflow";

    [ObservableProperty]
    private ObservableCollection<WorkflowNodeViewModel> _nodes = new();

    [ObservableProperty]
    private ObservableCollection<WorkflowConnectionViewModel> _connections = new();

    private WorkflowNodeViewModel? _selectedNode;

    public WorkflowEditorDialogViewModel(IDialogService dialogService, IWorkflowAutomationService workflowService, Guid? workflowId = null)
    {
        _dialogService = dialogService;
        _workflowService = workflowService;
        _workflowId = workflowId;

        if (_workflowId.HasValue)
        {
            _ = LoadWorkflowAsync(_workflowId.Value);
        }
        else
        {
            // Add default start node
            Nodes.Add(new WorkflowNodeViewModel("Start", "Trigger", 50, 50));
        }
    }

    private async Task LoadWorkflowAsync(Guid workflowId)
    {
        var result = await _workflowService.GetWorkflowAsync(workflowId);
        if (result.IsSuccess)
        {
            var workflow = result.Value;
            WorkflowName = workflow.Name;

            Nodes.Clear();
            Connections.Clear();

            // Reconstruct nodes from steps
            foreach (var step in workflow.Steps)
            {
                double x = 50, y = 50;
                if (step.Parameters.TryGetValue("X", out var xObj) && double.TryParse(xObj?.ToString(), out var xVal)) x = xVal;
                if (step.Parameters.TryGetValue("Y", out var yObj) && double.TryParse(yObj?.ToString(), out var yVal)) y = yVal;

                Nodes.Add(new WorkflowNodeViewModel(step.Name, step.StepType, x, y));
            }

            // Reconstruct connections (simplistic approach for now)
            for (int i = 0; i < Nodes.Count - 1; i++)
            {
                Connections.Add(new WorkflowConnectionViewModel(Nodes[i], Nodes[i+1]));
            }
        }
    }

    [RelayCommand]
    private void AddNode(string type)
    {
        double x = 50 + (Nodes.Count * 20);
        double y = 50 + (Nodes.Count * 50);

        string title = type switch
        {
            "Action" => "Game Action",
            "Condition" => "If / Else",
            "Delay" => "Wait",
            _ => type
        };

        Nodes.Add(new WorkflowNodeViewModel(title, type, x, y));
    }

    [RelayCommand]
    private void ConnectNodes()
    {
        if (Nodes.Count >= 2)
        {
            var source = Nodes[^2];
            var target = Nodes[^1];
            Connections.Add(new WorkflowConnectionViewModel(source, target));
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Nodes.Clear();
        Connections.Clear();
        Nodes.Add(new WorkflowNodeViewModel("Start", "Trigger", 50, 50));
    }

    [RelayCommand]
    private async Task Save()
    {
        var steps = Nodes.Select((n, index) => new WorkflowStepViewModel(
            n.Type,
            n.Title,
            new Dictionary<string, string> {
                { "X", n.X.ToString() },
                { "Y", n.Y.ToString() }
            },
            index
        )).ToList();

        // Map to Core WorkflowSteps
        var coreSteps = steps.Select(s => new SystemCommandStep(
            s.Name,
            "",
            s.StepType,
            s.Parameters.Select(p => $"{p.Key}={p.Value}").ToList()
        )).Cast<WorkflowStep>().ToList();

        var config = new WorkflowConfig(coreSteps);

        Result saveResult;
        Guid finalId = _workflowId ?? Guid.NewGuid();

        if (_workflowId.HasValue)
        {
            saveResult = await _workflowService.UpdateWorkflowAsync(_workflowId.Value, config);
        }
        else
        {
            var createResult = await _workflowService.CreateWorkflowAsync(config);
            saveResult = createResult;
            if (createResult.IsSuccess) finalId = createResult.Value.Id;
        }

        if (saveResult.IsSuccess)
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
                window?.Close(new WorkflowEditorResult(finalId, WorkflowName, "Created with Visual Editor", steps));
            }
        }
        else
        {
            await _dialogService.ShowErrorAsync("Save Error", $"Failed to save workflow: {saveResult.Error}");
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

    [ObservableProperty]
    private bool _isSelected;

    public WorkflowNodeViewModel(string title, string type, double x, double y)
    {
        Title = title;
        Type = type;
        X = x;
        Y = y;
    }
}

public partial class WorkflowConnectionViewModel : ObservableObject
{
    [ObservableProperty]
    private WorkflowNodeViewModel _source;

    [ObservableProperty]
    private WorkflowNodeViewModel _target;

    public WorkflowConnectionViewModel(WorkflowNodeViewModel source, WorkflowNodeViewModel target)
    {
        Source = source;
        Target = target;
    }
}
