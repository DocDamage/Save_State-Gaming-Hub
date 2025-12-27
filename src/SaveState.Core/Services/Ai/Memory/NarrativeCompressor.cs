using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Memory
{
    /// <summary>
    /// Compresses narrative history to keep prompts small while preserving meaning.
    /// Essential for 100+ hour playthroughs without exploding context windows.
    /// </summary>
    public interface INarrativeCompressor
    {
        /// <summary>
        /// Compress a full narrative into a summary
        /// </summary>
        Task<CompressedNarrative> CompressAsync(NarrativeInput input);

        /// <summary>
        /// Get a player-aligned summary (what mattered to THIS player)
        /// </summary>
        Task<string> GetPlayerAlignedSummaryAsync(string playerId, NarrativeInput input);

        /// <summary>
        /// Get an emotional summary (preserving tone, not just facts)
        /// </summary>
        Task<string> GetEmotionalSummaryAsync(NarrativeInput input);

        /// <summary>
        /// Summarize a story arc
        /// </summary>
        Task<ArcSummary> SummarizeArcAsync(StoryArc arc);

        /// <summary>
        /// Get compression statistics
        /// </summary>
        CompressionStatistics GetStatistics();
    }

    /// <summary>
    /// Input for narrative compression
    /// </summary>
    public class NarrativeInput
    {
        public List<NarrativeEvent> Events { get; set; } = new();
        public string? PlayerId { get; set; }
        public int MaxOutputTokens { get; set; } = 500;
        public bool PreserveEmotionalTone { get; set; } = true;
        public bool IncludePlayerChoices { get; set; } = true;
        public List<string>? PrioritizedCharacters { get; set; }
        public List<string>? PrioritizedThemes { get; set; }
    }

    /// <summary>
    /// A single narrative event
    /// </summary>
    public class NarrativeEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public EmotionalTone Tone { get; set; } = EmotionalTone.Neutral;
        public double Significance { get; set; } = 0.5;
        public List<string> InvolvedCharacters { get; set; } = new();
        public string? Location { get; set; }
        public string? QuestId { get; set; }
        public bool WasPlayerChoice { get; set; } = false;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Note: EmotionalTone enum is defined in EpisodicMemory.cs

    /// <summary>
    /// A compressed narrative
    /// </summary>
    public class CompressedNarrative
    {
        public string Summary { get; set; } = string.Empty;
        public string EmotionalSummary { get; set; } = string.Empty;
        public List<string> KeyMoments { get; set; } = new();
        public List<string> ImportantCharacters { get; set; } = new();
        public List<string> UnresolvedPlots { get; set; } = new();
        public EmotionalTone DominantTone { get; set; }
        public int OriginalEventCount { get; set; }
        public int TokenEstimate { get; set; }
        public double CompressionRatio { get; set; }
    }

    /// <summary>
    /// A story arc for summarization
    /// </summary>
    public class StoryArc
    {
        public string ArcId { get; set; } = string.Empty;
        public string ArcName { get; set; } = string.Empty;
        public ArcStatus Status { get; set; } = ArcStatus.InProgress;
        public List<NarrativeEvent> Events { get; set; } = new();
        public string? Resolution { get; set; }
    }

    /// <summary>
    /// Status of a story arc
    /// </summary>
    public enum ArcStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        Abandoned
    }

    /// <summary>
    /// Summary of a story arc
    /// </summary>
    public class ArcSummary
    {
        public string ArcId { get; set; } = string.Empty;
        public string OneLiner { get; set; } = string.Empty;
        public string DetailedSummary { get; set; } = string.Empty;
        public EmotionalTone Tone { get; set; }
        public List<string> KeyDecisions { get; set; } = new();
        public List<string> Consequences { get; set; } = new();
    }

    /// <summary>
    /// Compression statistics
    /// </summary>
    public class CompressionStatistics
    {
        public long TotalEventsCompressed { get; set; }
        public long TotalTokensSaved { get; set; }
        public double AverageCompressionRatio { get; set; }
        public int CachedSummaries { get; set; }
    }

    /// <summary>
    /// Default implementation of narrative compressor
    /// </summary>
    public class NarrativeCompressor : INarrativeCompressor
    {
        private readonly ConcurrentDictionary<string, CompressedNarrative> _cache = new();
        private readonly Func<string, Task<string>>? _llmGenerator;
        
        private long _totalEventsCompressed = 0;
        private long _totalTokensSaved = 0;
        private readonly List<double> _compressionRatios = new();

        public NarrativeCompressor(Func<string, Task<string>>? llmGenerator = null)
        {
            _llmGenerator = llmGenerator;
        }

        public async Task<CompressedNarrative> CompressAsync(NarrativeInput input)
        {
            // Calculate cache key
            var cacheKey = GenerateCacheKey(input);
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            // Sort events by significance and filter
            var significantEvents = input.Events
                .OrderByDescending(e => e.Significance)
                .ThenByDescending(e => e.WasPlayerChoice ? 1 : 0)
                .Take(Math.Min(input.Events.Count, 50))
                .ToList();

            // Build summaries
            var keyMoments = ExtractKeyMoments(significantEvents);
            var characters = ExtractImportantCharacters(significantEvents, input.PrioritizedCharacters);
            var unresolved = ExtractUnresolvedPlots(significantEvents);
            var dominantTone = DetermineDominantTone(significantEvents);

            // Generate textual summary
            string summary;
            string emotionalSummary;

            if (_llmGenerator != null && input.Events.Count > 10)
            {
                var prompt = BuildSummaryPrompt(significantEvents, input.MaxOutputTokens);
                summary = await _llmGenerator(prompt);
                
                var emotionalPrompt = BuildEmotionalSummaryPrompt(significantEvents);
                emotionalSummary = await _llmGenerator(emotionalPrompt);
            }
            else
            {
                summary = BuildLocalSummary(significantEvents);
                emotionalSummary = BuildLocalEmotionalSummary(significantEvents, dominantTone);
            }

            // Calculate compression
            var originalTokens = EstimateTokens(input.Events.Sum(e => e.Description.Length));
            var compressedTokens = EstimateTokens(summary.Length + emotionalSummary.Length);
            var ratio = originalTokens > 0 ? (double)compressedTokens / originalTokens : 1;

            _totalEventsCompressed += input.Events.Count;
            _totalTokensSaved += originalTokens - compressedTokens;
            lock (_compressionRatios)
            {
                _compressionRatios.Add(ratio);
            }

            var result = new CompressedNarrative
            {
                Summary = summary,
                EmotionalSummary = emotionalSummary,
                KeyMoments = keyMoments,
                ImportantCharacters = characters,
                UnresolvedPlots = unresolved,
                DominantTone = dominantTone,
                OriginalEventCount = input.Events.Count,
                TokenEstimate = compressedTokens,
                CompressionRatio = ratio
            };

            _cache[cacheKey] = result;
            return result;
        }

        public async Task<string> GetPlayerAlignedSummaryAsync(string playerId, NarrativeInput input)
        {
            // Focus on player choices and their consequences
            var playerEvents = input.Events
                .Where(e => e.WasPlayerChoice)
                .OrderByDescending(e => e.Significance)
                .ToList();

            if (!playerEvents.Any())
            {
                return "Your journey has just begun.";
            }

            if (_llmGenerator != null)
            {
                var prompt = $"Summarize these player decisions and their impact from the player's perspective:\n" +
                             string.Join("\n", playerEvents.Select(e => $"- {e.Description}"));
                return await _llmGenerator(prompt);
            }

            return BuildPlayerAlignedLocalSummary(playerEvents);
        }

        public async Task<string> GetEmotionalSummaryAsync(NarrativeInput input)
        {
            var compressed = await CompressAsync(input);
            return compressed.EmotionalSummary;
        }

        public async Task<ArcSummary> SummarizeArcAsync(StoryArc arc)
        {
            var arcEvents = arc.Events.OrderBy(e => e.Timestamp).ToList();
            var keyDecisions = arcEvents.Where(e => e.WasPlayerChoice).Select(e => e.Description).ToList();
            var dominantTone = DetermineDominantTone(arcEvents);

            string oneLiner;
            string detailed;

            if (_llmGenerator != null && arcEvents.Count > 5)
            {
                var prompt = $"Create a one-line summary and a detailed paragraph for this story arc '{arc.ArcName}':\n" +
                             string.Join("\n", arcEvents.Take(20).Select(e => $"- {e.Description}"));
                var response = await _llmGenerator(prompt);
                
                var lines = response.Split('\n', 2);
                oneLiner = lines[0];
                detailed = lines.Length > 1 ? lines[1] : lines[0];
            }
            else
            {
                oneLiner = $"The {arc.ArcName} arc: {arc.Status}";
                detailed = BuildLocalArcSummary(arc, arcEvents);
            }

            return new ArcSummary
            {
                ArcId = arc.ArcId,
                OneLiner = oneLiner,
                DetailedSummary = detailed,
                Tone = dominantTone,
                KeyDecisions = keyDecisions.Take(5).ToList(),
                Consequences = ExtractConsequences(arcEvents)
            };
        }

        public CompressionStatistics GetStatistics()
        {
            double avgRatio;
            lock (_compressionRatios)
            {
                avgRatio = _compressionRatios.Any() ? _compressionRatios.Average() : 0;
            }

            return new CompressionStatistics
            {
                TotalEventsCompressed = _totalEventsCompressed,
                TotalTokensSaved = _totalTokensSaved,
                AverageCompressionRatio = avgRatio,
                CachedSummaries = _cache.Count
            };
        }

        private List<string> ExtractKeyMoments(List<NarrativeEvent> events)
        {
            return events
                .Where(e => e.Significance > 0.7 || e.WasPlayerChoice)
                .Take(10)
                .Select(e => e.Description)
                .ToList();
        }

        private List<string> ExtractImportantCharacters(List<NarrativeEvent> events, List<string>? prioritized)
        {
            var characterCounts = new Dictionary<string, int>();
            
            foreach (var evt in events)
            {
                foreach (var character in evt.InvolvedCharacters)
                {
                    characterCounts.TryGetValue(character, out var count);
                    characterCounts[character] = count + 1;
                }
            }

            var sorted = characterCounts
                .OrderByDescending(c => prioritized?.Contains(c.Key) == true ? 1000 : 0)
                .ThenByDescending(c => c.Value)
                .Take(10)
                .Select(c => c.Key)
                .ToList();

            return sorted;
        }

        private List<string> ExtractUnresolvedPlots(List<NarrativeEvent> events)
        {
            // Simplified - in production would track quest states
            return events
                .Where(e => e.EventType.Contains("quest_start") || e.EventType.Contains("mystery"))
                .Where(e => !events.Any(r => r.EventType.Contains("quest_complete") && r.QuestId == e.QuestId))
                .Take(5)
                .Select(e => e.Description)
                .ToList();
        }

        private EmotionalTone DetermineDominantTone(List<NarrativeEvent> events)
        {
            if (!events.Any()) return EmotionalTone.Neutral;

            var toneCounts = events
                .GroupBy(e => e.Tone)
                .OrderByDescending(g => g.Sum(e => e.Significance))
                .FirstOrDefault();

            return toneCounts?.Key ?? EmotionalTone.Neutral;
        }

        private List<string> ExtractConsequences(List<NarrativeEvent> events)
        {
            return events
                .Where(e => e.EventType.Contains("consequence") || e.EventType.Contains("result"))
                .Take(5)
                .Select(e => e.Description)
                .ToList();
        }

        private string BuildSummaryPrompt(List<NarrativeEvent> events, int maxTokens)
        {
            var eventDescriptions = string.Join("\n", events.Take(20).Select(e => $"- {e.Description}"));
            return $"Summarize these story events concisely in about {maxTokens/4} words:\n{eventDescriptions}";
        }

        private string BuildEmotionalSummaryPrompt(List<NarrativeEvent> events)
        {
            var eventDescriptions = string.Join("\n", events.Take(15).Select(e => $"[{e.Tone}] {e.Description}"));
            return $"Create an emotional summary that captures the feelings and atmosphere of these events:\n{eventDescriptions}";
        }

        private string BuildLocalSummary(List<NarrativeEvent> events)
        {
            if (!events.Any()) return "The story awaits.";

            var sb = new StringBuilder();
            var significant = events.Take(5).ToList();

            foreach (var evt in significant)
            {
                sb.Append(evt.Description);
                if (!evt.Description.EndsWith('.')) sb.Append('.');
                sb.Append(' ');
            }

            return sb.ToString().Trim();
        }

        private string BuildLocalEmotionalSummary(List<NarrativeEvent> events, EmotionalTone tone)
        {
            var toneDescriptor = tone switch
            {
                EmotionalTone.Victory => "with triumph and glory",
                EmotionalTone.Sadness => "filled with sorrow and loss",
                EmotionalTone.Tense => "amid growing tension",
                EmotionalTone.Positive => "with growing hope",
                EmotionalTone.Negative => "under a cloud of melancholy",
                EmotionalTone.Curiosity => "shrouded in mystery",
                EmotionalTone.Joy => "in peaceful times",
                _ => ""
            };

            return $"The journey continues {toneDescriptor}...";
        }

        private string BuildPlayerAlignedLocalSummary(List<NarrativeEvent> events)
        {
            var choices = events.Take(3).Select(e => e.Description);
            return $"Your choices have shaped the world: {string.Join("; ", choices)}";
        }

        private string BuildLocalArcSummary(StoryArc arc, List<NarrativeEvent> events)
        {
            return $"The {arc.ArcName} began and progressed through {events.Count} events, " +
                   $"currently {arc.Status.ToString().ToLower()}.";
        }

        private string GenerateCacheKey(NarrativeInput input)
        {
            var eventIds = string.Join(",", input.Events.Take(10).Select(e => e.Id));
            return $"{input.PlayerId}:{eventIds}:{input.MaxOutputTokens}".GetHashCode().ToString();
        }

        private int EstimateTokens(int charCount)
        {
            // Rough estimate: ~4 chars per token
            return charCount / 4;
        }
    }
}
