using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

using SaveState.Presentation.ViewModels.Automation;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Automation tab containing Dashboard, Macro Recorder and Task Scheduler.
/// </summary>
public partial class AutomationViewModel : ObservableObject
{
    private readonly AutomationDashboardViewModel _dashboardViewModel;
    private readonly MacroRecorderViewModel _macroRecorderViewModel;
    private readonly TaskSchedulerViewModel _taskSchedulerViewModel;
    private readonly ILogger<AutomationViewModel> _logger;

    [ObservableProperty]
    private ObservableObject _currentSubView;

    [ObservableProperty]
    private string _selectedTab = "Dashboard";

    public AutomationViewModel(
        AutomationDashboardViewModel dashboardViewModel,
        MacroRecorderViewModel macroRecorderViewModel,
        TaskSchedulerViewModel taskSchedulerViewModel,
        ILogger<AutomationViewModel> logger)
    {
        _dashboardViewModel = dashboardViewModel;
        _macroRecorderViewModel = macroRecorderViewModel;
        _taskSchedulerViewModel = taskSchedulerViewModel;
        _logger = logger;

        // Default to Dashboard view
        _currentSubView = _dashboardViewModel;
    }

    /// <summary>
    /// Gets the Dashboard view model.
    /// </summary>
    public AutomationDashboardViewModel DashboardViewModel => _dashboardViewModel;

    /// <summary>
    /// Gets the Macro Recorder view model.
    /// </summary>
    public MacroRecorderViewModel MacroRecorderViewModel => _macroRecorderViewModel;

    /// <summary>
    /// Gets the Task Scheduler view model.
    /// </summary>
    public TaskSchedulerViewModel TaskSchedulerViewModel => _taskSchedulerViewModel;

    /// <summary>
    /// Command to switch to the Dashboard tab.
    /// </summary>
    [RelayCommand]
    private void ShowDashboard()
    {
        CurrentSubView = _dashboardViewModel;
        SelectedTab = "Dashboard";
        _logger.LogDebug("Switched to Dashboard sub-tab");
    }

    /// <summary>
    /// Command to switch to the Macros tab.
    /// </summary>
    [RelayCommand]
    private void ShowMacros()
    {
        CurrentSubView = _macroRecorderViewModel;
        SelectedTab = "Macros";
        _logger.LogDebug("Switched to Macros sub-tab");
    }

    /// <summary>
    /// Command to switch to the Scheduler tab.
    /// </summary>
    [RelayCommand]
    private void ShowScheduler()
    {
        CurrentSubView = _taskSchedulerViewModel;
        SelectedTab = "Scheduler";
        _logger.LogDebug("Switched to Scheduler sub-tab");
    }
}
