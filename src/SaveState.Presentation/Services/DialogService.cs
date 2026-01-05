using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Views.Dialogs;
using SaveState.Presentation.ViewModels.Automation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Implementation of the dialog service using Avalonia dialogs.
/// </summary>
public class DialogService : IDialogService
{
    private readonly ILogger<DialogService> _logger;

    public DialogService(ILogger<DialogService> logger)
    {
        _logger = logger;
    }

    private Window? GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public async Task<NoteEditorResult?> ShowNoteEditorAsync(Guid? noteId = null, string? initialContent = null)
    {
        try
        {
            var dialog = new NoteEditorDialog
            {
                DataContext = new ViewModels.Dialogs.NoteEditorDialogViewModel(noteId, initialContent)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<NoteEditorResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show note editor dialog");
            return null;
        }
    }

    public async Task<TagEditorResult?> ShowTagEditorAsync(string[] currentTags)
    {
        try
        {
            var dialog = new TagEditorDialog
            {
                DataContext = new ViewModels.Dialogs.TagEditorDialogViewModel(currentTags)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<TagEditorResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show tag editor dialog");
            return null;
        }
    }

    public async Task<GoalCreationResult?> ShowGoalCreationDialogAsync()
    {
        try
        {
            var dialog = new GoalCreationDialog
            {
                DataContext = new ViewModels.Dialogs.GoalCreationDialogViewModel()
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<GoalCreationResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show goal creation dialog");
            return null;
        }
    }

    public async Task<ReviewEditorResult?> ShowReviewEditorAsync(string? existingReview = null, int? existingRating = null)
    {
        try
        {
            var dialog = new ReviewEditorDialog
            {
                DataContext = new ViewModels.Dialogs.ReviewEditorDialogViewModel(existingReview, existingRating)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<ReviewEditorResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show review editor dialog");
            return null;
        }
    }

    public async Task<string[]?> ShowModFilePickerAsync()
    {
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for file picker");
                return null;
            }

            var storageProvider = mainWindow.StorageProvider;
            if (storageProvider == null)
            {
                _logger.LogWarning("Storage provider not available");
                return null;
            }

            var options = new FilePickerOpenOptions
            {
                Title = "Select Mod Files",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Mod Files")
                    {
                        Patterns = new[] { "*.zip", "*.rar", "*.7z", "*.pak", "*.mod" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            };

            var files = await storageProvider.OpenFilePickerAsync(options);
            return files?.Select(f => f.Path.LocalPath).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show mod file picker");
            return null;
        }
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        try
        {
            var dialog = new ConfirmationDialog
            {
                DataContext = new ViewModels.Dialogs.ConfirmationDialogViewModel(title, message, confirmText, cancelText)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for confirmation dialog");
                return false;
            }

            var result = await dialog.ShowDialog<bool>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show confirmation dialog");
            return false;
        }
    }

    public async Task ShowInformationAsync(string title, string message)
    {
        try
        {
            var dialog = new MessageDialog
            {
                DataContext = new ViewModels.Dialogs.MessageDialogViewModel(title, message, MessageDialogType.Information)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show information dialog");
        }
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        try
        {
            var dialog = new MessageDialog
            {
                DataContext = new ViewModels.Dialogs.MessageDialogViewModel(title, message, MessageDialogType.Error)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show error dialog");
        }
    }
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
                DataContext = new ViewModels.Dialogs.MacroRecorderDialogViewModel()
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

    public async Task<AddGameResult?> ShowAddGameWizardAsync()
    {
        try
        {
            // For now, use a simulated result or create a small dialog
            // In a real implementation, this would show a multi-step wizard
            _logger.LogInformation("Showing add game wizard");

            // Placeholder: Returning null for now as we don't have the wizard view yet
            // but this allows the UI to call it without crashing.
            await ShowInformationAsync("Add Game Wizard", "The Add Game Wizard will be available in the next update.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show add game wizard");
            return null;
        }
    }

    public async Task<EmulatorEditorResult?> ShowEmulatorEditorAsync(SaveState.Presentation.ViewModels.Shell.EmulatorViewModel? existingEmulator = null)
    {
        try
        {
            _logger.LogInformation("Showing emulator editor dialog");
            // Placeholder: Returning null for now
            await ShowInformationAsync("Emulator Configuration", "Emulator configuration dialog is under development.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show emulator editor dialog");
            return null;
        }
    }
}

public enum MessageDialogType
{
    Information,
    Warning,
    Error
}
