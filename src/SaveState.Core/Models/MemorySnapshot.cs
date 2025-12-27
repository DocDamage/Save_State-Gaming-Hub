namespace SaveState.Core.Models;

/// <summary>
/// Snapshot of memory state at a point in time for MBAD analysis
/// </summary>
public record MemorySnapshot
{
    /// <summary>
    /// When this snapshot was taken
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Values at watched memory addresses
    /// </summary>
    public Dictionary<long, int> WatchedAddresses { get; init; } = new();

    /// <summary>
    /// Number of memory writes since last snapshot
    /// </summary>
    public int WriteCount { get; init; }

    /// <summary>
    /// Number of memory reads since last snapshot
    /// </summary>
    public int ReadCount { get; init; }

    /// <summary>
    /// CPU usage percentage of the process
    /// </summary>
    public double CpuUsage { get; init; }

    /// <summary>
    /// List of active modules in the process
    /// </summary>
    public List<string> ActiveModules { get; init; } = new();

    /// <summary>
    /// Time delta from previous snapshot in milliseconds
    /// </summary>
    public double DeltaMs { get; init; }
}
