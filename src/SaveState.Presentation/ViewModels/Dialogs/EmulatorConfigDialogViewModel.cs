using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.RomManagement;
using SaveState.Application.RomManagement.Commands;
using SaveState.Application.RomManagement.Queries;
using SaveState.Presentation.Services;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for the emulator configuration dialog.
/// </summary>
public partial class EmulatorConfigDialogViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<EmulatorConfigDialogViewModel> _logger;
    private readonly SaveState.Core.RomManagement.Entities.Emulator? _existingEmulator;

    public EmulatorConfigDialogViewModel(
        IMediator mediator,
        IDialogService dialogService,
        ILogger<EmulatorConfigDialogViewModel> logger,
        SaveState.Core.RomManagement.Entities.Emulator? existingEmulator = null)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _existingEmulator = existingEmulator;

        AvailablePlatforms = new ObservableCollection<PlatformDto>();
        ValidationErrors = new ObservableCollection<string>();

        IsEditing = existingEmulator != null;
        DialogTitle = IsEditing ? "Edit Emulator" : "Add Emulator";

        if (IsEditing && existingEmulator != null)
        {
            Name = existingEmulator.Name;
            ExecutablePath = existingEmulator.ExecutablePath.Value;
            Version = existingEmulator.Version;
            Description = existingEmulator.Description;
            CommandLineArgs = existingEmulator.CommandLineArgs;
            EmulatorId = existingEmulator.Id;
        }

        _ = LoadPlatformsAsync();
    }

    /// <summary>
    /// Gets whether this is an edit operation.
    /// </summary>
    public bool IsEditing { get; }

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    [ObservableProperty]
    private string dialogTitle = "Add Emulator";

    /// <summary>
    /// Gets the current emulator name for display.
    /// </summary>
    [ObservableProperty]
    private string emulatorName = string.Empty;

    /// <summary>
    /// Gets the save button text.
    /// </summary>
    [ObservableProperty]
    private string saveButtonText = "Add Emulator";

    /// <summary>
    /// Gets the available platforms.
    /// </summary>
    public ObservableCollection<PlatformDto> AvailablePlatforms { get; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public ObservableCollection<string> ValidationErrors { get; }

    /// <summary>
    /// Gets whether there are validation errors.
    /// </summary>
    [ObservableProperty]
    private bool hasValidationErrors;

    /// <summary>
    /// Gets whether the form can be saved.
    /// </summary>
    [ObservableProperty]
    private bool canSave;

    /// <summary>
    /// Gets whether test launch is available.
    /// </summary>
    [ObservableProperty]
    private bool canTestLaunch;

    /// <summary>
    /// Gets or sets the emulator ID (for editing).
    /// </summary>
    [ObservableProperty]
    private Guid emulatorId;

    /// <summary>
    /// Gets or sets the emulator name.
    /// </summary>
    [ObservableProperty]
    private string name = string.Empty;

    /// <summary>
    /// Gets or sets the executable path.
    /// </summary>
    [ObservableProperty]
    private string executablePath = string.Empty;

    /// <summary>
    /// Gets or sets the selected platform.
    /// </summary>
    [ObservableProperty]
    private PlatformDto? selectedPlatform;

    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    [ObservableProperty]
    private string? version;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [ObservableProperty]
    private string? description;

    /// <summary>
    /// Gets or sets the command line arguments.
    /// </summary>
    [ObservableProperty]
    private string? commandLineArgs;

    /// <summary>
    /// Gets the executable status icon.
    /// </summary>
    [ObservableProperty]
    private string executableStatusIcon = "❓";

    /// <summary>
    /// Gets the executable status message.
    /// </summary>
    [ObservableProperty]
    private string executableStatusMessage = "Select an executable file";

    /// <summary>
    /// Gets the request close action.
    /// </summary>
    public Action? RequestClose { get; set; }

    private async Task LoadPlatformsAsync()
    {
        try
        {
            var result = await _mediator.Send(new GetPlatformsQuery());
            if (result.IsSuccess && result.Value is not null)
            {
                AvailablePlatforms.Clear();
                foreach (var platform in result.Value)
                {
                    AvailablePlatforms.Add(platform);
                }

                // Select current platform if editing
                if (IsEditing && _existingEmulator != null)
                {
                    SelectedPlatform = AvailablePlatforms.FirstOrDefault(p => p.Id == _existingEmulator.PlatformId);
                }
            }
            else
            {
                _logger.LogError("Failed to load platforms: {Error}", result.Error);
                ValidationErrors.Add("Failed to load platforms");
                HasValidationErrors = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load platforms");
            ValidationErrors.Add("Failed to load platforms");
            HasValidationErrors = true;
        }
    }

    partial void OnNameChanged(string value)
    {
        EmulatorName = value;
        ValidateForm();
    }

    partial void OnExecutablePathChanged(string value)
    {
        UpdateExecutableStatus();
        ValidateForm();
    }

    partial void OnSelectedPlatformChanged(PlatformDto? value)
    {
        ValidateForm();
    }

    private void UpdateExecutableStatus()
    {
        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            ExecutableStatusIcon = "❓";
            ExecutableStatusMessage = "Select an executable file";
            CanTestLaunch = false;
            return;
        }

        if (File.Exists(ExecutablePath))
        {
            ExecutableStatusIcon = "🟢";
            ExecutableStatusMessage = "Executable found and valid";
            CanTestLaunch = true;
        }
        else
        {
            ExecutableStatusIcon = "🔴";
            ExecutableStatusMessage = "Executable file not found";
            CanTestLaunch = false;
        }
    }

    private void ValidateForm()
    {
        ValidationErrors.Clear();

        if (string.IsNullOrWhiteSpace(Name))
            ValidationErrors.Add("Name is required");

        if (string.IsNullOrWhiteSpace(ExecutablePath))
            ValidationErrors.Add("Executable path is required");

        if (!File.Exists(ExecutablePath))
            ValidationErrors.Add("Executable file does not exist");

        if (SelectedPlatform == null)
            ValidationErrors.Add("Platform is required");

        // Check for duplicate names
        if (!string.IsNullOrWhiteSpace(Name) && SelectedPlatform != null)
        {
            // This would need to be implemented to check against existing emulators
        }

        HasValidationErrors = ValidationErrors.Any();
        CanSave = !HasValidationErrors && !string.IsNullOrWhiteSpace(Name) &&
                 !string.IsNullOrWhiteSpace(ExecutablePath) && SelectedPlatform != null;
    }

    [RelayCommand]
    private async Task BrowseExecutableAsync()
    {
        try
        {
            var result = await _dialogService.ShowFilePickerAsync(
                "Select Emulator Executable",
                new[] { "exe" });

            if (!string.IsNullOrEmpty(result))
            {
                ExecutablePath = result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to browse for executable");
        }
    }

    [RelayCommand]
    private async Task TestLaunchAsync()
    {
        if (!CanTestLaunch || string.IsNullOrWhiteSpace(ExecutablePath))
            return;

        try
        {
            // Simple test launch without arguments
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ExecutablePath,
                UseShellExecute = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(ExecutablePath)
            });

            _logger.LogInformation("Test launched emulator: {Path}", ExecutablePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test launch emulator");
            await _dialogService.ShowErrorAsync("Test Launch Failed",
                $"Failed to launch emulator: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanSave || SelectedPlatform is null)
            return;

        try
        {
            if (IsEditing)
            {
                var command = new UpdateEmulatorCommand
                {
                    Id = EmulatorId,
                    Name = Name,
                    ExecutablePath = ExecutablePath,
                    PlatformId = SelectedPlatform.Id,
                    Version = Version,
                    Description = Description,
                    CommandLineArgs = CommandLineArgs
                };

                var result = await _mediator.Send(command);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Updated emulator: {Name}", Name);
                    RequestClose?.Invoke();
                }
                else
                {
                    ValidationErrors.Add(result.Error!);
                    HasValidationErrors = true;
                }
            }
            else
            {
                var command = new AddEmulatorCommand
                {
                    Name = Name,
                    ExecutablePath = ExecutablePath,
                    PlatformId = SelectedPlatform.Id,
                    Version = Version,
                    Description = Description,
                    CommandLineArgs = CommandLineArgs
                };

                var result = await _mediator.Send(command);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Added emulator: {Name}", Name);
                    RequestClose?.Invoke();
                }
                else
                {
                    ValidationErrors.Add(result.Error!);
                    HasValidationErrors = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save emulator");
            ValidationErrors.Add("Failed to save emulator");
            HasValidationErrors = true;
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}