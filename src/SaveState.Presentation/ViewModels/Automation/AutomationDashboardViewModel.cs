using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MediatR;
using SaveState.Presentation.Services;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common.Services;
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

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private MacroMarketplaceViewModel _macroMarketplace;

    public bool HasScheduledTasks => ScheduledTasks.Count > 0;
    public bool HasWorkflows => Workflows.Count > 0;
    public bool HasMacros => Macros.Count > 0;

    private readonly Services.IDialogService _dialogService;
    private readonly IWorkflowAutomationService _workflowService;
    private readonly IMacroManager _macroManager;
    private readonly ILogger<AutomationDashboardViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    public AutomationDashboardViewModel(
        Services.IDialogService dialogService,
        IWorkflowAutomationService workflowService,
        IMacroManager macroManager,
        ILogger<AutomationDashboardViewModel> logger,
        ILoggerFactory loggerFactory,
        IMediator mediator,
        INotificationService notificationService,
        ITimeProvider timeProvider)
    {
        _dialogService = dialogService;
        _workflowService = workflowService;
        _macroManager = macroManager;
        _logger = logger;
        _timeProvider = timeProvider;

        MacroMarketplace = new MacroMarketplaceViewModel(mediator, macroManager, notificationService, timeProvider);

        // Load data
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await LoadWorkflowsAsync();
        await LoadMacrosAsync();
        await LoadScheduledTasksAsync();
        await LoadActivityHistoryAsync();
    }


    private async Task LoadWorkflowsAsync()
    {
        try
        {
            var result = await _workflowService.GetAllWorkflowsAsync();
            if (result.IsSuccess)
            {
                Workflows.Clear();
                foreach (var wf in result.Value)
                {
                    var vm = new WorkflowViewModel
                    {
                        Id = wf.Id,
                        Name = wf.Name,
                        Description = wf.Description,
                        StepsText = $"{wf.Config.Steps.Count} steps",
                        Icon = GetWorkflowIcon(wf.Config)
                    };

                    vm.EditAction = EditWorkflow;
                    vm.RunAction = RunWorkflow;

                    Workflows.Add(vm);
                }
                WorkflowsCount = Workflows.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflows");
        }
    }

    private async Task LoadMacrosAsync()
    {
        try
        {
            // For now, search all macros. In future, might want to filter or paginate
            var result = await _macroManager.SearchMacrosAsync(string.Empty, new MacroSearchFilters(), default);
            if (result.IsSuccess)
            {
                Macros.Clear();
                foreach (var macro in result.Value)
                {
                    var vm = new MacroViewModel
                    {
                        Id = macro.Id,
                        Name = macro.Name,
                        Description = macro.Description,
                        Duration = CalculateMacroDuration(macro),
                        ActionsText = $"{macro.Actions.Count} actions"
                    };

                    vm.EditAction = EditMacro;
                    vm.PlayAction = PlayMacro;

                    Macros.Add(vm);
                }
                MacrosCount = Macros.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load macros");
        }
    }

    private async Task LoadScheduledTasksAsync()
    {
        try
        {
            var result = await _workflowService.GetAllWorkflowsAsync();
            if (result.IsSuccess)
            {
                ScheduledTasks.Clear();
                foreach (var wf in result.Value.Where(w => w.Config.Trigger == WorkflowTrigger.Scheduled))
                {
                    var task = new ScheduledTaskViewModel
                    {
                        Name = wf.Name,
                        Schedule = wf.Config.Trigger == SaveState.Core.Automation.Services.DTOs.WorkflowTrigger.Scheduled ? "Scheduled" : "Manual",
                        IsEnabled = wf.IsEnabled,
                        NextRun = "Calculating...",
                        StatusColor = wf.IsEnabled ? "#10B981" : "#6B7280"
                    };

                    task.EditAction = EditTask;
                    task.DeleteAction = DeleteTask;
                    task.RunAction = RunTask;

                    ScheduledTasks.Add(task);
                }
                ActiveTasksCount = ScheduledTasks.Count(t => t.IsEnabled);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scheduled tasks");
        }
    }

    private async Task LoadActivityHistoryAsync()
    {
        try
        {
            var allWorkflows = await _workflowService.GetAllWorkflowsAsync();
            if (!allWorkflows.IsSuccess) return;

            var allHistory = new List<ActivityItemViewModel>();
            int totalExecutions = 0;

            foreach (var wf in allWorkflows.Value)
            {
                var historyResult = await _workflowService.GetExecutionHistoryAsync(wf.Id);
                if (historyResult.IsSuccess)
                {
                    totalExecutions += historyResult.Value.Count;
                    foreach (var exec in historyResult.Value)
                    {
                        allHistory.Add(new ActivityItemViewModel
                        {
                            ReferenceId = wf.Id,
                            Icon = exec.Success ? "✅" : "❌",
                            Message = $"Workflow '{wf.Name}' {(exec.Success ? "completed" : "failed")}",
                            Timestamp = GetTimeAgo(exec.StartedAt),
                            Status = exec.Success ? "Success" : "Failed",
                            StatusColor = exec.Success ? "#10B981" : "#EF4444"
                        });
                    }
                }
            }

            ExecutionsTodayCount = totalExecutions;
            RecentActivity.Clear();
            foreach (var item in allHistory.OrderByDescending(h => h.Timestamp).Take(20))
            {
                RecentActivity.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load activity history");
        }
    }

    private string GetTimeAgo(DateTime dateTime)
    {
        var span = _timeProvider.UtcNow - dateTime;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return $"{(int)span.TotalDays}d ago";
    }

    private string GetWorkflowIcon(WorkflowConfig config)
    {
        return config.Trigger switch
        {
            WorkflowTrigger.Manual => "👆",
            WorkflowTrigger.Scheduled => "⏰",
            WorkflowTrigger.OnGameLaunch => "🚀",
            WorkflowTrigger.OnGameExit => "🛑",
            WorkflowTrigger.OnEvent => "⚡",
            _ => "⚡"
        };
    }

    private string CalculateMacroDuration(SaveState.Core.Automation.Services.DTOs.Macro macro)
    {
        if (macro.Actions.Count == 0) return "0s";

        // Duration is roughly the timestamp of the last action
        var lastAction = macro.Actions[macro.Actions.Count - 1];
        var duration = lastAction.Timestamp;

        if (duration.TotalSeconds < 1) return "< 1s";
        if (duration.TotalMinutes < 1) return $"{duration.Seconds}s";
        return $"{duration.Minutes}m {duration.Seconds}s";
    }




    private async Task EditTaskAsync(ScheduledTaskViewModel task)
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

    private void EditTask(ScheduledTaskViewModel task)
    {
        _ = EditTaskAsync(task);
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

    private async Task EditWorkflowAsync(WorkflowViewModel workflow)
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

    private void EditWorkflow(WorkflowViewModel workflow)
    {
        _ = EditWorkflowAsync(workflow);
    }

    private async Task RunWorkflowAsync(WorkflowViewModel workflow)
    {
        try
        {
            _logger.LogInformation("Executing workflow {Name} ({Id})", workflow.Name, workflow.Id);

            RecentActivity.Insert(0, new ActivityItemViewModel
            {
                ReferenceId = workflow.Id,
                Icon = "🔄",
                Message = $"Workflow \'{workflow.Name}\' started",
                Timestamp = "Just now",
                Status = "Running",
                StatusColor = "#F59E0B"
            });

            var result = await _workflowService.ExecuteWorkflowAsync(workflow.Id);
            if (result.IsSuccess)
            {
                    RecentActivity.Insert(0, new ActivityItemViewModel
                {
                    ReferenceId = workflow.Id,
                    Icon = "✅",
                    Message = $"Workflow \'{workflow.Name}\' completed",
                    Timestamp = "Just now",
                    Status = "Success",
                    StatusColor = "#10B981"
                });
            }
            else
            {
                    RecentActivity.Insert(0, new ActivityItemViewModel
                {
                    ReferenceId = workflow.Id,
                    Icon = "❌",
                    Message = $"Workflow \'{workflow.Name}\' failed: {result.Error}",
                    Timestamp = "Just now",
                    Status = "Failed",
                    StatusColor = "#EF4444"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run workflow {Name}", workflow.Name);
        }
    }

    private void RunWorkflow(WorkflowViewModel workflow)
    {
        _ = RunWorkflowAsync(workflow);
    }

    private async Task EditMacroAsync(MacroViewModel macro)
    {
        try
        {
             _ = _dialogService.ShowMacroPlaybackDialogAsync(macro.Id, macro.Name);
             await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open macro editor for {MacroName}", macro.Name);
        }
    }

    private void EditMacro(MacroViewModel macro)
    {
        _ = EditMacroAsync(macro);
    }

    private void PlayMacro(MacroViewModel macro)
    {
        RecentActivity.Insert(0, new ActivityItemViewModel
        {
            ReferenceId = macro.Id,
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
        // Show task creation dialog
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
        var result = await _dialogService.ShowWorkflowEditorDialogAsync();
        if (result != null)
        {
            await LoadWorkflowsAsync(); // Refresh list
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

    [RelayCommand]
    private void OpenMarketplace()
    {
        CurrentView = MacroMarketplace;
    }

    [RelayCommand]
    private void ShowDashboard()
    {
        CurrentView = null; // Displays the main dashboard
        _ = LoadDataAsync(); // Refresh data when returning to dashboard
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
    public Guid Id { get; set; }

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
    public Guid Id { get; set; }

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
    public Guid? ReferenceId { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#10B981";
}
