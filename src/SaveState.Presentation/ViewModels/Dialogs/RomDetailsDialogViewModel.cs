using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.Services;
using MediatR;
using SaveState.Core.Common.ValueObjects;
using SaveState.Application.Common.Options;
using SaveState.Application.RomManagement.Commands;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for the ROM details dialog.
/// </summary>
public partial class RomDetailsDialogViewModel : ObservableObject
{
    private readonly RomFile _romFile;
    private readonly IRomFileRepository _romFileRepository;
    private readonly IEmulatorRepository _emulatorRepository;
    private readonly IPlatformExtensionRegistry _extensionRegistry;
    private readonly IRomVerificationService _romVerificationService;
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RomDetailsDialogViewModel> _logger;

    public RomDetailsDialogViewModel(
        RomFile romFile,
        IRomFileRepository romFileRepository,
        IEmulatorRepository emulatorRepository,
        IPlatformExtensionRegistry extensionRegistry,
        IRomVerificationService romVerificationService,
        IMediator mediator,
        IDialogService dialogService,
        ILogger<RomDetailsDialogViewModel> logger)
    {
        _romFile = romFile ?? throw new ArgumentNullException(nameof(romFile));
        _romFileRepository = romFileRepository ?? throw new ArgumentNullException(nameof(romFileRepository));
        _emulatorRepository = emulatorRepository ?? throw new ArgumentNullException(nameof(emulatorRepository));
        _extensionRegistry = extensionRegistry ?? throw new ArgumentNullException(nameof(extensionRegistry));
        _romVerificationService = romVerificationService ?? throw new ArgumentNullException(nameof(romVerificationService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        AvailableEmulators = new ObservableCollection<EmulatorViewModel>();

        RomTitle = romFile.Title;
        FileName = System.IO.Path.GetFileName(romFile.FilePath.Value);
        FilePath = romFile.FilePath.Value;
        FileSize = $"{romFile.FileSize / 1024.0 / 1024.0:F1} MB";
        RomStatus = romFile.Status.ToString();
        PlatformName = romFile.Platform?.Name?.Value ?? "Unknown";
        LastScanned = romFile.ScannedAt;

        var extensions = _extensionRegistry.GetExtensions(PlatformName);
        FileExtensions = string.Join(", ", extensions);

        _ = LoadAvailableEmulatorsAsync();
        UpdateSelectedEmulator();
    }

    /// <summary>
    /// Gets the ROM title.
    /// </summary>
    [ObservableProperty]
    private string romTitle = string.Empty;

    /// <summary>
    /// Gets the file name.
    /// </summary>
    [ObservableProperty]
    private string fileName = string.Empty;

    /// <summary>
    /// Gets the full file path.
    /// </summary>
    [ObservableProperty]
    private string filePath = string.Empty;

    /// <summary>
    /// Gets the file size.
    /// </summary>
    [ObservableProperty]
    private string fileSize = string.Empty;

    /// <summary>
    /// Gets the ROM status.
    /// </summary>
    [ObservableProperty]
    private string romStatus = string.Empty;

    /// <summary>
    /// Gets the platform name.
    /// </summary>
    [ObservableProperty]
    private string platformName = string.Empty;

    /// <summary>
    /// Gets the file extensions for this platform.
    /// </summary>
    [ObservableProperty]
    private string fileExtensions = string.Empty;

    /// <summary>
    /// Gets when the ROM was last scanned.
    /// </summary>
    [ObservableProperty]
    private DateTime lastScanned;

    /// <summary>
    /// Gets the available emulators.
    /// </summary>
    public ObservableCollection<EmulatorViewModel> AvailableEmulators { get; }

    /// <summary>
    /// Gets or sets the selected emulator.
    /// </summary>
    [ObservableProperty]
    private EmulatorViewModel? selectedEmulator;

    /// <summary>
    /// Gets or sets whether to launch in fullscreen.
    /// </summary>
    [ObservableProperty]
    private bool launchFullscreen;

    /// <summary>
    /// Gets or sets whether VSync is enabled.
    /// </summary>
    [ObservableProperty]
    private bool enableVSync = true;

    /// <summary>
    /// Gets or sets custom launch arguments.
    /// </summary>
    [ObservableProperty]
    private string customArguments = string.Empty;

    /// <summary>
    /// Gets whether the ROM can be launched.
    /// </summary>
    [ObservableProperty]
    private bool canLaunch;

    /// <summary>
    /// Gets the request close action.
    /// </summary>
    public Action? RequestClose { get; set; }

    private async Task LoadAvailableEmulatorsAsync()
    {
        try
        {
            var emulators = await _emulatorRepository.GetAllByPlatformIdAsync(_romFile.PlatformId);
            AvailableEmulators.Clear();

            foreach (var emulator in emulators)
            {
                var emulatorVm = new EmulatorViewModel(emulator, _dialogService, _logger);
                AvailableEmulators.Add(emulatorVm);
            }

            UpdateSelectedEmulator();
            UpdateCanLaunch();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load available emulators");
        }
    }

    private void UpdateSelectedEmulator()
    {
        // Select the first available emulator by default
        SelectedEmulator = AvailableEmulators.FirstOrDefault(e => e.IsAvailable);
    }

    private void UpdateCanLaunch()
    {
        CanLaunch = SelectedEmulator?.IsAvailable == true;
    }

    partial void OnSelectedEmulatorChanged(EmulatorViewModel? value)
    {
        UpdateCanLaunch();
    }

    [RelayCommand]
    private async Task LaunchRomAsync()
    {
        if (SelectedEmulator == null || !CanLaunch)
            return;

        try
        {
            var launchOptions = new SaveState.Application.Common.Options.LaunchOptions
            {
                Arguments = CustomArguments
            };

            var command = new SaveState.Application.RomManagement.Commands.LaunchRomCommand
            {
                RomFileId = (RomFileId)_romFile.Id,
                Options = launchOptions
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully launched ROM {RomTitle}", RomTitle);
                RequestClose?.Invoke();
            }
            else
            {
                _logger.LogWarning("Failed to launch ROM {RomTitle}: {Error}", RomTitle, result.Error);
                await _dialogService.ShowErrorAsync(
                    "Launch Failed",
                    $"Failed to launch ROM '{RomTitle}': {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch ROM {RomTitle}", RomTitle);
            await _dialogService.ShowErrorAsync(
                "Launch Error",
                $"An error occurred while launching '{RomTitle}': {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task VerifyRomAsync()
    {
        try
        {
            _logger.LogInformation("Verifying ROM: {RomTitle}", RomTitle);

            var result = await _romVerificationService.VerifyRomAsync(_romFile);

            if (result.IsValid)
            {
                _logger.LogInformation("ROM verification successful for {RomTitle}. Checksum: {Checksum}",
                    RomTitle, result.ActualChecksum);

                await _dialogService.ShowInformationAsync(
                    "ROM Verification",
                    $"ROM verification successful!\n\nChecksum: {result.ActualChecksum}");
            }
            else
            {
                _logger.LogWarning("ROM verification failed for {RomTitle}. Expected: {Expected}, Actual: {Actual}, Error: {Error}",
                    RomTitle, result.ExpectedChecksum, result.ActualChecksum, result.ErrorMessage);

                await _dialogService.ShowInformationAsync(
                    "ROM Verification Failed",
                    $"ROM verification failed!\n\n{result.ErrorMessage}\n\nExpected: {result.ExpectedChecksum}\nActual: {result.ActualChecksum}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify ROM: {RomTitle}", RomTitle);
            await _dialogService.ShowInformationAsync(
                "Verification Error",
                $"Failed to verify ROM: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task EditMetadataAsync()
    {
        try
        {
            _logger.LogInformation("Editing metadata for ROM: {RomTitle}", RomTitle);

            // Show metadata editing dialog
            var result = await _dialogService.ShowRomMetadataDialogAsync(
                _romFile.Title,
                _romFile.Description,
                _romFile.Region,
                _romFile.Version);

            if (result != null)
            {
                var (title, description, region, version) = result;

                // Update metadata using command
                var command = new UpdateRomMetadataCommand
                {
                    RomFileId = (Guid)_romFile.Id,
                    Title = title,
                    Description = description,
                    Region = region,
                    Version = version
                };

                var updateResult = await _mediator.Send(command);
                if (updateResult.IsSuccess)
                {
                    _logger.LogInformation("Successfully updated metadata for ROM: {RomTitle}", RomTitle);

                    // Update local properties
                    RomTitle = title;

                    // Show success message
                    await _dialogService.ShowMessageDialogAsync(
                        "Metadata Updated",
                        "ROM metadata has been successfully updated.");
                }
                else
                {
                    _logger.LogWarning("Failed to update ROM metadata: {Error}", updateResult.Error);
                    await _dialogService.ShowMessageDialogAsync(
                        "Update Failed",
                        $"Failed to update ROM metadata: {updateResult.Error}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit metadata for ROM: {RomTitle}", RomTitle);
            await _dialogService.ShowInformationAsync(
                "Edit Error",
                $"Failed to edit ROM metadata: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportToLibraryAsync()
    {
        try
        {
            _logger.LogInformation("Importing ROM '{RomTitle}' to game library", RomTitle);

            // Create the import command
            var command = new ImportRomToLibraryCommand
            {
                RomFileId = SaveState.Core.Common.ValueObjects.RomFileId.From((Guid)_romFile.Id),
                TitleOverride = null, // Use ROM title
                Description = _romFile.Description,
                CreateIfNotExists = true
            };

            // Send the command
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                var importResult = result.Value;
                _logger.LogInformation(
                    "Successfully imported ROM '{RomTitle}' to game library as '{GameTitle}' (Game ID: {GameId})",
                    RomTitle, importResult.GameTitle, importResult.GameId);

                // Show success message
                var message = importResult.GameWasCreated
                    ? $"Successfully imported ROM to library!\n\nCreated new game: '{importResult.GameTitle}'"
                    : $"Successfully imported ROM to library!\n\nAdded to existing game: '{importResult.GameTitle}'";

                await _dialogService.ShowMessageDialogAsync("Import Successful", message);

                // Optionally close the dialog
                RequestClose?.Invoke();
            }
            else
            {
                _logger.LogWarning("Failed to import ROM to library: {Error}", result.Error);
                await _dialogService.ShowMessageDialogAsync(
                    "Import Failed",
                    $"Failed to import ROM to library: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import ROM '{RomTitle}' to library", RomTitle);
            await _dialogService.ShowInformationAsync(
                "Import Error",
                $"Failed to import ROM to library: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}

/// <summary>
/// Simplified emulator view model for the dialog.
/// </summary>
public class EmulatorViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly ILogger _logger;

    public EmulatorViewModel(SaveState.Core.RomManagement.Entities.Emulator emulator, IDialogService dialogService, ILogger logger)
    {
        Emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public SaveState.Core.RomManagement.Entities.Emulator Emulator { get; }

    public string Name => Emulator.Name;
    public string Platform => Emulator.Platform?.Name.Value ?? "Unknown";
    public string Version => Emulator.Version ?? "Unknown";
    public string ExecutablePath => Emulator.ExecutablePath?.Value ?? "Not configured";
    public bool IsAvailable => !string.IsNullOrEmpty(ExecutablePath) && File.Exists(ExecutablePath);
}
