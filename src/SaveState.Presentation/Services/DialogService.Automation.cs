using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Presentation.Views.Dialogs;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.Automation;
using System;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Automation related dialogs for the dialog service.
/// </summary>
public partial class DialogService : IDialogService
{
    #region Automation Dialogs

    public async Task<TaskCreationResult?> ShowTaskCreationDialogAsync(ScheduledTaskViewModel? existingTask = null)
    {
        try
        {
            var dialog = new TaskCreationDialog
            {
                DataContext = new ViewModels.Dialogs.TaskCreationDialogViewModel(existingTask != null ? new TaskCreationResult(existingTask.Name, existingTask.Schedule, existingTask.IsEnabled) : null)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for task creation dialog");
                return null;
            }

            return await dialog.ShowDialog<TaskCreationResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show task creation dialog");
            return null;
        }
    }

    public async Task<WorkflowCreationResult?> ShowWorkflowCreationDialogAsync(WorkflowViewModel? existingWorkflow = null)
    {
        try
        {
            var dialog = new WorkflowCreationDialog
            {
                DataContext = new ViewModels.Dialogs.WorkflowCreationDialogViewModel(existingWorkflow != null ? new WorkflowCreationResult(existingWorkflow.Name, existingWorkflow.Description, existingWorkflow.Icon) : null)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for workflow creation dialog");
                return null;
            }

            return await dialog.ShowDialog<WorkflowCreationResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show workflow creation dialog");
            return null;
        }
    }

    public async Task<MacroViewModel?> ShowMacroRecorderDialogAsync()
    {
        try
        {
            var dialog = new MacroRecorderDialog
            {
                DataContext = new ViewModels.Dialogs.MacroRecorderDialogViewModel(
                    _macroService,
                    _timeProvider,
                    _loggerFactory.CreateLogger<ViewModels.Dialogs.MacroRecorderDialogViewModel>())
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for macro recorder dialog");
                return null;
            }

            return await dialog.ShowDialog<MacroViewModel?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show macro recorder dialog");
            return null;
        }
    }

    public async Task ShowAutomationSettingsDialogAsync()
    {
        try
        {
            await ShowInformationAsync("Settings", "Automation settings are coming soon.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show automation settings dialog");
        }
    }

    public async Task<WorkflowEditorResult?> ShowWorkflowEditorDialogAsync(
        Guid? workflowId = null)
    {
        try
        {
            _logger.LogInformation("Showing workflow editor for workflow {WorkflowId}", workflowId);

            var vm = new ViewModels.Dialogs.WorkflowEditorDialogViewModel(this, _workflowService);
            var dialog = new WorkflowEditorDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<WorkflowEditorResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show workflow editor dialog");
            return null;
        }
    }

    public async Task ShowMacroPlaybackDialogAsync(
        Guid macroId,
        string macroName)
    {
        try
        {
            _logger.LogInformation("Showing macro playback for {MacroName} ({MacroId})", macroName, macroId);

            // Load the macro
            var macro = await _macroService.GetMacroAsync(macroId);
            if (macro == null)
            {
                await ShowErrorAsync("Macro Not Found", $"Could not find macro '{macroName}'.");
                return;
            }

            // Create progress dialog
            var progressMessage = $"Playing macro '{macroName}'...";
            var cancellationToken = new System.Threading.CancellationTokenSource();

            // Execute the macro in background
            var playbackTask = Task.Run(async () =>
            {
                try
                {
                    await _macroService.PlayMacroAsync(macroId, new MacroPlaybackOptions(), cancellationToken.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during macro playback");
                    throw;
                }
            });

            // Show a simple progress notification
            // In a real implementation, you might want a proper progress dialog
            await ShowInformationAsync("Macro Playback", progressMessage);

            // Wait for completion
            try
            {
                await playbackTask;
                _logger.LogInformation("Macro playback completed successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Macro playback was cancelled");
                await ShowInformationAsync("Macro Playback", "Macro playback was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Macro playback failed");
                await ShowErrorAsync("Macro Playback Failed", $"Failed to play macro: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show macro playback dialog");
        }
    }

    #endregion
}
