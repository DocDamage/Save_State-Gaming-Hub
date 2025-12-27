using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Safety
{
    public class ProvenanceRecord
    {
        public string GenerationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SourceAgent { get; set; } = string.Empty;
        public string PromptHash { get; set; } = string.Empty;
        public double ValidationScore { get; set; }
        public bool IsQuarantined { get; set; }
        public string? ContentExcerpt { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    public interface IProvenanceLedger
    {
        Task RecordGenerationAsync(string agentId, string prompt, string content, double score, bool quarantined);
        Task<ProvenanceRecord?> GetRecordAsync(string generationId);
    }

    public class ProvenanceLedger : IProvenanceLedger
    {
        private readonly ConcurrentQueue<ProvenanceRecord> _ledger = new();
        // In a real app, this would be a database. For V1, ephemeral is fine.

        public Task RecordGenerationAsync(string agentId, string prompt, string content, double score, bool quarantined)
        {
            var record = new ProvenanceRecord
            {
                SourceAgent = agentId,
                PromptHash = prompt.GetHashCode().ToString(),
                ValidationScore = score,
                IsQuarantined = quarantined,
                ContentExcerpt = content.Length > 50 ? content.Substring(0, 50) + "..." : content
            };

            _ledger.Enqueue(record);
            return Task.CompletedTask;
        }

        public Task<ProvenanceRecord?> GetRecordAsync(string generationId)
        {
            // Linear search for now
            foreach (var rec in _ledger)
            {
                if (rec.GenerationId == generationId) return Task.FromResult<ProvenanceRecord?>(rec);
            }
            return Task.FromResult<ProvenanceRecord?>(null);
        }
    }
}
