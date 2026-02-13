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
/// Handler for adding new emulators.
/// </summary>
public class AddEmulatorCommandHandler : IRequestHandler<AddEmulatorCommand, Result<EmulatorResult>>
{
    private readonly IEmulatorRepository _emulatorRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly ILogger<AddEmulatorCommandHandler> _logger;

    public AddEmulatorCommandHandler(
        IEmulatorRepository emulatorRepository,
        IPlatformRepository platformRepository,
        ILogger<AddEmulatorCommandHandler> logger)
    {
        _emulatorRepository = emulatorRepository ?? throw new ArgumentNullException(nameof(emulatorRepository));
        _platformRepository = platformRepository ?? throw new ArgumentNullException(nameof(platformRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the command to add a new emulator.
    /// </summary>
    /// <param name="request">The add emulator command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created emulator information.</returns>
    public async Task<Result<EmulatorResult>> Handle(AddEmulatorCommand request, CancellationToken ct)
    {
        // Validate platform exists
        var platform = await _platformRepository.GetByIdAsync(request.PlatformId, ct).ConfigureAwait(false);
        if (platform is null)
            return Result.Failure<EmulatorResult>("Platform not found", ErrorType.NotFound);

        // Validate executable path
        if (!File.Exists(request.ExecutablePath))
            return Result.Failure<EmulatorResult>("Executable file does not exist", ErrorType.Validation);

        // Check if emulator with same name and platform already exists
        var existingEmulators = await _emulatorRepository.GetAllAsync(ct).ConfigureAwait(false);
        var existingEmulator = existingEmulators.FirstOrDefault(e =>
            e.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase) &&
            e.PlatformId == request.PlatformId);

        if (existingEmulator != null)
            return Result.Failure<EmulatorResult>("Emulator with this name already exists for the platform", ErrorType.Conflict);

        // Create new emulator
        var filePath = new FilePath(request.ExecutablePath);
        var emulator = new SaveState.Core.RomManagement.Entities.Emulator(request.Name, filePath, request.PlatformId);

        if (!string.IsNullOrEmpty(request.Version))
            emulator.UpdateVersion(request.Version);

        if (!string.IsNullOrEmpty(request.Description))
            emulator.UpdateDescription(request.Description);

        if (!string.IsNullOrEmpty(request.CommandLineArgs))
            emulator.SetCommandLineArgs(request.CommandLineArgs);

        // Save to repository
        await _emulatorRepository.AddAsync(emulator, ct).ConfigureAwait(false);

        _logger.LogInformation("Added emulator {EmulatorName} for platform {PlatformName}",
            emulator.Name, platform.Name.Value);

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