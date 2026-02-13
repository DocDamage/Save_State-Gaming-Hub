using SaveState.Core.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.RomManagement.Services;

/// <summary>
/// Represents an emulator installation option for the setup wizard.
/// </summary>
public class EmulatorInstallationOption
{
    /// <summary>
    /// Gets or sets the emulator name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the emulator description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the download URL.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the emulator version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the recommended emulator.
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// Gets or sets whether the emulator requires manual installation.
    /// </summary>
    public bool RequiresManualInstall { get; set; }

    /// <summary>
    /// Gets or sets the platform this emulator supports.
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the emulator icon URL.
    /// </summary>
    public string IconUrl { get; set; } = string.Empty;
}

/// <summary>
/// Provides logic for installing emulators.
/// </summary>
public interface IEmulatorInstallationService
{
    /// <summary>
    /// Installs the emulator with the specified ID.
    /// </summary>
    Task<Result> InstallEmulatorAsync(Guid emulatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available emulator installation options.
    /// </summary>
    Task<Result<IReadOnlyList<EmulatorInstallationOption>>> GetAvailableOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and installs an emulator.
    /// </summary>
    /// <param name="option">The installation option.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> DownloadAndInstallAsync(EmulatorInstallationOption option, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
