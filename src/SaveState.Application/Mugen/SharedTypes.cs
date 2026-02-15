using System;

namespace SaveState.Application.Mugen;

/// <summary>
/// Shared types used across multiple MUGEN services.
/// </summary>
// Common enums that are used across services
public enum TrendDirection
{
    Increasing,
    Decreasing,
    Stable,
    Volatile
}

public enum ReportType
{
    Summary,
    Detailed,
    Performance,
    Analytics
}

public enum WidgetType
{
    Chart,
    Table,
    Metric,
    Timeline
}

public enum ExportFormat
{
    PDF,
    CSV,
    JSON,
    XML
}

// Additional shared enums
public enum PerformanceAnalysis
{
    Basic,
    Advanced,
    Comprehensive
}

// Common records that are used across services
public record PerformanceMetrics(
    double AverageScore,
    TimeSpan AverageTime,
    int TotalAttempts,
    double ImprovementRate);

public record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public record ConfigurationSettings(
    bool Enabled,
    int MaxItems,
    TimeSpan Timeout,
    IReadOnlyDictionary<string, object> Parameters);

// Mathematical types
public record Vector2(double X, double Y);
public record Vector3(double X, double Y, double Z)
{
    public static Vector3 operator *(Vector3 v, double s) => new Vector3(v.X * s, v.Y * s, v.Z * s);
    public static Vector3 operator *(double s, Vector3 v) => v * s;
    public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>
    /// Calculates the length (magnitude) of the vector.
    /// </summary>
    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Returns a normalized copy of this vector.
    /// </summary>
    public Vector3 Normalize()
    {
        var length = Length();
        if (length == 0)
            return new Vector3(0, 0, 0);
        return new Vector3(X / length, Y / length, Z / length);
    }

    /// <summary>
    /// Implicit conversion from System.Numerics.Vector3.
    /// </summary>
    public static implicit operator Vector3(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);

    /// <summary>
    /// Implicit conversion to System.Numerics.Vector3.
    /// </summary>
    public static implicit operator System.Numerics.Vector3(Vector3 v) => new System.Numerics.Vector3((float)v.X, (float)v.Y, (float)v.Z);
}
// Emotional resonance types
public enum EmotionalTrigger
{
    Victory,
    Defeat,
    TimePressure,
    SkillExecution,
    ComboChain,
    CharacterSwitch,
    RoundEnd
}

public record EmotionalState(
    EmotionalTrigger Trigger,
    double Intensity,
    TimeSpan Duration,
    IReadOnlyDictionary<string, double> Metrics);

// Combat session types
public record CombatSessionRequest(
    string PlayerId,
    string OpponentId,
    string StageId,
    IReadOnlyList<string> EnabledFeatures);

public record CombatSession(
    string SessionId,
    string PlayerId,
    string OpponentId,
    DateTime StartTime,
    CombatSessionRequest Request,
    IReadOnlyDictionary<string, object> Metrics);

// Procedural content generation types
public record StageDimensions(
    Vector2 Size,
    Vector2 CameraBounds,
    IReadOnlyList<Vector2> SpawnPoints);

// Symbiotic partner types
public record SynergyEffect(
    string EffectId,
    string Description,
    double Multiplier,
    TimeSpan Duration);

// Educational types
public record LessonProgressUpdate(
    string LessonId,
    string StudentId,
    double ProgressPercentage,
    bool Completed,
    DateTime Timestamp);

// Performance optimization types
public record OptimizationPerformanceAnalysis(
    string SessionId,
    OptimizationPerformanceMetrics Metrics,
    IReadOnlyList<PerformanceBottleneck> Bottlenecks,
    IReadOnlyList<OptimizationSuggestion> OptimizationSuggestions,
    DateTime AnalysisTimestamp,
    float OverallHealthScore);

public record OptimizationPerformanceMetrics(
    string SessionId,
    float AverageResponseTime,
    float PeakMemoryUsage,
    float CacheHitRate,
    float CpuUtilization,
    float NetworkLatency,
    int TotalRequests,
    float ErrorRate,
    DateTime CreatedAt);

public record PerformanceBottleneck(
    string Type,
    float Severity,
    string Description);

public record OptimizationSuggestion(
    string SuggestionId,
    string Type,
    string Description,
    float ExpectedImprovement,
    int Priority);

public record OptimizationResult(
    string SessionId,
    int OptimizationsApplied,
    int SuccessfulOptimizations,
    float PerformanceImprovement,
    DateTime AppliedAt);

public record AppliedOptimization(
    string SuggestionId,
    bool Success,
    float ImprovementAchieved,
    DateTime AppliedAt);

public record CacheOptimization(
    string SessionId,
    int CacheHits,
    int CacheMisses,
    float HitRate,
    int OptimizationsApplied,
    float MemorySaved,
    DateTime OptimizedAt);

public record CacheAnalysis(
    int CacheHits,
    int CacheMisses,
    float HitRate,
    int TotalRequests,
    float MemoryUsage);

public record CacheOptimizationStrategy(
    string StrategyId,
    string Type,
    float ExpectedMemoryIncrease,
    float ExpectedHitRateImprovement);

public record AppliedCacheOptimization(
    string StrategyId,
    float HitRateImprovement,
    float MemoryIncrease,
    DateTime AppliedAt);

public record BatchingOptimization(
    string SessionId,
    int OperationsBatched,
    int NetworkCallsReduced,
    float LatencyReduction,
    DateTime OptimizedAt);

public record BatchingAnalysis(
    int BatchableOperations,
    int CurrentBatchSize,
    int OptimalBatchSize,
    int NetworkCallsCurrent,
    int NetworkCallsOptimized);

public record BatchingStrategy(
    string StrategyId,
    int BatchSize,
    float ExpectedNetworkReduction,
    float ExpectedLatencyReduction);

public record BatchingResult(
    string StrategyId,
    int BatchSize,
    int NetworkCallsSaved,
    float LatencyReduction,
    DateTime AppliedAt);

public record MemoryOptimization(
    string SessionId,
    float InitialMemoryUsage,
    float OptimizedMemoryUsage,
    float MemoryReduction,
    int GarbageCollectionsReduced,
    DateTime OptimizedAt);

public record MemoryAnalysis(
    float CurrentUsage,
    float PeakUsage,
    int GcCyclesPerMinute,
    int ObjectCount,
    int LargeObjects);

public record MemoryStrategy(
    string StrategyId,
    string Type,
    float ExpectedMemorySavings,
    int ExpectedGcReduction);

public record MemoryOptimizationResult(
    string StrategyId,
    float MemorySaved,
    int GcCyclesSaved,
    DateTime AppliedAt);

public record LoadBalancingResult(
    string SessionId,
    float LoadVarianceBefore,
    float LoadVarianceAfter,
    int ThreadsOptimized,
    bool CpuUtilizationBalanced,
    DateTime BalancedAt);

public record LoadAnalysis(
    int ThreadCount,
    float[] CpuUtilizationPerThread,
    float LoadVariance,
    int[] BottleneckThreads);

public record LoadBalancingStrategy(
    string StrategyId,
    string Type,
    int[] TargetThreads,
    float ExpectedVarianceReduction);

public record LoadBalancingResultItem(
    string StrategyId,
    int ThreadsBalanced,
    bool CpuBalanced,
    float VarianceReduction,
    DateTime AppliedAt);

public record PerformanceReport(
    string SessionId,
    OptimizationPerformanceAnalysis Analysis,
    CacheOptimization CacheOptimization,
    BatchingOptimization BatchingOptimization,
    MemoryOptimization MemoryOptimization,
    LoadBalancingResult LoadBalancing,
    float OverallScore,
    DateTime ReportGeneratedAt);

public record PerformanceEvent(
    string SessionId,
    string EventType,
    float Duration,
    float MemoryUsage,
    DateTime Timestamp);

public record PerformanceThresholds(
    float MaxResponseTime,
    float MaxMemoryUsage,
    float MinCacheHitRate,
    float MaxCpuUtilization,
    float MaxNetworkLatency);

public record OptimizationStrategies(
    string[] CacheStrategies,
    string[] BatchingStrategies,
    string[] MemoryStrategies,
    string[] LoadBalancingStrategies);

public record OptimizationProfile(
    string SessionId,
    PerformanceThresholds Thresholds,
    OptimizationStrategies Strategies,
    DateTime CreatedAt,
    DateTime LastUpdated);
