using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// No-op implementation of network optimization for non-Windows platforms.
/// Logs warnings but does not crash, allowing the application to run on Linux/macOS.
/// </summary>
public sealed class NoOpNetworkOptimizerService : INetworkOptimizerService
{
    private readonly ILogger<NoOpNetworkOptimizerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoOpNetworkOptimizerService"/> class.
    /// </summary>
    public NoOpNetworkOptimizerService(ILogger<NoOpNetworkOptimizerService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsRunningAsAdmin => false;

    /// <inheritdoc />
    public bool IsPlatformSupported => false;

    /// <inheritdoc />
    public Task<Result<NetworkOptimizationResult>> ApplyCloudGamingOptimizationsAsync(CancellationToken ct = default)
    {
        _logger.LogWarning("Network optimization is not supported on this platform");

        return Task.FromResult(Result.Success(new NetworkOptimizationResult(
            Success: false,
            AppliedOptimizations: Array.Empty<string>(),
            FailedOptimizations: new[] { "Platform not supported" },
            Message: "Network optimization is only available on Windows")));
    }

    /// <inheritdoc />
    public Task<Result> RevertOptimizationsAsync(CancellationToken ct = default)
    {
        _logger.LogWarning("Network optimization revert is not supported on this platform");
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<NetworkOptimizationStatus>> GetOptimizationStatusAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(new NetworkOptimizationStatus(
            IsOptimized: false,
            TcpFastOpenEnabled: false,
            NagleAlgorithmDisabled: false,
            ReceiveWindowAutoTuningEnabled: false,
            CurrentMtu: null,
            StatusMessage: "Network optimization is not available on this platform")));
    }

    /// <inheritdoc />
    public Task<Result> LaunchElevatedOptimizerAsync(CancellationToken ct = default)
    {
        _logger.LogWarning("Elevated optimizer is not available on this platform");
        return Task.FromResult(Result.Failure("Elevated optimizer is only available on Windows"));
    }
}
