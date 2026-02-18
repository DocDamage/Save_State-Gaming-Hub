namespace SaveState.Application.Mugen.Services.CrossPhase.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.CrossPhase;
using SaveState.Core.Common.Services;

public class OptimizationEngine
{
    private readonly ILogger<OptimizationEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public OptimizationEngine(ILogger<OptimizationEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public float CalculatePerformanceImpact(
        IReadOnlyList<MechanicInteraction> interactions,
        UnifiedPerformanceMetrics currentMetrics)
    {
        _logger.LogDebug("Calculating performance impact for {Count} interactions", interactions.Count);

        var baseImpact = interactions.Count * 0.05f;
        var efficiencyFactor = currentMetrics.IntegrationEfficiency;

        return baseImpact / Math.Max(efficiencyFactor, 0.1f);
    }

    public UnifiedPerformanceMetrics CalculateUnifiedPerformanceMetrics(
        IReadOnlyList<MechanicType> activeMechanics,
        IReadOnlyDictionary<string, float> performanceData)
    {
        _logger.LogDebug("Calculating unified performance metrics for {Count} mechanics", activeMechanics.Count);

        var avgResponseTime = performanceData.TryGetValue("ResponseTime", out var rt) ? rt : 16.6f;
        var memoryUsage = performanceData.TryGetValue("MemoryUsage", out var mem) ? mem : 100f;
        var efficiency = Math.Max(0.1f, 1.0f - (activeMechanics.Count * 0.05f));
        var overhead = activeMechanics.Count * 0.02f;

        return new UnifiedPerformanceMetrics
        {
            AverageResponseTime = avgResponseTime,
            PeakMemoryUsage = memoryUsage,
            IntegrationEfficiency = efficiency,
            CrossPhaseOverhead = overhead
        };
    }

    public Task<IntegrationOptimization> OptimizeIntegrationAsync(
        string sessionId,
        UnifiedGameState currentState,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Optimizing integration for session {SessionId}", sessionId);

        var bottlenecks = IdentifyBottlenecks(currentState);
        var optimizationsApplied = ApplyOptimizations(bottlenecks);
        var performanceImprovement = CalculateImprovement(bottlenecks.Count, optimizationsApplied);

        var result = new IntegrationOptimization
        {
            SessionId = sessionId,
            BottlenecksIdentified = bottlenecks.Count,
            OptimizationsApplied = optimizationsApplied,
            PerformanceImprovement = performanceImprovement,
            OptimizationTimestamp = _timeProvider.UtcNow
        };

        return Task.FromResult(result);
    }

    private static List<CrossPhasePerformanceBottleneck> IdentifyBottlenecks(UnifiedGameState state)
    {
        var bottlenecks = new List<CrossPhasePerformanceBottleneck>();

        if (state.PerformanceMetrics.CrossPhaseOverhead > 0.3f)
        {
            bottlenecks.Add(new CrossPhasePerformanceBottleneck
            {
                BottleneckType = "HighOverhead",
                Severity = state.PerformanceMetrics.CrossPhaseOverhead,
                Description = "Excessive cross-phase communication overhead"
            });
        }

        if (state.PerformanceMetrics.IntegrationEfficiency < 0.5f)
        {
            bottlenecks.Add(new CrossPhasePerformanceBottleneck
            {
                BottleneckType = "LowEfficiency",
                Severity = 1.0f - state.PerformanceMetrics.IntegrationEfficiency,
                Description = "Low mechanic integration efficiency"
            });
        }

        return bottlenecks;
    }

    private static int ApplyOptimizations(IReadOnlyList<CrossPhasePerformanceBottleneck> bottlenecks)
    {
        return bottlenecks.Count(b => b.Severity < 0.8f);
    }

    private static float CalculateImprovement(int bottlenecksCount, int optimizationsApplied)
    {
        return bottlenecksCount > 0
            ? (float)optimizationsApplied / bottlenecksCount * 0.5f
            : 0f;
    }
}
