using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for profiling and optimizing MUGEN character and application performance.
/// Provides real-time monitoring, bottleneck detection, and optimization recommendations.
/// </summary>
public interface IPerformanceProfilerService
{
    #region Session Management

    /// <summary>
    /// Starts a new profiling session.
    /// </summary>
    Task<Result<ProfilingSession>> StartSessionAsync(
        string name,
        ProfilingConfiguration configuration,
        CancellationToken ct = default);

    /// <summary>
    /// Stops the current profiling session.
    /// </summary>
    Task<Result<ProfilingReport>> StopSessionAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current active session.
    /// </summary>
    Task<Result<ProfilingSession>> GetActiveSessionAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Pauses profiling.
    /// </summary>
    Task<Result> PauseProfilingAsync(CancellationToken ct = default);

    /// <summary>
    /// Resumes profiling.
    /// </summary>
    Task<Result> ResumeProfilingAsync(CancellationToken ct = default);

    #endregion

    #region Real-time Monitoring

    /// <summary>
    /// Gets current performance metrics.
    /// </summary>
    Task<Result<PerfMetrics>> GetCurrentMetricsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets frame rate statistics.
    /// </summary>
    Task<Result<FrameRateStats>> GetFrameRateStatsAsync(
        TimeSpan? window = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets memory usage statistics.
    /// </summary>
    Task<Result<MemoryStats>> GetMemoryStatsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets CPU usage statistics.
    /// </summary>
    Task<Result<CpuStats>> GetCpuStatsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets GPU usage statistics.
    /// </summary>
    Task<Result<GpuStats>> GetGpuStatsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets loading time metrics.
    /// </summary>
    Task<Result<LoadingMetrics>> GetLoadingMetricsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Subscribes to real-time performance updates.
    /// </summary>
    IAsyncEnumerable<PerformanceSnapshot> SubscribeToMetricsAsync(
        MetricsSubscriptionOptions options,
        CancellationToken ct = default);

    #endregion

    #region Character Profiling

    /// <summary>
    /// Profiles a character's performance.
    /// </summary>
    Task<Result<CharacterProfileResult>> ProfileCharacterAsync(
        Guid characterId,
        CharacterProfilingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Profiles character loading performance.
    /// </summary>
    Task<Result<LoadingProfile>> ProfileCharacterLoadingAsync(
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Profiles character animation performance.
    /// </summary>
    Task<Result<AnimationProfile>> ProfileAnimationsAsync(
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Profiles AI performance.
    /// </summary>
    Task<Result<AiProfile>> ProfileAiPerformanceAsync(
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets character resource usage.
    /// </summary>
    Task<Result<ResourceUsage>> GetCharacterResourceUsageAsync(
        Guid characterId,
        CancellationToken ct = default);

    #endregion

    #region Battle Profiling

    /// <summary>
    /// Starts battle performance profiling.
    /// </summary>
    Task<Result> StartBattleProfilingAsync(
        BattleProfilingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets battle performance analysis.
    /// </summary>
    Task<Result<BattlePerformanceAnalysis>> GetBattleAnalysisAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets frame time breakdown during battle.
    /// </summary>
    Task<Result<FrameTimeBreakdown>> GetFrameTimeBreakdownAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Detects performance spikes.
    /// </summary>
    Task<Result<IReadOnlyList<PerformanceSpike>>> DetectSpikesAsync(
        SpikeDetectionOptions options,
        CancellationToken ct = default);

    #endregion

    #region Bottleneck Analysis

    /// <summary>
    /// Analyzes performance bottlenecks.
    /// </summary>
    Task<Result<BottleneckAnalysis>> AnalyzeBottlenecksAsync(
        BottleneckAnalysisOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Identifies memory leaks.
    /// </summary>
    Task<Result<MemoryLeakReport>> DetectMemoryLeaksAsync(
        MemoryLeakDetectionOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes thread performance.
    /// </summary>
    Task<Result<ThreadAnalysis>> AnalyzeThreadsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets rendering pipeline analysis.
    /// </summary>
    Task<Result<RenderingAnalysis>> AnalyzeRenderingAsync(
        CancellationToken ct = default);

    #endregion

    #region Optimization

    /// <summary>
    /// Generates optimization recommendations.
    /// </summary>
    Task<Result<IReadOnlyList<OptimizationRecommendation>>> GetOptimizationSuggestionsAsync(
        OptimizationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Simulates optimization impact.
    /// </summary>
    Task<Result<OptimizationImpact>> SimulateOptimizationAsync(
        OptimizationRecommendation recommendation,
        CancellationToken ct = default);

    /// <summary>
    /// Applies automatic optimizations.
    /// </summary>
    Task<Result<AutoOptimizationResult>> ApplyAutoOptimizationsAsync(
        AutoOptimizationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Optimizes character assets.
    /// </summary>
    Task<Result<AssetOptimizationResult>> OptimizeAssetsAsync(
        Guid characterId,
        AssetOptimizationOptions options,
        CancellationToken ct = default);

    #endregion

    #region Benchmarking

    /// <summary>
    /// Runs performance benchmark.
    /// </summary>
    Task<Result<BenchmarkResult>> RunBenchmarkAsync(
        BenchmarkConfiguration configuration,
        CancellationToken ct = default);

    /// <summary>
    /// Compares benchmark results.
    /// </summary>
    Task<Result<BenchmarkComparison>> CompareBenchmarksAsync(
        IReadOnlyList<string> benchmarkIds,
        CancellationToken ct = default);

    /// <summary>
    /// Gets baseline performance metrics.
    /// </summary>
    Task<Result<PerformanceBaseline>> GetBaselineAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Sets performance baseline.
    /// </summary>
    Task<Result> SetBaselineAsync(
        string description,
        CancellationToken ct = default);

    #endregion

    #region Reporting

    /// <summary>
    /// Generates performance report.
    /// </summary>
    Task<Result<PerfReport>> GenerateReportAsync(
        ReportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Exports profiling data.
    /// </summary>
    Task<Result<string>> ExportProfilingDataAsync(
        string sessionId,
        ExportFormat format,
        CancellationToken ct = default);

    /// <summary>
    /// Gets performance history.
    /// </summary>
    Task<Result<IReadOnlyList<HistoricalMetrics>>> GetPerformanceHistoryAsync(
        TimeSpan period,
        CancellationToken ct = default);

    #endregion

    #region Alerts and Thresholds

    /// <summary>
    /// Sets performance threshold.
    /// </summary>
    Task<Result> SetThresholdAsync(
        PerformanceThreshold threshold,
        CancellationToken ct = default);

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    Task<Result<IReadOnlyList<PerformanceAlert>>> GetActiveAlertsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Acknowledges alert.
    /// </summary>
    Task<Result> AcknowledgeAlertAsync(
        string alertId,
        CancellationToken ct = default);

    #endregion
}

#region Request/Response Models

/// <summary>
/// Profiling session.
/// </summary>
public record ProfilingSession(
    string Id,
    string Name,
    DateTime StartedAt,
    ProfilingConfiguration Configuration,
    ProfilingStatus Status,
    TimeSpan Duration,
    IReadOnlyList<string> Tags);

/// <summary>
/// Profiling status.
/// </summary>
public enum ProfilingStatus
{
    Running,
    Paused,
    Stopped,
    Error
}

/// <summary>
/// Profiling configuration.
/// </summary>
public record ProfilingConfiguration(
    bool TrackCpu,
    bool TrackMemory,
    bool TrackGpu,
    bool TrackFrameRate,
    bool TrackLoading,
    int SamplingRateMs,
    IReadOnlyList<string> CustomCounters);

/// <summary>
/// Profiling report.
/// </summary>
public record ProfilingReport(
    string SessionId,
    DateTime GeneratedAt,
    TimeSpan Duration,
    PerformanceSummary Summary,
    IReadOnlyList<PerfIssue> Issues,
    IReadOnlyList<OptimizationRecommendation> Recommendations);

/// <summary>
/// Performance summary.
/// </summary>
public record PerformanceSummary(
    double AverageFps,
    double MinFps,
    double MaxFps,
    double AverageFrameTime,
    long PeakMemoryUsage,
    double AverageCpuUsage,
    int TotalFrames,
    int DroppedFrames);

/// <summary>
/// Performance issue.
/// </summary>
public record PerfIssue(
    PerfIssueSeverity Severity,
    string Category,
    string Description,
    TimeSpan Timestamp,
    IReadOnlyList<string> Context);

/// <summary>
/// Issue severity.
/// </summary>
public enum PerfIssueSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Performance metrics.
/// </summary>
public record PerfMetrics(
    double CurrentFps,
    double CurrentFrameTime,
    long MemoryUsage,
    double CpuUsage,
    double GpuUsage,
    int ActiveThreads,
    DateTime Timestamp);

/// <summary>
/// Frame rate statistics.
/// </summary>
public record FrameRateStats(
    double Average,
    double Minimum,
    double Maximum,
    double Percentile1,
    double Percentile5,
    double Percentile95,
    double Percentile99,
    int TotalFrames,
    int DroppedFrames,
    double DropPercentage);

/// <summary>
/// Memory statistics.
/// </summary>
public record MemoryStats(
    long CurrentUsage,
    long PeakUsage,
    long ManagedHeap,
    long UnmanagedMemory,
    long GcHeap,
    int GcCollectionsGen0,
    int GcCollectionsGen1,
    int GcCollectionsGen2);

/// <summary>
/// CPU statistics.
/// </summary>
public record CpuStats(
    double TotalUsage,
    double UserTime,
    double SystemTime,
    int ThreadCount,
    double AverageThreadUsage,
    IReadOnlyList<CoreUsage> PerCoreUsage);

/// <summary>
/// Core usage.
/// </summary>
public record CoreUsage(int CoreId, double Usage);

/// <summary>
/// GPU statistics.
/// </summary>
public record GpuStats(
    double Usage,
    long MemoryUsage,
    long MemoryTotal,
    double Temperature,
    int FrameBufferCount);

/// <summary>
/// Loading metrics.
/// </summary>
public record LoadingMetrics(
    TimeSpan TotalLoadTime,
    IReadOnlyList<LoadingPhase> Phases);

/// <summary>
/// Loading phase.
/// </summary>
public record LoadingPhase(
    string Name,
    TimeSpan Duration,
    long MemoryDelta);

/// <summary>
/// Metrics subscription options.
/// </summary>
public record MetricsSubscriptionOptions(
    int UpdateIntervalMs,
    bool IncludeCpu,
    bool IncludeMemory,
    bool IncludeGpu,
    bool IncludeFrameRate);

/// <summary>
/// Performance snapshot.
/// </summary>
public record PerformanceSnapshot(
    DateTime Timestamp,
    PerfMetrics Metrics,
    IReadOnlyList<string> ActiveEvents);

/// <summary>
/// Character profiling options.
/// </summary>
public record CharacterProfilingOptions(
    bool ProfileLoading,
    bool ProfileAnimations,
    bool ProfileAi,
    bool ProfileRendering,
    int TestDurationSeconds);

/// <summary>
/// Character profile result.
/// </summary>
public record CharacterProfileResult(
    Guid CharacterId,
    TimeSpan Duration,
    CharacterPerformanceMetrics Metrics,
    IReadOnlyList<CharacterBottleneck> Bottlenecks);

/// <summary>
/// Character performance metrics.
/// </summary>
public record CharacterPerformanceMetrics(
    long MemoryUsage,
    int SpriteCount,
    int AnimationCount,
    int SoundCount,
    double AverageLoadTime,
    double AverageFrameTime);

/// <summary>
/// Character bottleneck.
/// </summary>
public record CharacterBottleneck(
    BottleneckType Type,
    string Description,
    double Impact,
    string? SuggestedFix);

/// <summary>
/// Bottleneck type.
/// </summary>
public enum BottleneckType
{
    Memory,
    Cpu,
    Gpu,
    Disk,
    Network
}

/// <summary>
/// Loading profile.
/// </summary>
public record LoadingProfile(
    TimeSpan TotalTime,
    IReadOnlyList<LoadingPhaseDetail> Phases);

/// <summary>
/// Loading phase detail.
/// </summary>
public record LoadingPhaseDetail(
    string Name,
    TimeSpan StartTime,
    TimeSpan Duration,
    long MemoryBefore,
    long MemoryAfter);

/// <summary>
/// Animation profile.
/// </summary>
public record AnimationProfile(
    int TotalAnimations,
    int TotalFrames,
    double AverageFrameTime,
    IReadOnlyList<AnimationPerformance> AnimationPerformances);

/// <summary>
/// Animation performance.
/// </summary>
public record AnimationPerformance(
    int ActionNumber,
    string Name,
    int FrameCount,
    double AverageFrameTime,
    int MemoryUsage);

/// <summary>
/// AI profile.
/// </summary>
public record AiProfile(
    double AverageDecisionTime,
    double MaxDecisionTime,
    int DecisionsPerSecond,
    IReadOnlyList<string> SlowestStates);

/// <summary>
/// Resource usage.
/// </summary>
public record ResourceUsage(
    long SffSize,
    long AirSize,
    long SoundSize,
    int PaletteCount,
    int SpriteCount,
    long TotalSize);

/// <summary>
/// Battle profiling options.
/// </summary>
public record BattleProfilingOptions(
    bool ProfileFrameTimes,
    bool ProfileMemory,
    bool ProfileAi,
    bool ProfileRendering,
    int TargetFrameRate);

/// <summary>
/// Battle performance analysis.
/// </summary>
public record BattlePerformanceAnalysis(
    TimeSpan Duration,
    double AverageFps,
    int TotalFrames,
    int SlowFrames,
    IReadOnlyList<BattlePhase> Phases);

/// <summary>
/// Battle phase.
/// </summary>
public record BattlePhase(
    string Name,
    TimeSpan StartTime,
    TimeSpan Duration,
    double AverageFps,
    int SlowFrameCount);

/// <summary>
/// Frame time breakdown.
/// </summary>
public record FrameTimeBreakdown(
    double Total,
    double UpdateTime,
    double RenderTime,
    double AiTime,
    double PhysicsTime,
    double AudioTime);

/// <summary>
/// Performance spike.
/// </summary>
public record PerformanceSpike(
    DateTime Timestamp,
    double FrameTime,
    double NormalFrameTime,
    SpikeType Type,
    string? Cause);

/// <summary>
/// Spike type.
/// </summary>
public enum SpikeType
{
    FrameTime,
    Memory,
    Cpu
}

/// <summary>
/// Spike detection options.
/// </summary>
public record SpikeDetectionOptions(
    double FrameTimeThreshold,
    double MemoryThreshold,
    TimeSpan MinDuration);

/// <summary>
/// Bottleneck analysis options.
/// </summary>
public record BottleneckAnalysisOptions(
    TimeSpan AnalysisWindow,
    bool IncludeCpu,
    bool IncludeMemory,
    bool IncludeGpu,
    bool IncludeIo);

/// <summary>
/// Bottleneck analysis.
/// </summary>
public record BottleneckAnalysis(
    BottleneckType PrimaryBottleneck,
    double Severity,
    IReadOnlyList<BottleneckDetails> Details);

/// <summary>
/// Bottleneck details.
/// </summary>
public record BottleneckDetails(
    BottleneckType Type,
    double Impact,
    string Description,
    IReadOnlyList<string> ContributingFactors);

/// <summary>
/// Memory leak report.
/// </summary>
public record MemoryLeakReport(
    bool LeakDetected,
    long LeakedBytes,
    IReadOnlyList<LeakSuspect> Suspects);

/// <summary>
/// Leak suspect.
/// </summary>
public record LeakSuspect(
    string TypeName,
    int InstanceCount,
    long TotalSize);

/// <summary>
/// Memory leak detection options.
/// </summary>
public record MemoryLeakDetectionOptions(
    TimeSpan MonitoringDuration,
    long MinLeakThreshold);

/// <summary>
/// Thread analysis.
/// </summary>
public record ThreadAnalysis(
    int TotalThreads,
    int ActiveThreads,
    int BlockedThreads,
    IReadOnlyList<ThreadDetails> ThreadDetails);

/// <summary>
/// Thread details.
/// </summary>
public record ThreadDetails(
    int ThreadId,
    string Name,
    ProfilerThreadState State,
    double CpuUsage,
    TimeSpan ExecutionTime);

/// <summary>
/// Thread state.
/// </summary>
public enum ProfilerThreadState
{
    Running,
    Waiting,
    Blocked,
    Sleeping
}

/// <summary>
/// Rendering analysis.
/// </summary>
public record RenderingAnalysis(
    double AverageRenderTime,
    int DrawCalls,
    int Vertices,
    int TextureSwitches,
    IReadOnlyList<RenderPass> RenderPasses);

/// <summary>
/// Render pass.
/// </summary>
public record RenderPass(
    string Name,
    double Duration,
    int DrawCalls);

/// <summary>
/// Optimization recommendation.
/// </summary>
public record OptimizationRecommendation(
    string Id,
    OptimizationCategory Category,
    string Title,
    string Description,
    double ExpectedImprovement,
    OptimizationDifficultyLevel Difficulty,
    IReadOnlyList<string> Steps);

/// <summary>
/// Optimization category.
/// </summary>
public enum OptimizationCategory
{
    Memory,
    Cpu,
    Gpu,
    Loading,
    Assets,
    Code
}

/// <summary>
/// Difficulty level.
/// </summary>
public enum OptimizationDifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Expert
}

/// <summary>
/// Optimization options.
/// </summary>
public record OptimizationOptions(
    bool IncludeMemory,
    bool IncludeCpu,
    bool IncludeGpu,
    bool IncludeLoading,
    OptimizationDifficultyLevel MaxDifficulty);

/// <summary>
/// Optimization impact.
/// </summary>
public record OptimizationImpact(
    string RecommendationId,
    double EstimatedFpsGain,
    long EstimatedMemorySaving,
    TimeSpan EstimatedTimeToImplement);

/// <summary>
/// Auto optimization options.
/// </summary>
public record AutoOptimizationOptions(
    bool ApplySafeOptimizations,
    bool ApplyExperimentalOptimizations,
    double MaxRiskLevel);

/// <summary>
/// Auto optimization result.
/// </summary>
public record AutoOptimizationResult(
    int OptimizationsApplied,
    double FpsImprovement,
    long MemorySaved,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Asset optimization options.
/// </summary>
public record AssetOptimizationOptions(
    bool OptimizeSprites,
    bool OptimizeSounds,
    bool OptimizePalettes,
    int QualityLevel);

/// <summary>
/// Asset optimization result.
/// </summary>
public record AssetOptimizationResult(
    long BytesSaved,
    int SpritesOptimized,
    int SoundsOptimized,
    double QualityImpact);

/// <summary>
/// Benchmark configuration.
/// </summary>
public record BenchmarkConfiguration(
    string Name,
    string Description,
    int DurationSeconds,
    IReadOnlyList<string> Scenarios);

/// <summary>
/// Benchmark result.
/// </summary>
public record BenchmarkResult(
    string Id,
    string Name,
    DateTime RunAt,
    BenchmarkMetrics Metrics);

/// <summary>
/// Benchmark metrics.
/// </summary>
public record BenchmarkMetrics(
    double AverageFps,
    double MinFps,
    double MaxFps,
    long PeakMemory,
    double AverageCpuUsage,
    TimeSpan TotalTime);

/// <summary>
/// Benchmark comparison.
/// </summary>
public record BenchmarkComparison(
    IReadOnlyList<BenchmarkResult> Benchmarks,
    BenchmarkResult Baseline,
    IReadOnlyList<MetricComparison> Comparisons);

/// <summary>
/// Metric comparison.
/// </summary>
public record MetricComparison(
    string MetricName,
    double BaselineValue,
    double CurrentValue,
    double PercentageChange);

/// <summary>
/// Performance baseline.
/// </summary>
public record PerformanceBaseline(
    string Id,
    string Description,
    DateTime SetAt,
    BenchmarkMetrics Metrics);

/// <summary>
/// Report options.
/// </summary>
public record ReportOptions(
    string SessionId,
    bool IncludeGraphs,
    bool IncludeRecommendations,
    bool IncludeRawData,
    ReportFormat Format);

/// <summary>
/// Report format.
/// </summary>
public enum ReportFormat
{
    Html,
    Pdf,
    Json,
    Csv
}

/// <summary>
/// Performance report.
/// </summary>
public record PerfReport(
    string SessionId,
    DateTime GeneratedAt,
    string Content,
    ReportFormat Format);

/// <summary>
/// Export format.
/// </summary>
public enum ExportFormat
{
    Json,
    Xml,
    Csv
}

/// <summary>
/// Historical metrics.
/// </summary>
public record HistoricalMetrics(
    DateTime Timestamp,
    double AverageFps,
    long MemoryUsage,
    double CpuUsage);

/// <summary>
/// Performance threshold.
/// </summary>
public record PerformanceThreshold(
    string Id,
    ThresholdType Type,
    double MinValue,
    double MaxValue,
    ThresholdAction Action);

/// <summary>
/// Threshold type.
/// </summary>
public enum ThresholdType
{
    Fps,
    Memory,
    Cpu,
    FrameTime
}

/// <summary>
/// Threshold action.
/// </summary>
public enum ThresholdAction
{
    Log,
    Alert,
    Pause,
    Stop
}

/// <summary>
/// Performance alert.
/// </summary>
public record PerformanceAlert(
    string Id,
    AlertSeverity Severity,
    string Title,
    string Message,
    DateTime Timestamp,
    bool Acknowledged);

/// <summary>
/// Alert severity.
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

#endregion
