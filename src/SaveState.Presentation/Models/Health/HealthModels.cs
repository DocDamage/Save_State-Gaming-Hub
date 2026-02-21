namespace SaveState.Presentation.Models.Health;

/// <summary>
/// Represents the health status of a system component.
/// </summary>
public enum HealthStatus
{
    /// <summary>Component is operating normally.</summary>
    Healthy,

    /// <summary>Component is operating with reduced functionality or performance.</summary>
    Degraded,

    /// <summary>Component is not functioning correctly.</summary>
    Unhealthy,

    /// <summary>Health status is unknown.</summary>
    Unknown
}

/// <summary>
/// Represents the database health information.
/// </summary>
public class DatabaseHealth
{
    /// <summary>Current health status of the database.</summary>
    public HealthStatus Status { get; set; }

    /// <summary>Database query response time.</summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>Timestamp of the last database backup.</summary>
    public DateTime LastBackup { get; set; }

    /// <summary>Size of the database in bytes.</summary>
    public long DatabaseSize { get; set; }

    /// <summary>Last error message, if any.</summary>
    public string? LastError { get; set; }
}

/// <summary>
/// Represents the health status of an external API.
/// </summary>
public class ApiHealthStatus
{
    /// <summary>Name of the API.</summary>
    public string ApiName { get; set; } = string.Empty;

    /// <summary>Current health status.</summary>
    public HealthStatus Status { get; set; }

    /// <summary>Response time of the last check.</summary>
    public TimeSpan? ResponseTime { get; set; }

    /// <summary>Error message, if the API is unhealthy.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Timestamp of the last health check.</summary>
    public DateTime LastChecked { get; set; }
}

/// <summary>
/// Represents cache statistics.
/// </summary>
public class CacheStatistics
{
    /// <summary>Cache hit rate (0.0 to 1.0).</summary>
    public double HitRate { get; set; }

    /// <summary>Size of the cache in bytes.</summary>
    public long SizeInBytes { get; set; }

    /// <summary>Number of entries in the cache.</summary>
    public int EntryCount { get; set; }

    /// <summary>Number of entries evicted from the cache.</summary>
    public int EvictionCount { get; set; }

    /// <summary>Average time to lookup an entry.</summary>
    public TimeSpan? AverageLookupTime { get; set; }
}

/// <summary>
/// Represents system resource utilization.
/// </summary>
public class SystemResources
{
    /// <summary>CPU usage percentage (0-100).</summary>
    public double CpuPercentage { get; set; }

    /// <summary>Memory usage percentage (0-100).</summary>
    public double MemoryPercentage { get; set; }

    /// <summary>GPU usage percentage (0-100).</summary>
    public double GpuPercentage { get; set; }

    /// <summary>Disk usage percentage (0-100).</summary>
    public double DiskPercentage { get; set; }

    /// <summary>Available memory in bytes.</summary>
    public long AvailableMemoryBytes { get; set; }

    /// <summary>Total system memory in bytes.</summary>
    public long TotalMemoryBytes { get; set; }
}

/// <summary>
/// Represents an error log entry.
/// </summary>
public class ErrorLogEntry
{
    /// <summary>Timestamp when the error occurred.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Component that generated the error.</summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>Error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Stack trace, if available.</summary>
    public string? StackTrace { get; set; }

    /// <summary>Severity of the error.</summary>
    public ErrorSeverity Severity { get; set; }
}

/// <summary>
/// Represents the severity level of an error.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Warning that doesn't prevent functionality.</summary>
    Warning,

    /// <summary>Error that affects functionality.</summary>
    Error,

    /// <summary>Critical error that prevents system operation.</summary>
    Critical
}

/// <summary>
/// Represents the overall health summary of the system.
/// </summary>
public class OverallHealthSummary
{
    /// <summary>Overall health status of the system.</summary>
    public HealthStatus OverallStatus { get; set; }

    /// <summary>Number of healthy services.</summary>
    public int HealthyServices { get; set; }

    /// <summary>Number of degraded services.</summary>
    public int DegradedServices { get; set; }

    /// <summary>Number of unhealthy services.</summary>
    public int UnhealthyServices { get; set; }

    /// <summary>Timestamp of the last health update.</summary>
    public DateTime LastUpdated { get; set; }
}
