using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Marker interface that composes focused performance profiling contracts.
/// </summary>
public interface IPerformanceProfilerService :
    IProfilingSessionService,
    IPerformanceMetricsService,
    ICharacterProfilingService,
    IBattleProfilingService,
    IBottleneckAnalysisService,
    IOptimizationService,
    IBenchmarkService,
    IPerformanceReportingService,
    IPerformanceAlertService
{
}

/// <summary>
/// Session lifecycle operations for the performance profiler.
/// </summary>
public interface IProfilingSessionService
{
    Task<Result<ProfilingSession>> StartSessionAsync(
        string name,
        ProfilingConfiguration configuration,
        CancellationToken ct = default);

    Task<Result<ProfilingReport>> StopSessionAsync(
        CancellationToken ct = default);

    Task<Result<ProfilingSession>> GetActiveSessionAsync(
        CancellationToken ct = default);

    Task<Result> PauseProfilingAsync(CancellationToken ct = default);

    Task<Result> ResumeProfilingAsync(CancellationToken ct = default);
}

/// <summary>
/// Real-time monitoring and metric streaming operations.
/// </summary>
public interface IPerformanceMetricsService
{
    Task<Result<PerfMetrics>> GetCurrentMetricsAsync(
        CancellationToken ct = default);

    Task<Result<FrameRateStats>> GetFrameRateStatsAsync(
        TimeSpan? window = null,
        CancellationToken ct = default);

    Task<Result<MemoryStats>> GetMemoryStatsAsync(
        CancellationToken ct = default);

    Task<Result<CpuStats>> GetCpuStatsAsync(
        CancellationToken ct = default);

    Task<Result<GpuStats>> GetGpuStatsAsync(
        CancellationToken ct = default);

    Task<Result<LoadingMetrics>> GetLoadingMetricsAsync(
        CancellationToken ct = default);

    IAsyncEnumerable<PerformanceSnapshot> SubscribeToMetricsAsync(
        MetricsSubscriptionOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// Character-specific profiling operations.
/// </summary>
public interface ICharacterProfilingService
{
    Task<Result<CharacterProfileResult>> ProfileCharacterAsync(
        Guid characterId,
        CharacterProfilingOptions options,
        CancellationToken ct = default);

    Task<Result<LoadingProfile>> ProfileCharacterLoadingAsync(
        Guid characterId,
        CancellationToken ct = default);

    Task<Result<AnimationProfile>> ProfileAnimationsAsync(
        Guid characterId,
        CancellationToken ct = default);

    Task<Result<AiProfile>> ProfileAiPerformanceAsync(
        Guid characterId,
        CancellationToken ct = default);

    Task<Result<ResourceUsage>> GetCharacterResourceUsageAsync(
        Guid characterId,
        CancellationToken ct = default);
}

/// <summary>
/// Battle-focused profiling operations.
/// </summary>
public interface IBattleProfilingService
{
    Task<Result> StartBattleProfilingAsync(
        BattleProfilingOptions options,
        CancellationToken ct = default);

    Task<Result<BattlePerformanceAnalysis>> GetBattleAnalysisAsync(
        CancellationToken ct = default);

    Task<Result<FrameTimeBreakdown>> GetFrameTimeBreakdownAsync(
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<PerformanceSpike>>> DetectSpikesAsync(
        SpikeDetectionOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// Bottleneck and deep diagnostics operations.
/// </summary>
public interface IBottleneckAnalysisService
{
    Task<Result<BottleneckAnalysis>> AnalyzeBottlenecksAsync(
        BottleneckAnalysisOptions options,
        CancellationToken ct = default);

    Task<Result<MemoryLeakReport>> DetectMemoryLeaksAsync(
        MemoryLeakDetectionOptions options,
        CancellationToken ct = default);

    Task<Result<ThreadAnalysis>> AnalyzeThreadsAsync(
        CancellationToken ct = default);

    Task<Result<RenderingAnalysis>> AnalyzeRenderingAsync(
        CancellationToken ct = default);
}

/// <summary>
/// Recommendation and optimization operations.
/// </summary>
public interface IOptimizationService
{
    Task<Result<IReadOnlyList<OptimizationRecommendation>>> GetOptimizationSuggestionsAsync(
        OptimizationOptions options,
        CancellationToken ct = default);

    Task<Result<OptimizationImpact>> SimulateOptimizationAsync(
        OptimizationRecommendation recommendation,
        CancellationToken ct = default);

    Task<Result<AutoOptimizationResult>> ApplyAutoOptimizationsAsync(
        AutoOptimizationOptions options,
        CancellationToken ct = default);

    Task<Result<AssetOptimizationResult>> OptimizeAssetsAsync(
        Guid characterId,
        AssetOptimizationOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// Benchmark execution and baseline comparison operations.
/// </summary>
public interface IBenchmarkService
{
    Task<Result<BenchmarkResult>> RunBenchmarkAsync(
        BenchmarkConfiguration configuration,
        CancellationToken ct = default);

    Task<Result<BenchmarkComparison>> CompareBenchmarksAsync(
        IReadOnlyList<string> benchmarkIds,
        CancellationToken ct = default);

    Task<Result<PerformanceBaseline>> GetBaselineAsync(
        CancellationToken ct = default);

    Task<Result> SetBaselineAsync(
        string description,
        CancellationToken ct = default);
}

/// <summary>
/// Report generation and export operations.
/// </summary>
public interface IPerformanceReportingService
{
    Task<Result<PerfReport>> GenerateReportAsync(
        ReportOptions options,
        CancellationToken ct = default);

    Task<Result<string>> ExportProfilingDataAsync(
        string sessionId,
        ExportFormat format,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<HistoricalMetrics>>> GetPerformanceHistoryAsync(
        TimeSpan period,
        CancellationToken ct = default);
}

/// <summary>
/// Threshold and alerting operations.
/// </summary>
public interface IPerformanceAlertService
{
    Task<Result> SetThresholdAsync(
        PerformanceThreshold threshold,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<PerformanceAlert>>> GetActiveAlertsAsync(
        CancellationToken ct = default);

    Task<Result> AcknowledgeAlertAsync(
        string alertId,
        CancellationToken ct = default);
}
