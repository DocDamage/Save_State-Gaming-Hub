using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Memory
{
    /// <summary>
    /// Enhanced Short-Term Memory with advanced features:
    /// - Semantic similarity scoring
    /// - Importance-based retention
    /// - Automatic consolidation
    /// - Thread-safe operations
    /// - Memory decay simulation
    /// - Deduplication
    /// </summary>
    public class EnhancedMemoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Context { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public int TokenCount { get; set; }
        public float ImportanceScore { get; set; } = 0.5f;
        public float DecayRate { get; set; } = 0.1f;
        public int AccessCount { get; set; } = 0;
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
        public List<string> Keywords { get; set; } = new();
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public string? ParentMemoryId { get; set; }
        public List<string> RelatedMemoryIds { get; set; } = new();
        public MemoryType Type { get; set; } = MemoryType.Interaction;
        public bool IsConsolidated { get; set; } = false;
        public string? ConsolidatedIntoId { get; set; }
    }

    public enum MemoryType
    {
        Interaction,
        Decision,
        Discovery,
        Achievement,
        Failure,
        Emotional,
        System
    }

    public enum MemoryPriority
    {
        Critical = 100,    // Never auto-delete
        High = 75,         // Keep for extended periods
        Normal = 50,       // Standard retention
        Low = 25,          // Short retention
        Ephemeral = 0      // Can be deleted immediately when space needed
    }

    public class MemoryQueryOptions
    {
        public string Query { get; set; } = string.Empty;
        public int MaxResults { get; set; } = 10;
        public float MinRelevanceScore { get; set; } = 0.0f;
        public MemoryType? FilterType { get; set; }
        public string? FilterContext { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IncludeConsolidated { get; set; } = false;
        public bool BoostRecent { get; set; } = true;
        public bool BoostFrequent { get; set; } = true;
    }

    public class EnhancedMemoryQueryResult
    {
        public EnhancedMemoryEntry Entry { get; set; } = null!;
        public float RelevanceScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
        public List<string> MatchedTerms { get; set; } = new();
    }

    public class ConsolidationResult
    {
        public int MemoriesProcessed { get; set; }
        public int MemoriesConsolidated { get; set; }
        public int MemoriesPruned { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public interface IEnhancedShortTermMemory
    {
        Task<EnhancedMemoryEntry> AddAsync(string input, string output, MemoryAddOptions? options = null);
        Task<IEnumerable<EnhancedMemoryQueryResult>> QueryAsync(MemoryQueryOptions options);
        Task<EnhancedMemoryEntry?> GetByIdAsync(string id);
        Task<bool> UpdateImportanceAsync(string id, float newImportance);
        Task<bool> LinkMemoriesAsync(string id1, string id2);
        Task<ConsolidationResult> ConsolidateAsync(CancellationToken ct = default);
        Task<string> BuildContextWindowAsync(int maxTokens, MemoryQueryOptions? filter = null);
        Task SimulateDecayAsync();
        Task<int> PruneAsync(int targetCount);
        void Clear();
        void StartNewSession();
        int Count { get; }
        int ActiveCount { get; }
        string SessionId { get; }
        MemoryStatistics GetStatistics();
    }

    public class MemoryAddOptions
    {
        public string? Context { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public MemoryType Type { get; set; } = MemoryType.Interaction;
        public MemoryPriority Priority { get; set; } = MemoryPriority.Normal;
        public float? CustomImportance { get; set; }
        public string? ParentMemoryId { get; set; }
        public bool AutoExtractKeywords { get; set; } = true;
        public bool ComputeEmbedding { get; set; } = false;
    }

    public class MemoryStatistics
    {
        public int TotalMemories { get; set; }
        public int ActiveMemories { get; set; }
        public int ConsolidatedMemories { get; set; }
        public int TotalTokens { get; set; }
        public Dictionary<MemoryType, int> ByType { get; set; } = new();
        public Dictionary<string, int> ByContext { get; set; } = new();
        public DateTime OldestMemory { get; set; }
        public DateTime NewestMemory { get; set; }
        public float AverageImportance { get; set; }
        public int TotalAccesses { get; set; }
    }

    public class EnhancedShortTermMemory : IEnhancedShortTermMemory
    {
        private readonly ConcurrentDictionary<string, EnhancedMemoryEntry> _memories = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _contextIndex = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _keywordIndex = new();
        private readonly EnhancedMemoryConfig _config;
        private readonly SemaphoreSlim _consolidationLock = new(1, 1);
        private readonly Random _random = new();
        private string _sessionId = Guid.NewGuid().ToString();
        private DateTime _sessionStart = DateTime.UtcNow;
        private int _totalTokens = 0;

        public int Count => _memories.Count;
        public int ActiveCount => _memories.Values.Count(m => !m.IsConsolidated);
        public string SessionId => _sessionId;

        public EnhancedShortTermMemory(EnhancedMemoryConfig? config = null)
        {
            _config = config ?? new EnhancedMemoryConfig();
        }

        public async Task<EnhancedMemoryEntry> AddAsync(string input, string output, MemoryAddOptions? options = null)
        {
            options ??= new MemoryAddOptions();

            // Edge case: Handle empty inputs
            if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(output))
            {
                throw new ArgumentException("Both input and output cannot be empty");
            }

            // Edge case: Truncate extremely long inputs
            var sanitizedInput = SanitizeText(input, _config.MaxInputLength);
            var sanitizedOutput = SanitizeText(output, _config.MaxOutputLength);

            var tokenCount = EstimateTokens(sanitizedInput) + EstimateTokens(sanitizedOutput);

            // Edge case: Check for duplicate/near-duplicate
            if (_config.DeduplicationEnabled)
            {
                var existingDuplicate = await FindDuplicateAsync(sanitizedInput, sanitizedOutput);
                if (existingDuplicate != null)
                {
                    // Update existing instead of creating new
                    existingDuplicate.AccessCount++;
                    existingDuplicate.LastAccessed = DateTime.UtcNow;
                    existingDuplicate.ImportanceScore = Math.Min(1.0f, existingDuplicate.ImportanceScore + 0.1f);
                    return existingDuplicate;
                }
            }

            var entry = new EnhancedMemoryEntry
            {
                Input = sanitizedInput,
                Output = sanitizedOutput,
                Context = options.Context ?? "general",
                Metadata = options.Metadata ?? new Dictionary<string, object>(),
                TokenCount = tokenCount,
                Type = options.Type,
                ImportanceScore = options.CustomImportance ?? CalculateInitialImportance(options),
                DecayRate = CalculateDecayRate(options.Priority),
                ParentMemoryId = options.ParentMemoryId
            };

            // Extract keywords for fast retrieval
            if (options.AutoExtractKeywords)
            {
                entry.Keywords = ExtractKeywords($"{sanitizedInput} {sanitizedOutput}");
            }

            // Link to parent if specified
            if (!string.IsNullOrEmpty(options.ParentMemoryId) && 
                _memories.TryGetValue(options.ParentMemoryId, out var parent))
            {
                parent.RelatedMemoryIds.Add(entry.Id);
            }

            _memories[entry.Id] = entry;
            Interlocked.Add(ref _totalTokens, tokenCount);

            // Update indices
            UpdateIndices(entry);

            // Edge case: Auto-prune if over limit
            if (_memories.Count > _config.MaxEntries)
            {
                await PruneAsync(_config.MaxEntries - _config.PruneBuffer);
            }

            if (_totalTokens > _config.MaxTokens)
            {
                await PruneByTokensAsync(_config.MaxTokens - _config.TokenBuffer);
            }

            return entry;
        }

        public async Task<IEnumerable<EnhancedMemoryQueryResult>> QueryAsync(MemoryQueryOptions options)
        {
            // Edge case: Empty query
            if (string.IsNullOrWhiteSpace(options.Query) && options.FilterType == null && 
                options.FilterContext == null)
            {
                // Return most recent memories
                return _memories.Values
                    .Where(m => !m.IsConsolidated || options.IncludeConsolidated)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(options.MaxResults)
                    .Select(m => new EnhancedMemoryQueryResult
                    {
                        Entry = m,
                        RelevanceScore = 1.0f,
                        MatchReason = "recency"
                    });
            }

            var queryTerms = ExtractKeywords(options.Query);
            var results = new List<EnhancedMemoryQueryResult>();

            await Task.Run(() =>
            {
                foreach (var memory in _memories.Values)
                {
                    // Apply filters
                    if (memory.IsConsolidated && !options.IncludeConsolidated) continue;
                    if (options.FilterType.HasValue && memory.Type != options.FilterType.Value) continue;
                    if (!string.IsNullOrEmpty(options.FilterContext) && 
                        !memory.Context.Equals(options.FilterContext, StringComparison.OrdinalIgnoreCase)) continue;
                    if (options.FromDate.HasValue && memory.Timestamp < options.FromDate.Value) continue;
                    if (options.ToDate.HasValue && memory.Timestamp > options.ToDate.Value) continue;

                    var (score, matchedTerms, reason) = CalculateRelevance(memory, queryTerms, options);
                    
                    if (score >= options.MinRelevanceScore)
                    {
                        results.Add(new EnhancedMemoryQueryResult
                        {
                            Entry = memory,
                            RelevanceScore = score,
                            MatchReason = reason,
                            MatchedTerms = matchedTerms
                        });

                        // Update access stats
                        memory.AccessCount++;
                        memory.LastAccessed = DateTime.UtcNow;
                    }
                }
            });

            return results
                .OrderByDescending(r => r.RelevanceScore)
                .Take(options.MaxResults);
        }

        public Task<EnhancedMemoryEntry?> GetByIdAsync(string id)
        {
            _memories.TryGetValue(id, out var entry);
            if (entry != null)
            {
                entry.AccessCount++;
                entry.LastAccessed = DateTime.UtcNow;
            }
            return Task.FromResult(entry);
        }

        public Task<bool> UpdateImportanceAsync(string id, float newImportance)
        {
            if (_memories.TryGetValue(id, out var entry))
            {
                entry.ImportanceScore = Math.Clamp(newImportance, 0, 1);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> LinkMemoriesAsync(string id1, string id2)
        {
            if (_memories.TryGetValue(id1, out var entry1) && _memories.TryGetValue(id2, out var entry2))
            {
                if (!entry1.RelatedMemoryIds.Contains(id2))
                    entry1.RelatedMemoryIds.Add(id2);
                if (!entry2.RelatedMemoryIds.Contains(id1))
                    entry2.RelatedMemoryIds.Add(id1);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public async Task<ConsolidationResult> ConsolidateAsync(CancellationToken ct = default)
        {
            if (!await _consolidationLock.WaitAsync(0, ct))
            {
                // Already consolidating
                return new ConsolidationResult();
            }

            try
            {
                var startTime = DateTime.UtcNow;
                var result = new ConsolidationResult();

                // Group similar memories for consolidation
                var candidates = _memories.Values
                    .Where(m => !m.IsConsolidated && 
                               m.ImportanceScore < _config.ConsolidationThreshold &&
                               (DateTime.UtcNow - m.Timestamp).TotalMinutes > _config.MinAgeForConsolidationMinutes)
                    .GroupBy(m => m.Context)
                    .ToList();

                foreach (var group in candidates)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    var memories = group.OrderBy(m => m.Timestamp).ToList();
                    result.MemoriesProcessed += memories.Count;

                    if (memories.Count < 2) continue;

                    // Find clusters of similar memories
                    var clusters = ClusterSimilarMemories(memories);
                    
                    foreach (var cluster in clusters.Where(c => c.Count >= 2))
                    {
                        var consolidated = ConsolidateCluster(cluster);
                        _memories[consolidated.Id] = consolidated;
                        
                        foreach (var original in cluster)
                        {
                            original.IsConsolidated = true;
                            original.ConsolidatedIntoId = consolidated.Id;
                        }
                        
                        result.MemoriesConsolidated += cluster.Count;
                    }
                }

                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }
            finally
            {
                _consolidationLock.Release();
            }
        }

        public async Task<string> BuildContextWindowAsync(int maxTokens, MemoryQueryOptions? filter = null)
        {
            var memories = filter != null 
                ? (await QueryAsync(filter)).Select(r => r.Entry)
                : _memories.Values.Where(m => !m.IsConsolidated).OrderByDescending(m => m.Timestamp);

            var contextBuilder = new System.Text.StringBuilder();
            var tokenCount = 0;

            foreach (var entry in memories)
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

        public Task SimulateDecayAsync()
        {
            var now = DateTime.UtcNow;
            
            foreach (var memory in _memories.Values.Where(m => !m.IsConsolidated))
            {
                var hoursSinceCreation = (now - memory.Timestamp).TotalHours;
                var hoursSinceAccess = (now - memory.LastAccessed).TotalHours;
                
                // Decay formula: importance decreases over time but access resets decay
                var decay = memory.DecayRate * (float)(hoursSinceAccess / 24.0);
                var accessBonus = Math.Min(0.3f, memory.AccessCount * 0.05f);
                
                memory.ImportanceScore = Math.Max(0, memory.ImportanceScore - decay + accessBonus);
            }

            return Task.CompletedTask;
        }

        public Task<int> PruneAsync(int targetCount)
        {
            if (_memories.Count <= targetCount) return Task.FromResult(0);

            var toRemove = _memories.Values
                .Where(m => m.ImportanceScore < _config.CriticalImportanceThreshold)
                .OrderBy(m => m.ImportanceScore)
                .ThenBy(m => m.LastAccessed)
                .Take(_memories.Count - targetCount)
                .ToList();

            var removed = 0;
            foreach (var memory in toRemove)
            {
                if (_memories.TryRemove(memory.Id, out var removed_m))
                {
                    Interlocked.Add(ref _totalTokens, -removed_m.TokenCount);
                    RemoveFromIndices(removed_m);
                    removed++;
                }
            }

            return Task.FromResult(removed);
        }

        private Task<int> PruneByTokensAsync(int targetTokens)
        {
            var removed = 0;
            while (_totalTokens > targetTokens && _memories.Count > 0)
            {
                var victim = _memories.Values
                    .Where(m => m.ImportanceScore < _config.CriticalImportanceThreshold)
                    .OrderBy(m => m.ImportanceScore)
                    .FirstOrDefault();

                if (victim != null && _memories.TryRemove(victim.Id, out var removed_m))
                {
                    Interlocked.Add(ref _totalTokens, -removed_m.TokenCount);
                    RemoveFromIndices(removed_m);
                    removed++;
                }
                else break;
            }
            return Task.FromResult(removed);
        }

        public void Clear()
        {
            _memories.Clear();
            _contextIndex.Clear();
            _keywordIndex.Clear();
            _totalTokens = 0;
        }

        public void StartNewSession()
        {
            // Archive current session memories
            foreach (var memory in _memories.Values)
            {
                memory.Metadata["previous_session"] = _sessionId;
                memory.ImportanceScore *= 0.5f; // Reduce importance of old session memories
            }
            
            _sessionId = Guid.NewGuid().ToString();
            _sessionStart = DateTime.UtcNow;
        }

        public MemoryStatistics GetStatistics()
        {
            var active = _memories.Values.Where(m => !m.IsConsolidated).ToList();
            return new MemoryStatistics
            {
                TotalMemories = _memories.Count,
                ActiveMemories = active.Count,
                ConsolidatedMemories = _memories.Count - active.Count,
                TotalTokens = _totalTokens,
                ByType = active.GroupBy(m => m.Type).ToDictionary(g => g.Key, g => g.Count()),
                ByContext = active.GroupBy(m => m.Context).ToDictionary(g => g.Key, g => g.Count()),
                OldestMemory = active.Any() ? active.Min(m => m.Timestamp) : DateTime.UtcNow,
                NewestMemory = active.Any() ? active.Max(m => m.Timestamp) : DateTime.UtcNow,
                AverageImportance = active.Any() ? active.Average(m => m.ImportanceScore) : 0,
                TotalAccesses = active.Sum(m => m.AccessCount)
            };
        }

        // ============ Private Helper Methods ============

        private string SanitizeText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            // Remove control characters
            text = Regex.Replace(text, @"[\x00-\x1F\x7F]", " ");
            
            // Normalize whitespace
            text = Regex.Replace(text, @"\s+", " ").Trim();
            
            // Truncate if too long
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength - 3) + "...";
            }
            
            return text;
        }

        private Task<EnhancedMemoryEntry?> FindDuplicateAsync(string input, string output)
        {
            var inputHash = input.GetHashCode();

            return Task.FromResult(_memories.Values
                .Where(m => m.Input.GetHashCode() == inputHash)
                .FirstOrDefault(m =>
                    m.Input.Equals(input, StringComparison.OrdinalIgnoreCase) ||
                    CalculateSimilarity(m.Input, input) > _config.DuplicateThreshold));
        }

        private float CalculateInitialImportance(MemoryAddOptions options)
        {
            var baseImportance = options.Priority switch
            {
                MemoryPriority.Critical => 1.0f,
                MemoryPriority.High => 0.8f,
                MemoryPriority.Normal => 0.5f,
                MemoryPriority.Low => 0.3f,
                MemoryPriority.Ephemeral => 0.1f,
                _ => 0.5f
            };

            // Boost for certain memory types
            baseImportance += options.Type switch
            {
                MemoryType.Decision => 0.2f,
                MemoryType.Achievement => 0.3f,
                MemoryType.Discovery => 0.15f,
                _ => 0
            };

            return Math.Min(1.0f, baseImportance);
        }

        private float CalculateDecayRate(MemoryPriority priority)
        {
            return priority switch
            {
                MemoryPriority.Critical => 0.0f,
                MemoryPriority.High => 0.02f,
                MemoryPriority.Normal => 0.05f,
                MemoryPriority.Low => 0.15f,
                MemoryPriority.Ephemeral => 0.3f,
                _ => 0.1f
            };
        }

        private List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            var stopWords = new HashSet<string> { 
                "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
                "have", "has", "had", "do", "does", "did", "will", "would", "could",
                "should", "may", "might", "must", "can", "this", "that", "these",
                "those", "i", "you", "he", "she", "it", "we", "they", "what", "which",
                "who", "whom", "where", "when", "why", "how", "and", "or", "but",
                "if", "then", "else", "for", "of", "to", "from", "in", "on", "at"
            };

            return text.ToLowerInvariant()
                .Split(new[] { ' ', ',', '.', '!', '?', '-', '_', ':', ';', '"', '\'' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .Distinct()
                .Take(20)
                .ToList();
        }

        private void UpdateIndices(EnhancedMemoryEntry entry)
        {
            // Context index
            _contextIndex.AddOrUpdate(
                entry.Context.ToLowerInvariant(),
                new HashSet<string> { entry.Id },
                (_, existing) => { existing.Add(entry.Id); return existing; }
            );

            // Keyword index
            foreach (var keyword in entry.Keywords)
            {
                _keywordIndex.AddOrUpdate(
                    keyword,
                    new HashSet<string> { entry.Id },
                    (_, existing) => { existing.Add(entry.Id); return existing; }
                );
            }
        }

        private void RemoveFromIndices(EnhancedMemoryEntry entry)
        {
            if (_contextIndex.TryGetValue(entry.Context.ToLowerInvariant(), out var contextSet))
            {
                contextSet.Remove(entry.Id);
            }

            foreach (var keyword in entry.Keywords)
            {
                if (_keywordIndex.TryGetValue(keyword, out var keywordSet))
                {
                    keywordSet.Remove(entry.Id);
                }
            }
        }

        private (float score, List<string> matchedTerms, string reason) CalculateRelevance(
            EnhancedMemoryEntry memory, List<string> queryTerms, MemoryQueryOptions options)
        {
            float score = 0;
            var matchedTerms = new List<string>();
            var reasons = new List<string>();

            // Keyword matching
            var keywordMatches = memory.Keywords.Intersect(queryTerms).ToList();
            if (keywordMatches.Any())
            {
                score += keywordMatches.Count * 0.3f;
                matchedTerms.AddRange(keywordMatches);
                reasons.Add("keywords");
            }

            // Content matching
            var contentLower = $"{memory.Input} {memory.Output}".ToLowerInvariant();
            var contentMatches = queryTerms.Where(t => contentLower.Contains(t)).ToList();
            score += contentMatches.Count * 0.2f;
            matchedTerms.AddRange(contentMatches.Except(keywordMatches));
            if (contentMatches.Any()) reasons.Add("content");

            // Importance bonus
            score += memory.ImportanceScore * 0.2f;

            // Recency bonus
            if (options.BoostRecent)
            {
                var hoursSinceCreation = (DateTime.UtcNow - memory.Timestamp).TotalHours;
                var recencyBonus = Math.Max(0, 1.0f - (float)(hoursSinceCreation / 24.0)) * 0.2f;
                score += recencyBonus;
                if (recencyBonus > 0.1f) reasons.Add("recent");
            }

            // Access frequency bonus
            if (options.BoostFrequent && memory.AccessCount > 0)
            {
                var frequencyBonus = Math.Min(0.2f, memory.AccessCount * 0.02f);
                score += frequencyBonus;
                if (frequencyBonus > 0.05f) reasons.Add("frequent");
            }

            return (Math.Min(1.0f, score), matchedTerms.Distinct().ToList(), string.Join("+", reasons));
        }

        private float CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2)) return 0;
            
            var words1 = new HashSet<string>(text1.ToLowerInvariant().Split(' '));
            var words2 = new HashSet<string>(text2.ToLowerInvariant().Split(' '));
            
            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();
            
            return union > 0 ? (float)intersection / union : 0;
        }

        private List<List<EnhancedMemoryEntry>> ClusterSimilarMemories(List<EnhancedMemoryEntry> memories)
        {
            var clusters = new List<List<EnhancedMemoryEntry>>();
            var assigned = new HashSet<string>();

            foreach (var memory in memories)
            {
                if (assigned.Contains(memory.Id)) continue;

                var cluster = new List<EnhancedMemoryEntry> { memory };
                assigned.Add(memory.Id);

                foreach (var other in memories)
                {
                    if (assigned.Contains(other.Id)) continue;
                    
                    var similarity = CalculateSimilarity(memory.Input, other.Input);
                    if (similarity > _config.ClusteringSimilarityThreshold)
                    {
                        cluster.Add(other);
                        assigned.Add(other.Id);
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;
        }

        private EnhancedMemoryEntry ConsolidateCluster(List<EnhancedMemoryEntry> cluster)
        {
            // Create a consolidated summary
            var consolidated = new EnhancedMemoryEntry
            {
                Context = cluster.First().Context,
                Type = cluster.First().Type,
                Input = $"[Consolidated from {cluster.Count} interactions]",
                Output = string.Join(" | ", cluster.Select(m => 
                    m.Output.Length > 50 ? m.Output.Substring(0, 50) + "..." : m.Output)),
                ImportanceScore = cluster.Average(m => m.ImportanceScore) + 0.1f,
                AccessCount = cluster.Sum(m => m.AccessCount),
                Keywords = cluster.SelectMany(m => m.Keywords).Distinct().ToList(),
                RelatedMemoryIds = cluster.Select(m => m.Id).ToList()
            };

            consolidated.TokenCount = EstimateTokens(consolidated.Input) + EstimateTokens(consolidated.Output);
            
            return consolidated;
        }

        private int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (int)Math.Ceiling(text.Length / 4.0);
        }
    }

    public class EnhancedMemoryConfig
    {
        public int MaxEntries { get; set; } = 500;
        public int MaxTokens { get; set; } = 50000;
        public int MaxInputLength { get; set; } = 5000;
        public int MaxOutputLength { get; set; } = 10000;
        public int PruneBuffer { get; set; } = 50;
        public int TokenBuffer { get; set; } = 5000;
        public bool DeduplicationEnabled { get; set; } = true;
        public float DuplicateThreshold { get; set; } = 0.85f;
        public float ConsolidationThreshold { get; set; } = 0.3f;
        public float CriticalImportanceThreshold { get; set; } = 0.9f;
        public float ClusteringSimilarityThreshold { get; set; } = 0.6f;
        public int MinAgeForConsolidationMinutes { get; set; } = 30;
    }
}
