using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Ai.Memory
{
    /// <summary>
    /// Mid-term memory for quests, decisions, player habits.
    /// - Vector embeddings for semantic search (simulated with TF-IDF)
    /// - Episode structure: Event → Context → Outcome → Emotion
    /// - Time-decay scoring for relevance
    /// - JSON storage with semantic indexing
    /// </summary>
    public enum EmotionalTone
    {
        Neutral,
        Positive,
        Negative,
        Tense,
        Victory,
        Defeat,
        Curiosity,
        Fear,
        Joy,
        Sadness
    }

    public class Episode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Event { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public EmotionalTone Emotion { get; set; } = EmotionalTone.Neutral;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public float Significance { get; set; } = 0.5f;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> WorldState { get; set; } = new();
        public string? GameId { get; set; }
        public string? QuestId { get; set; }
        public string? CharacterId { get; set; }
        
        // Computed embedding (TF-IDF vector simplified as term frequencies)
        public Dictionary<string, float> Embedding { get; set; } = new();
        
        // Access tracking
        public int AccessCount { get; set; } = 0;
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    }

    public class EpisodicMemoryConfig
    {
        public string StoragePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveState", "Memory", "episodic");
        public int MaxEpisodes { get; set; } = 1000;
        public double DecayHalfLifeHours { get; set; } = 168; // 1 week
        public float MinSignificance { get; set; } = 0.1f;
        public bool AutoPersist { get; set; } = true;
    }

    public interface IEpisodicMemory
    {
        Task<Episode> RecordEpisode(string eventDesc, string context, string outcome, 
            EmotionalTone emotion = EmotionalTone.Neutral, float significance = 0.5f,
            List<string>? tags = null, Dictionary<string, object>? worldState = null);
        Task<IEnumerable<Episode>> SemanticSearch(string query, int maxResults = 10);
        Task<IEnumerable<Episode>> GetByEmotion(EmotionalTone emotion, int maxResults = 10);
        Task<IEnumerable<Episode>> GetByQuest(string questId);
        Task<IEnumerable<Episode>> GetByCharacter(string characterId);
        Task<IEnumerable<Episode>> GetRecent(int count);
        Task<Episode?> GetById(string id);
        Task<bool> PromoteFromShortTerm(MemoryEntry entry, EmotionalTone emotion, float significance);
        Task SaveAsync();
        Task LoadAsync();
        int Count { get; }
    }

    public class EpisodicMemory : IEpisodicMemory
    {
        private readonly ILogger _logger = Log.ForContext<EpisodicMemory>();
        private readonly List<Episode> _episodes = new();
        private readonly Dictionary<string, Episode> _episodeIndex = new();
        private readonly Dictionary<string, HashSet<string>> _tagIndex = new();
        private readonly Dictionary<string, float> _idfScores = new();
        private readonly EpisodicMemoryConfig _config;
        private bool _loaded = false;

        public int Count => _episodes.Count;

        public EpisodicMemory(EpisodicMemoryConfig? config = null)
        {
            _config = config ?? new EpisodicMemoryConfig();
            Directory.CreateDirectory(_config.StoragePath);
        }

        public async Task<Episode> RecordEpisode(string eventDesc, string context, string outcome,
            EmotionalTone emotion = EmotionalTone.Neutral, float significance = 0.5f,
            List<string>? tags = null, Dictionary<string, object>? worldState = null)
        {
            if (!_loaded) await LoadAsync();

            var episode = new Episode
            {
                Event = eventDesc,
                Context = context,
                Outcome = outcome,
                Emotion = emotion,
                Significance = significance,
                Tags = tags ?? new List<string>(),
                WorldState = worldState ?? new Dictionary<string, object>()
            };

            // Compute embedding
            episode.Embedding = ComputeEmbedding($"{eventDesc} {context} {outcome}");

            // Add to collections
            _episodes.Add(episode);
            _episodeIndex[episode.Id] = episode;

            // Index by tags
            foreach (var tag in episode.Tags)
            {
                if (!_tagIndex.ContainsKey(tag))
                {
                    _tagIndex[tag] = new HashSet<string>();
                }
                _tagIndex[tag].Add(episode.Id);
            }

            // Update IDF scores
            UpdateIdfScores();

            // Prune if necessary
            await PruneIfNeeded();

            // Auto-persist
            if (_config.AutoPersist)
            {
                await SaveAsync();
            }

            return episode;
        }

        public async Task<IEnumerable<Episode>> SemanticSearch(string query, int maxResults = 10)
        {
            if (!_loaded) await LoadAsync();

            var queryEmbedding = ComputeEmbedding(query);

            return _episodes
                .Select(ep => new
                {
                    Episode = ep,
                    Similarity = CosineSimilarity(queryEmbedding, ep.Embedding),
                    DecayedSignificance = ApplyTimeDecay(ep)
                })
                .OrderByDescending(x => x.Similarity * x.DecayedSignificance)
                .Take(maxResults)
                .Select(x =>
                {
                    x.Episode.AccessCount++;
                    x.Episode.LastAccessed = DateTime.UtcNow;
                    return x.Episode;
                });
        }

        public async Task<IEnumerable<Episode>> GetByEmotion(EmotionalTone emotion, int maxResults = 10)
        {
            if (!_loaded) await LoadAsync();

            return _episodes
                .Where(ep => ep.Emotion == emotion)
                .OrderByDescending(ep => ApplyTimeDecay(ep))
                .Take(maxResults);
        }

        public async Task<IEnumerable<Episode>> GetByQuest(string questId)
        {
            if (!_loaded) await LoadAsync();

            return _episodes
                .Where(ep => ep.QuestId == questId)
                .OrderBy(ep => ep.Timestamp);
        }

        public async Task<IEnumerable<Episode>> GetByCharacter(string characterId)
        {
            if (!_loaded) await LoadAsync();

            return _episodes
                .Where(ep => ep.CharacterId == characterId)
                .OrderByDescending(ep => ep.Timestamp);
        }

        public async Task<IEnumerable<Episode>> GetRecent(int count)
        {
            if (!_loaded) await LoadAsync();

            return _episodes
                .OrderByDescending(ep => ep.Timestamp)
                .Take(count);
        }

        public async Task<Episode?> GetById(string id)
        {
            if (!_loaded) await LoadAsync();

            return _episodeIndex.TryGetValue(id, out var episode) ? episode : null;
        }

        public async Task<bool> PromoteFromShortTerm(MemoryEntry entry, EmotionalTone emotion, float significance)
        {
            try
            {
                await RecordEpisode(
                    eventDesc: entry.Input,
                    context: entry.Context,
                    outcome: entry.Output,
                    emotion: emotion,
                    significance: significance,
                    tags: entry.Metadata.ContainsKey("tags") 
                        ? (entry.Metadata["tags"] as List<string>) ?? new List<string>()
                        : new List<string>(),
                    worldState: entry.Metadata
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SaveAsync()
        {
            var filePath = Path.Combine(_config.StoragePath, "episodes.json");
            var json = JsonSerializer.Serialize(_episodes, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task LoadAsync()
        {
            if (_loaded) return;

            var filePath = Path.Combine(_config.StoragePath, "episodes.json");
            if (File.Exists(filePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var episodes = JsonSerializer.Deserialize<List<Episode>>(json);
                    if (episodes != null)
                    {
                        _episodes.Clear();
                        _episodeIndex.Clear();
                        _tagIndex.Clear();

                        foreach (var episode in episodes)
                        {
                            _episodes.Add(episode);
                            _episodeIndex[episode.Id] = episode;
                            
                            foreach (var tag in episode.Tags)
                            {
                                if (!_tagIndex.ContainsKey(tag))
                                {
                                    _tagIndex[tag] = new HashSet<string>();
                                }
                                _tagIndex[tag].Add(episode.Id);
                            }
                        }

                        UpdateIdfScores();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Error loading episodic memory");
                }
            }

            _loaded = true;
        }

        private Dictionary<string, float> ComputeEmbedding(string text)
        {
            var embedding = new Dictionary<string, float>();
            var words = Tokenize(text);
            var wordCount = words.Count;

            // Term frequency
            foreach (var word in words)
            {
                if (!embedding.ContainsKey(word))
                {
                    embedding[word] = 0;
                }
                embedding[word] += 1.0f / wordCount;
            }

            // Apply IDF weights if available
            foreach (var term in embedding.Keys.ToList())
            {
                if (_idfScores.TryGetValue(term, out var idf))
                {
                    embedding[term] *= idf;
                }
            }

            return embedding;
        }

        private List<string> Tokenize(string text)
        {
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();
        }

        private void UpdateIdfScores()
        {
            var documentFrequency = new Dictionary<string, int>();
            var totalDocs = _episodes.Count;

            foreach (var episode in _episodes)
            {
                var terms = episode.Embedding.Keys.ToHashSet();
                foreach (var term in terms)
                {
                    if (!documentFrequency.ContainsKey(term))
                    {
                        documentFrequency[term] = 0;
                    }
                    documentFrequency[term]++;
                }
            }

            _idfScores.Clear();
            foreach (var kvp in documentFrequency)
            {
                _idfScores[kvp.Key] = (float)Math.Log((totalDocs + 1.0) / (kvp.Value + 1.0)) + 1.0f;
            }
        }

        private float CosineSimilarity(Dictionary<string, float> a, Dictionary<string, float> b)
        {
            var allTerms = a.Keys.Union(b.Keys);
            float dotProduct = 0, normA = 0, normB = 0;

            foreach (var term in allTerms)
            {
                var valA = a.TryGetValue(term, out var va) ? va : 0;
                var valB = b.TryGetValue(term, out var vb) ? vb : 0;

                dotProduct += valA * valB;
                normA += valA * valA;
                normB += valB * valB;
            }

            if (normA == 0 || normB == 0) return 0;
            return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private float ApplyTimeDecay(Episode episode)
        {
            var hoursSinceCreation = (DateTime.UtcNow - episode.Timestamp).TotalHours;
            var decayFactor = Math.Pow(0.5, hoursSinceCreation / _config.DecayHalfLifeHours);
            
            // Access recency bonus
            var hoursSinceAccess = (DateTime.UtcNow - episode.LastAccessed).TotalHours;
            var accessBonus = Math.Pow(0.5, hoursSinceAccess / (_config.DecayHalfLifeHours / 2));
            
            // Access frequency bonus
            var frequencyBonus = Math.Min(1.0, episode.AccessCount / 10.0);

            return episode.Significance * (float)(decayFactor * 0.6 + accessBonus * 0.3 + frequencyBonus * 0.1);
        }

        private async Task PruneIfNeeded()
        {
            if (_episodes.Count <= _config.MaxEpisodes) return;

            // Remove episodes below minimum significance after decay
            var toRemove = _episodes
                .Where(ep => ApplyTimeDecay(ep) < _config.MinSignificance)
                .OrderBy(ep => ApplyTimeDecay(ep))
                .Take(_episodes.Count - _config.MaxEpisodes)
                .ToList();

            foreach (var episode in toRemove)
            {
                _episodes.Remove(episode);
                _episodeIndex.Remove(episode.Id);
                
                foreach (var tag in episode.Tags)
                {
                    if (_tagIndex.TryGetValue(tag, out var tagSet))
                    {
                        tagSet.Remove(episode.Id);
                    }
                }
            }

            if (_config.AutoPersist)
            {
                await SaveAsync();
            }
        }

        /// <summary>
        /// Build narrative summary from episodes
        /// </summary>
        public string BuildNarrativeSummary(int maxEpisodes = 10)
        {
            var recentSignificant = _episodes
                .OrderByDescending(ep => ApplyTimeDecay(ep))
                .Take(maxEpisodes);

            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== Recent Memory Summary ===");

            foreach (var ep in recentSignificant)
            {
                summary.AppendLine($"• [{ep.Emotion}] {ep.Event}");
                summary.AppendLine($"  Outcome: {ep.Outcome}");
            }

            return summary.ToString();
        }
    }
}
