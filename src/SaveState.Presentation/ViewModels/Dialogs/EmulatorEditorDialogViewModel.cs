using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class EmulatorEditorDialogViewModel : ObservableObject
{
    private readonly ILogger<EmulatorEditorDialogViewModel> _logger;
    private readonly IDialogService? _dialogService;

    [ObservableProperty]
    private string _emulatorName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _executablePath = string.Empty;

    [ObservableProperty]
    private string _commandLineTemplate = "{ROM_PATH}";

    [ObservableProperty]
    private string? _workingDirectory;

    [ObservableProperty]
    private bool _supportsSaveStates = true;

    [ObservableProperty]
    private bool _autoDetect = true;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private ObservableCollection<string> _availablePlatforms = new()
    {
        "Nintendo - Nintendo Entertainment System",
        "Nintendo - Super Nintendo Entertainment System",
        "Nintendo - Nintendo 64",
        "Nintendo - GameCube",
        "Nintendo - Wii",
        "Nintendo - Game Boy",
        "Nintendo - Game Boy Color",
        "Nintendo - Game Boy Advance",
        "Nintendo - Nintendo DS",
        "Sega - Master System",
        "Sega - Genesis",
        "Sega - Saturn",
        "Sega - Dreamcast",
        "Sony - PlayStation",
        "Sony - PlayStation 2",
        "Sony - PlayStation 3",
        "Sony - PlayStation Portable",
        "Microsoft - Xbox",
        "Microsoft - Xbox 360",
        "Atari - 2600",
        "Atari - 7800",
        "Arcade"
    };

    [ObservableProperty]
    private string? _selectedPlatform;

    [ObservableProperty]
    private bool _canTest;

    private Action<EmulatorEditorResult?>? _closeAction;

    public EmulatorEditorDialogViewModel(
        ILogger<EmulatorEditorDialogViewModel> logger,
        IDialogService? dialogService = null)
    {
        _logger = logger;
        _dialogService = dialogService;
    }

    public EmulatorEditorDialogViewModel(
        SaveState.Core.RomManagement.Entities.Emulator? existingEmulator = null,
        ILogger<EmulatorEditorDialogViewModel>? logger = null,
        IDialogService? dialogService = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<EmulatorEditorDialogViewModel>.Instance;
        _dialogService = dialogService;

        if (existingEmulator != null)
        {
            EmulatorName = existingEmulator.Name;
            DisplayName = existingEmulator.Description ?? existingEmulator.Name;
            ExecutablePath = existingEmulator.ExecutablePath.Value;
            // Load other properties from existingEmulator if available
        }
    }

    partial void OnExecutablePathChanged(string value)
    {
        CanTest = !string.IsNullOrWhiteSpace(value) && File.Exists(value);
    }

    [RelayCommand]
    private async Task BrowseExecutable()
    {
        try
        {
            if (_dialogService != null)
            {
                var result = await _dialogService.ShowOpenFileDialogAsync(
                    "Select Emulator Executable",
                    new[] { "exe", "app", "sh", "bat" });

                if (!string.IsNullOrEmpty(result))
                {
                    ExecutablePath = result;

                    // Auto-fill emulator name if empty
                    if (string.IsNullOrWhiteSpace(EmulatorName))
                    {
                        EmulatorName = Path.GetFileNameWithoutExtension(ExecutablePath);
                    }
                    if (string.IsNullOrWhiteSpace(DisplayName))
                    {
                        DisplayName = EmulatorName;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for emulator executable");
        }
    }

    [RelayCommand]
    private async Task TestEmulator()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ExecutablePath) || !File.Exists(ExecutablePath))
            {
                if (_dialogService != null)
                {
                    await _dialogService.ShowErrorAsync("Test Failed", "Executable path is invalid or file does not exist.");
                }
                return;
            }

            // Test if executable can be launched
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ExecutablePath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                // Give it a moment to start
                await Task.Delay(1000);

                if (!process.HasExited)
                {
                    process.Kill();
                    if (_dialogService != null)
                    {
                        await _dialogService.ShowInformationAsync("Test Successful", "Emulator launched successfully!");
                    }
                }
                else
                {
                    if (_dialogService != null)
                    {
                        await _dialogService.ShowWarningAsync("Test Warning", "Emulator exited immediately. It may require additional configuration.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing emulator");
            if (_dialogService != null)
            {
                await _dialogService.ShowErrorAsync("Test Failed", $"Failed to launch emulator: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(EmulatorName))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            return;
        }

        var result = new EmulatorEditorResult(
            EmulatorName,
            DisplayName,
            ExecutablePath,
            SelectedPlatform,
            CommandLineTemplate,
            WorkingDirectory,
            SupportsSaveStates,
            AutoDetect,
            Notes);

        _closeAction?.Invoke(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }

    public void SetCloseAction(Action<EmulatorEditorResult?> closeAction)
    {
        _closeAction = closeAction;
    }
}

public record EmulatorEditorResult(
    string Name,
    string DisplayName,
    string ExecutablePath,
    string? Platform,
    string CommandLineTemplate,
    string? WorkingDirectory,
    bool SupportsSaveStates,
    bool AutoDetect,
    string? Notes);
