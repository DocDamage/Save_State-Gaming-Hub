namespace SaveState.Core.Common.Interfaces;

public interface IProcessLauncher
{
    Task<ProcessInfo> LaunchAsync(LaunchConfiguration config, CancellationToken ct = default);
}

public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public string? ExecutablePath { get; set; }
    public IReadOnlyList<string> Arguments { get; set; } = Array.Empty<string>();
}

public class LaunchConfiguration
{
    public string ExecutablePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public bool WaitForExit { get; set; } = false;
    public TimeSpan? Timeout { get; set; }
}
