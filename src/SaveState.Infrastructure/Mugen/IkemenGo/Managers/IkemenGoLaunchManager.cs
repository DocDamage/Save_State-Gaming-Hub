using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.IkemenGo.Managers;

/// <summary>
/// Manages IKEMEN GO process launch, monitoring, and termination.
/// </summary>
public sealed class IkemenGoLaunchManager : IDisposable
{
    private readonly ILogger<IkemenGoLaunchManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<int, Process> _runningProcesses = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IkemenGoLaunchManager"/> class.
    /// </summary>
    public IkemenGoLaunchManager(
        ILogger<IkemenGoLaunchManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Launches IKEMEN GO with specified options.
    /// </summary>
    public async Task<Result<IkemenGoProcess>> LaunchAsync(
        string executablePath,
        IkemenGoLaunchOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Launching IKEMEN GO from {Path}", executablePath);

            if (!File.Exists(executablePath))
            {
                return Result<IkemenGoProcess>.Failure("IKEMEN GO executable not found", ErrorType.NotFound);
            }

            var workingDirectory = Path.GetDirectoryName(executablePath)!;
            var arguments = BuildLaunchArguments(options);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, e) =>
            {
                _runningProcesses.TryRemove(process.Id, out _);
                _logger.LogInformation("IKEMEN GO process {ProcessId} exited", process.Id);
            };

            if (!process.Start())
            {
                return Result<IkemenGoProcess>.Failure("Failed to start IKEMEN GO process", ErrorType.Internal);
            }

            var result = new IkemenGoProcess(
                process.Id,
                executablePath,
                _timeProvider.UtcNow,
                options);

            _runningProcesses[process.Id] = process;

            _logger.LogInformation("IKEMEN GO launched successfully with PID {ProcessId}", process.Id);
            return Result<IkemenGoProcess>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch IKEMEN GO");
            return Result<IkemenGoProcess>.Failure($"Launch failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Launches IKEMEN GO in training mode.
    /// </summary>
    public Task<Result<IkemenGoProcess>> LaunchTrainingModeAsync(
        string executablePath,
        string character1,
        string character2,
        string? stage = null,
        CancellationToken ct = default)
    {
        var options = new IkemenGoLaunchOptions(
            Path.GetDirectoryName(executablePath)!,
            null,
            false,
            true,
            false,
            null,
            new List<string> { character1, character2 },
            stage);

        return LaunchAsync(executablePath, options, ct);
    }

    /// <summary>
    /// Launches IKEMEN GO in online versus mode.
    /// </summary>
    public Task<Result<IkemenGoProcess>> LaunchOnlineVersusAsync(
        string executablePath,
        string connectionString,
        CancellationToken ct = default)
    {
        var options = new IkemenGoLaunchOptions(
            Path.GetDirectoryName(executablePath)!,
            null,
            false,
            false,
            true,
            connectionString,
            null,
            null);

        return LaunchAsync(executablePath, options, ct);
    }

    /// <summary>
    /// Monitors running IKEMEN GO process.
    /// </summary>
    public Task<Result<IkemenGoProcessStatus>> GetProcessStatusAsync(
        int processId,
        CancellationToken ct = default)
    {
        try
        {
            if (_runningProcesses.TryGetValue(processId, out var cachedProcess))
            {
                try
                {
                    cachedProcess.Refresh();

                    var status = new IkemenGoProcessStatus(
                        processId,
                        !cachedProcess.HasExited,
                        cachedProcess.HasExited ? TimeSpan.Zero : cachedProcess.TotalProcessorTime,
                        cachedProcess.HasExited ? 0 : cachedProcess.WorkingSet64,
                        null,
                        null);

                    return Task.FromResult(Result<IkemenGoProcessStatus>.Success(status));
                }
                catch (InvalidOperationException)
                {
                    // Process has exited but still in cache
                    _runningProcesses.TryRemove(processId, out _);
                }
            }

            // Try to find process by ID even if not in our cache
            try
            {
                var process = Process.GetProcessById(processId);
                if (process.HasExited)
                {
                    return Task.FromResult(Result<IkemenGoProcessStatus>.Failure("Process has exited", ErrorType.NotFound));
                }

                var status = new IkemenGoProcessStatus(
                    processId,
                    true,
                    process.TotalProcessorTime,
                    process.WorkingSet64,
                    null,
                    null);

                return Task.FromResult(Result<IkemenGoProcessStatus>.Success(status));
            }
            catch (ArgumentException)
            {
                return Task.FromResult(Result<IkemenGoProcessStatus>.Failure("Process not found", ErrorType.NotFound));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get process status");
            return Task.FromResult(Result<IkemenGoProcessStatus>.Failure($"Status check failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Terminates IKEMEN GO process.
    /// </summary>
    public async Task<Result> TerminateAsync(
        int processId,
        bool force = false,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Terminating IKEMEN GO process {ProcessId} (force={Force})", processId, force);

            // First check our cache
            if (_runningProcesses.TryGetValue(processId, out var cachedProcess))
            {
                try
                {
                    if (!cachedProcess.HasExited)
                    {
                        if (force)
                        {
                            cachedProcess.Kill(true);
                        }
                        else
                        {
                            cachedProcess.CloseMainWindow();
                        }

                        await cachedProcess.WaitForExitAsync(ct).ConfigureAwait(false);
                    }

                    _runningProcesses.TryRemove(processId, out _);
                    return Result.Success();
                }
                catch (InvalidOperationException)
                {
                    // Already exited
                    _runningProcesses.TryRemove(processId, out _);
                    return Result.Success();
                }
            }

            // Try to find and kill process by ID
            try
            {
                var process = Process.GetProcessById(processId);
                if (!process.HasExited)
                {
                    if (force)
                    {
                        process.Kill(true);
                    }
                    else
                    {
                        process.CloseMainWindow();
                    }

                    await process.WaitForExitAsync(ct).ConfigureAwait(false);
                }

                return Result.Success();
            }
            catch (ArgumentException)
            {
                // Process not found - already exited
                return Result.Success();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate process");
            return Result.Failure($"Termination failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets all running IKEMEN GO processes.
    /// </summary>
    public Task<Result<IReadOnlyList<IkemenGoProcess>>> GetRunningProcessesAsync(
        CancellationToken ct = default)
    {
        try
        {
            var processes = _runningProcesses
                .Where(kvp =>
                {
                    try
                    {
                        kvp.Value.Refresh();
                        return !kvp.Value.HasExited;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .Select(kvp => new IkemenGoProcess(
                    kvp.Value.Id,
                    kvp.Value.StartInfo.FileName,
                    kvp.Value.StartTime.ToUniversalTime(),
                    new IkemenGoLaunchOptions(
                        kvp.Value.StartInfo.WorkingDirectory,
                        null,
                        false,
                        false,
                        false,
                        null,
                        null,
                        null)))
                .ToList();

            return Task.FromResult(Result<IReadOnlyList<IkemenGoProcess>>.Success(processes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get running processes");
            return Task.FromResult(Result<IReadOnlyList<IkemenGoProcess>>.Failure($"Failed to get processes: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Waits for a process to exit.
    /// </summary>
    public async Task<Result> WaitForExitAsync(
        int processId,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        try
        {
            if (_runningProcesses.TryGetValue(processId, out var process))
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (timeout.HasValue)
                {
                    cts.CancelAfter(timeout.Value);
                }

                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                return Result.Success();
            }

            // Try to find process outside our cache
            try
            {
                var externalProcess = Process.GetProcessById(processId);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (timeout.HasValue)
                {
                    cts.CancelAfter(timeout.Value);
                }

                await externalProcess.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                return Result.Success();
            }
            catch (ArgumentException)
            {
                return Result.Failure("Process not found", ErrorType.NotFound);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure("Operation cancelled", ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to wait for process exit");
            return Result.Failure($"Wait failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Disposes the manager and terminates all tracked processes.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        foreach (var kvp in _runningProcesses)
        {
            try
            {
                var process = kvp.Value;
                if (!process.HasExited)
                {
                    process.Kill(true);
                    process.WaitForExit(5000);
                }
                process.Dispose();
            }
            catch { /* Ignore cleanup errors */ }
        }

        _runningProcesses.Clear();
        _disposed = true;
    }

    private string BuildLaunchArguments(IkemenGoLaunchOptions options)
    {
        var args = new List<string>();

        if (options.QuickVersus)
            args.Add("-quick");

        if (options.TrainingMode)
            args.Add("-training");

        if (options.OnlineMode && !string.IsNullOrEmpty(options.ConnectionString))
            args.Add($"-online {options.ConnectionString}");

        if (!string.IsNullOrEmpty(options.ConfigPath))
            args.Add($"-config \"{options.ConfigPath}\"");

        if (options.Characters?.Count > 0)
        {
            var charList = string.Join(",", options.Characters);
            args.Add($"-p1 {options.Characters[0]}");
            if (options.Characters.Count > 1)
                args.Add($"-p2 {options.Characters[1]}");
        }

        if (!string.IsNullOrEmpty(options.Stage))
            args.Add($"-stage \"{options.Stage}\"");

        return string.Join(" ", args);
    }
}
