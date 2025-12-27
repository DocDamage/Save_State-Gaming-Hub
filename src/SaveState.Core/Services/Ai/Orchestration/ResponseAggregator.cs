using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Orchestration
{
    /// <summary>
    /// Combines multi-agent outputs coherently.
    /// - Conflict resolution
    /// - Tone normalization  
    /// - Final formatting
    /// </summary>
    public class AgentOutput
    {
        public string AgentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public IntentCategory Intent { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class AggregatedResponse
    {
        public string FinalContent { get; set; } = string.Empty;
        public List<AgentOutput> SourceOutputs { get; set; } = new();
        public bool HadConflicts { get; set; }
        public string? ConflictResolutionNote { get; set; }
        public float OverallConfidence { get; set; }
    }

    public interface IResponseAggregator
    {
        Task<AggregatedResponse> AggregateAsync(List<AgentOutput> outputs);
        Task<string> NormalizeTone(string content, string targetTone);
    }

    public class ResponseAggregator : IResponseAggregator
    {
        private readonly ILlmService? _llmService;

        public ResponseAggregator(ILlmService? llmService = null)
        {
            _llmService = llmService;
        }

        public Task<AggregatedResponse> AggregateAsync(List<AgentOutput> outputs)
        {
            if (outputs.Count == 0)
                return Task.FromResult(new AggregatedResponse { FinalContent = "" });

            if (outputs.Count == 1)
            {
                return Task.FromResult(new AggregatedResponse
                {
                    FinalContent = outputs[0].Content,
                    SourceOutputs = outputs,
                    OverallConfidence = outputs[0].Confidence
                });
            }

            // Sort by confidence
            var sorted = outputs.OrderByDescending(o => o.Confidence).ToList();
            
            // Detect conflicts
            var conflicts = DetectConflicts(sorted);
            
            // Merge outputs
            var merged = MergeOutputs(sorted, conflicts);

            return Task.FromResult(new AggregatedResponse
            {
                FinalContent = merged,
                SourceOutputs = outputs,
                HadConflicts = conflicts.Count > 0,
                ConflictResolutionNote = conflicts.Count > 0
                    ? $"Resolved {conflicts.Count} conflict(s)" : null,
                OverallConfidence = sorted.Average(o => o.Confidence)
            });
        }

        private List<(int, int, string)> DetectConflicts(List<AgentOutput> outputs)
        {
            var conflicts = new List<(int, int, string)>();
            
            for (int i = 0; i < outputs.Count; i++)
            {
                for (int j = i + 1; j < outputs.Count; j++)
                {
                    var conflict = FindContradiction(outputs[i].Content, outputs[j].Content);
                    if (conflict != null)
                    {
                        conflicts.Add((i, j, conflict));
                    }
                }
            }
            
            return conflicts;
        }

        private string? FindContradiction(string a, string b)
        {
            // Simple contradiction detection
            var aLower = a.ToLowerInvariant();
            var bLower = b.ToLowerInvariant();
            
            var negations = new[] { ("is", "is not"), ("can", "cannot"), ("has", "has no") };
            foreach (var (pos, neg) in negations)
            {
                if (aLower.Contains(pos) && bLower.Contains(neg) ||
                    aLower.Contains(neg) && bLower.Contains(pos))
                {
                    return $"Potential contradiction between statements";
                }
            }
            
            return null;
        }

        private string MergeOutputs(List<AgentOutput> outputs, List<(int, int, string)> conflicts)
        {
            // Group by intent type
            var grouped = outputs.GroupBy(o => o.Intent).ToList();
            
            if (grouped.Count == 1)
            {
                // Same intent - take highest confidence
                return outputs[0].Content;
            }

            // Different intents - combine intelligently
            var sb = new StringBuilder();
            foreach (var group in grouped.OrderByDescending(g => g.Max(o => o.Confidence)))
            {
                var best = group.OrderByDescending(o => o.Confidence).First();
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(best.Content);
            }

            return sb.ToString();
        }

        public Task<string> NormalizeTone(string content, string targetTone)
        {
            // Simple tone adjustments without LLM
            return Task.FromResult(targetTone switch
            {
                "formal" => content.Replace("gonna", "going to").Replace("wanna", "want to"),
                "casual" => content,
                "urgent" => content + "!",
                _ => content
            });
        }
    }
}
