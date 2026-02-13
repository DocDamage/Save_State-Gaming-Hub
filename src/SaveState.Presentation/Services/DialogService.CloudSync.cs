using Avalonia.Platform.Storage;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Views.Dialogs;
using SaveState.Presentation.Views.Library.GameDetail;
using SaveState.Presentation.ViewModels.Dialogs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Cloud/Sync and Price related dialogs for the dialog service.
/// </summary>
public partial class DialogService : IDialogService
{
    #region Cloud/Sync Dialogs

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

    public async Task<CloudProviderConfigResult?> ShowCloudProviderConfigDialogAsync(
        CloudProviderConfigResult? currentSettings = null)
    {
        try
        {
            var dialog = new CloudProviderConfigDialog
            {
                DataContext = new ViewModels.Dialogs.CloudProviderConfigDialogViewModel(currentSettings)
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

    #endregion

    #region Price Dialogs

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

            var vm = new ViewModels.Dialogs.PriceHistoryViewModel();
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

    #endregion
}
