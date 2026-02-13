using SaveState.Core.Common;

namespace SaveState.Core.Sync.Services;

/// <summary>
/// Service for optimizing network settings for cloud gaming.
/// Provides TCP/QoS optimizations with admin detection and safe fallbacks.
/// </summary>
public interface INetworkOptimizerService
{
    /// <summary>
    /// Gets whether the current process is running with administrator privileges.
    /// </summary>
    bool IsRunningAsAdmin { get; }

    /// <summary>
    /// Gets whether network optimization is supported on the current platform.
    /// </summary>
    bool IsPlatformSupported { get; }

    /// <summary>
    /// Applies TCP optimizations for cloud gaming (reduces latency, enables TCP Fast Open, etc.).
    /// Requires administrator privileges on Windows.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure with details.</returns>
    Task<Result<NetworkOptimizationResult>> ApplyCloudGamingOptimizationsAsync(CancellationToken ct = default);

    /// <summary>
    /// Reverts TCP optimizations to system defaults.
    /// Requires administrator privileges on Windows.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RevertOptimizationsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current network optimization status.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current optimization status.</returns>
    Task<Result<NetworkOptimizationStatus>> GetOptimizationStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Launches an elevated helper process to apply optimizations when not running as admin.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the elevated process was launched successfully.</returns>
    Task<Result> LaunchElevatedOptimizerAsync(CancellationToken ct = default);
}

/// <summary>
/// Result of applying network optimizations.
/// </summary>
public sealed record NetworkOptimizationResult(
    bool Success,
    IReadOnlyList<string> AppliedOptimizations,
    IReadOnlyList<string> FailedOptimizations,
    string? Message);

/// <summary>
/// Current network optimization status.
/// </summary>
public sealed record NetworkOptimizationStatus(
    bool IsOptimized,
    bool TcpFastOpenEnabled,
    bool NagleAlgorithmDisabled,
    bool ReceiveWindowAutoTuningEnabled,
    int? CurrentMtu,
    string StatusMessage);
