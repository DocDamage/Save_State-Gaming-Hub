using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Telemetry
{
    /// <summary>
    /// AI Observability layer - tracks what matters for AI quality.
    /// Measures hallucination frequency, lore violations, drift, latency, and more.
    /// </summary>
    public interface IAiTelemetry
    {
        /// <summary>
        /// Record an AI interaction
        /// </summary>
        void RecordInteraction(AiInteractionEvent interaction);

        /// <summary>
        /// Record a hallucination detection
        /// </summary>
        void RecordHallucination(HallucinationEvent hallucination);

        /// <summary>
        /// Record a lore violation
        /// </summary>
        void RecordLoreViolation(LoreViolationEvent violation);

        /// <summary>
        /// Record latency metrics
        /// </summary>
        void RecordLatency(LatencyEvent latency);

        /// <summary>
        /// Get current metrics summary
        /// </summary>
        TelemetrySummary GetSummary(TimeSpan? window = null);

        /// <summary>
        /// Get quality score for AI outputs
        /// </summary>
        AiQualityScore GetQualityScore();

        /// <summary>
        /// Export telemetry data
        /// </summary>
        string ExportData(TelemetryExportFormat format);
    }

    /// <summary>
    /// An AI interaction event
    /// </summary>
    public class AiInteractionEvent
    {
        public string InteractionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string InteractionType { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
        public string? ModelUsed { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public TimeSpan Latency { get; set; }
        public bool WasSuccessful { get; set; } = true;
        public bool UsedCache { get; set; }
        public bool UsedFallback { get; set; }
        public double? QualityScore { get; set; }
    }

    /// <summary>
    /// A hallucination detection event
    /// </summary>
    public class HallucinationEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string HallucinationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? OriginalContent { get; set; }
        public string? CorrectedContent { get; set; }
        public double Confidence { get; set; }
        public string DetectionMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// A lore violation event
    /// </summary>
    public class LoreViolationEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ViolationType { get; set; } = string.Empty;
        public string ViolatedLoreId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "minor";
        public bool WasAutoRecovered { get; set; }
    }

    /// <summary>
    /// A latency measurement event
    /// </summary>
    public class LatencyEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string OperationType { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public TimeSpan? TimeToFirstToken { get; set; }
        public bool MetTarget { get; set; } = true;
        public TimeSpan? Target { get; set; }
    }

    /// <summary>
    /// Summary of telemetry data
    /// </summary>
    public class TelemetrySummary
    {
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public long TotalInteractions { get; set; }
        public long SuccessfulInteractions { get; set; }
        public long FailedInteractions { get; set; }
        public long HallucinationsDetected { get; set; }
        public long LoreViolations { get; set; }
        public double AverageLatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public double CacheHitRate { get; set; }
        public double FallbackRate { get; set; }
        public long TotalTokensUsed { get; set; }
        public Dictionary<string, long> InteractionsByType { get; set; } = new();
        public Dictionary<string, long> HallucinationsByType { get; set; } = new();
    }

    /// <summary>
    /// AI quality score
    /// </summary>
    public class AiQualityScore
    {
        public double OverallScore { get; set; } // 0-100
        public double AccuracyScore { get; set; }
        public double ConsistencyScore { get; set; }
        public double LatencyScore { get; set; }
        public double ReliabilityScore { get; set; }
        public string Grade { get; set; } = "C"; // A, B, C, D, F
        public List<string> ImprovementSuggestions { get; set; } = new();
    }

    /// <summary>
    /// Export format for telemetry
    /// </summary>
    public enum TelemetryExportFormat
    {
        Json,
        Csv,
        Prometheus,
        OpenTelemetry
    }

    /// <summary>
    /// Default implementation of AI telemetry
    /// </summary>
    public class AiTelemetry : IAiTelemetry
    {
        private readonly ConcurrentBag<AiInteractionEvent> _interactions = new();
        private readonly ConcurrentBag<HallucinationEvent> _hallucinations = new();
        private readonly ConcurrentBag<LoreViolationEvent> _loreViolations = new();
        private readonly ConcurrentBag<LatencyEvent> _latencies = new();

        // Aggregated counters for performance
        private long _totalInteractions = 0;
        private long _successfulInteractions = 0;
        private long _cacheHits = 0;
        private long _fallbacksUsed = 0;
        private long _totalTokens = 0;

        public void RecordInteraction(AiInteractionEvent interaction)
        {
            _interactions.Add(interaction);
            System.Threading.Interlocked.Increment(ref _totalInteractions);
            
            if (interaction.WasSuccessful)
                System.Threading.Interlocked.Increment(ref _successfulInteractions);
            if (interaction.UsedCache)
                System.Threading.Interlocked.Increment(ref _cacheHits);
            if (interaction.UsedFallback)
                System.Threading.Interlocked.Increment(ref _fallbacksUsed);
            
            System.Threading.Interlocked.Add(ref _totalTokens, 
                interaction.InputTokens + interaction.OutputTokens);
        }

        public void RecordHallucination(HallucinationEvent hallucination)
        {
            _hallucinations.Add(hallucination);
        }

        public void RecordLoreViolation(LoreViolationEvent violation)
        {
            _loreViolations.Add(violation);
        }

        public void RecordLatency(LatencyEvent latency)
        {
            _latencies.Add(latency);
        }

        public TelemetrySummary GetSummary(TimeSpan? window = null)
        {
            var endTime = DateTime.UtcNow;
            var startTime = window.HasValue
                ? endTime - window.Value
                : DateTime.MinValue;

            var windowInteractions = _interactions
                .Where(i => i.Timestamp >= startTime && i.Timestamp <= endTime)
                .ToList();

            var windowLatencies = _latencies
                .Where(l => l.Timestamp >= startTime && l.Timestamp <= endTime)
                .Select(l => l.Duration.TotalMilliseconds)
                .OrderBy(l => l)
                .ToList();

            var avgLatency = windowLatencies.Any() ? windowLatencies.Average() : 0;
            var p95Latency = windowLatencies.Any() 
                ? windowLatencies[(int)(windowLatencies.Count * 0.95)] 
                : 0;

            var interactionsByType = windowInteractions
                .GroupBy(i => i.InteractionType)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            var hallucinationsByType = _hallucinations
                .Where(h => h.Timestamp >= startTime)
                .GroupBy(h => h.HallucinationType)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            return new TelemetrySummary
            {
                WindowStart = startTime,
                WindowEnd = endTime,
                TotalInteractions = windowInteractions.Count,
                SuccessfulInteractions = windowInteractions.Count(i => i.WasSuccessful),
                FailedInteractions = windowInteractions.Count(i => !i.WasSuccessful),
                HallucinationsDetected = _hallucinations.Count(h => h.Timestamp >= startTime),
                LoreViolations = _loreViolations.Count(v => v.Timestamp >= startTime),
                AverageLatencyMs = avgLatency,
                P95LatencyMs = p95Latency,
                CacheHitRate = windowInteractions.Any()
                    ? (double)windowInteractions.Count(i => i.UsedCache) / windowInteractions.Count * 100
                    : 0,
                FallbackRate = windowInteractions.Any()
                    ? (double)windowInteractions.Count(i => i.UsedFallback) / windowInteractions.Count * 100
                    : 0,
                TotalTokensUsed = windowInteractions.Sum(i => i.InputTokens + i.OutputTokens),
                InteractionsByType = interactionsByType,
                HallucinationsByType = hallucinationsByType
            };
        }

        public AiQualityScore GetQualityScore()
        {
            var summary = GetSummary(TimeSpan.FromHours(24));

            // Calculate component scores
            var accuracyScore = CalculateAccuracyScore(summary);
            var consistencyScore = CalculateConsistencyScore(summary);
            var latencyScore = CalculateLatencyScore(summary);
            var reliabilityScore = CalculateReliabilityScore(summary);

            var overallScore = (accuracyScore + consistencyScore + latencyScore + reliabilityScore) / 4;

            var grade = overallScore switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };

            var suggestions = GenerateImprovementSuggestions(summary, 
                accuracyScore, consistencyScore, latencyScore, reliabilityScore);

            return new AiQualityScore
            {
                OverallScore = overallScore,
                AccuracyScore = accuracyScore,
                ConsistencyScore = consistencyScore,
                LatencyScore = latencyScore,
                ReliabilityScore = reliabilityScore,
                Grade = grade,
                ImprovementSuggestions = suggestions
            };
        }

        public string ExportData(TelemetryExportFormat format)
        {
            var summary = GetSummary();

            return format switch
            {
                TelemetryExportFormat.Json => ExportJson(summary),
                TelemetryExportFormat.Csv => ExportCsv(summary),
                TelemetryExportFormat.Prometheus => ExportPrometheus(summary),
                _ => ExportJson(summary)
            };
        }

        private double CalculateAccuracyScore(TelemetrySummary summary)
        {
            if (summary.TotalInteractions == 0) return 100;
            
            var hallucinationPenalty = summary.HallucinationsDetected * 5;
            var lorePenalty = summary.LoreViolations * 10;
            
            return Math.Max(0, 100 - hallucinationPenalty - lorePenalty);
        }

        private double CalculateConsistencyScore(TelemetrySummary summary)
        {
            if (summary.TotalInteractions == 0) return 100;
            return 100 - (summary.LoreViolations * 5);
        }

        private double CalculateLatencyScore(TelemetrySummary summary)
        {
            // Target is 500ms average, 1000ms P95
            var avgScore = summary.AverageLatencyMs switch
            {
                < 200 => 100,
                < 500 => 90,
                < 1000 => 70,
                < 2000 => 50,
                _ => 30
            };

            var p95Score = summary.P95LatencyMs switch
            {
                < 500 => 100,
                < 1000 => 85,
                < 2000 => 65,
                < 5000 => 40,
                _ => 20
            };

            return (avgScore + p95Score) / 2;
        }

        private double CalculateReliabilityScore(TelemetrySummary summary)
        {
            if (summary.TotalInteractions == 0) return 100;
            
            var successRate = (double)summary.SuccessfulInteractions / summary.TotalInteractions * 100;
            var fallbackPenalty = summary.FallbackRate * 0.2; // Small penalty for fallbacks
            
            return Math.Max(0, successRate - fallbackPenalty);
        }

        private List<string> GenerateImprovementSuggestions(
            TelemetrySummary summary,
            double accuracy, double consistency, double latency, double reliability)
        {
            var suggestions = new List<string>();

            if (accuracy < 80)
            {
                suggestions.Add("Consider improving hallucination detection or adding more lore constraints");
            }

            if (latency < 80)
            {
                suggestions.Add("Latency is impacting quality. Consider caching more responses or using smaller models");
            }

            if (reliability < 90)
            {
                suggestions.Add("Reliability could be improved. Check for error patterns and add better fallbacks");
            }

            if (summary.CacheHitRate < 20)
            {
                suggestions.Add("Low cache hit rate. Consider expanding cache coverage for common queries");
            }

            if (summary.FallbackRate > 20)
            {
                suggestions.Add("High fallback rate indicates timeout or rate limit issues");
            }

            return suggestions;
        }

        private string ExportJson(TelemetrySummary summary)
        {
            return JsonSerializer.Serialize(summary, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        private string ExportCsv(TelemetrySummary summary)
        {
            return $"metric,value\n" +
                   $"total_interactions,{summary.TotalInteractions}\n" +
                   $"successful_interactions,{summary.SuccessfulInteractions}\n" +
                   $"hallucinations,{summary.HallucinationsDetected}\n" +
                   $"lore_violations,{summary.LoreViolations}\n" +
                   $"avg_latency_ms,{summary.AverageLatencyMs:F2}\n" +
                   $"p95_latency_ms,{summary.P95LatencyMs:F2}\n" +
                   $"cache_hit_rate,{summary.CacheHitRate:F2}\n" +
                   $"fallback_rate,{summary.FallbackRate:F2}\n" +
                   $"total_tokens,{summary.TotalTokensUsed}";
        }

        private string ExportPrometheus(TelemetrySummary summary)
        {
            return $"# HELP ai_interactions_total Total AI interactions\n" +
                   $"ai_interactions_total {summary.TotalInteractions}\n" +
                   $"# HELP ai_hallucinations_total Total hallucinations detected\n" +
                   $"ai_hallucinations_total {summary.HallucinationsDetected}\n" +
                   $"# HELP ai_lore_violations_total Total lore violations\n" +
                   $"ai_lore_violations_total {summary.LoreViolations}\n" +
                   $"# HELP ai_latency_avg_ms Average latency in milliseconds\n" +
                   $"ai_latency_avg_ms {summary.AverageLatencyMs:F2}\n" +
                   $"# HELP ai_latency_p95_ms P95 latency in milliseconds\n" +
                   $"ai_latency_p95_ms {summary.P95LatencyMs:F2}";
        }
    }
}
