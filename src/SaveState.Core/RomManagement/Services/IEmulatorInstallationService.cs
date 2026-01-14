using SaveState.Core.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.RomManagement.Services;

/// <summary>
/// Provides logic for installing emulators.
/// </summary>
public interface IEmulatorInstallationService
{
    /// <summary>
    /// Installs the emulator with the specified ID.
    /// </summary>
    Task<Result> InstallEmulatorAsync(Guid emulatorId, CancellationToken cancellationToken = default);
}
