using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.ValueObjects;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.RomManagement.Commands.Handlers;

/// <summary>
/// Handler for updating existing emulators.
/// </summary>
public class UpdateEmulatorCommandHandler : IRequestHandler<UpdateEmulatorCommand, Result<EmulatorResult>>
{
    private readonly IEmulatorRepository _emulatorRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly ILogger<UpdateEmulatorCommandHandler> _logger;

    public UpdateEmulatorCommandHandler(
        IEmulatorRepository emulatorRepository,
        IPlatformRepository platformRepository,
        ILogger<UpdateEmulatorCommandHandler> logger)
    {
        _emulatorRepository = emulatorRepository ?? throw new ArgumentNullException(nameof(emulatorRepository));
        _platformRepository = platformRepository ?? throw new ArgumentNullException(nameof(platformRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the command to update an emulator.
    /// </summary>
    /// <param name="request">The update emulator command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated emulator information.</returns>
    public async Task<Result<EmulatorResult>> Handle(UpdateEmulatorCommand request, CancellationToken ct)
    {
        // Get existing emulator
        var emulator = await _emulatorRepository.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (emulator is null)
            return Result.Failure<EmulatorResult>("Emulator not found", ErrorType.NotFound);

        // Validate platform exists
        var platform = await _platformRepository.GetByIdAsync(request.PlatformId, ct).ConfigureAwait(false);
        if (platform is null)
            return Result.Failure<EmulatorResult>("Platform not found", ErrorType.NotFound);

        // Validate executable path
        if (!File.Exists(request.ExecutablePath))
            return Result.Failure<EmulatorResult>("Executable file does not exist", ErrorType.Validation);

        // Check for name conflicts (excluding this emulator)
        var existingEmulators = await _emulatorRepository.GetAllAsync(ct).ConfigureAwait(false);
        var conflictingEmulator = existingEmulators.FirstOrDefault(e =>
            e.Id != request.Id &&
            e.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase) &&
            e.PlatformId == request.PlatformId);

        if (conflictingEmulator != null)
            return Result.Failure<EmulatorResult>("Emulator with this name already exists for the platform", ErrorType.Conflict);

        // Update emulator properties
        var originalName = emulator.Name;
        var originalExecutablePath = emulator.ExecutablePath.Value;
        var originalPlatformId = emulator.PlatformId;

        // Update the emulator entity with new values
        emulator.UpdateName(request.Name);
        emulator.UpdateExecutablePath(new FilePath(request.ExecutablePath));
        emulator.UpdatePlatform(request.PlatformId);

        if (request.Version is not null)
            emulator.UpdateVersion(request.Version);

        if (request.Description is not null)
            emulator.UpdateDescription(request.Description);

        if (request.CommandLineArgs is not null)
            emulator.SetCommandLineArgs(request.CommandLineArgs);

        // Save the updated emulator
        await _emulatorRepository.UpdateAsync(emulator, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Emulator {EmulatorId} updated successfully. Name: '{OriginalName}' -> '{NewName}', " +
            "Path: '{OriginalPath}' -> '{NewPath}', Platform: {OriginalPlatformId} -> {NewPlatformId}",
            emulator.Id,
            originalName,
            emulator.Name,
            originalExecutablePath,
            emulator.ExecutablePath.Value,
            originalPlatformId,
            emulator.PlatformId);

        var result = new EmulatorResult(
            emulator.Id,
            emulator.Name,
            emulator.ExecutablePath.Value,
            emulator.PlatformId,
            emulator.Version,
            emulator.Description,
            emulator.CommandLineArgs,
            emulator.IsAvailable);

        return Result.Success(result);
    }
}