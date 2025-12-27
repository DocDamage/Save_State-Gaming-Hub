using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Core.Services.Ai.Uncertainty
{
    /// <summary>
    /// Estimates AI confidence in outputs.
    /// - Token probability analysis (simulated)
    /// - Knowledge base coverage check
    /// - Returns confidence 0-1
    /// </summary>
    public class ConfidenceAssessment
    {
        public float OverallConfidence { get; set; }
        public float KnowledgeCoverage { get; set; }
        public float ResponseQuality { get; set; }
        public float TopicRelevance { get; set; }
        public List<string> UncertainAreas { get; set; } = new();
        public string ConfidenceLevel { get; set; } = "medium";
    }

    public interface IConfidenceScorer
    {
        ConfidenceAssessment Score(string output, ConfidenceContext context);
        float ScoreKnowledgeCoverage(string query, IEnumerable<string> knowledgeBase);
        string GetConfidenceLevel(float score);
    }

    public class ConfidenceContext
    {
        public string? OriginalQuery { get; set; }
        public List<string>? KnowledgeBaseHits { get; set; }
        public int? KnowledgeBaseSize { get; set; }
        public string? ExpectedFormat { get; set; }
        public Dictionary<string, object>? WorldState { get; set; }
    }

    public class ConfidenceScorer : IConfidenceScorer
    {
        private readonly HashSet<string> _uncertaintyIndicators = new()
        {
            "perhaps", "maybe", "might", "possibly", "could be", "unclear",
            "uncertain", "unknown", "not sure", "it seems", "appears to",
            "legend says", "rumored", "some believe", "allegedly"
        };

        private readonly HashSet<string> _confidenceIndicators = new()
        {
            "definitely", "certainly", "always", "never", "absolutely",
            "without doubt", "confirmed", "established", "proven", "known"
        };

        public ConfidenceAssessment Score(string output, ConfidenceContext context)
        {
            var assessment = new ConfidenceAssessment();

            // Knowledge coverage
            assessment.KnowledgeCoverage = context.KnowledgeBaseHits?.Count > 0 && context.KnowledgeBaseSize > 0
                ? Math.Min(1.0f, (float)context.KnowledgeBaseHits.Count / 5) // Expect ~5 relevant hits
                : 0.5f;

            // Response quality (length, structure)
            assessment.ResponseQuality = ScoreResponseQuality(output, context);

            // Topic relevance
            assessment.TopicRelevance = context.OriginalQuery != null
                ? ScoreRelevance(output, context.OriginalQuery)
                : 0.7f;

            // Check for uncertainty language
            var (uncertaintyPenalty, uncertainAreas) = AnalyzeUncertainty(output);
            assessment.UncertainAreas = uncertainAreas;

            // Combine scores
            assessment.OverallConfidence = (
                assessment.KnowledgeCoverage * 0.3f +
                assessment.ResponseQuality * 0.3f +
                assessment.TopicRelevance * 0.4f
            ) * (1 - uncertaintyPenalty * 0.3f);

            assessment.ConfidenceLevel = GetConfidenceLevel(assessment.OverallConfidence);

            return assessment;
        }

        public float ScoreKnowledgeCoverage(string query, IEnumerable<string> knowledgeBase)
        {
            var queryTerms = query.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .ToHashSet();

            int totalHits = 0;
            foreach (var entry in knowledgeBase)
            {
                var entryLower = entry.ToLowerInvariant();
                var matches = queryTerms.Count(t => entryLower.Contains(t));
                if (matches >= 2) totalHits++;
            }

            return Math.Min(1.0f, totalHits / 3.0f);
        }

        public string GetConfidenceLevel(float score)
        {
            return score switch
            {
                >= 0.8f => "high",
                >= 0.5f => "medium",
                >= 0.3f => "low",
                _ => "very_low"
            };
        }

        private float ScoreResponseQuality(string output, ConfidenceContext context)
        {
            float score = 0.5f;

            // Length check
            if (output.Length > 100) score += 0.1f;
            if (output.Length > 300) score += 0.1f;
            if (output.Length < 20) score -= 0.3f;

            // Structure check
            if (output.Contains('.') && output.Split('.').Length > 1) score += 0.1f;
            if (output.Contains('\n')) score += 0.05f;

            // Format compliance
            if (!string.IsNullOrEmpty(context.ExpectedFormat))
            {
                if (context.ExpectedFormat == "list" && output.Contains("•")) score += 0.1f;
                if (context.ExpectedFormat == "narrative" && output.Length > 200) score += 0.1f;
            }

            return Math.Clamp(score, 0, 1);
        }

        private float ScoreRelevance(string output, string query)
        {
            var queryTerms = query.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .ToList();

            var outputLower = output.ToLowerInvariant();
            
            int matches = queryTerms.Count(t => outputLower.Contains(t));
            return queryTerms.Count > 0 ? (float)matches / queryTerms.Count : 0.5f;
        }

        private (float penalty, List<string> areas) AnalyzeUncertainty(string output)
        {
            var outputLower = output.ToLowerInvariant();
            var uncertainAreas = new List<string>();
            int uncertaintyCount = 0;

            foreach (var indicator in _uncertaintyIndicators)
            {
                if (outputLower.Contains(indicator))
                {
                    uncertaintyCount++;
                    
                    // Find the sentence containing uncertainty
                    var sentences = output.Split('.');
                    foreach (var sentence in sentences)
                    {
                        if (sentence.ToLowerInvariant().Contains(indicator))
                        {
                            uncertainAreas.Add(sentence.Trim());
                            break;
                        }
                    }
                }
            }

            // Check for confidence boosters
            int confidenceCount = 0;
            foreach (var indicator in _confidenceIndicators)
            {
                if (outputLower.Contains(indicator)) confidenceCount++;
            }

            float netUncertainty = (uncertaintyCount - confidenceCount * 0.5f) / 10f;
            return (Math.Clamp(netUncertainty, 0, 1), uncertainAreas.Distinct().Take(3).ToList());
        }
    }
}
