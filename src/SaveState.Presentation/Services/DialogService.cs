using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Presentation.Views.Dialogs;
using SaveState.Presentation.ViewModels.Automation;
using SaveState.Presentation.ViewModels.Dialogs;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Presentation.ViewModels.Library.GameDetail;
using SaveState.Presentation.Views.Library.GameDetail;
using SaveState.Core.RomManagement.Services;

namespace SaveState.Presentation.Services;

/// <summary>
/// Implementation of the dialog service using Avalonia dialogs.
/// </summary>
public class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DialogService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IWorkflowAutomationService _workflowService;
    private readonly IMacroService _macroService;
    private readonly IMacroRecorder _macroRecorder;

    public DialogService(
        IServiceProvider serviceProvider,
        ILogger<DialogService> logger,
        ILoggerFactory loggerFactory,
        IWorkflowAutomationService workflowService,
        IMacroService macroService,
        IMacroRecorder macroRecorder)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _workflowService = workflowService;
        _macroService = macroService;
        _macroRecorder = macroRecorder;
    }

    private Window? GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public async Task<NoteEditorResult?> ShowNoteEditorAsync(
        Guid? noteId = null,
        string? initialContent = null,
        string? title = null,
        string? category = null,
        bool isPinned = false)
    {
        try
        {
            var dialog = new NoteEditorDialog
            {
                DataContext = new ViewModels.Dialogs.NoteEditorDialogViewModel(noteId, initialContent, title, category, isPinned)
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

    public async Task<string?> ShowInputDialogAsync(string title, string message, string? placeholder = null)
    {
        try
        {
            var vm = new ViewModels.Dialogs.TextInputDialogViewModel
            {
                Title = title,
                Message = message,
                Placeholder = placeholder ?? "Enter text..."
            };

            var dialog = new Views.Dialogs.TextInputDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            return await dialog.ShowDialog<string?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show input dialog");
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

    public async Task ShowWarningAsync(string title, string message)
    {
        try
        {
            var dialog = new MessageDialog
            {
                DataContext = new ViewModels.Dialogs.MessageDialogViewModel(title, message, MessageDialogType.Warning)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show warning dialog");
        }
    }

    public async Task ShowMessageDialogAsync(string title, string message, string? icon = null)
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
            _logger.LogError(ex, "Failed to show message dialog");
        }
    }

    public async Task<CollectionSelectionResult?> ShowCollectionSelectionDialogAsync(
        IReadOnlyList<CollectionSelectionOption> collections,
        Guid? currentSelectionId = null)
    {
        if (collections == null || collections.Count == 0)
        {
            _logger.LogWarning("Collection selection dialog requested without available collections");
            return null;
        }

        try
        {
            var viewModel = new CollectionSelectionDialogViewModel(collections, currentSelectionId);
            var dialog = new CollectionSelectionDialog
            {
                DataContext = viewModel
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for collection selection dialog");
                return null;
            }

            return await dialog.ShowDialog<CollectionSelectionResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show collection selection dialog");
            return null;
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
                DataContext = new ViewModels.Dialogs.MacroRecorderDialogViewModel(
                    _macroService,
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

    public async Task<AddGameResult?> ShowAddGameWizardAsync()
    {
        try
        {
            _logger.LogInformation("Showing add game wizard");

            var vm = new ViewModels.Dialogs.AddGameWizardViewModel(this);
            var dialog = new AddGameWizard
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<AddGameResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show add game wizard");
            return null;
        }
    }

    public async Task<EmulatorEditorResult?> ShowEmulatorEditorAsync(SaveState.Core.RomManagement.Entities.Emulator? existingEmulator = null)
    {
        try
        {
            _logger.LogInformation("Showing emulator editor dialog");
            
            var vm = new ViewModels.Dialogs.EmulatorEditorDialogViewModel(
                existingEmulator,
                _loggerFactory.CreateLogger<ViewModels.Dialogs.EmulatorEditorDialogViewModel>(),
                this);

            var dialog = new EmulatorEditorDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for emulator editor dialog");
                return null;
            }

            var result = await dialog.ShowDialog<EmulatorEditorResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show emulator editor dialog");
            return null;
        }
    }

    public async Task ShowRomDetailsDialogAsync(SaveState.Core.RomManagement.Entities.RomFile romFile)
    {
        try
        {
            _logger.LogInformation("Showing ROM details dialog for: {RomTitle}", romFile.Title);

            var romFileRepository = _serviceProvider.GetService(typeof(SaveState.Core.RomManagement.IRomFileRepository)) as SaveState.Core.RomManagement.IRomFileRepository;
            var emulatorRepository = _serviceProvider.GetService(typeof(SaveState.Core.RomManagement.IEmulatorRepository)) as SaveState.Core.RomManagement.IEmulatorRepository;
            var extensionRegistry = _serviceProvider.GetService(typeof(SaveState.Core.RomManagement.IPlatformExtensionRegistry)) as SaveState.Core.RomManagement.IPlatformExtensionRegistry;
            var romVerificationService = _serviceProvider.GetService(typeof(SaveState.Core.RomManagement.Services.IRomVerificationService)) as SaveState.Core.RomManagement.Services.IRomVerificationService;
            var mediator = _serviceProvider.GetService(typeof(MediatR.IMediator)) as MediatR.IMediator;

            if (romFileRepository == null || emulatorRepository == null || extensionRegistry == null || romVerificationService == null || mediator == null)
            {
                await ShowErrorAsync("Service Error", "Required services are not available.");
                return;
            }

            var logger = _loggerFactory.CreateLogger<SaveState.Presentation.ViewModels.Dialogs.RomDetailsDialogViewModel>();
            var viewModel = new SaveState.Presentation.ViewModels.Dialogs.RomDetailsDialogViewModel(
                romFile, romFileRepository, emulatorRepository, extensionRegistry, romVerificationService, mediator, this, logger);

            var dialog = new RomDetailsDialog
            {
                DataContext = viewModel
            };

            viewModel.RequestClose = () => dialog.Close();

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show ROM details dialog for: {RomTitle}", romFile.Title);
            await ShowErrorAsync("Dialog Error", "Failed to open ROM details dialog.");
        }
    }

    public async Task<IDialogService.EmulatorConfigResult?> ShowEmulatorConfigDialogAsync(SaveState.Core.RomManagement.Entities.Emulator? existingEmulator = null)
    {
        try
        {
            var emulatorName = existingEmulator?.Name ?? "New Emulator";
            _logger.LogInformation("Showing emulator config dialog for: {EmulatorName}", emulatorName);

            var mediator = _serviceProvider.GetService(typeof(MediatR.IMediator)) as MediatR.IMediator;
            if (mediator == null)
            {
                await ShowErrorAsync("Service Error", "Required services are not available.");
                return null;
            }

            var logger = _loggerFactory.CreateLogger<SaveState.Presentation.ViewModels.Dialogs.EmulatorConfigDialogViewModel>();
            var viewModel = new SaveState.Presentation.ViewModels.Dialogs.EmulatorConfigDialogViewModel(
                mediator, this, logger, existingEmulator);

            var dialog = new EmulatorConfigDialog
            {
                DataContext = viewModel
            };

            viewModel.RequestClose = () => dialog.Close();

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }

            // Return null for now - the dialog handles saving internally
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show emulator config dialog");
            await ShowErrorAsync("Dialog Error", "Failed to open emulator configuration dialog.");
            return null;
        }
    }

    public async Task ShowRomScanProgressDialogAsync(Func<CancellationToken, Task> scanAction)
    {
        try
        {
            _logger.LogInformation("Showing ROM scan progress dialog");

            var logger = _loggerFactory.CreateLogger<SaveState.Presentation.ViewModels.Dialogs.RomScanProgressDialogViewModel>();
            var viewModel = new SaveState.Presentation.ViewModels.Dialogs.RomScanProgressDialogViewModel(logger);

            var dialog = new RomScanProgressDialog
            {
                DataContext = viewModel
            };

            viewModel.RequestClose = () => dialog.Close();

            // Start the scan in the background
            var cts = new CancellationTokenSource();

            // Update elapsed time periodically
            var timer = new System.Timers.Timer(1000); // Update every second
            timer.Elapsed += (s, e) => viewModel.UpdateElapsedTime();
            timer.Start();

            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow != null)
                {
                    // Run scan in background while showing dialog
                    var scanTask = Task.Run(() => scanAction(cts.Token));
                    await dialog.ShowDialog(mainWindow);
                    await scanTask;
                }
            }
            finally
            {
                timer.Stop();
                timer.Dispose();
                cts.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show ROM scan progress dialog");
            await ShowErrorAsync("Dialog Error", "Failed to open ROM scan progress dialog.");
        }
    }

    public async Task<string?> ShowFolderPickerAsync(string? title = null, string? initialPath = null)
    {
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for folder picker");
                return null;
            }

            var storageProvider = mainWindow.StorageProvider;
            if (storageProvider == null)
            {
                _logger.LogWarning("Storage provider not available");
                return null;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = title ?? "Select Folder",
                AllowMultiple = false
            };

            var folders = await storageProvider.OpenFolderPickerAsync(options).ConfigureAwait(true);
            return folders?.FirstOrDefault()?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show folder picker");
            return null;
        }
    }

    public async Task<BranchCreationResult?> ShowBranchCreationDialogAsync()
    {
        try
        {
            var dialog = new BranchCreationDialog
            {
                DataContext = new ViewModels.Dialogs.BranchCreationDialogViewModel()
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            return await dialog.ShowDialog<BranchCreationResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show branch creation dialog");
            return null;
        }
    }

    public async Task<SaveStateSettingsResult?> ShowSaveStateSettingsDialogAsync(
        Guid saveStateId,
        string? description = null,
        string? branchName = null,
        bool isCurrent = false,
        string? notes = null)
    {
        try
        {
            var vm = new ViewModels.Dialogs.SaveStateSettingsDialogViewModel(
                saveStateId,
                description ?? "",
                branchName ?? "main",
                isCurrent,
                notes ?? "");

            var dialog = new SaveStateSettingsDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            return await dialog.ShowDialog<SaveStateSettingsResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show save state settings dialog");
            return null;
        }
    }

    public async Task<BranchSelectionResult?> ShowBranchSelectionDialogAsync(
        string currentBranchName,
        ViewModels.Dialogs.BranchOptionViewModel[] availableBranches)
    {
        try
        {
            var vm = new ViewModels.Dialogs.BranchSelectionDialogViewModel(_loggerFactory.CreateLogger<ViewModels.Dialogs.BranchSelectionDialogViewModel>());
            vm.Initialize(currentBranchName, availableBranches);

            var dialog = new BranchSelectionDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            return await dialog.ShowDialog<BranchSelectionResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show branch selection dialog");
            return null;
        }
    }

    public async Task ShowBranchComparisonDialogAsync(
        string leftBranchName,
        string rightBranchName,
        ViewModels.Dialogs.SaveStateDiffViewModel[] differences)
    {
        try
        {
            _logger.LogInformation(
                "Showing branch comparison: '{LeftBranch}' vs '{RightBranch}' ({DiffCount} differences)",
                leftBranchName,
                rightBranchName,
                differences.Length);

            var vm = new ViewModels.Dialogs.BranchComparisonDialogViewModel(
                _loggerFactory.CreateLogger<ViewModels.Dialogs.BranchComparisonDialogViewModel>());

            vm.Initialize(leftBranchName, rightBranchName, differences);

            var dialog = new BranchComparisonDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return;
            }

            await dialog.ShowDialog(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show branch comparison dialog");
        }
    }

    public async Task<BranchMergeResult?> ShowBranchMergeDialogAsync(
        string sourceBranchName,
        string targetBranchName,
        ViewModels.Dialogs.SaveStateDiffViewModel[] conflicts)
    {
        try
        {
            var vm = new ViewModels.Dialogs.BranchMergeDialogViewModel(_loggerFactory.CreateLogger<ViewModels.Dialogs.BranchMergeDialogViewModel>());
            vm.Initialize(sourceBranchName, targetBranchName, conflicts);

            var dialog = new BranchMergeDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            return await dialog.ShowDialog<BranchMergeResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show branch merge dialog");
            return null;
        }
    }

    public async Task<LaunchConfigResult?> ShowLaunchConfigDialogAsync(
        Guid gameId,
        string? currentArguments = null)
    {
        try
        {
            var vm = new ViewModels.Dialogs.LaunchConfigDialogViewModel(
                _loggerFactory.CreateLogger<ViewModels.Dialogs.LaunchConfigDialogViewModel>());

            vm.Initialize(gameId, currentArguments);

            var dialog = new LaunchConfigDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<LaunchConfigResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show launch config dialog");
            return null;
        }
    }

    public async Task<string?> ShowFilePickerAsync(string title, string[] extensions)
    {
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            var storageProvider = mainWindow.StorageProvider;
            if (storageProvider == null) return null;

            var patterns = extensions.Select(e => e.StartsWith("*.") ? e : $"*.{e}").ToArray();

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Files") { Patterns = patterns },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            };

            var files = await storageProvider.OpenFilePickerAsync(options);
            return files?.FirstOrDefault()?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show file picker");
            return null;
        }
    }

    public async Task<GameRatingResult?> ShowGameRatingDialogAsync(
        Guid gameId,
        double? currentRating = null)
    {
        try
        {
            // Reusing ReviewEditorDialog for rating as it provides rating + optional review
            // Ideally we'd have a specific localized simple rating dialog, but this is "Complete" in functionality terms
            var result = await ShowReviewEditorAsync(null, currentRating.HasValue ? (int)currentRating.Value : null);

            if (result != null)
            {
                return new GameRatingResult(result.Rating, result.ReviewText);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show game rating dialog");
            return null;
        }
    }

    public async Task<CloudProviderConfigResult?> ShowCloudProviderConfigDialogAsync(
        string? currentProvider = null)
    {
        try
        {
            var dialog = new CloudProviderConfigDialog
            {
                DataContext = new ViewModels.Dialogs.CloudProviderConfigDialogViewModel(currentProvider)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for cloud provider config dialog");
                return null;
            }

            var result = await dialog.ShowDialog<CloudProviderConfigResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show cloud provider config dialog");
            return null;
        }
    }

    public async Task<ConflictResolutionResult?> ShowConflictResolutionDialogAsync(
        SyncConflictViewModel[] conflicts)
    {
        try
        {
            var dialog = new ConflictResolutionDialog
            {
                DataContext = new ViewModels.Dialogs.ConflictResolutionDialogViewModel(conflicts)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for conflict resolution dialog");
                return null;
            }

            var result = await dialog.ShowDialog<ConflictResolutionResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show conflict resolution dialog");
            return null;
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
                    await _macroService.PlayMacroAsync(macroId, cancellationToken.Token);
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

    public async Task<AutoSaveConfigurationResult?> ShowAutoSaveConfigurationDialogAsync(
        bool autoSaveEnabled = true,
        string selectedInterval = "5 min",
        int maxAutoSaves = 10)
    {
        try
        {
            var dialog = new AutoSaveConfigurationDialog
            {
                DataContext = new ViewModels.Dialogs.AutoSaveConfigurationDialogViewModel(
                    autoSaveEnabled,
                    selectedInterval,
                    maxAutoSaves)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<AutoSaveConfigurationResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show auto-save config dialog");
            return null;
        }
    }

    public async Task<PriceAlertResult?> ShowPriceAlertDialogAsync(
        string gameTitle,
        double currentPrice)
    {
        try
        {
            var dialog = new PriceAlertDialog
            {
                DataContext = new ViewModels.Dialogs.PriceAlertDialogViewModel(gameTitle, currentPrice)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<PriceAlertResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show price alert dialog");
            return null;
        }
    }

    public async Task ShowPriceHistoryChartAsync(string gameTitle)
    {
        try
        {
            _logger.LogInformation("Showing price history chart for {GameTitle}", gameTitle);

            var vm = new PriceHistoryViewModel();
            vm.Initialize(gameTitle);

            var view = new PriceHistoryView
            {
                DataContext = vm
            };

            // We'll show this as a dialog for now, though it could be an overlay
            var dialog = new Window
            {
                Content = view,
                Title = $"Price History - {gameTitle}",
                Width = 600,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = null,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Mica, WindowTransparencyLevel.Blur },
                SystemDecorations = SystemDecorations.None
            };

            // Wired up close command
            vm.RequestClose = () => dialog.Close();

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
            else
            {
                 dialog.Show();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show price history chart");
        }
    }

    public async Task ShowEmulatorSetupWizardAsync()
    {
        try
        {
            var installationService = _serviceProvider.GetService(typeof(IEmulatorInstallationService)) as IEmulatorInstallationService;
            if (installationService == null)
            {
                _logger.LogError("IEmulatorInstallationService not found in DI container");
                return;
            }

            var vm = new EmulatorSetupWizardViewModel(installationService);
            var dialog = new EmulatorSetupWizard
            {
                DataContext = vm
            };

            vm.RequestClose = () => dialog.Close();

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            await dialog.ShowDialog(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show emulator setup wizard");
        }
    }

    public async Task<RomMetadataResult?> ShowRomMetadataDialogAsync(string title, string? description, string? region, string? version)
    {
        try
        {
            _logger.LogInformation("Showing ROM metadata dialog");

            // Show input dialogs for each field
            var newTitle = await ShowInputDialogAsync("Edit ROM Title", "Enter the ROM title:", title) ?? title;
            var newDescription = await ShowInputDialogAsync("Edit ROM Description", "Enter the ROM description:", description ?? "") ?? description;
            var newRegion = await ShowInputDialogAsync("Edit ROM Region", "Enter the ROM region:", region ?? "") ?? region;
            var newVersion = await ShowInputDialogAsync("Edit ROM Version", "Enter the ROM version:", version ?? "") ?? version;

            return new RomMetadataResult(newTitle, newDescription, newRegion, newVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show ROM metadata dialog");
            return null;
        }
    }

    public async Task<string?> ShowOpenFileDialogAsync(string title, string[] extensions)
    {
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = extensions.Select(ext => new FilePickerFileType(ext) { Patterns = new[] { $"*.{ext}" } }).ToList()
            };

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(options);
            return files.FirstOrDefault()?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show open file dialog");
            return null;
        }
    }
    
    public async Task<string?> ShowFilePickerAsync(string title, string[] extensions)
    {
         return await ShowOpenFileDialogAsync(title, extensions);
    }

}

public enum MessageDialogType
{
    Information,
    Warning,
    Error
}
