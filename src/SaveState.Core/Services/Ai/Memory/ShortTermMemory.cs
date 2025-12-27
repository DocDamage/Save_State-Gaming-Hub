using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Core.Services.Ai.Memory
{
    /// <summary>
    /// Session-scoped memory for current playthrough context.
    /// - Rolling context window (last N interactions)
    /// - Input/output pairs with timestamps
    /// - Ephemeral - cleared on session end
    /// - Fast in-memory dictionary storage
    /// </summary>
    public class MemoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Context { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public int TokenCount { get; set; }
    }

    public class ShortTermMemoryConfig
    {
        public int MaxEntries { get; set; } = 50;
        public int MaxTokens { get; set; } = 4000;
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(2);
        public bool AutoPruneOnAdd { get; set; } = true;
    }

    public interface IShortTermMemory
    {
        void Add(string input, string output, string? context = null, Dictionary<string, object>? metadata = null);
        IEnumerable<MemoryEntry> GetRecent(int count);
        IEnumerable<MemoryEntry> GetByContext(string context);
        IEnumerable<MemoryEntry> Search(string query, int maxResults = 10);
        string BuildContextWindow(int maxTokens);
        void Clear();
        void StartNewSession();
        IEnumerable<MemoryEntry> GetPromotionCandidates(float significanceThreshold = 0.7f);
        int Count { get; }
        string SessionId { get; }
    }

    public class ShortTermMemory : IShortTermMemory
    {
        private readonly LinkedList<MemoryEntry> _entries = new();
        private readonly Dictionary<string, List<MemoryEntry>> _contextIndex = new();
        private readonly ShortTermMemoryConfig _config;
        private string _sessionId = Guid.NewGuid().ToString();
        private DateTime _sessionStart = DateTime.UtcNow;
        private int _totalTokens = 0;

        public int Count => _entries.Count;
        public string SessionId => _sessionId;

        public ShortTermMemory(ShortTermMemoryConfig? config = null)
        {
            _config = config ?? new ShortTermMemoryConfig();
        }

        public void Add(string input, string output, string? context = null, Dictionary<string, object>? metadata = null)
        {
            // Check session timeout
            if (DateTime.UtcNow - _sessionStart > _config.SessionTimeout)
            {
                StartNewSession();
            }

            var tokenCount = EstimateTokens(input) + EstimateTokens(output);
            
            var entry = new MemoryEntry
            {
                Input = input,
                Output = output,
                Context = context ?? "general",
                Metadata = metadata ?? new Dictionary<string, object>(),
                TokenCount = tokenCount,
                Timestamp = DateTime.UtcNow
            };

            _entries.AddLast(entry);
            _totalTokens += tokenCount;

            // Index by context
            if (!_contextIndex.ContainsKey(entry.Context))
            {
                _contextIndex[entry.Context] = new List<MemoryEntry>();
            }
            _contextIndex[entry.Context].Add(entry);

            // Auto-prune if enabled
            if (_config.AutoPruneOnAdd)
            {
                PruneExcess();
            }
        }

        public IEnumerable<MemoryEntry> GetRecent(int count)
        {
            return _entries.Reverse().Take(count);
        }

        public IEnumerable<MemoryEntry> GetByContext(string context)
        {
            if (_contextIndex.TryGetValue(context, out var entries))
            {
                return entries.OrderByDescending(e => e.Timestamp);
            }
            return Enumerable.Empty<MemoryEntry>();
        }

        public IEnumerable<MemoryEntry> Search(string query, int maxResults = 10)
        {
            var queryLower = query.ToLowerInvariant();
            var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return _entries
                .Select(e => new
                {
                    Entry = e,
                    Score = CalculateRelevanceScore(e, queryTerms)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Entry.Timestamp)
                .Take(maxResults)
                .Select(x => x.Entry);
        }

        private float CalculateRelevanceScore(MemoryEntry entry, string[] queryTerms)
        {
            var content = $"{entry.Input} {entry.Output} {entry.Context}".ToLowerInvariant();
            float score = 0;

            foreach (var term in queryTerms)
            {
                if (content.Contains(term))
                {
                    score += 1.0f;
                    // Bonus for exact word match
                    if (content.Split(' ').Contains(term))
                    {
                        score += 0.5f;
                    }
                }
            }

            // Recency bonus (decay over time)
            var ageMinutes = (DateTime.UtcNow - entry.Timestamp).TotalMinutes;
            var recencyBonus = Math.Max(0, 1.0f - (float)(ageMinutes / 60.0));
            score += recencyBonus * 0.3f;

            return score;
        }

        public string BuildContextWindow(int maxTokens)
        {
            var contextBuilder = new System.Text.StringBuilder();
            var tokenCount = 0;

            foreach (var entry in _entries.Reverse())
            {
                var entryText = $"[{entry.Context}] User: {entry.Input}\nAssistant: {entry.Output}\n\n";
                var entryTokens = EstimateTokens(entryText);

                if (tokenCount + entryTokens > maxTokens)
                    break;

                contextBuilder.Insert(0, entryText);
                tokenCount += entryTokens;
            }

            return contextBuilder.ToString();
        }

        public void Clear()
        {
            _entries.Clear();
            _contextIndex.Clear();
            _totalTokens = 0;
        }

        public void StartNewSession()
        {
            Clear();
            _sessionId = Guid.NewGuid().ToString();
            _sessionStart = DateTime.UtcNow;
        }

        private void PruneExcess()
        {
            // Prune by count
            while (_entries.Count > _config.MaxEntries && _entries.First != null)
            {
                RemoveOldest();
            }

            // Prune by tokens
            while (_totalTokens > _config.MaxTokens && _entries.First != null)
            {
                RemoveOldest();
            }
        }

        private void RemoveOldest()
        {
            var oldest = _entries.First?.Value;
            if (oldest == null) return;

            _entries.RemoveFirst();
            _totalTokens -= oldest.TokenCount;

            if (_contextIndex.TryGetValue(oldest.Context, out var contextList))
            {
                contextList.Remove(oldest);
                if (contextList.Count == 0)
                {
                    _contextIndex.Remove(oldest.Context);
                }
            }
        }

        private int EstimateTokens(string text)
        {
            // Rough estimation: ~4 characters per token for English
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        /// <summary>
        /// Get entries eligible for promotion to episodic memory
        /// </summary>
        public IEnumerable<MemoryEntry> GetPromotionCandidates(float significanceThreshold = 0.7f)
        {
            return _entries.Where(e =>
            {
                // Criteria for promotion:
                // 1. Has significant metadata
                // 2. Longer interactions (more context)
                // 3. Contains decision points or important events
                var hasSignificantMetadata = e.Metadata.Count > 0;
                var isSubstantial = e.TokenCount > 100;
                var containsDecision = e.Input.Contains("choose", StringComparison.OrdinalIgnoreCase) ||
                                       e.Input.Contains("decide", StringComparison.OrdinalIgnoreCase) ||
                                       e.Output.Contains("consequence", StringComparison.OrdinalIgnoreCase);

                var score = (hasSignificantMetadata ? 0.3f : 0) +
                           (isSubstantial ? 0.3f : 0) +
                           (containsDecision ? 0.4f : 0);

                return score >= significanceThreshold;
            });
        }
    }
}
