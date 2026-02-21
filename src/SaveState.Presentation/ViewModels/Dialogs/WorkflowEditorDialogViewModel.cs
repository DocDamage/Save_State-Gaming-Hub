using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class WorkflowEditorDialogViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<WorkflowEditorDialogViewModel>? _logger;
    private Guid? _workflowId;

    // Validation constants
    private const int MaxWorkflowNameLength = 100;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorkflowNameValid))]
    private string _workflowName = "New Workflow";

    [ObservableProperty]
    private ObservableCollection<WorkflowNodeViewModel> _nodes = new();

    [ObservableProperty]
    private ObservableCollection<WorkflowConnectionViewModel> _connections = new();

    [ObservableProperty]
    private string _validationError = string.Empty;

    private WorkflowNodeViewModel? _selectedNode;

    /// <summary>
    /// Gets whether the workflow name is valid.
    /// </summary>
    public bool IsWorkflowNameValid => 
        !string.IsNullOrWhiteSpace(WorkflowName) && 
        WorkflowName.Length <= MaxWorkflowNameLength &&
        !InvalidCharsPattern.IsMatch(WorkflowName);

    public WorkflowEditorDialogViewModel(
        IDialogService dialogService, 
        IWorkflowAutomationService workflowService, 
        ILogger<WorkflowEditorDialogViewModel>? logger = null,
        Guid? workflowId = null)
    {
        _dialogService = dialogService;
        _workflowService = workflowService;
        _logger = logger;
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

    partial void OnWorkflowNameChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxWorkflowNameLength)
        {
            WorkflowName = value[..MaxWorkflowNameLength];
            return;
        }

        // Update validation error
        if (!IsWorkflowNameValid)
        {
            if (string.IsNullOrWhiteSpace(value))
                ValidationError = "Workflow name is required.";
            else if (value?.Length > MaxWorkflowNameLength)
                ValidationError = $"Name must not exceed {MaxWorkflowNameLength} characters.";
            else
                ValidationError = "Name contains invalid characters.";
        }
        else
        {
            ValidationError = string.Empty;
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
        // Validate workflow name
        if (!IsWorkflowNameValid)
        {
            await _dialogService.ShowErrorAsync("Validation Error", "Please enter a valid workflow name.");
            return;
        }

        // Validate that workflow has at least one node
        if (Nodes.Count == 0)
        {
            await _dialogService.ShowErrorAsync("Validation Error", "Workflow must have at least one node.");
            return;
        }

        try
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
                CloseDialog(new WorkflowEditorResult(finalId, WorkflowName.Trim(), "Created with Visual Editor", steps));
            }
            else
            {
                _logger?.LogError("Failed to save workflow: {Error}", saveResult.Error);
                await _dialogService.ShowErrorAsync("Save Error", $"Failed to save workflow: {saveResult.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while saving workflow");
            await _dialogService.ShowErrorAsync("Save Error", $"An unexpected error occurred: {ex.Message}");
        }
    }

    private void CloseDialog(WorkflowEditorResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
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
