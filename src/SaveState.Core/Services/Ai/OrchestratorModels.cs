using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Data models and interfaces for the UltimateAiOrchestrator.
    /// Extracted for better organization and maintainability.
    /// </summary>

    /// <summary>
    /// Status of pipeline execution.
    /// </summary>
    public enum PipelineStatus
    {
        Success,
        PartialSuccess,
        Failed,
        Cancelled,
        SuccessWithFallback
    }

    /// <summary>
    /// Represents a stage in the AI processing pipeline.
    /// </summary>
    public class AiPipelineStage
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 0;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool CriticalStage { get; set; } = false;
        public bool SkipOnError { get; set; } = true;
    }

    /// <summary>
    /// Context that flows through the AI pipeline during request processing.
    /// </summary>
    public class PipelineContext
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string Input { get; set; } = string.Empty;
        public string? Output { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public List<PipelineStageResult> StageResults { get; set; } = new();
        public PipelineContextData Data { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool IsCancelled { get; set; }
        public string? CancellationReason { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    /// <summary>
    /// Result of a single pipeline stage execution.
    /// </summary>
    public class PipelineStageResult
    {
        public string StageName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public bool WasSkipped { get; set; }
        public StageMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Final result of executing the complete AI pipeline.
    /// </summary>
    public class PipelineResult
    {
        public string RequestId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Output { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<PipelineStageResult> StageResults { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public StageMetadata Metadata { get; set; } = new();
        public string? FallbackUsed { get; set; }
        public bool UsedCache { get; set; }
        public float QualityScore { get; set; }

        // Additional properties for compatibility with existing code
        public string Input { get; set; } = string.Empty;
        public PipelineStatus Status { get; set; }
        public string? Error { get; set; }
        public PipelineContextData ContextData { get; set; } = new();
        public double ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Configuration for an A/B testing experiment.
    /// </summary>
    public class ExperimentConfig
    {
        public string ExperimentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, ExperimentVariant> Variants { get; set; } = new();
        public float TrafficPercentage { get; set; } = 0.1f;
        public bool IsActive { get; set; } = false;
    }

    /// <summary>
    /// A single variant within an A/B testing experiment.
    /// </summary>
    public class ExperimentVariant
    {
        public string VariantId { get; set; } = string.Empty;
        public float Weight { get; set; } = 0.5f;
        public VariantConfiguration Config { get; set; } = new();
    }

    /// <summary>
    /// Data structure for observability events emitted by the orchestrator.
    /// </summary>
    public class ObservabilityData
    {
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Stage { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public EventData Data { get; set; } = new();
        public TimeSpan? Latency { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Delegate types for pipeline extensibility.
    /// </summary>
    public delegate Task PipelineStageHandler(PipelineContext context);
    public delegate Task<bool> PipelineCondition(PipelineContext context);
    public delegate void ObservabilityHandler(ObservabilityData data);

    /// <summary>
    /// Interface for the ultimate AI orchestrator with full pipeline management.
    /// </summary>
    public interface IUltimateAiOrchestrator
    {
        // Pipeline management
        void AddStage(string name, PipelineStageHandler handler, AiPipelineStage? config = null);
        void RemoveStage(string name);
        void SetStageCondition(string stageName, PipelineCondition condition);

        // Execution
        Task<PipelineResult> ExecuteAsync(string input, PipelineContextData? initialData = null, CancellationToken ct = default);
        Task<PipelineResult> ExecuteWithFallbackAsync(string input, Func<string, Task<string>> fallback, CancellationToken ct = default);

        // Caching
        void EnableCache(string keyPattern, TimeSpan ttl);
        void InvalidateCache(string keyPattern);
        void ClearCache();

        // A/B Testing
        void RegisterExperiment(ExperimentConfig config);
        void EndExperiment(string experimentId);
        string? GetAssignedVariant(string userId, string experimentId);

        // Observability
        void AddObserver(ObservabilityHandler handler);
        OrchestratorMetrics GetMetrics();
        List<ObservabilityData> GetRecentEvents(int count = 100);

        // Health
        Task<HealthCheckResult> CheckHealthAsync();
        void EnableSelfHealing(bool enable);
    }

    /// <summary>
    /// Component interfaces for the UltimateAiOrchestrator subsystem.
    /// </summary>

    public interface IPipelineOrchestrator
    {
        void AddStage(string name, PipelineStageHandler handler, AiPipelineStage? config = null);
        void RemoveStage(string name);
        void SetStageCondition(string stageName, PipelineCondition condition);
        Task<PipelineResult> ExecuteAsync(string input, PipelineContextData? contextData = null, CancellationToken cancellationToken = default);
        Task<PipelineResult> ExecuteWithFallbackAsync(string input, Func<string, Task<string>> fallbackGenerator, PipelineContextData? contextData = null, CancellationToken cancellationToken = default);
        IEnumerable<string> GetStageNames();
        AiPipelineStage? GetStageConfig(string stageName);
    }

    public interface IAiCacheCoordinator
    {
        void EnableCache(string keyPattern, TimeSpan ttl);
        void Store(string key, string value, TimeSpan ttl);
        string? Get(string key);
        void InvalidateCache(string keyPattern);
        void ClearCache();
        (int TotalEntries, int ExpiredEntries, DateTime? NextExpiry) GetStats();
        void CleanupExpiredEntries();
    }

    public interface IAiExperimentCoordinator
    {
        void RegisterExperiment(ExperimentConfig config);
        void EndExperiment(string experimentId);
        string? GetAssignedVariant(string userId, string experimentId);
        IEnumerable<ExperimentConfig> GetActiveExperiments();
        ExperimentConfig? GetExperiment(string experimentId);
        Dictionary<string, int> GetExperimentStats(string experimentId);
    }

    public interface IAiMetricsAggregator
    {
        void RecordRequestStart();
        void RecordRequestSuccess(long latencyMs);
        void RecordRequestFailure();
        void RecordCacheHit();
        void RecordCacheMiss();
        void RecordFallbackUsed();
        void RecordStageExecution(string stageName, bool success, long latencyMs);
        void AddObservabilityEvent(ObservabilityData data);
        void AddObserver(ObservabilityHandler handler);
        OrchestratorMetrics GetMetrics();
        List<ObservabilityData> GetRecentEvents(int count = 50);
        void ResetMetrics();
    }

    public interface IAiHealthCoordinator
    {
        Task<HealthCheckResult> CheckHealthAsync();
        void EnableSelfHealing(bool enable);
        Task<bool> PerformSelfHealingAsync();
        Task<Dictionary<string, object>> GetDiagnosticInfoAsync();
    }

    public interface IAiPipelineBuilder
    {
        void BuildStandardPipeline();
    }

    /// <summary>
    /// Aggregate metrics for the orchestrator.
    /// </summary>
    public class OrchestratorMetrics
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public float SuccessRate => TotalRequests > 0 ? (float)SuccessfulRequests / TotalRequests : 0;
        public TimeSpan AverageLatency { get; set; }
        public TimeSpan P50Latency { get; set; }
        public TimeSpan P95Latency { get; set; }
        public TimeSpan P99Latency { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public float CacheHitRate => CacheHits + CacheMisses > 0 ? (float)CacheHits / (CacheHits + CacheMisses) : 0;
        public Dictionary<string, StageMetrics> StageMetrics { get; set; } = new();
        public int ActiveExperiments { get; set; }
        public int FallbacksUsed { get; set; }
    }

    /// <summary>
    /// Metrics for a single pipeline stage.
    /// </summary>
    public class StageMetrics
    {
        public string StageName { get; set; } = string.Empty;
        public int Executions { get; set; }
        public int Successes { get; set; }
        public int Failures { get; set; }
        public int Skipped { get; set; }
        public TimeSpan AverageLatency { get; set; }
    }

    /// <summary>
    /// Result of a health check across orchestrator components.
    /// </summary>
    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Health status of a single component.
    /// </summary>
    public class ComponentHealth
    {
        public string Name { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = "unknown";
        public TimeSpan? ResponseTime { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Details { get; set; }
    }

    /// <summary>
    /// Configuration options for the orchestrator.
    /// </summary>
    public class UltimateOrchestratorConfig
    {
        public int MaxInputLength { get; set; } = 50000;
        public bool TruncateLongInputs { get; set; } = true;
        public bool EnableCaching { get; set; } = true;
        public TimeSpan DefaultCacheTtl { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxCacheSize { get; set; } = 1000;
        public int MaxEventHistory { get; set; } = 1000;
        public int LatencyHistorySize { get; set; } = 1000;
        public Dictionary<string, TimeSpan> CachePatterns { get; set; } = new();
    }
}
