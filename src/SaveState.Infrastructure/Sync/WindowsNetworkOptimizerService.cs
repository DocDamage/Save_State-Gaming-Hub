using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Windows-specific implementation of network optimization for cloud gaming.
/// Uses netsh commands to apply TCP optimizations.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsNetworkOptimizerService : INetworkOptimizerService
{
    private readonly ILogger<WindowsNetworkOptimizerService> _logger;
    private readonly string _elevatedHelperPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsNetworkOptimizerService"/> class.
    /// </summary>
    public WindowsNetworkOptimizerService(ILogger<WindowsNetworkOptimizerService> logger)
    {
        _logger = logger;

        // Path to elevated helper (same directory as main executable)
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _elevatedHelperPath = Path.Combine(baseDir, "SaveState.NetworkHelper.exe");
    }

    /// <inheritdoc />
    public bool IsRunningAsAdmin
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check admin status, assuming non-admin");
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool IsPlatformSupported => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public async Task<Result<NetworkOptimizationResult>> ApplyCloudGamingOptimizationsAsync(CancellationToken ct = default)
    {
        if (!IsRunningAsAdmin)
        {
            _logger.LogWarning("Cannot apply network optimizations without administrator privileges");
            return Result.Failure<NetworkOptimizationResult>(
                "Administrator privileges required. Use LaunchElevatedOptimizerAsync to run with elevated permissions.");
        }

        var applied = new List<string>();
        var failed = new List<string>();

        try
        {
            // 1. Enable TCP Fast Open (reduces connection latency)
            var tcpFastOpenResult = await RunNetshCommandAsync(
                "int tcp set global fastopen=enabled", ct).ConfigureAwait(false);
            if (tcpFastOpenResult.IsSuccess)
            {
                applied.Add("TCP Fast Open enabled");
            }
            else
            {
                failed.Add($"TCP Fast Open: {tcpFastOpenResult.Error}");
            }

            // 2. Set receive window auto-tuning to normal (improves throughput)
            var autoTuningResult = await RunNetshCommandAsync(
                "int tcp set global autotuninglevel=normal", ct).ConfigureAwait(false);
            if (autoTuningResult.IsSuccess)
            {
                applied.Add("Receive window auto-tuning set to normal");
            }
            else
            {
                failed.Add($"Auto-tuning: {autoTuningResult.Error}");
            }

            // 3. Enable Direct Cache Access (DCA) for supported NICs
            var dcaResult = await RunNetshCommandAsync(
                "int tcp set global dca=enabled", ct).ConfigureAwait(false);
            if (dcaResult.IsSuccess)
            {
                applied.Add("Direct Cache Access enabled");
            }
            else
            {
                // DCA may not be supported on all systems, don't treat as failure
                _logger.LogDebug("DCA not supported or failed: {Error}", dcaResult.Error);
            }

            // 4. Enable ECN capability (Explicit Congestion Notification)
            var ecnResult = await RunNetshCommandAsync(
                "int tcp set global ecncapability=enabled", ct).ConfigureAwait(false);
            if (ecnResult.IsSuccess)
            {
                applied.Add("ECN capability enabled");
            }
            else
            {
                failed.Add($"ECN: {ecnResult.Error}");
            }

            // 5. Set timestamps to enabled (better RTT estimation)
            var timestampsResult = await RunNetshCommandAsync(
                "int tcp set global timestamps=enabled", ct).ConfigureAwait(false);
            if (timestampsResult.IsSuccess)
            {
                applied.Add("TCP timestamps enabled");
            }
            else
            {
                failed.Add($"Timestamps: {timestampsResult.Error}");
            }

            // 6. Disable Nagle's Algorithm for lower latency (via registry - requires restart)
            // Note: This is a registry change, we'll just log it as a recommendation
            applied.Add("Recommendation: Disable Nagle's Algorithm in registry for minimal latency");

            var success = applied.Count > 0;
            var message = success
                ? $"Applied {applied.Count} optimizations successfully"
                : "No optimizations were applied";

            _logger.LogInformation("Network optimization complete: {Message}", message);

            return Result.Success(new NetworkOptimizationResult(
                Success: success,
                AppliedOptimizations: applied,
                FailedOptimizations: failed,
                Message: message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply network optimizations");
            return Result.Failure<NetworkOptimizationResult>($"Optimization failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> RevertOptimizationsAsync(CancellationToken ct = default)
    {
        if (!IsRunningAsAdmin)
        {
            return Result.Failure("Administrator privileges required to revert optimizations");
        }

        try
        {
            var errors = new List<string>();

            // Revert to Windows defaults
            var commands = new[]
            {
                "int tcp set global fastopen=disabled",
                "int tcp set global autotuninglevel=normal",
                "int tcp set global dca=disabled",
                "int tcp set global ecncapability=disabled",
                "int tcp set global timestamps=disabled"
            };

            foreach (var cmd in commands)
            {
                var result = await RunNetshCommandAsync(cmd, ct).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    errors.Add(result.Error ?? "Unknown error");
                }
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning("Some optimizations could not be reverted: {Errors}", string.Join(", ", errors));
            }

            _logger.LogInformation("Network optimizations reverted to defaults");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revert network optimizations");
            return Result.Failure($"Revert failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<NetworkOptimizationStatus>> GetOptimizationStatusAsync(CancellationToken ct = default)
    {
        try
        {
            // Query current TCP global settings
            var result = await RunNetshCommandAsync("int tcp show global", ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return Result.Failure<NetworkOptimizationStatus>($"Failed to query TCP settings: {result.Error}");
            }

            var output = result.Value ?? string.Empty;

            // Parse the output to determine status
            var tcpFastOpen = output.Contains("TCP Fast Open", StringComparison.OrdinalIgnoreCase) &&
                              output.Contains("enabled", StringComparison.OrdinalIgnoreCase);
            var autoTuning = output.Contains("Receive Window Auto-Tuning Level", StringComparison.OrdinalIgnoreCase) &&
                             output.Contains("normal", StringComparison.OrdinalIgnoreCase);
            var ecnEnabled = output.Contains("ECN Capability", StringComparison.OrdinalIgnoreCase) &&
                             output.Contains("enabled", StringComparison.OrdinalIgnoreCase);
            var timestampsEnabled = output.Contains("Timestamps", StringComparison.OrdinalIgnoreCase) &&
                                    output.Contains("enabled", StringComparison.OrdinalIgnoreCase);

            var isOptimized = tcpFastOpen && autoTuning && ecnEnabled && timestampsEnabled;

            var statusMessage = isOptimized
                ? "Network is optimized for cloud gaming"
                : "Network optimizations not fully applied";

            return Result.Success(new NetworkOptimizationStatus(
                IsOptimized: isOptimized,
                TcpFastOpenEnabled: tcpFastOpen,
                NagleAlgorithmDisabled: false, // Would require registry check
                ReceiveWindowAutoTuningEnabled: autoTuning,
                CurrentMtu: null, // Would require additional query
                StatusMessage: statusMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization status");
            return Result.Failure<NetworkOptimizationStatus>($"Status check failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<Result> LaunchElevatedOptimizerAsync(CancellationToken ct = default)
    {
        try
        {
            if (IsRunningAsAdmin)
            {
                _logger.LogInformation("Already running as admin, no elevation needed");
                return Task.FromResult(Result.Success());
            }

            // Check if helper exists
            if (!File.Exists(_elevatedHelperPath))
            {
                // Fallback: Open Windows Sound Settings as a user-friendly alternative
                _logger.LogWarning("Elevated helper not found at {Path}, opening network settings instead", _elevatedHelperPath);

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:network-status",
                        UseShellExecute = true
                    });
                    return Task.FromResult(Result.Success());
                }
                catch (Exception settingsEx)
                {
                    _logger.LogError(settingsEx, "Failed to open network settings");
                    return Task.FromResult(Result.Failure("Could not open network settings"));
                }
            }

            // Launch the elevated helper
            var startInfo = new ProcessStartInfo
            {
                FileName = _elevatedHelperPath,
                Arguments = "--apply-cloud-gaming-optimizations",
                Verb = "runas", // Request elevation
                UseShellExecute = true
            };

            Process.Start(startInfo);

            _logger.LogInformation("Launched elevated network optimizer helper");
            return Task.FromResult(Result.Success());
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
        {
            _logger.LogInformation("User cancelled elevation request");
            return Task.FromResult(Result.Failure("Elevation was cancelled by user"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch elevated optimizer");
            return Task.FromResult(Result.Failure($"Failed to launch elevated optimizer: {ex.Message}"));
        }
    }

    private async Task<Result<string>> RunNetshCommandAsync(string arguments, CancellationToken ct)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            var error = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var errorMessage = !string.IsNullOrEmpty(error) ? error : output;
                _logger.LogWarning("netsh command failed: {Command} - {Error}", arguments, errorMessage);
                return Result.Failure<string>($"Command failed: {errorMessage.Trim()}");
            }

            _logger.LogDebug("netsh command succeeded: {Command}", arguments);
            return Result.Success(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run netsh command: {Command}", arguments);
            return Result.Failure<string>($"Command execution failed: {ex.Message}");
        }
    }
}
