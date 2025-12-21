using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Memory
{
    /// <summary>
    /// Unified interface combining all memory layers.
    /// - Query routing to appropriate layer
    /// - Memory consolidation (short → episodic promotion)
    /// - Context assembly for LLM prompts
    /// </summary>
    public enum MemoryLayer
    {
        ShortTerm,
        Episodic,
        Canonical,
        All
    }

    public class MemoryQueryResult
    {
        public string Content { get; set; } = string.Empty;
        public MemoryLayer Source { get; set; }
        public float Relevance { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ConsolidatedContext
    {
        public string ShortTermContext { get; set; } = string.Empty;
        public string EpisodicContext { get; set; } = string.Empty;
        public string CanonicalContext { get; set; } = string.Empty;
        public string CombinedPromptInjection { get; set; } = string.Empty;
        public int TotalTokenEstimate { get; set; }
    }

    public class MemoryOrchestratorConfig
    {
        public int MaxShortTermTokens { get; set; } = 2000;
        public int MaxEpisodicTokens { get; set; } = 1500;
        public int MaxCanonicalTokens { get; set; } = 1000;
        public float PromotionThreshold { get; set; } = 0.7f;
        public int ConsolidationIntervalMinutes { get; set; } = 30;
        public bool AutoConsolidate { get; set; } = true;
    }

    public interface IMemoryOrchestrator
    {
        Task RecordInteraction(string input, string output, string? context = null,
            Dictionary<string, object>? metadata = null);
        Task<IEnumerable<MemoryQueryResult>> Query(string query, MemoryLayer layer = MemoryLayer.All, int maxResults = 10);
        Task<ConsolidatedContext> BuildContext(string currentQuery, List<string>? relevantTopics = null);
        Task ConsolidateMemories();
        Task<bool> ValidateAgainstCanon(string statement);
        Task AddCanonicalFact(string statement, FactCategory category, string source);
        IShortTermMemory ShortTerm { get; }
        IEpisodicMemory Episodic { get; }
        ICanonicalMemory Canonical { get; }
    }

    public class MemoryOrchestrator : IMemoryOrchestrator
    {
        private readonly IShortTermMemory _shortTerm;
        private readonly IEpisodicMemory _episodic;
        private readonly ICanonicalMemory _canonical;
        private readonly MemoryOrchestratorConfig _config;
        private DateTime _lastConsolidation = DateTime.UtcNow;

        public IShortTermMemory ShortTerm => _shortTerm;
        public IEpisodicMemory Episodic => _episodic;
        public ICanonicalMemory Canonical => _canonical;

        public MemoryOrchestrator(
            IShortTermMemory? shortTerm = null,
            IEpisodicMemory? episodic = null,
            ICanonicalMemory? canonical = null,
            MemoryOrchestratorConfig? config = null)
        {
            _shortTerm = shortTerm ?? new ShortTermMemory();
            _episodic = episodic ?? new EpisodicMemory();
            _canonical = canonical ?? new CanonicalMemory();
            _config = config ?? new MemoryOrchestratorConfig();
        }

        public async Task RecordInteraction(string input, string output, string? context = null,
            Dictionary<string, object>? metadata = null)
        {
            // Always store in short-term memory
            _shortTerm.Add(input, output, context, metadata);

            // Check if auto-consolidation is due
            if (_config.AutoConsolidate && 
                (DateTime.UtcNow - _lastConsolidation).TotalMinutes >= _config.ConsolidationIntervalMinutes)
            {
                await ConsolidateMemories();
            }
        }

        public async Task<IEnumerable<MemoryQueryResult>> Query(string query, MemoryLayer layer = MemoryLayer.All, int maxResults = 10)
        {
            var results = new List<MemoryQueryResult>();

            if (layer == MemoryLayer.All || layer == MemoryLayer.ShortTerm)
            {
                var shortTermResults = _shortTerm.Search(query, maxResults);
                results.AddRange(shortTermResults.Select(e => new MemoryQueryResult
                {
                    Content = $"User: {e.Input}\nAssistant: {e.Output}",
                    Source = MemoryLayer.ShortTerm,
                    Relevance = 0.8f, // Recent is considered relevant
                    Timestamp = e.Timestamp,
                    Metadata = e.Metadata
                }));
            }

            if (layer == MemoryLayer.All || layer == MemoryLayer.Episodic)
            {
                var episodicResults = await _episodic.SemanticSearch(query, maxResults);
                results.AddRange(episodicResults.Select(e => new MemoryQueryResult
                {
                    Content = $"[{e.Emotion}] Event: {e.Event}\nContext: {e.Context}\nOutcome: {e.Outcome}",
                    Source = MemoryLayer.Episodic,
                    Relevance = e.Significance,
                    Timestamp = e.Timestamp,
                    Metadata = e.WorldState.ToDictionary(k => k.Key, v => v.Value)
                }));
            }

            if (layer == MemoryLayer.All || layer == MemoryLayer.Canonical)
            {
                var canonicalResults = await _canonical.Query(query);
                results.AddRange(canonicalResults.Take(maxResults).Select(f => new MemoryQueryResult
                {
                    Content = f.Statement,
                    Source = MemoryLayer.Canonical,
                    Relevance = f.Confidence,
                    Timestamp = f.CreatedAt,
                    Metadata = new Dictionary<string, object>
                    {
                        ["category"] = f.Category.ToString(),
                        ["tags"] = f.Tags
                    }
                }));
            }

            // Sort by relevance and recency
            return results
                .OrderByDescending(r => r.Relevance * GetRecencyWeight(r.Timestamp))
                .Take(maxResults);
        }

        public async Task<ConsolidatedContext> BuildContext(string currentQuery, List<string>? relevantTopics = null)
        {
            var context = new ConsolidatedContext();
            var topics = relevantTopics ?? ExtractTopics(currentQuery);

            // Build short-term context (recent conversation)
            context.ShortTermContext = _shortTerm.BuildContextWindow(_config.MaxShortTermTokens);

            // Build episodic context (relevant memories)
            var episodicMemories = await _episodic.SemanticSearch(currentQuery, 5);
            var episodicBuilder = new StringBuilder();
            episodicBuilder.AppendLine("=== Relevant Memories ===");
            foreach (var memory in episodicMemories)
            {
                episodicBuilder.AppendLine($"• [{memory.Emotion}] {memory.Event} → {memory.Outcome}");
            }
            context.EpisodicContext = episodicBuilder.ToString();

            // Build canonical context (lore and rules)
            context.CanonicalContext = await _canonical.BuildLoreContext(topics, 10);

            // Combine all contexts with proper structure
            var combinedBuilder = new StringBuilder();
            
            // Canonical first (highest priority - ground truth)
            if (!string.IsNullOrEmpty(context.CanonicalContext))
            {
                combinedBuilder.AppendLine(context.CanonicalContext);
                combinedBuilder.AppendLine();
            }

            // Episodic second (relevant past events)
            if (!string.IsNullOrEmpty(context.EpisodicContext))
            {
                combinedBuilder.AppendLine(context.EpisodicContext);
                combinedBuilder.AppendLine();
            }

            // Short-term last (recent conversation)
            if (!string.IsNullOrEmpty(context.ShortTermContext))
            {
                combinedBuilder.AppendLine("=== Recent Conversation ===");
                combinedBuilder.AppendLine(context.ShortTermContext);
            }

            context.CombinedPromptInjection = combinedBuilder.ToString();
            context.TotalTokenEstimate = EstimateTokens(context.CombinedPromptInjection);

            return context;
        }

        public async Task ConsolidateMemories()
        {
            _lastConsolidation = DateTime.UtcNow;

            // Get candidates for promotion from short-term to episodic
            var candidates = _shortTerm.GetPromotionCandidates(_config.PromotionThreshold);

            foreach (var candidate in candidates)
            {
                // Determine emotion based on content analysis
                var emotion = AnalyzeEmotion(candidate.Output);
                
                // Calculate significance
                var significance = CalculateSignificance(candidate);

                // Promote to episodic memory
                await _episodic.PromoteFromShortTerm(candidate, emotion, significance);
            }
        }

        public async Task<bool> ValidateAgainstCanon(string statement)
        {
            var result = await _canonical.ValidateStatement(statement);
            return result.IsValid;
        }

        public async Task AddCanonicalFact(string statement, FactCategory category, string source)
        {
            await _canonical.AddFact(statement, category, source);
        }

        private List<string> ExtractTopics(string query)
        {
            // Simple topic extraction - extract nouns and key phrases
            var words = query.ToLowerInvariant()
                .Split(new[] { ' ', '.', ',', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .Distinct()
                .ToList();

            // Filter out common words
            var stopWords = new HashSet<string> { "what", "when", "where", "which", "that", "this", "have", "from", "about" };
            return words.Where(w => !stopWords.Contains(w)).Take(5).ToList();
        }

        private EmotionalTone AnalyzeEmotion(string text)
        {
            var textLower = text.ToLowerInvariant();

            // Simple keyword-based emotion detection
            if (textLower.Contains("victory") || textLower.Contains("won") || textLower.Contains("success"))
                return EmotionalTone.Victory;
            if (textLower.Contains("defeat") || textLower.Contains("lost") || textLower.Contains("failed"))
                return EmotionalTone.Defeat;
            if (textLower.Contains("happy") || textLower.Contains("joy") || textLower.Contains("excited"))
                return EmotionalTone.Joy;
            if (textLower.Contains("sad") || textLower.Contains("grief") || textLower.Contains("sorrow"))
                return EmotionalTone.Sadness;
            if (textLower.Contains("fear") || textLower.Contains("scared") || textLower.Contains("terrified"))
                return EmotionalTone.Fear;
            if (textLower.Contains("tense") || textLower.Contains("danger") || textLower.Contains("critical"))
                return EmotionalTone.Tense;
            if (textLower.Contains("curious") || textLower.Contains("wonder") || textLower.Contains("discover"))
                return EmotionalTone.Curiosity;
            if (textLower.Contains("good") || textLower.Contains("great") || textLower.Contains("positive"))
                return EmotionalTone.Positive;
            if (textLower.Contains("bad") || textLower.Contains("terrible") || textLower.Contains("negative"))
                return EmotionalTone.Negative;

            return EmotionalTone.Neutral;
        }

        private float CalculateSignificance(MemoryEntry entry)
        {
            float significance = 0.5f;

            // Length indicates depth of interaction
            if (entry.TokenCount > 200) significance += 0.2f;
            if (entry.TokenCount > 500) significance += 0.1f;

            // Metadata presence indicates importance
            if (entry.Metadata.Count > 0) significance += 0.1f;

            // Context-aware boosting
            var importantContexts = new[] { "quest", "decision", "combat", "story" };
            if (importantContexts.Any(c => entry.Context.Contains(c, StringComparison.OrdinalIgnoreCase)))
            {
                significance += 0.1f;
            }

            return Math.Min(1.0f, significance);
        }

        private float GetRecencyWeight(DateTime timestamp)
        {
            var hoursSince = (DateTime.UtcNow - timestamp).TotalHours;
            return (float)Math.Pow(0.95, hoursSince / 24); // Decay over days
        }

        private int EstimateTokens(string text)
        {
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        /// <summary>
        /// Get a summary of current memory state
        /// </summary>
        public string GetMemoryStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Memory Status ===");
            sb.AppendLine($"Short-Term: {_shortTerm.Count} entries (Session: {_shortTerm.SessionId[..8]}...)");
            sb.AppendLine($"Episodic: {_episodic.Count} episodes");
            sb.AppendLine($"Canonical: {_canonical.Count} facts");
            sb.AppendLine($"Last Consolidation: {_lastConsolidation:HH:mm:ss}");
            return sb.ToString();
        }
    }
}
