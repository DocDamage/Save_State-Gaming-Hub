using SaveState.Core.Common;
using SaveState.Application.Mugen.Models.PerformanceOptimization;

namespace SaveState.Application.Mugen.Services.PerformanceOptimization.Interfaces;

/// <summary>
/// Performance optimization service interface for real-time processing of advanced mechanics.
/// </summary>
public interface IPerformanceOptimizationService
{
    /// <summary>
    /// Analyzes performance for a session.
    /// </summary>
    Task<Result<OptimizationPerformanceAnalysis>> AnalyzePerformanceAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Applies optimizations based on suggestions.
    /// </summary>
    Task<Result<OptimizationResult>> ApplyOptimizationsAsync(string sessionId, IReadOnlyList<OptimizationSuggestion> suggestions, CancellationToken ct = default);

    /// <summary>
    /// Optimizes caching for a session.
    /// </summary>
    Task<Result<CacheOptimization>> OptimizeCachingAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Optimizes batching for a session.
    /// </summary>
    Task<Result<BatchingOptimization>> OptimizeBatchingAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Optimizes memory usage for a session.
    /// </summary>
    Task<Result<MemoryOptimization>> OptimizeMemoryAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Balances load for a session.
    /// </summary>
    Task<Result<LoadBalancingResult>> BalanceLoadAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Generates a complete performance report.
    /// </summary>
    Task<Result<PerformanceReport>> GenerateReportAsync(string sessionId, CancellationToken ct = default);
}
