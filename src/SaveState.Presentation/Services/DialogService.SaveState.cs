using Microsoft.Extensions.Logging;
using SaveState.Presentation.Views.Dialogs;
using SaveState.Presentation.ViewModels.Dialogs;
using System;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Save state and branch related dialogs for the dialog service.
/// </summary>
public partial class DialogService : IDialogService
{
    #region Save State/Branch Dialogs

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

    #endregion
}
