namespace SaveState.Application.Mugen.Models.PerformanceOptimization;

// Type aliases to existing types in SharedTypes.cs
using OptimizationSuggestion = SaveState.Application.Mugen.OptimizationSuggestion;
using AppliedOptimization = SaveState.Application.Mugen.AppliedOptimization;
using OptimizationResult = SaveState.Application.Mugen.OptimizationResult;
using CacheOptimization = SaveState.Application.Mugen.CacheOptimization;
using BatchingOptimization = SaveState.Application.Mugen.BatchingOptimization;
using MemoryOptimization = SaveState.Application.Mugen.MemoryOptimization;
using LoadBalancingResult = SaveState.Application.Mugen.LoadBalancingResult;

/// <summary>
/// Complete performance report.
/// </summary>
public record PerformanceReport(
    string SessionId,
    OptimizationPerformanceAnalysis Analysis,
    CacheOptimization CacheOptimization,
    BatchingOptimization BatchingOptimization,
    MemoryOptimization MemoryOptimization,
    LoadBalancingResult LoadBalancingResult,
    float OverallScore,
    DateTime GeneratedAt
);
