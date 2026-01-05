using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Automation.Commands;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common.Enums;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for task scheduling and backup automation.
/// </summary>
public partial class TaskSchedulerViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IBackupScheduler _backupScheduler;
    private readonly IWorkflowAutomationService _workflowService;
    private readonly INotificationService _notificationService;
    private readonly SaveState.Application.CloudServices.Services.IBackupService _backupService;
    private readonly ILogger<TaskSchedulerViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private Workflow? _selectedSchedule;

    [ObservableProperty]
    private BackupResult? _selectedHistoryItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _scheduleName = string.Empty;

    [ObservableProperty]
    private string _scheduleDescription = string.Empty;

    [ObservableProperty]
    private BackupFrequency _selectedFrequency = BackupFrequency.Daily;

    [ObservableProperty]
    private TimeSpan _scheduleTime = new TimeSpan(2, 0, 0); // 2 AM default

    [ObservableProperty]
    private bool _includeGameFiles = true;

    [ObservableProperty]
    private bool _includeSaveStates = true;

    [ObservableProperty]
    private bool _includeScreenshots;

    [ObservableProperty]
    private bool _compressBackup = true;

    [ObservableProperty]
    private int _maxBackupsToKeep = 10;

    [ObservableProperty]
    private bool _isCreateDialogVisible;

    [ObservableProperty]
    private int _totalSchedules;

    [ObservableProperty]
    private int _activeSchedules;

    [ObservableProperty]
    private int _totalBackups;

    public TaskSchedulerViewModel(
        IMediator mediator,
        IBackupScheduler backupScheduler,
        IWorkflowAutomationService workflowService,
        INotificationService notificationService,
        SaveState.Application.CloudServices.Services.IBackupService backupService,
        ILogger<TaskSchedulerViewModel> logger)
    {
        _mediator = mediator;
        _backupScheduler = backupScheduler;
        _workflowService = workflowService;
        _notificationService = notificationService;
        _backupService = backupService;
        _logger = logger;

        Schedules = new ObservableCollection<Workflow>();
        BackupHistory = new ObservableCollection<BackupResult>();
        FrequencyOptions = new ObservableCollection<BackupFrequency>
        {
            BackupFrequency.Manual,
            BackupFrequency.Daily,
            BackupFrequency.Weekly,
            BackupFrequency.Monthly
        };

        // Initialize async
        _ = LoadSchedulesAsync();
        _ = LoadHistoryAsync(); // Also load history on init
    }

    /// <summary>
    /// Gets the collection of backup schedules.
    /// </summary>
    public ObservableCollection<Workflow> Schedules { get; }

    /// <summary>
    /// Gets the backup history.
    /// </summary>
    public ObservableCollection<BackupResult> BackupHistory { get; }

    /// <summary>
    /// Gets the frequency options for backup schedules.
    /// </summary>
    public ObservableCollection<BackupFrequency> FrequencyOptions { get; }

    /// <summary>
    /// Command to show the create schedule dialog.
    /// </summary>
    [RelayCommand]
    private void ShowCreateDialog()
    {
        IsCreateDialogVisible = true;

        // Reset form
        ScheduleName = string.Empty;
        ScheduleDescription = string.Empty;
        SelectedFrequency = BackupFrequency.Daily;
        ScheduleTime = new TimeSpan(2, 0, 0);
        IncludeGameFiles = true;
        IncludeSaveStates = true;
        IncludeScreenshots = false;
        CompressBackup = true;
        MaxBackupsToKeep = 10;
    }

    /// <summary>
    /// Command to cancel creating a schedule.
    /// </summary>
    [RelayCommand]
    private void CancelCreateDialog()
    {
        IsCreateDialogVisible = false;
    }

    /// <summary>
    /// Command to create a new backup schedule.
    /// </summary>
    [RelayCommand]
    private async Task CreateScheduleAsync()
    {
        if (string.IsNullOrWhiteSpace(ScheduleName))
        {
            _notificationService.ShowWarning("Please enter a schedule name");
            return;
        }

        try
        {
            var config = new BackupScheduleConfig(
                Guid.NewGuid(),
                ScheduleName,
                ScheduleDescription,
                SelectedFrequency,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SaveState", "Backups"),
                ScheduleTime,
                SelectedFrequency == BackupFrequency.Weekly
                    ? new[] { DayOfWeek.Sunday }
                    : null,
                DateTime.Today,
                null,
                new BackupOptions(
                    IncludeSaveStates,
                    IncludeGameFiles,
                    IncludeScreenshots,
                    CompressBackup,
                    null,
                    null),
                true,
                CompressBackup,
                new RetentionPolicy(
                    MaxBackupsToKeep,
                    null,
                    false,
                    false));

            var command = new ScheduleBackupCommand(config);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Schedule created: {ScheduleName}");
                IsCreateDialogVisible = false;
                await LoadSchedulesAsync();
            }
            else
            {
                _notificationService.ShowError($"Failed to create schedule: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup schedule");
            _notificationService.ShowError("Failed to create schedule");
        }
    }

    /// <summary>
    /// Command to enable a schedule.
    /// </summary>
    [RelayCommand]
    private async Task EnableScheduleAsync(Workflow schedule)
    {
        if (schedule == null) return;

        try
        {
            var command = new SetWorkflowEnabledCommand(schedule.Id, true);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Schedule enabled: {schedule.Name}");
                await LoadSchedulesAsync();
            }
            else
            {
                _notificationService.ShowError($"Failed to enable schedule: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable schedule");
            _notificationService.ShowError("Failed to enable schedule");
        }
    }

    /// <summary>
    /// Command to disable a schedule.
    /// </summary>
    [RelayCommand]
    private async Task DisableScheduleAsync(Workflow schedule)
    {
        if (schedule == null) return;

        try
        {
            var command = new SetWorkflowEnabledCommand(schedule.Id, false);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowInfo($"Schedule disabled: {schedule.Name}");
                await LoadSchedulesAsync();
            }
            else
            {
                _notificationService.ShowError($"Failed to disable schedule: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable schedule");
            _notificationService.ShowError("Failed to disable schedule");
        }
    }

    /// <summary>
    /// Command to delete a schedule.
    /// </summary>
    [RelayCommand]
    private async Task DeleteScheduleAsync(Workflow schedule)
    {
        if (schedule == null) return;

        try
        {
            var command = new DeleteWorkflowCommand(schedule.Id);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                Schedules.Remove(schedule);
                _notificationService.ShowSuccess($"Schedule deleted: {schedule.Name}");

                if (SelectedSchedule == schedule)
                {
                    SelectedSchedule = null;
                }
            }
            else
            {
                _notificationService.ShowError($"Failed to delete schedule: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete schedule");
            _notificationService.ShowError("Failed to delete schedule");
        }
    }

    /// <summary>
    /// Command to run a schedule immediately.
    /// </summary>
    [RelayCommand]
    private async Task RunNowAsync(Workflow schedule)
    {
        if (schedule == null) return;

        try
        {
            var command = new ExecuteWorkflowCommand(schedule.Id, null);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Executing: {schedule.Name}");
                await LoadHistoryAsync();
            }
            else
            {
                _notificationService.ShowError($"Failed to execute: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute schedule");
            _notificationService.ShowError("Failed to execute schedule");
        }
    }

    /// <summary>
    /// Command to refresh the schedules list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadSchedulesAsync();
        await LoadHistoryAsync();
    }

    /// <summary>
    /// Command to view schedule details.
    /// </summary>
    [RelayCommand]
    private void ViewScheduleDetails(Workflow schedule)
    {
        SelectedSchedule = schedule;
    }

    private async Task LoadSchedulesAsync()
    {
        try
        {
            IsLoading = true;

            // Get all workflows (schedules)
            var result = await _workflowService.GetAllWorkflowsAsync();

            Schedules.Clear();
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var workflow in result.Value)
                {
                    Schedules.Add(workflow);
                }
            }

            // Update statistics
            TotalSchedules = Schedules.Count;
            ActiveSchedules = Schedules.Count(s => s.IsEnabled);

            _logger.LogInformation("Loaded {Count} schedules", Schedules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load schedules");
            _notificationService.ShowError("Failed to load schedules");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            BackupHistory.Clear();

            // Load backup history from the service (persisted on disk)
            var history = await _backupService.GetBackupHistoryAsync();

            foreach (var metadata in history.OrderByDescending(h => h.CreatedAt).Take(50))
            {
                // Map metadata to DTO
                var result = new BackupResult(
                    Id: metadata.BackupId.Value,
                    ScheduleId: Guid.Empty, // Disk backups don't track schedule ID in metadata yet
                    GameId: Guid.Empty, // No specific game for full backups
                    StartedAt: metadata.CreatedAt,
                    CompletedAt: metadata.CreatedAt,
                    Status: BackupStatus.Success,
                    TotalSizeBytes: metadata.TotalSize,
                    FilesBackedUp: metadata.GamesBackedUp,
                    BackupPath: string.Empty,
                    Errors: Array.Empty<string>());

                BackupHistory.Add(result);
            }

            TotalBackups = BackupHistory.Count;
            _logger.LogInformation("Loaded {Count} backup history records from disk", TotalBackups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load backup history");
            _notificationService.ShowError("Failed to load backup history");
        }
    }
}
