using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;

namespace SaveState.Application.RomManagement.Commands.Handlers;

/// <summary>
/// Handler for launching ROM files.
/// Validates ROM existence and launches emulator processes.
/// </summary>
public class LaunchRomCommandHandler : IRequestHandler<LaunchRomCommand, Result<ProcessInfo>>
{
    private readonly IRomFileRepository _romFileRepository;
    private readonly IEmulatorRepository _emulatorRepository;
    private readonly IProcessLauncher _processLauncher;
    private readonly ILogger<LaunchRomCommandHandler> _logger;

    public LaunchRomCommandHandler(
        IRomFileRepository romFileRepository,
        IEmulatorRepository emulatorRepository,
        IProcessLauncher processLauncher,
        ILogger<LaunchRomCommandHandler> logger)
    {
        _romFileRepository = romFileRepository ?? throw new ArgumentNullException(nameof(romFileRepository));
        _emulatorRepository = emulatorRepository ?? throw new ArgumentNullException(nameof(emulatorRepository));
        _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the command to launch a ROM.
    /// </summary>
    /// <param name="request">The launch ROM command with ROM file ID.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the process information or an error.</returns>
    public async Task<Result<ProcessInfo>> Handle(LaunchRomCommand request, CancellationToken ct)
    {
        var romFile = await _romFileRepository.GetByIdAsync(request.RomFileId, ct).ConfigureAwait(false);
        if (romFile is null)
            return Result.Failure<ProcessInfo>("ROM file not found", ErrorType.NotFound);

        // Find an appropriate emulator for this ROM's platform
        var emulator = await FindEmulatorForPlatformAsync(romFile.PlatformId, ct).ConfigureAwait(false);
        if (emulator is null)
            return Result.Failure<ProcessInfo>($"No emulator found for platform {romFile.Platform?.Name ?? "Unknown"}", ErrorType.NotFound);

        // Check if emulator executable exists
        if (!emulator.IsAvailable)
            return Result.Failure<ProcessInfo>($"Emulator executable not found: {emulator.ExecutablePath}", ErrorType.Validation);

        // Build launch arguments
        var launchArgs = BuildLaunchArguments(emulator, romFile);

        _logger.LogInformation("Launching ROM {RomTitle} with emulator {EmulatorName}",
            romFile.Title, emulator.Name);

        // Launch the emulator process
        var launchConfig = new LaunchConfiguration
        {
            ExecutablePath = emulator.ExecutablePath.Value,
            Arguments = launchArgs,
            WorkingDirectory = System.IO.Path.GetDirectoryName(emulator.ExecutablePath.Value),
            WaitForExit = false
        };

        var processInfo = await _processLauncher.LaunchAsync(launchConfig, ct).ConfigureAwait(false);
        var result = Result.Success(processInfo);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully launched ROM {RomTitle} (PID: {ProcessId})",
                romFile.Title, result.Value.ProcessId);
        }
        else
        {
            _logger.LogWarning("Failed to launch ROM {RomTitle}: {Error}",
                romFile.Title, result.Error);
        }

        return result;
    }

    private async Task<SaveState.Core.RomManagement.Entities.Emulator?> FindEmulatorForPlatformAsync(Guid platformId, CancellationToken ct)
    {
        // Get the emulator for this platform
        var emulator = await _emulatorRepository.GetByPlatformIdAsync(platformId, ct).ConfigureAwait(false);

        // Check if it's available
        return emulator?.IsAvailable == true ? emulator : null;
    }

    private string BuildLaunchArguments(SaveState.Core.RomManagement.Entities.Emulator emulator, RomFile romFile)
    {
        var romPath = romFile.FilePath.Value;

        // If emulator has custom command line args, use them with ROM path substitution
        if (!string.IsNullOrEmpty(emulator.CommandLineArgs))
        {
            return emulator.CommandLineArgs.Replace("{ROM}", romPath);
        }

        // Default: just pass the ROM path as argument
        return $"\"{romPath}\"";
    }
}