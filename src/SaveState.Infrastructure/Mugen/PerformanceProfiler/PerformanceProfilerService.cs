using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen.PerformanceProfiler.Managers;

namespace SaveState.Infrastructure.Mugen.PerformanceProfiler;

/// <summary>
/// Implementation of performance profiler service for MUGEN.
/// Acts as a thin coordinator delegating to specialized managers for all operations.
/// </summary>
public class PerformanceProfilerService : IPerformanceProfilerService
{
    private readonly ILogger<PerformanceProfilerService> _logger;
    private readonly ProfilingSessionManager _sessionManager;
    private readonly MetricsCollectionManager _metricsManager;
    private readonly CharacterProfilerManager _characterProfiler;
    private readonly BattleProfilerManager _battleProfiler;
    private readonly BottleneckAnalyzerManager _bottleneckAnalyzer;
    private readonly OptimizationManager _optimizationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceProfilerService"/> class.
    /// </summary>
    public PerformanceProfilerService(
        ILogger<PerformanceProfilerService> logger,
        ProfilingSessionManager sessionManager,
        MetricsCollectionManager metricsManager,
        CharacterProfilerManager characterProfiler,
        BattleProfilerManager battleProfiler,
        BottleneckAnalyzerManager bottleneckAnalyzer,
        OptimizationManager optimizationManager)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _metricsManager = metricsManager;
        _characterProfiler = characterProfiler;
        _battleProfiler = battleProfiler;
        _bottleneckAnalyzer = bottleneckAnalyzer;
        _optimizationManager = optimizationManager;
    }

    #region Session Management

    /// <inheritdoc />
    public Task<Result<ProfilingSession>> StartSessionAsync(
        string name,
        ProfilingConfiguration configuration,
        CancellationToken ct = default)
        => _sessionManager.StartSessionAsync(name, configuration, ct);

    /// <inheritdoc />
    public Task<Result<ProfilingReport>> StopSessionAsync(CancellationToken ct = default)
        => _sessionManager.StopSessionAsync(ct);

    /// <inheritdoc />
    public Task<Result<ProfilingSession>> GetActiveSessionAsync(CancellationToken ct = default)
        => _sessionManager.GetActiveSessionAsync(ct);

    /// <inheritdoc />
    public Task<Result> PauseProfilingAsync(CancellationToken ct = default)
        => _sessionManager.PauseProfilingAsync(ct);

    /// <inheritdoc />
    public Task<Result> ResumeProfilingAsync(CancellationToken ct = default)
        => _sessionManager.ResumeProfilingAsync(ct);

    #endregion

    #region Real-time Monitoring

    /// <inheritdoc />
    public Task<Result<PerfMetrics>> GetCurrentMetricsAsync(CancellationToken ct = default)
        => _metricsManager.GetCurrentMetricsAsync(ct);

    /// <inheritdoc />
    public Task<Result<FrameRateStats>> GetFrameRateStatsAsync(TimeSpan? window = null, CancellationToken ct = default)
        => _metricsManager.GetFrameRateStatsAsync(window, ct);

    /// <inheritdoc />
    public Task<Result<MemoryStats>> GetMemoryStatsAsync(CancellationToken ct = default)
        => _metricsManager.GetMemoryStatsAsync(ct);

    /// <inheritdoc />
    public Task<Result<CpuStats>> GetCpuStatsAsync(CancellationToken ct = default)
        => _metricsManager.GetCpuStatsAsync(ct);

    /// <inheritdoc />
    public Task<Result<GpuStats>> GetGpuStatsAsync(CancellationToken ct = default)
        => _metricsManager.GetGpuStatsAsync(ct);

    /// <inheritdoc />
    public Task<Result<LoadingMetrics>> GetLoadingMetricsAsync(CancellationToken ct = default)
        => _metricsManager.GetLoadingMetricsAsync(ct);

    /// <inheritdoc />
    public IAsyncEnumerable<PerformanceSnapshot> SubscribeToMetricsAsync(
        MetricsSubscriptionOptions options,
        CancellationToken ct = default)
        => _metricsManager.SubscribeToMetricsAsync(options, ct);

    #endregion

    #region Character Profiling

    /// <inheritdoc />
    public Task<Result<CharacterProfileResult>> ProfileCharacterAsync(
        Guid characterId,
        CharacterProfilingOptions options,
        CancellationToken ct = default)
        => _characterProfiler.ProfileCharacterAsync(characterId, options, ct);

    /// <inheritdoc />
    public Task<Result<LoadingProfile>> ProfileCharacterLoadingAsync(Guid characterId, CancellationToken ct = default)
        => _characterProfiler.ProfileCharacterLoadingAsync(characterId, ct);

    /// <inheritdoc />
    public Task<Result<AnimationProfile>> ProfileAnimationsAsync(Guid characterId, CancellationToken ct = default)
        => _characterProfiler.ProfileAnimationsAsync(characterId, ct);

    /// <inheritdoc />
    public Task<Result<AiProfile>> ProfileAiPerformanceAsync(Guid characterId, CancellationToken ct = default)
        => _characterProfiler.ProfileAiPerformanceAsync(characterId, ct);

    /// <inheritdoc />
    public Task<Result<ResourceUsage>> GetCharacterResourceUsageAsync(Guid characterId, CancellationToken ct = default)
        => _characterProfiler.GetCharacterResourceUsageAsync(characterId, ct);

    #endregion

    #region Battle Profiling

    /// <inheritdoc />
    public Task<Result> StartBattleProfilingAsync(BattleProfilingOptions options, CancellationToken ct = default)
        => _battleProfiler.StartBattleProfilingAsync(options, ct);

    /// <inheritdoc />
    public Task<Result<BattlePerformanceAnalysis>> GetBattleAnalysisAsync(CancellationToken ct = default)
        => _battleProfiler.GetBattleAnalysisAsync(ct);

    /// <inheritdoc />
    public Task<Result<FrameTimeBreakdown>> GetFrameTimeBreakdownAsync(CancellationToken ct = default)
        => _battleProfiler.GetFrameTimeBreakdownAsync(ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<PerformanceSpike>>> DetectSpikesAsync(
        SpikeDetectionOptions options,
        CancellationToken ct = default)
        => _battleProfiler.DetectSpikesAsync(options, ct);

    #endregion

    #region Bottleneck Analysis

    /// <inheritdoc />
    public Task<Result<BottleneckAnalysis>> AnalyzeBottlenecksAsync(
        BottleneckAnalysisOptions options,
        CancellationToken ct = default)
        => _bottleneckAnalyzer.AnalyzeBottlenecksAsync(options, ct);

    /// <inheritdoc />
    public Task<Result<MemoryLeakReport>> DetectMemoryLeaksAsync(
        MemoryLeakDetectionOptions options,
        CancellationToken ct = default)
        => _bottleneckAnalyzer.DetectMemoryLeaksAsync(options, ct);

    /// <inheritdoc />
    public Task<Result<ThreadAnalysis>> AnalyzeThreadsAsync(CancellationToken ct = default)
        => _bottleneckAnalyzer.AnalyzeThreadsAsync(ct);

    /// <inheritdoc />
    public Task<Result<RenderingAnalysis>> AnalyzeRenderingAsync(CancellationToken ct = default)
        => _bottleneckAnalyzer.AnalyzeRenderingAsync(ct);

    #endregion

    #region Optimization

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<OptimizationRecommendation>>> GetOptimizationSuggestionsAsync(
        OptimizationOptions options,
        CancellationToken ct = default)
        => _optimizationManager.GetOptimizationSuggestionsAsync(options, ct);

    /// <inheritdoc />
    public Task<Result<OptimizationImpact>> SimulateOptimizationAsync(
        OptimizationRecommendation recommendation,
        CancellationToken ct = default)
        => _optimizationManager.SimulateOptimizationAsync(recommendation, ct);

    /// <inheritdoc />
    public Task<Result<AutoOptimizationResult>> ApplyAutoOptimizationsAsync(
        AutoOptimizationOptions options,
        CancellationToken ct = default)
        => _optimizationManager.ApplyAutoOptimizationsAsync(options, ct);

    /// <inheritdoc />
    public Task<Result<AssetOptimizationResult>> OptimizeAssetsAsync(
        Guid characterId,
        AssetOptimizationOptions options,
        CancellationToken ct = default)
        => _optimizationManager.OptimizeAssetsAsync(characterId, options, ct);

    #endregion

    #region Benchmarking

    /// <inheritdoc />
    public Task<Result<BenchmarkResult>> RunBenchmarkAsync(
        BenchmarkConfiguration configuration,
        CancellationToken ct = default)
        => _optimizationManager.RunBenchmarkAsync(configuration, ct);

    /// <inheritdoc />
    public Task<Result<BenchmarkComparison>> CompareBenchmarksAsync(
        IReadOnlyList<string> benchmarkIds,
        CancellationToken ct = default)
        => _optimizationManager.CompareBenchmarksAsync(benchmarkIds, ct);

    /// <inheritdoc />
    public Task<Result<PerformanceBaseline>> GetBaselineAsync(CancellationToken ct = default)
        => _optimizationManager.GetBaselineAsync(ct);

    /// <inheritdoc />
    public async Task<Result> SetBaselineAsync(string description, CancellationToken ct = default)
    {
        var currentMetrics = await _metricsManager.GetCurrentMetricsAsync(ct);
        if (currentMetrics.IsFailure || currentMetrics.Value is null)
        {
            return Result.Failure("Failed to get current metrics", ErrorType.Internal);
        }

        return await _optimizationManager.SetBaselineAsync(currentMetrics.Value, description, ct);
    }

    #endregion

    #region Reporting

    /// <inheritdoc />
    public Task<Result<PerfReport>> GenerateReportAsync(ReportOptions options, CancellationToken ct = default)
        => _optimizationManager.GenerateReportAsync(options, ct);

    /// <inheritdoc />
    public Task<Result<string>> ExportProfilingDataAsync(
        string sessionId,
        ExportFormat format,
        CancellationToken ct = default)
        => _optimizationManager.ExportProfilingDataAsync(sessionId, format, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<HistoricalMetrics>>> GetPerformanceHistoryAsync(
        TimeSpan period,
        CancellationToken ct = default)
        => _optimizationManager.GetPerformanceHistoryAsync(period, ct);

    #endregion

    #region Alerts and Thresholds

    /// <inheritdoc />
    public Task<Result> SetThresholdAsync(PerformanceThreshold threshold, CancellationToken ct = default)
        => _optimizationManager.SetThresholdAsync(threshold, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<PerformanceAlert>>> GetActiveAlertsAsync(CancellationToken ct = default)
        => _optimizationManager.GetActiveAlertsAsync(ct);

    /// <inheritdoc />
    public Task<Result> AcknowledgeAlertAsync(string alertId, CancellationToken ct = default)
        => _optimizationManager.AcknowledgeAlertAsync(alertId, ct);

    #endregion
}
