using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Models;
using SaveState.Core.Automation.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Automation;

/// <summary>
/// ViewModel for the visual workflow editor (Automation Studio).
/// </summary>
public sealed partial class WorkflowEditorViewModel : ObservableObject
{
    private readonly IAutomationStudioService _automationService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<WorkflowEditorViewModel> _logger;

    /// <summary>
    /// List of all workflows.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<WorkflowEditViewModel> _workflows = new();

    /// <summary>
    /// The currently selected workflow.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveWorkflowCommand), nameof(DeleteWorkflowCommand), nameof(DuplicateWorkflowCommand), nameof(ExecuteWorkflowCommand))]
    private WorkflowEditViewModel? _selectedWorkflow;

    /// <summary>
    /// The workflow currently being edited.
    /// </summary>
    [ObservableProperty]
    private WorkflowEditModel? _editingWorkflow;

    /// <summary>
    /// Available workflow templates.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<WorkflowTemplate> _templates = new();

    /// <summary>
    /// Available trigger types.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AutomationTrigger> _availableTriggers = new();

    /// <summary>
    /// Available action types.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AutomationAction> _availableActions = new();

    /// <summary>
    /// Visual nodes for the workflow canvas.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<WorkflowNodeViewModel> _workflowNodes = new();

    /// <summary>
    /// Whether the editor is in edit mode.
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// Whether a workflow is being created from template.
    /// </summary>
    [ObservableProperty]
    private bool _isCreatingFromTemplate;

    /// <summary>
    /// Search filter for workflows.
    /// </summary>
    [ObservableProperty]
    private string _searchFilter = string.Empty;

    /// <summary>
    /// Whether the view model is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Current error message.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Initializes a new instance of the WorkflowEditorViewModel.
    /// </summary>
    public WorkflowEditorViewModel(
        IAutomationStudioService automationService,
        ITimeProvider timeProvider,
        ILogger<WorkflowEditorViewModel> logger)
    {
        _automationService = automationService;
        _timeProvider = timeProvider;
        _logger = logger;

        _ = LoadDataAsync();
    }

    /// <summary>
    /// Loads initial data.
    /// </summary>
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Load workflows
            var workflowsResult = await _automationService.GetWorkflowsAsync();
            if (workflowsResult.IsSuccess)
            {
                Workflows.Clear();
                foreach (var workflow in workflowsResult.Value)
                {
                    Workflows.Add(new WorkflowEditViewModel(workflow));
                }
            }

            // Load templates
            var templatesResult = await _automationService.GetWorkflowTemplatesAsync();
            if (templatesResult.IsSuccess)
            {
                Templates.Clear();
                foreach (var template in templatesResult.Value)
                {
                    Templates.Add(template);
                }
            }

            // Load triggers and actions
            var triggersResult = await _automationService.GetAvailableTriggersAsync();
            if (triggersResult.IsSuccess)
            {
                AvailableTriggers = new ObservableCollection<AutomationTrigger>(triggersResult.Value);
            }

            var actionsResult = await _automationService.GetAvailableActionsAsync();
            if (actionsResult.IsSuccess)
            {
                AvailableActions = new ObservableCollection<AutomationAction>(actionsResult.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow editor data");
            ErrorMessage = "Failed to load data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new empty workflow.
    /// </summary>
    [RelayCommand]
    private void CreateNewWorkflow()
    {
        EditingWorkflow = new WorkflowEditModel
        {
            Id = Guid.NewGuid(),
            Name = "New Workflow",
            Description = "",
            IsEnabled = true,
            Trigger = AutomationTrigger.GameLaunched,
            TriggerConfig = TriggerConfiguration.Default(),
            Actions = new ObservableCollection<WorkflowActionEditModel>(),
            CreatedAt = _timeProvider.UtcNow
        };

        WorkflowNodes.Clear();
        IsEditing = true;
        IsCreatingFromTemplate = false;
    }

    /// <summary>
    /// Creates a workflow from a template.
    /// </summary>
    [RelayCommand]
    private void CreateFromTemplate(WorkflowTemplate template)
    {
        var workflow = template.CreateWorkflow(
            $"My {template.Name}",
            template.Description,
            _timeProvider);

        EditingWorkflow = WorkflowEditModel.FromWorkflow(workflow);
        IsEditing = true;
        IsCreatingFromTemplate = true;

        // Generate visual nodes
        GenerateWorkflowNodes();
    }

    /// <summary>
    /// Edits the selected workflow.
    /// </summary>
    [RelayCommand]
    private void EditSelectedWorkflow()
    {
        if (SelectedWorkflow?.Workflow is null) return;

        EditingWorkflow = WorkflowEditModel.FromWorkflow(SelectedWorkflow.Workflow);
        IsEditing = true;
        IsCreatingFromTemplate = false;

        GenerateWorkflowNodes();
    }

    /// <summary>
    /// Saves the current workflow.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveWorkflow))]
    private async Task SaveWorkflowAsync()
    {
        if (EditingWorkflow is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var workflow = EditingWorkflow.ToWorkflow(_timeProvider);

            // Validate
            var validationResult = await _automationService.ValidateWorkflowAsync(workflow);
            if (validationResult.IsSuccess && !validationResult.Value.IsValid)
            {
                ErrorMessage = string.Join("\n", validationResult.Value.Errors);
                return;
            }

            // Check if creating new or updating
            var isNew = !Workflows.Any(w => w.Id == workflow.Id);

            Result result;
            if (isNew)
            {
                var createResult = await _automationService.CreateWorkflowAsync(workflow);
                result = createResult.IsSuccess
                    ? Result.Success()
                    : Result.Failure(createResult.Error ?? "Failed to create workflow", createResult.ErrorType);
            }
            else
            {
                result = await _automationService.UpdateWorkflowAsync(workflow);
            }

            if (result.IsSuccess)
            {
                await LoadDataAsync();
                IsEditing = false;
                EditingWorkflow = null;

                // Select the saved workflow
                var savedVm = Workflows.FirstOrDefault(w => w.Id == workflow.Id);
                if (savedVm is not null)
                {
                    SelectedWorkflow = savedVm;
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to save workflow";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workflow");
            ErrorMessage = "An error occurred while saving. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSaveWorkflow => EditingWorkflow is not null && !string.IsNullOrWhiteSpace(EditingWorkflow.Name);

    /// <summary>
    /// Cancels the current edit.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        IsCreatingFromTemplate = false;
        EditingWorkflow = null;
        WorkflowNodes.Clear();
    }

    /// <summary>
    /// Deletes the selected workflow.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteWorkflow))]
    private async Task DeleteWorkflowAsync()
    {
        if (SelectedWorkflow is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _automationService.DeleteWorkflowAsync(SelectedWorkflow.Id);

            if (result.IsSuccess)
            {
                Workflows.Remove(SelectedWorkflow);
                SelectedWorkflow = null;
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to delete workflow";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workflow");
            ErrorMessage = "An error occurred while deleting. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanDeleteWorkflow => SelectedWorkflow is not null;

    /// <summary>
    /// Duplicates the selected workflow.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDuplicateWorkflow))]
    private async Task DuplicateWorkflowAsync()
    {
        if (SelectedWorkflow is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var newName = $"{SelectedWorkflow.Name} (Copy)";
            var result = await _automationService.DuplicateWorkflowAsync(SelectedWorkflow.Id, newName);

            if (result.IsSuccess)
            {
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to duplicate workflow";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate workflow");
            ErrorMessage = "An error occurred while duplicating. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanDuplicateWorkflow => SelectedWorkflow is not null;

    /// <summary>
    /// Executes the selected workflow manually.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteWorkflow))]
    private async Task ExecuteWorkflowAsync()
    {
        if (SelectedWorkflow is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var context = WorkflowExecutionContext.Create(
                SelectedWorkflow.Id,
                "Manual Execution",
                new Dictionary<string, object>(),
                _timeProvider);

            var result = await _automationService.ExecuteWorkflowAsync(SelectedWorkflow.Id, context);

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Workflow execution failed";
            }
            else
            {
                await LoadDataAsync(); // Refresh to get updated execution count
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow");
            ErrorMessage = "An error occurred during execution. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanExecuteWorkflow => SelectedWorkflow is not null;

    /// <summary>
    /// Toggles the enabled state of the selected workflow.
    /// </summary>
    [RelayCommand]
    private async Task ToggleWorkflowEnabledAsync()
    {
        if (SelectedWorkflow is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Result result;
            if (SelectedWorkflow.IsEnabled)
            {
                result = await _automationService.DisableWorkflowAsync(SelectedWorkflow.Id);
            }
            else
            {
                result = await _automationService.EnableWorkflowAsync(SelectedWorkflow.Id);
            }

            if (result.IsSuccess)
            {
                // Refresh to get updated state
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to toggle workflow state";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle workflow state");
            ErrorMessage = "An error occurred. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Adds an action to the current workflow.
    /// </summary>
    [RelayCommand]
    private void AddAction(AutomationAction actionType)
    {
        if (EditingWorkflow is null) return;

        var action = new WorkflowActionEditModel
        {
            Id = Guid.NewGuid(),
            Type = actionType,
            Parameters = new Dictionary<string, object>(),
            Order = EditingWorkflow.Actions.Count + 1,
            DelaySeconds = null
        };

        EditingWorkflow.Actions.Add(action);
        GenerateWorkflowNodes();
    }

    /// <summary>
    /// Removes an action from the current workflow.
    /// </summary>
    [RelayCommand]
    private void RemoveAction(WorkflowActionEditModel action)
    {
        if (EditingWorkflow is null) return;

        EditingWorkflow.Actions.Remove(action);

        // Reorder remaining actions
        for (int i = 0; i < EditingWorkflow.Actions.Count; i++)
        {
            EditingWorkflow.Actions[i].Order = i + 1;
        }

        GenerateWorkflowNodes();
    }

    /// <summary>
    /// Moves an action up in the execution order.
    /// </summary>
    [RelayCommand]
    private void MoveActionUp(WorkflowActionEditModel action)
    {
        if (EditingWorkflow is null || action.Order <= 1) return;

        var index = action.Order - 1;
        var otherAction = EditingWorkflow.Actions.FirstOrDefault(a => a.Order == action.Order - 1);

        if (otherAction is not null)
        {
            otherAction.Order = action.Order;
            action.Order = index;
        }

        GenerateWorkflowNodes();
    }

    /// <summary>
    /// Moves an action down in the execution order.
    /// </summary>
    [RelayCommand]
    private void MoveActionDown(WorkflowActionEditModel action)
    {
        if (EditingWorkflow is null || action.Order >= EditingWorkflow.Actions.Count) return;

        var otherAction = EditingWorkflow.Actions.FirstOrDefault(a => a.Order == action.Order + 1);

        if (otherAction is not null)
        {
            otherAction.Order = action.Order;
            action.Order = action.Order + 1;
        }

        GenerateWorkflowNodes();
    }

    /// <summary>
    /// Refreshes the workflow list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// Generates visual nodes from the current workflow.
    /// </summary>
    private void GenerateWorkflowNodes()
    {
        WorkflowNodes.Clear();

        if (EditingWorkflow is null) return;

        // Add trigger node
        var triggerNode = new WorkflowNodeViewModel
        {
            Id = Guid.NewGuid(),
            Type = NodeType.Trigger,
            Label = EditingWorkflow.Trigger.ToString(),
            X = 100,
            Y = 100,
            Icon = GetTriggerIcon(EditingWorkflow.Trigger)
        };
        WorkflowNodes.Add(triggerNode);

        // Add action nodes
        double y = 250;
        foreach (var action in EditingWorkflow.Actions.OrderBy(a => a.Order))
        {
            var actionNode = new WorkflowNodeViewModel
            {
                Id = action.Id,
                Type = NodeType.Action,
                Label = action.Type.ToString(),
                X = 100,
                Y = y,
                Icon = GetActionIcon(action.Type),
                DelaySeconds = action.DelaySeconds
            };
            WorkflowNodes.Add(actionNode);
            y += 150;
        }
    }

    private static string GetTriggerIcon(AutomationTrigger trigger) => trigger switch
    {
        AutomationTrigger.GameLaunched => "Play",
        AutomationTrigger.GameClosed => "Stop",
        AutomationTrigger.AchievementUnlocked => "Trophy",
        AutomationTrigger.TimeOfDay => "Clock",
        AutomationTrigger.DayOfWeek => "Calendar",
        AutomationTrigger.HardwareChange => "Hardware",
        AutomationTrigger.NotificationReceived => "Bell",
        AutomationTrigger.SaveStateCreated => "Save",
        AutomationTrigger.PlaytimeMilestone => "Timer",
        _ => "Bolt"
    };

    private static string GetActionIcon(AutomationAction action) => action switch
    {
        AutomationAction.LaunchGame => "Play",
        AutomationAction.EnableBlueLightFilter => "Eye",
        AutomationAction.SetDoNotDisturb => "BellOff",
        AutomationAction.SendNotification => "Message",
        AutomationAction.AdjustVolume => "Volume",
        AutomationAction.ChangeDisplaySettings => "Monitor",
        AutomationAction.PostToDiscord => "Chat",
        AutomationAction.StartRecording => "Record",
        AutomationAction.EnablePerformanceMode => "Zap",
        AutomationAction.RunScript => "Code",
        AutomationAction.AdjustRgbLighting => "Palette",
        AutomationAction.LaunchApplication => "App",
        AutomationAction.CloseApplication => "X",
        _ => "Bolt"
    };
}

/// <summary>
/// ViewModel wrapper for a workflow.
/// </summary>
public sealed class WorkflowEditViewModel : ObservableObject
{
    private readonly Workflow _workflow;
    private bool _isEnabled;

    public WorkflowEditViewModel(Workflow workflow)
    {
        _workflow = workflow;
        _isEnabled = workflow.IsEnabled;
    }

    public Guid Id => _workflow.Id;
    public string Name => _workflow.Name;
    public string Description => _workflow.Description;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public AutomationTrigger Trigger => _workflow.Trigger;
    public int ActionCount => _workflow.Actions.Count;
    public DateTime? LastExecuted => _workflow.LastExecuted;
    public int ExecutionCount => _workflow.ExecutionCount;
    public Workflow Workflow => _workflow;

    public string TriggerDisplay => Trigger.ToString();
    public string StatusDisplay => IsEnabled ? "Enabled" : "Disabled";
}

/// <summary>
/// Edit model for creating/modifying workflows.
/// </summary>
public sealed class WorkflowEditModel : ObservableObject
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required bool IsEnabled { get; set; }
    public required AutomationTrigger Trigger { get; set; }
    public required TriggerConfiguration TriggerConfig { get; set; }
    public required ObservableCollection<WorkflowActionEditModel> Actions { get; set; }
    public required DateTime CreatedAt { get; set; }

    public static WorkflowEditModel FromWorkflow(Workflow workflow)
    {
        return new WorkflowEditModel
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description,
            IsEnabled = workflow.IsEnabled,
            Trigger = workflow.Trigger,
            TriggerConfig = workflow.TriggerConfig,
            Actions = new ObservableCollection<WorkflowActionEditModel>(
                workflow.Actions.Select(a => new WorkflowActionEditModel
                {
                    Id = a.Id,
                    Type = a.Type,
                    Parameters = new Dictionary<string, object>(a.Parameters),
                    Order = a.Order,
                    DelaySeconds = a.DelaySeconds
                })),
            CreatedAt = workflow.CreatedAt
        };
    }

    public Workflow ToWorkflow(ITimeProvider timeProvider)
    {
        return new Workflow
        {
            Id = Id,
            Name = Name,
            Description = Description,
            IsEnabled = IsEnabled,
            Trigger = Trigger,
            TriggerConfig = TriggerConfig,
            Actions = Actions.Select(a => new WorkflowAction
            {
                Id = a.Id,
                Type = a.Type,
                Parameters = a.Parameters,
                Order = a.Order,
                DelaySeconds = a.DelaySeconds
            }).ToList(),
            CreatedAt = CreatedAt,
            LastExecuted = null,
            ExecutionCount = 0
        };
    }
}

/// <summary>
/// Edit model for workflow actions.
/// </summary>
public sealed class WorkflowActionEditModel : ObservableObject
{
    private int _order;

    public required Guid Id { get; set; }
    public required AutomationAction Type { get; set; }
    public required Dictionary<string, object> Parameters { get; set; }
    public required int Order
    {
        get => _order;
        set => SetProperty(ref _order, value);
    }
    public required int? DelaySeconds { get; set; }
}

/// <summary>
/// ViewModel for a visual workflow node.
/// </summary>
public sealed class WorkflowNodeViewModel : ObservableObject
{
    public required Guid Id { get; set; }
    public required NodeType Type { get; set; }
    public required string Label { get; set; }
    public required double X { get; set; }
    public required double Y { get; set; }
    public required string Icon { get; set; }
    public int? DelaySeconds { get; set; }

    public bool HasDelay => DelaySeconds.HasValue && DelaySeconds.Value > 0;
    public string DelayText => HasDelay ? $"{DelaySeconds}s delay" : string.Empty;
}
