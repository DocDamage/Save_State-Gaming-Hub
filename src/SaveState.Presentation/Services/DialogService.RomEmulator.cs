using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Presentation.Views.Dialogs;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.Emulators;
using SaveState.Core.RomManagement.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// ROM and Emulator related dialogs for the dialog service.
/// </summary>
public partial class DialogService : IDialogService
{
    #region ROM/Emulator Dialogs

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

    #endregion
}
