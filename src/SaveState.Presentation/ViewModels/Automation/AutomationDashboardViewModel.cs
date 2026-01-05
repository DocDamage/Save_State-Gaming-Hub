using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Automation;

/// <summary>
/// ViewModel for the automation dashboard.
/// </summary>
public partial class AutomationDashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private int _activeTasksCount;

    [ObservableProperty]
    private string _activeTasksText = "running";

    [ObservableProperty]
    private int _macrosCount;

    [ObservableProperty]
    private string _macrosText = "saved";

    [ObservableProperty]
    private int _workflowsCount;

    [ObservableProperty]
    private string _workflowsText = "configured";

    [ObservableProperty]
    private int _executionsTodayCount;

    [ObservableProperty]
    private string _executionsTodayText = "successful";

    [ObservableProperty]
    private ObservableCollection<ScheduledTaskViewModel> _scheduledTasks = new();

    [ObservableProperty]
    private ObservableCollection<WorkflowViewModel> _workflows = new();

    [ObservableProperty]
    private ObservableCollection<MacroViewModel> _macros = new();

    [ObservableProperty]
    private ObservableCollection<ActivityItemViewModel> _recentActivity = new();

    public bool HasScheduledTasks => ScheduledTasks.Count > 0;
    public bool HasWorkflows => Workflows.Count > 0;
    public bool HasMacros => Macros.Count > 0;

    private readonly Services.IDialogService _dialogService;
    private readonly ILogger<AutomationDashboardViewModel> _logger;

    public AutomationDashboardViewModel(Services.IDialogService dialogService, ILogger<AutomationDashboardViewModel> logger)
    {
        _dialogService = dialogService;
        _logger = logger;
        // Design-time data
        LoadDesignTimeData();
    }

    private void LoadDesignTimeData()
    {
        ActiveTasksCount = 3;
        MacrosCount = 12;
        WorkflowsCount = 5;
        ExecutionsTodayCount = 47;

        // Scheduled Tasks
        ScheduledTasks.Add(new ScheduledTaskViewModel
        {
            Name = "Backup Save States",
            Schedule = "Daily at 2:00 AM",
            NextRun = "Next run: Tomorrow at 2:00 AM",
            IsEnabled = true,
            StatusColor = "#10B981"
        });

        ScheduledTasks.Add(new ScheduledTaskViewModel
        {
            Name = "Update Game Metadata",
            Schedule = "Weekly on Sunday",
            NextRun = "Next run: Sunday at 12:00 PM",
            IsEnabled = true,
            StatusColor = "#10B981"
        });

        ScheduledTasks.Add(new ScheduledTaskViewModel
        {
            Name = "Clean Temp Files",
            Schedule = "Every 3 days",
            NextRun = "Next run: Jan 6 at 10:00 AM",
            IsEnabled = false,
            StatusColor = "#6B7280"
        });

        // Workflows
        Workflows.Add(new WorkflowViewModel
        {
            Icon = "🎮",
            Name = "Gaming Session Startup",
            Description = "Optimizes system, launches Discord, and starts performance monitoring",
            StepsText = "5 steps"
        });

        Workflows.Add(new WorkflowViewModel
        {
            Icon = "💾",
            Name = "Save State Backup",
            Description = "Creates backup of all save states and uploads to cloud",
            StepsText = "3 steps"
        });

        Workflows.Add(new WorkflowViewModel
        {
            Icon = "📊",
            Name = "Weekly Report",
            Description = "Generates gaming statistics and sends email summary",
            StepsText = "4 steps"
        });

        // Macros
        Macros.Add(new MacroViewModel
        {
            Name = "Quick Save",
            Description = "F5 → Wait 500ms → Confirm",
            Duration = "0.5s",
            ActionsText = "3 actions"
        });

        Macros.Add(new MacroViewModel
        {
            Name = "Screenshot Combo",
            Description = "F12 → Wait 100ms → Enter",
            Duration = "0.1s",
            ActionsText = "3 actions"
        });

        Macros.Add(new MacroViewModel
        {
            Name = "Auto-Loot",
            Description = "E → Tab → E → Tab (loop)",
            Duration = "2.0s",
            ActionsText = "8 actions"
        });

        // Recent Activity
        RecentActivity.Add(new ActivityItemViewModel
        {
            Icon = "✅",
            Message = "Workflow 'Gaming Session Startup' completed",
            Timestamp = "2 minutes ago",
            Status = "Success",
            StatusColor = "#10B981"
        });

        RecentActivity.Add(new ActivityItemViewModel
        {
            Icon = "▶️",
            Message = "Macro 'Quick Save' executed",
            Timestamp = "15 minutes ago",
            Status = "Success",
            StatusColor = "#10B981"
        });

        RecentActivity.Add(new ActivityItemViewModel
        {
            Icon = "📅",
            Message = "Task 'Backup Save States' ran",
            Timestamp = "2 hours ago",
            Status = "Success",
            StatusColor = "#10B981"
        });

        RecentActivity.Add(new ActivityItemViewModel
        {
            Icon = "⚠️",
            Message = "Workflow 'Weekly Report' failed",
            Timestamp = "3 hours ago",
            Status = "Failed",
            StatusColor = "#EF4444"
        });

        // Wire up actions for design-time data
        foreach (var task in ScheduledTasks)
        {
            task.EditAction = EditTask;
            task.DeleteAction = DeleteTask;
            task.RunAction = RunTask;
        }

        foreach (var workflow in Workflows)
        {
            workflow.EditAction = EditWorkflow;
            workflow.RunAction = RunWorkflow;
        }

        foreach (var macro in Macros)
        {
            macro.EditAction = EditMacro;
            macro.PlayAction = PlayMacro;
        }
    }

    private async void EditTask(ScheduledTaskViewModel task)
    {
        try
        {
            var result = await _dialogService.ShowTaskCreationDialogAsync(task);
            if (result != null)
            {
                task.Name = result.Name;
                task.Schedule = result.Schedule;
                task.IsEnabled = result.IsEnabled;
                // Update other properties if needed
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit task {TaskName}", task.Name);
            await _dialogService.ShowErrorAsync("Error", $"Failed to edit task: {ex.Message}");
        }
    }

    private void DeleteTask(ScheduledTaskViewModel task)
    {
        ScheduledTasks.Remove(task);
        ActiveTasksCount = ScheduledTasks.Count(t => t.IsEnabled);
    }

    private void RunTask(ScheduledTaskViewModel task)
    {
        RecentActivity.Insert(0, new ActivityItemViewModel
        {
            Icon = "📅",
            Message = $"Task '{task.Name}' ran successfully",
            Timestamp = "Just now",
            Status = "Success",
            StatusColor = "#10B981"
        });
    }

    private async void EditWorkflow(WorkflowViewModel workflow)
    {
        try
        {
            var result = await _dialogService.ShowWorkflowCreationDialogAsync(workflow);
            if (result != null)
            {
                workflow.Name = result.Name;
                workflow.Description = result.Description;
                workflow.Icon = result.Icon;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit workflow {WorkflowName}", workflow.Name);
            await _dialogService.ShowErrorAsync("Error", $"Failed to edit workflow: {ex.Message}");
        }
    }

    private void RunWorkflow(WorkflowViewModel workflow)
    {
        RecentActivity.Insert(0, new ActivityItemViewModel
        {
            Icon = "🔄",
            Message = $"Workflow '{workflow.Name}' started",
            Timestamp = "Just now",
            Status = "Running",
            StatusColor = "#F59E0B"
        });
    }

    private async void EditMacro(MacroViewModel macro)
    {
        try
        {
            await _dialogService.ShowInformationAsync("Edit Macro", "Macro editing is not yet implemented.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open macro editor for {MacroName}", macro.Name);
        }
    }

    private void PlayMacro(MacroViewModel macro)
    {
        RecentActivity.Insert(0, new ActivityItemViewModel
        {
            Icon = "▶️",
            Message = $"Macro '{macro.Name}' played",
            Timestamp = "Just now",
            Status = "Success",
            StatusColor = "#10B981"
        });
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        await _dialogService.ShowAutomationSettingsDialogAsync();
    }

    [RelayCommand]
    private async Task CreateAutomation()
    {
        // For now, simplify to just showing a choice or defaulting to Task
        // In full implementation, this could be a wizard that asks what type of automation
        var result = await _dialogService.ShowTaskCreationDialogAsync();
        if (result != null)
        {
            ScheduledTasks.Add(new ScheduledTaskViewModel
            {
                Name = result.Name,
                Schedule = result.Schedule,
                IsEnabled = result.IsEnabled,
                NextRun = "Pending calculation...",
                StatusColor = result.IsEnabled ? "#10B981" : "#6B7280"
            });
            ActiveTasksCount = ScheduledTasks.Count(t => t.IsEnabled);

            var newTask = ScheduledTasks.Last();
            newTask.EditAction = EditTask;
            newTask.DeleteAction = DeleteTask;
            newTask.RunAction = RunTask;
        }
    }

    [RelayCommand]
    private async Task CreateTask()
    {
        var result = await _dialogService.ShowTaskCreationDialogAsync();
        if (result != null)
        {
            ScheduledTasks.Add(new ScheduledTaskViewModel
            {
                Name = result.Name,
                Schedule = result.Schedule,
                IsEnabled = result.IsEnabled,
                NextRun = "Pending calculation...",
                StatusColor = result.IsEnabled ? "#10B981" : "#6B7280"
            });
            ActiveTasksCount = ScheduledTasks.Count(t => t.IsEnabled);

            var newTask = ScheduledTasks.Last();
            newTask.EditAction = EditTask;
            newTask.DeleteAction = DeleteTask;
            newTask.RunAction = RunTask;
        }
    }

    [RelayCommand]
    private async Task CreateWorkflow()
    {
        var result = await _dialogService.ShowWorkflowCreationDialogAsync();
        if (result != null)
        {
            Workflows.Add(new WorkflowViewModel
            {
                Name = result.Name,
                Description = result.Description,
                Icon = result.Icon,
                StepsText = "0 steps"
            });
            WorkflowsCount = Workflows.Count;

            // Wire up actions
            var newWorkflow = Workflows.Last();
            newWorkflow.EditAction = EditWorkflow;
            newWorkflow.RunAction = RunWorkflow;
        }
    }

    [RelayCommand]
    private async Task RecordMacro()
    {
        var newMacro = await _dialogService.ShowMacroRecorderDialogAsync();
        if (newMacro != null)
        {
            // Wire up actions
            newMacro.EditAction = EditMacro;
            newMacro.PlayAction = PlayMacro;

            Macros.Add(newMacro);
            MacrosCount = Macros.Count;
        }
    }
}

/// <summary>
/// ViewModel for a scheduled task.
/// </summary>
public partial class ScheduledTaskViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _schedule = string.Empty;

    [ObservableProperty]
    private string _nextRun = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _statusColor = "#10B981";

    public Action<ScheduledTaskViewModel>? EditAction { get; set; }
    public Action<ScheduledTaskViewModel>? DeleteAction { get; set; }
    public Action<ScheduledTaskViewModel>? RunAction { get; set; }

    [RelayCommand]
    private void RunNow() => RunAction?.Invoke(this);

    [RelayCommand]
    private void Edit() => EditAction?.Invoke(this);

    [RelayCommand]
    private void Delete() => DeleteAction?.Invoke(this);
}

/// <summary>
/// ViewModel for a workflow.
/// </summary>
public partial class WorkflowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _icon = "🔄";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _stepsText = string.Empty;

    public Action<WorkflowViewModel>? EditAction { get; set; }
    public Action<WorkflowViewModel>? RunAction { get; set; }

    [RelayCommand]
    private void Run() => RunAction?.Invoke(this);

    [RelayCommand]
    private void Edit() => EditAction?.Invoke(this);
}

/// <summary>
/// ViewModel for a macro.
/// </summary>
public partial class MacroViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _duration = string.Empty;

    [ObservableProperty]
    private string _actionsText = string.Empty;

    public Action<MacroViewModel>? EditAction { get; set; }
    public Action<MacroViewModel>? PlayAction { get; set; }

    [RelayCommand]
    private void Play() => PlayAction?.Invoke(this);

    [RelayCommand]
    private void Edit() => EditAction?.Invoke(this);
}

/// <summary>
/// ViewModel for an activity item.
/// </summary>
public class ActivityItemViewModel
{
    public string Icon { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#10B981";
}
