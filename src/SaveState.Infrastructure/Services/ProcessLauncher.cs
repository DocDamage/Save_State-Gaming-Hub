using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Services;

public class ProcessLauncher : IProcessLauncher
{
    private readonly ITimeProvider _timeProvider;

    public ProcessLauncher(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ProcessInfo> LaunchAsync(LaunchConfiguration config, CancellationToken ct = default)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = config.ExecutablePath,
            Arguments = config.Arguments ?? string.Empty,
            WorkingDirectory = config.WorkingDirectory ?? Path.GetDirectoryName(config.ExecutablePath),
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        var process = System.Diagnostics.Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start process");
        }

        var processInfo = new ProcessInfo
        {
            ProcessId = process.Id,
            ProcessName = process.ProcessName,
            StartedAt = _timeProvider.UtcNow,
            ExecutablePath = config.ExecutablePath,
            Arguments = string.IsNullOrEmpty(config.Arguments) ? Array.Empty<string>() : config.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        };

        // If we need to wait for exit, do it asynchronously
        if (config.WaitForExit)
        {
            using var timeoutCts = config.Timeout.HasValue ? new CancellationTokenSource(config.Timeout.Value) : null;
            using var linkedCts = timeoutCts is not null ? CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token) : null;

            try
            {
                await process.WaitForExitAsync(linkedCts?.Token ?? ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
            {
                process.Kill();
                throw new TimeoutException($"Process timed out after {config.Timeout.Value.TotalSeconds} seconds");
            }
        }

        return processInfo;
    }
}
