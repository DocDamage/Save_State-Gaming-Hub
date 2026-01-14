using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Microsoft.Extensions.Logging;
using MediatR;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Services;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.ViewModels.Dialogs;

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

    public Action? RequestClose { get; set; }

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
        _romFile = romFile;
        _romFileRepository = romFileRepository;
        _emulatorRepository = emulatorRepository;
        _extensionRegistry = extensionRegistry;
        _romVerificationService = romVerificationService;
        _mediator = mediator;
        _dialogService = dialogService;
        _logger = logger;
    }
}

public partial class EmulatorConfigDialogViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<EmulatorConfigDialogViewModel> _logger;
    private readonly SaveState.Core.RomManagement.Entities.Emulator? _existingEmulator;

    public Action? RequestClose { get; set; }

    public EmulatorConfigDialogViewModel(
        IMediator mediator,
        IDialogService dialogService,
        ILogger<EmulatorConfigDialogViewModel> logger,
        SaveState.Core.RomManagement.Entities.Emulator? existingEmulator)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _logger = logger;
        _existingEmulator = existingEmulator;
    }
}

public partial class RomScanProgressDialogViewModel : ObservableObject
{
    private readonly ILogger<RomScanProgressDialogViewModel> _logger;
    public Action? RequestClose { get; set; }

    public RomScanProgressDialogViewModel(ILogger<RomScanProgressDialogViewModel> logger)
    {
        _logger = logger;
    }

    public void UpdateElapsedTime()
    {
        // Implementation
    }
}

public partial class EmulatorSetupWizardViewModel : ObservableObject
{
    private readonly IEmulatorInstallationService _installationService;
    public Action? RequestClose { get; set; }

    public EmulatorSetupWizardViewModel(IEmulatorInstallationService installationService)
    {
        _installationService = installationService;
    }
}
