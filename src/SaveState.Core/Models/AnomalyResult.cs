namespace SaveState.Core.Models;

/// <summary>
/// Result of MBAD anomaly analysis
/// </summary>
public record AnomalyResult
{
    /// <summary>
    /// Whether an anomaly was detected
    /// </summary>
    public bool IsAnomaly { get; init; }

    /// <summary>
    /// Confidence score (0-1) of the detection
    /// </summary>
    public double ConfidenceScore { get; init; }

    /// <summary>
    /// Type of anomaly detected
    /// </summary>
    public string? AnomalyType { get; init; }

    /// <summary>
    /// Human-readable description of the anomaly
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Feature contributions to the anomaly score
    /// </summary>
    public Dictionary<string, double> FeatureContributions { get; init; } = new();

    /// <summary>
    /// Addresses involved in the anomaly
    /// </summary>
    public List<long> SuspiciousAddresses { get; init; } = new();

    /// <summary>
    /// No anomaly result
    /// </summary>
    public static AnomalyResult None => new() { IsAnomaly = false, ConfidenceScore = 0 };
}

/// <summary>
/// Types of anomalies that can be detected
/// </summary>
public static class AnomalyTypes
{
    public const string RapidValueChange = "RapidValueChange";
    public const string ImpossibleValue = "ImpossibleValue";
    public const string ExternalWrite = "ExternalWrite";
    public const string PatternMatch = "PatternMatch";
    public const string StatisticalOutlier = "StatisticalOutlier";
}
