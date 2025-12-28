using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.EdgeCases
{
    public interface IEdgeCaseStatisticsCollector
    {
        void ReportEdgeCase(EdgeCaseDetection edgeCase);
        EdgeCaseStatistics GetStatistics(int recoveriesAttempted, int recoveriesSuccesses);
        void IncrementDetectionCount(EdgeCaseType type, int count = 1);
    }

    public class EdgeCaseStatisticsCollector : IEdgeCaseStatisticsCollector
    {
        private readonly EdgeCaseConfig _config;
        private readonly ConcurrentQueue<EdgeCaseDetection> _recentDetections = new();
        private readonly ConcurrentDictionary<EdgeCaseType, int> _detectionCounts = new();

        public EdgeCaseStatisticsCollector(EdgeCaseConfig? config = null)
        {
            _config = config ?? new EdgeCaseConfig();
        }

        public void ReportEdgeCase(EdgeCaseDetection edgeCase)
        {
            _recentDetections.Enqueue(edgeCase);
            while (_recentDetections.Count > _config.MaxRecentDetections)
            {
                _recentDetections.TryDequeue(out _);
            }

            _detectionCounts.AddOrUpdate(edgeCase.Type, 1, (_, c) => c + 1);
        }

        public void IncrementDetectionCount(EdgeCaseType type, int count = 1)
        {
             _detectionCounts.AddOrUpdate(type, count, (_, c) => c + count);
        }

        public EdgeCaseStatistics GetStatistics(int recoveriesAttempted, int recoveriesSuccesses)
        {
            var detections = _recentDetections.ToArray();
            return new EdgeCaseStatistics
            {
                TotalDetections = detections.Length,
                DetectionsByType = new Dictionary<EdgeCaseType, int>(_detectionCounts),
                InjectionAttemptsBlocked = _detectionCounts.GetValueOrDefault(EdgeCaseType.InjectionAttempt, 0),
                TruncationsApplied = _detectionCounts.GetValueOrDefault(EdgeCaseType.TooLongInput, 0),
                RecoveriesAttempted = recoveriesAttempted,
                RecoveriesSuccessful = recoveriesSuccesses,
                AverageSeverity = detections.Any() ? detections.Average(d => d.Severity) : 0
            };
        }
    }
}
