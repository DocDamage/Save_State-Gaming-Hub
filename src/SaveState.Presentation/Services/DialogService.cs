using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Automation.Services;
using SaveState.Core.Common.Services;
using SaveState.Core.Performance.Services;
using SaveState.Infrastructure.Monitoring;
using SaveState.Presentation.Views.Dialogs;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.Settings;
using SaveState.Presentation.Models.Data;
using SaveState.Presentation.Services;
using System;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Implementation of the dialog service using Avalonia dialogs.
/// </summary>
public partial class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DialogService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IWorkflowAutomationService _workflowService;
    private readonly IMacroService _macroService;
    private readonly IMacroRecorder _macroRecorder;
    private readonly ITimeProvider _timeProvider;

    public DialogService(
        IServiceProvider serviceProvider,
        ILogger<DialogService> logger,
        ILoggerFactory loggerFactory,
        IWorkflowAutomationService workflowService,
        IMacroService macroService,
        IMacroRecorder macroRecorder,
        ITimeProvider timeProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _timeProvider = timeProvider;
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

    #region Basic Dialogs

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

    public async Task ShowErrorAsync(string message)
    {
        await ShowErrorAsync("Error", message);
    }

    public async Task ShowSuccessAsync(string message)
    {
        try
        {
            var dialog = new MessageDialog
            {
                DataContext = new ViewModels.Dialogs.MessageDialogViewModel("Success", message, MessageDialogType.Information)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show success dialog");
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

    public async Task<string?> ShowInputDialogAsync(
        string title,
        string message,
        string? placeholder = null,
        bool isSensitive = false)
    {
        try
        {
            var vm = new ViewModels.Dialogs.TextInputDialogViewModel
            {
                Title = title,
                Message = message,
                Placeholder = placeholder ?? "Enter text...",
                IsSensitive = isSensitive
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

    #endregion

    #region Process Selector

    /// <inheritdoc />
    public async Task<int?> ShowProcessSelectorAsync()
    {
        try
        {
            var vm = new ProcessSelectorDialogViewModel();

            var dialog = new ProcessSelectorDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for process selector dialog");
                return null;
            }

            return await dialog.ShowDialog<int?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show process selector dialog");
            return null;
        }
    }

    #endregion

    #region Error Log Viewer

    /// <inheritdoc />
    public async Task ShowErrorLogViewerAsync()
    {
        try
        {
            var vm = new ErrorLogViewerDialogViewModel();

            var dialog = new ErrorLogViewerDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show error log viewer dialog");
        }
    }

    #endregion

    #region Generic Dialog

    /// <inheritdoc />
    public async Task<TResult?> ShowDialogAsync<TResult>(object viewModel)
    {
        try
        {
            Window dialog = viewModel switch
            {
                AccountConnectionWizardViewModel => new AccountConnectionWizard(),
                _ => throw new ArgumentException($"Unknown view model type: {viewModel.GetType().Name}")
            };

            dialog.DataContext = viewModel;

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return default;
            }

            var result = await dialog.ShowDialog<TResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show dialog for view model {ViewModelType}", viewModel.GetType().Name);
            return default;
        }
    }

    #endregion

    #region Performance Dashboard Dialogs

    /// <inheritdoc />
    public async Task ShowGamePerformanceDetailAsync(GamePerformanceStats gameStats)
    {
        try
        {
            // Note: GamePerformanceDetailViewModel requires GamePerformanceStats in constructor
            // We'll create a new instance with the required parameters
            var timeProvider = _serviceProvider.GetRequiredService<ITimeProvider>();
            var detailedVm = new GamePerformanceDetailViewModel(
                timeProvider,
                gameStats,
                null, // IPerformanceService - not implemented yet
                _serviceProvider.GetService<ISystemResourceManager>(),
                _serviceProvider.GetService<IPerformanceMonitor>(),
                null, // ErrorTrackingService - use null for now
                _serviceProvider.GetService<INotificationService>());

            var dialog = new GamePerformanceDetailView
            {
                DataContext = detailedVm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show game performance detail dialog");
        }
    }

    #endregion

    #region Data Management Dialogs

    /// <inheritdoc />
    public async Task<ImportPreviewResult?> ShowImportPreviewAsync(ImportPreview preview, string? filePath = null)
    {
        try
        {
            var vm = _serviceProvider.GetRequiredService<ImportPreviewDialogViewModel>();
            var timeProvider = _serviceProvider.GetRequiredService<ITimeProvider>();
            
            // Create a new instance with required services since it needs them in constructor
            var previewVm = new ImportPreviewDialogViewModel(
                this,
                _serviceProvider.GetRequiredService<INotificationService>(),
                timeProvider);
            
            previewVm.Initialize(filePath ?? "import_file.json", preview);

            var dialog = new ImportPreviewDialog();
            dialog.Initialize(previewVm);

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for import preview dialog");
                return null;
            }

            var result = await dialog.ShowDialog<ImportPreviewResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show import preview dialog");
            return null;
        }
    }

    #endregion
}
