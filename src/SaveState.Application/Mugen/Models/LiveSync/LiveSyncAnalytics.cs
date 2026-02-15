namespace SaveState.Application.Mugen.Models.LiveSync;

/// <summary>
/// Direction for synchronization operations.
/// </summary>
public enum SyncDirection
{
    Upload,
    Download,
    Bidirectional
}

/// <summary>
/// Represents the health status of synchronization for an account.
/// </summary>
public class SyncHealth
{
    /// <summary>
    /// Overall health status.
    /// </summary>
    public SyncHealthStatus Status { get; set; }

    /// <summary>
    /// Health score from 0.0 to 1.0.
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Last successful sync timestamp.
    /// </summary>
    public DateTime LastSuccessfulSync { get; set; }

    /// <summary>
    /// Number of consecutive sync failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Platform-specific health metrics.
    /// </summary>
    public Dictionary<PlatformType, float> PlatformHealth { get; set; } = new();

    /// <summary>
    /// Issues detected during health check.
    /// </summary>
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Health status levels for sync operations.
/// </summary>
public enum SyncHealthStatus
{
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

/// <summary>
/// Represents the completeness of account data across platforms.
/// </summary>
public class DataCompleteness
{
    /// <summary>
    /// Overall completeness score from 0.0 to 1.0.
    /// </summary>
    public float OverallCompleteness { get; set; }

    /// <summary>
    /// Completeness score for each data category.
    /// </summary>
    public Dictionary<string, float> Categories { get; set; } = new();

    /// <summary>
    /// Missing data fields detected.
    /// </summary>
    public List<string> MissingFields { get; set; } = new();

    /// <summary>
    /// Platforms with incomplete data.
    /// </summary>
    public List<PlatformType> IncompletePlatforms { get; set; } = new();
}
