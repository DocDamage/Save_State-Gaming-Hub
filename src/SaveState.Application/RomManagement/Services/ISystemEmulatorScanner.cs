using SaveState.Core.Common;
using SaveState.Core.Configuration;

namespace SaveState.Application.RomManagement.Services;

/// <summary>
/// Service for scanning the system for installed emulators.
/// </summary>
public interface ISystemEmulatorScanner
{
    /// <summary>
    /// Scans the entire system for emulator installations.
    /// </summary>
    /// <param name="options">Scanning options.</param>
    /// <param name="progress">Optional progress reporting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing discovered emulators.</returns>
    Task<Result<IReadOnlyList<DiscoveredEmulator>>> ScanSystemAsync(
        EmulatorScanningOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Represents a discovered emulator installation.
/// </summary>
public record DiscoveredEmulator(
    string Name,
    string ExecutablePath,
    string? Version,
    string? Publisher,
    DateTime? InstallDate,
    long SizeBytes,
    EmulatorType Type);

/// <summary>
/// Types of emulators that can be discovered.
/// </summary>
public enum EmulatorType
{
    /// <summary>
    /// Multi-system emulator like RetroArch.
    /// </summary>
    MultiSystem,

    /// <summary>
    /// Single-system emulator.
    /// </summary>
    SingleSystem,

    /// <summary>
    /// Unknown type.
    /// </summary>
    Unknown
}