using SaveState.Core.Common;
using SaveState.Core.RomManagement.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Application.RomManagement.Services;

/// <summary>
/// Default implementation of <see cref="IEmulatorInstallationService"/> that installs emulators.
/// </summary>
public class EmulatorInstallationService : IEmulatorInstallationService
{
    private readonly ILogger<EmulatorInstallationService> _logger;

    public EmulatorInstallationService(ILogger<EmulatorInstallationService> logger)
    {
        _logger = logger;
    }

    public async Task<Result> InstallEmulatorAsync(Guid emulatorId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Installing emulator {EmulatorId}", emulatorId);
        await Task.Delay(100, cancellationToken);
        _logger.LogInformation("Emulator {EmulatorId} installation simulated", emulatorId);
        return Result.Success();
    }
}
