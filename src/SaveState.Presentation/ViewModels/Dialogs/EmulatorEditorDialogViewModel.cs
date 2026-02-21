using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class EmulatorEditorDialogViewModel : ObservableObject
{
    private readonly ILogger<EmulatorEditorDialogViewModel> _logger;
    private readonly IDialogService? _dialogService;

    // Validation constants
    private const int MaxNameLength = 100;
    private const int MaxDisplayNameLength = 100;
    private const int MaxPathLength = 260;
    private const int MaxCommandLineLength = 1000;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmulatorNameValid))]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _emulatorName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDisplayNameValid))]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
    private string _displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExecutablePathValid))]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
    [NotifyPropertyChangedFor(nameof(CanTest))]
    private string _executablePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCommandLineTemplateValid))]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
    private string _commandLineTemplate = "{ROM_PATH}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorkingDirectoryValid))]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
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
    private string _validationError = string.Empty;

    private Action<EmulatorEditorResult?>? _closeAction;

    /// <summary>
    /// Gets whether the emulator name is valid.
    /// </summary>
    public bool IsEmulatorNameValid => 
        !string.IsNullOrWhiteSpace(EmulatorName) && 
        EmulatorName.Length <= MaxNameLength &&
        !InvalidCharsPattern.IsMatch(EmulatorName);

    /// <summary>
    /// Gets whether the display name is valid.
    /// </summary>
    public bool IsDisplayNameValid => 
        string.IsNullOrEmpty(DisplayName) || 
        (DisplayName.Length <= MaxDisplayNameLength && !InvalidCharsPattern.IsMatch(DisplayName));

    /// <summary>
    /// Gets whether the executable path is valid.
    /// </summary>
    public bool IsExecutablePathValid => 
        !string.IsNullOrWhiteSpace(ExecutablePath) && 
        ExecutablePath.Length <= MaxPathLength;

    /// <summary>
    /// Gets whether the command line template is valid.
    /// </summary>
    public bool IsCommandLineTemplateValid => 
        CommandLineTemplate.Length <= MaxCommandLineLength;

    /// <summary>
    /// Gets whether the working directory is valid.
    /// </summary>
    public bool IsWorkingDirectoryValid => 
        string.IsNullOrEmpty(WorkingDirectory) || 
        (WorkingDirectory.Length <= MaxPathLength);

    /// <summary>
    /// Gets whether there are any validation errors.
    /// </summary>
    public bool HasValidationErrors => 
        !IsEmulatorNameValid || 
        !IsDisplayNameValid || 
        !IsExecutablePathValid || 
        !IsCommandLineTemplateValid ||
        !IsWorkingDirectoryValid;

    /// <summary>
    /// Gets whether the test button should be enabled.
    /// </summary>
    public bool CanTest => IsExecutablePathValid && File.Exists(ExecutablePath);

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
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxPathLength)
        {
            ExecutablePath = value[..MaxPathLength];
            return;
        }
        OnPropertyChanged(nameof(CanTest));
    }

    partial void OnEmulatorNameChanged(string value)
    {
        if (value?.Length > MaxNameLength)
        {
            EmulatorName = value[..MaxNameLength];
        }
        UpdateValidationError();
    }

    partial void OnDisplayNameChanged(string value)
    {
        if (value?.Length > MaxDisplayNameLength)
        {
            DisplayName = value[..MaxDisplayNameLength];
        }
        UpdateValidationError();
    }

    partial void OnCommandLineTemplateChanged(string value)
    {
        if (value?.Length > MaxCommandLineLength)
        {
            CommandLineTemplate = value[..MaxCommandLineLength];
        }
        UpdateValidationError();
    }

    partial void OnWorkingDirectoryChanged(string? value)
    {
        if (value?.Length > MaxPathLength)
        {
            WorkingDirectory = value[..MaxPathLength];
        }
        UpdateValidationError();
    }

    private void UpdateValidationError()
    {
        if (!IsEmulatorNameValid)
        {
            if (string.IsNullOrWhiteSpace(EmulatorName))
                ValidationError = "Emulator name is required.";
            else if (EmulatorName.Length > MaxNameLength)
                ValidationError = $"Name must not exceed {MaxNameLength} characters.";
            else
                ValidationError = "Name contains invalid characters.";
        }
        else if (!IsExecutablePathValid)
        {
            ValidationError = "Valid executable path is required.";
        }
        else if (!IsCommandLineTemplateValid)
        {
            ValidationError = $"Command line template must not exceed {MaxCommandLineLength} characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }
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
        if (HasValidationErrors)
        {
            UpdateValidationError();
            return;
        }

        var result = new EmulatorEditorResult(
            EmulatorName.Trim(),
            string.IsNullOrWhiteSpace(DisplayName) ? EmulatorName.Trim() : DisplayName.Trim(),
            ExecutablePath.Trim(),
            SelectedPlatform,
            CommandLineTemplate.Trim(),
            string.IsNullOrWhiteSpace(WorkingDirectory) ? null : WorkingDirectory.Trim(),
            SupportsSaveStates,
            AutoDetect,
            Notes?.Trim());

        _closeAction?.Invoke(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }

    /// <summary>
    /// Gets whether the save button should be enabled.
    /// </summary>
    public bool CanSave => !HasValidationErrors;

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
