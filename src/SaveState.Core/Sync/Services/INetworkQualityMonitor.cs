using SaveState.Core.Common;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Core.Sync.Services;

/// <summary>
/// Service for monitoring network quality and performance.
/// </summary>
public interface INetworkQualityMonitor
{
    /// <summary>
    /// Performs a comprehensive network quality test.
    /// </summary>
    Task<Result<NetworkQualityTestResult>> PerformQualityTestAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets real-time network quality metrics.
    /// </summary>
    Task<Result<NetworkQuality>> GetCurrentQualityAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Starts continuous network monitoring.
    /// </summary>
    Task<Result> StartMonitoringAsync(
        TimeSpan interval,
        CancellationToken ct = default);

    /// <summary>
    /// Stops network monitoring.
    /// </summary>
    Task<Result> StopMonitoringAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets network quality history over a time period.
    /// </summary>
    Task<Result<IReadOnlyList<NetworkQuality>>> GetQualityHistoryAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if the current network quality is sufficient for cloud gaming.
    /// </summary>
    Task<Result<bool>> IsQualitySufficientForCloudGamingAsync(
        CloudGamingProvider provider,
        CancellationToken ct = default);

    /// <summary>
    /// Gets detailed network diagnostics information.
    /// </summary>
    Task<Result<NetworkDiagnostics>> GetNetworkDiagnosticsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Event raised when network quality changes significantly.
    /// </summary>
    event EventHandler<NetworkQualityChangedEventArgs>? NetworkQualityChanged;

    /// <summary>
    /// Gets the current monitoring status.
    /// </summary>
    bool IsMonitoring { get; }
}