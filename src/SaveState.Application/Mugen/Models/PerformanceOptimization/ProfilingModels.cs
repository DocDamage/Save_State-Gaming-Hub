namespace SaveState.Application.Mugen.Models.PerformanceOptimization;

// Type aliases to existing types in SharedTypes.cs
using OptimizationPerformanceMetrics = SaveState.Application.Mugen.OptimizationPerformanceMetrics;
using PerformanceBottleneck = SaveState.Application.Mugen.PerformanceBottleneck;
using OptimizationSuggestion = SaveState.Application.Mugen.OptimizationSuggestion;

/// <summary>
/// Complete performance analysis result.
/// </summary>
public record OptimizationPerformanceAnalysis(
    string SessionId,
    OptimizationPerformanceMetrics Metrics,
    IReadOnlyList<PerformanceBottleneck> Bottlenecks,
    IReadOnlyList<OptimizationSuggestion> Suggestions,
    DateTime AnalysisTime,
    float OverallHealthScore
);

/// <summary>
/// Performance optimization profile for a session.
/// </summary>
public record OptimizationProfile(
    string SessionId,
    DateTime CreatedAt,
    DateTime LastOptimized,
    IReadOnlyList<string> AppliedStrategies,
    float BaselineScore
);

/// <summary>
/// Result of a profiling operation.
/// </summary>
public record ProfileResult(
    string ProfileId,
    string SessionId,
    DateTime StartTime,
    DateTime EndTime,
    TimeSpan Duration,
    IReadOnlyDictionary<string, float> Measurements,
    bool IsComplete
);

/// <summary>
/// A performance event for tracking.
/// </summary>
public record PerformanceEvent(
    string SessionId,
    string EventType,
    float Duration,
    float MemoryUsage,
    DateTime Timestamp
);
