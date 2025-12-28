using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.Production
{
    public interface IAiStatisticsCollector
    {
        void RecordRequest(bool success, float latencyMs, float confidence, string? agent, string? intent, bool isCacheHit, bool isValidationFailure, bool isEdgeCase);
        ProductionAiStats GetStats();
    }

    public class AiStatisticsCollector : IAiStatisticsCollector
    {
        private int _totalRequests = 0;
        private int _successfulRequests = 0;
        private int _failedRequests = 0;
        private int _cacheHits = 0;
        private int _edgeCasesHandled = 0;
        private int _validationFailures = 0;
        private double _totalLatency = 0;
        private double _totalConfidence = 0;
        private readonly ConcurrentDictionary<string, int> _requestsByAgent = new();
        private readonly ConcurrentDictionary<string, int> _requestsByIntent = new();

        public void RecordRequest(bool success, float latencyMs, float confidence, string? agent, string? intent, bool isCacheHit, bool isValidationFailure, bool isEdgeCase)
        {
            Interlocked.Increment(ref _totalRequests);

            if (isCacheHit) Interlocked.Increment(ref _cacheHits);
            if (isValidationFailure) Interlocked.Increment(ref _validationFailures);
            if (isEdgeCase) Interlocked.Increment(ref _edgeCasesHandled);

            if (success)
            {
                Interlocked.Increment(ref _successfulRequests);
                lock (this)
                {
                    _totalLatency += latencyMs;
                    _totalConfidence += confidence;
                }
            }
            else
            {
                Interlocked.Increment(ref _failedRequests);
            }

            if (!string.IsNullOrEmpty(agent))
            {
                _requestsByAgent.AddOrUpdate(agent, 1, (_, c) => c + 1);
            }

            if (!string.IsNullOrEmpty(intent))
            {
                _requestsByIntent.AddOrUpdate(intent, 1, (_, c) => c + 1);
            }
        }

        public ProductionAiStats GetStats()
        {
            int successCount = _successfulRequests;
            return new ProductionAiStats
            {
                TotalRequests = _totalRequests,
                SuccessfulRequests = successCount,
                FailedRequests = _failedRequests,
                CacheHits = _cacheHits,
                AverageLatencyMs = successCount > 0 ? (float)(_totalLatency / successCount) : 0,
                AverageConfidence = successCount > 0 ? (float)(_totalConfidence / successCount) : 0,
                RequestsByAgent = new Dictionary<string, int>(_requestsByAgent),
                RequestsByIntent = new Dictionary<string, int>(_requestsByIntent),
                EdgeCasesHandled = _edgeCasesHandled,
                ValidationFailures = _validationFailures
            };
        }
    }
}
