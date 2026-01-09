using SaveState.Core.Common;
using SaveState.Core.Configuration;

namespace SaveState.Application.RomManagement.Services;

/// <summary>
/// Service for scanning the system for MUGEN installations.
/// </summary>
public interface ISystemMugenScanner
{
    /// <summary>
    /// Scans the entire system for MUGEN installations.
    /// </summary>
    /// <param name="options">Scanning options.</param>
    /// <param name="progress">Optional progress reporting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing discovered MUGEN installations.</returns>
    Task<Result<IReadOnlyList<DiscoveredMugenInstallation>>> ScanSystemAsync(
        MugenScanningOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Represents a discovered MUGEN installation.
/// </summary>
public record DiscoveredMugenInstallation(
    string Name,
    string InstallPath,
    string? Version,
    MugenEngineType EngineType,
    int CharacterCount,
    int StageCount,
    long TotalSizeBytes,
    DateTime? InstallDate,
    bool IsValidInstallation);

/// <summary>
/// Types of MUGEN engines.
/// </summary>
public enum MugenEngineType
{
    /// <summary>
    /// Original MUGEN engine.
    /// </summary>
    Original,

    /// <summary>
    /// Ikemen GO engine.
    /// </summary>
    IkemenGo,

    /// <summary>
    /// Unknown or custom engine.
    /// </summary>
    Unknown
}