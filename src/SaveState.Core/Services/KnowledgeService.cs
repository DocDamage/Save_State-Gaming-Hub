using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SaveState.Core.Data;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;

namespace SaveState.Core.Services;

/// <summary>
/// Enhanced Knowledge Service for RAG with:
/// - Hybrid search (keyword + semantic)
/// - Auto-learning from successful cheat sessions
/// - Query reranking for better relevance
/// - Overlapping chunks for better context
/// </summary>
public class KnowledgeService : IKnowledgeService
{
    private readonly SaveStateDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStore;
    private readonly ILogger _logger = Log.ForContext<KnowledgeService>();
    
    // Auto-learning cache
    private readonly List<LearnedCheat> _learnedCheats = new();
    private const int MaxLearnedCheats = 100;

    public KnowledgeService(
        SaveStateDbContext db,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStore)
    {
        _db = db;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    public async Task<KnowledgeEntry> AddKnowledgeAsync(string content, string category, Dictionary<string, string>? metadata = null)
    {
        float[]? embedding = null;
        
        if (_embeddingService.IsConfigured)
        {
            embedding = await _embeddingService.GetEmbeddingAsync(content);
        }

        var entry = new KnowledgeEntry
        {
            Id = Guid.NewGuid(),
            Content = content,
            Category = category,
            Embedding = embedding != null ? EmbeddingService.SerializeEmbedding(embedding) : null,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.KnowledgeEntries.Add(entry);
        await _db.SaveChangesAsync();

        _logger.Information("Added knowledge entry {Id} in category {Category}", entry.Id, category);
        return entry;
    }

    /// <summary>
    /// Auto-learn from a successful cheat session
    /// </summary>
    public async Task LearnFromCheatAsync(string gameName, string cheatName, long address, string valueType, string description)
    {
        var content = $"Game: {gameName}\nCheat: {cheatName}\nAddress: 0x{address:X}\nType: {valueType}\n{description}";
        
        await AddKnowledgeAsync(content, KnowledgeCategories.CheatGuides, new Dictionary<string, string>
        {
            ["game"] = gameName,
            ["cheat_name"] = cheatName,
            ["address"] = address.ToString("X"),
            ["value_type"] = valueType,
            ["learned_at"] = DateTime.UtcNow.ToString("O"),
            ["source"] = "auto_learned"
        });

        _learnedCheats.Add(new LearnedCheat
        {
            GameName = gameName,
            CheatName = cheatName,
            Address = address,
            ValueType = valueType,
            LearnedAt = DateTime.UtcNow
        });

        if (_learnedCheats.Count > MaxLearnedCheats)
            _learnedCheats.RemoveAt(0);

        _logger.Information("Auto-learned cheat: {Game} - {Cheat}", gameName, cheatName);
    }

    public async Task<List<KnowledgeEntry>> GetAllAsync(string? category = null)
    {
        var query = _db.KnowledgeEntries.AsQueryable();
        
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(k => k.Category == category);
        }

        return await query.OrderByDescending(k => k.CreatedAt).ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entry = await _db.KnowledgeEntries.FindAsync(id);
        if (entry != null)
        {
            _db.KnowledgeEntries.Remove(entry);
            await _db.SaveChangesAsync();
            _logger.Information("Deleted knowledge entry {Id}", id);
        }
    }

    /// <summary>
    /// Hybrid search: combines keyword matching + semantic search for best results
    /// </summary>
    public async Task<string> GetRelevantContextAsync(string query, int maxTokens = 2000)
    {
        var results = new List<SearchResult>();

        // Phase 1: Keyword search (fast, exact matches)
        var keywordResults = await KeywordSearchAsync(query, limit: 10);
        results.AddRange(keywordResults);

        // Phase 2: Semantic search (slower, but finds related concepts)
        if (_embeddingService.IsConfigured)
        {
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);
            var semanticResults = await _vectorStore.SearchAsync(queryEmbedding, topK: 10);
            
            foreach (var sr in semanticResults)
            {
                var entryId = Guid.TryParse(sr.Id, out var parsed) ? parsed : Guid.Empty;
                
                // Avoid duplicates
                if (!results.Any(r => r.EntryId == entryId))
                {
                    results.Add(new SearchResult
                    {
                        EntryId = entryId,
                        Content = sr.Content,
                        Category = sr.Category,
                        SimilarityScore = sr.SimilarityScore
                    });
                }
                else
                {
                    // Boost score for entries found by both methods
                    var existing = results.First(r => r.EntryId == entryId);
                    existing.SimilarityScore = Math.Min(1.0, existing.SimilarityScore + 0.2);
                }
            }
        }

        // Phase 3: Rerank by relevance
        results = RerankResults(results, query);

        if (results.Count == 0)
        {
            return "";
        }

        // Build context string
        var sb = new StringBuilder();
        int charLimit = maxTokens * 4;
        int currentChars = 0;

        foreach (var result in results.Where(r => r.SimilarityScore > 0.3).Take(5))
        {
            var section = $"[{result.Category}] (relevance: {result.SimilarityScore:P0})\n{result.Content}\n\n";
            
            if (currentChars + section.Length > charLimit)
            {
                var remaining = charLimit - currentChars;
                if (remaining > 100)
                {
                    sb.Append(section.Substring(0, remaining));
                    sb.Append("...[truncated]");
                }
                break;
            }

            sb.Append(section);
            currentChars += section.Length;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Keyword-based search using LIKE queries
    /// </summary>
    private async Task<List<SearchResult>> KeywordSearchAsync(string query, int limit = 10)
    {
        var results = new List<SearchResult>();
        
        // Extract keywords (ignore common words)
        var keywords = ExtractKeywords(query);
        
        if (keywords.Count == 0)
            return results;

        // Build search query
        var entries = await _db.KnowledgeEntries
            .Where(k => keywords.Any(kw => k.Content.ToLower().Contains(kw.ToLower())))
            .Take(limit)
            .ToListAsync();

        foreach (var entry in entries)
        {
            // Calculate keyword match score
            var matchCount = keywords.Count(kw => 
                entry.Content.Contains(kw, StringComparison.OrdinalIgnoreCase));
            var score = (double)matchCount / keywords.Count;

            results.Add(new SearchResult
            {
                EntryId = entry.Id,
                Content = entry.Content,
                Category = entry.Category,
                SimilarityScore = score,
                Metadata = entry.Metadata
            });
        }

        return results.OrderByDescending(r => r.SimilarityScore).ToList();
    }

    /// <summary>
    /// Rerank results based on query-specific relevance
    /// </summary>
    private List<SearchResult> RerankResults(List<SearchResult> results, string query)
    {
        var queryLower = query.ToLower();
        
        foreach (var result in results)
        {
            var boost = 0.0;
            var contentLower = result.Content.ToLower();
            
            // Boost if content mentions the game name in query
            var gameNameMatch = Regex.Match(queryLower, @"(for|in|about)\s+([a-z0-9\s]+)", RegexOptions.IgnoreCase);
            if (gameNameMatch.Success)
            {
                var gameName = gameNameMatch.Groups[2].Value.Trim();
                if (contentLower.Contains(gameName))
                {
                    boost += 0.3;
                }
            }

            // Boost if query asks about specific cheat type mentioned in content
            var cheatTypes = new[] { "health", "money", "ammo", "god mode", "infinite", "unlimited", "max" };
            foreach (var cheatType in cheatTypes)
            {
                if (queryLower.Contains(cheatType) && contentLower.Contains(cheatType))
                {
                    boost += 0.2;
                    break;
                }
            }

            // Boost recent entries slightly
            if (result.Metadata != null)
            {
                try
                {
                    var meta = JsonSerializer.Deserialize<Dictionary<string, string>>(result.Metadata);
                    if (meta != null && meta.ContainsKey("learned_at"))
                    {
                        if (DateTime.TryParse(meta["learned_at"], out var learnedAt))
                        {
                            if ((DateTime.UtcNow - learnedAt).TotalDays < 7)
                            {
                                boost += 0.1;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to parse metadata during reranking");
                }
            }

            result.SimilarityScore = Math.Min(1.0, result.SimilarityScore + boost);
        }

        return results.OrderByDescending(r => r.SimilarityScore).ToList();
    }

    /// <summary>
    /// Extract meaningful keywords from query
    /// </summary>
    private static List<string> ExtractKeywords(string query)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could",
            "should", "may", "might", "must", "can", "to", "of", "in", "for",
            "on", "with", "at", "by", "from", "as", "into", "through", "during",
            "before", "after", "above", "below", "between", "under", "again",
            "further", "then", "once", "here", "there", "when", "where", "why",
            "how", "all", "each", "few", "more", "most", "other", "some", "such",
            "no", "nor", "not", "only", "own", "same", "so", "than", "too", "very",
            "just", "and", "but", "if", "or", "because", "until", "while", "this",
            "that", "what", "which", "who", "whom", "these", "those", "i", "me",
            "my", "myself", "we", "our", "ours", "you", "your", "find", "get"
        };

        var words = Regex.Split(query.ToLower(), @"\W+")
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .Distinct()
            .ToList();

        return words;
    }

    public async Task<int> ImportFromFileAsync(string filePath, string category)
    {
        if (!File.Exists(filePath))
        {
            _logger.Warning("File not found: {Path}", filePath);
            return 0;
        }

        var content = await File.ReadAllTextAsync(filePath);
        
        // Split with overlap for better context
        var chunks = ChunkTextWithOverlap(content, maxChars: 1500, overlap: 200);
        int importCount = 0;
        
        foreach (var chunk in chunks)
        {
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                await AddKnowledgeAsync(chunk, category, new Dictionary<string, string>
                {
                    ["source"] = Path.GetFileName(filePath),
                    ["imported_at"] = DateTime.UtcNow.ToString("O")
                });
                importCount++;
            }
        }

        _logger.Information("Imported {Count} chunks from {File}", importCount, filePath);
        return importCount;
    }

    public async Task RebuildIndexAsync()
    {
        if (!_embeddingService.IsConfigured)
        {
            _logger.Warning("Cannot rebuild index: embedding service not configured");
            return;
        }

        var entries = await _db.KnowledgeEntries.ToListAsync();
        _logger.Information("Rebuilding embeddings for {Count} entries", entries.Count);

        foreach (var entry in entries)
        {
            var embedding = await _embeddingService.GetEmbeddingAsync(entry.Content);
            entry.Embedding = EmbeddingService.SerializeEmbedding(embedding);
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.Information("Rebuilt all embeddings");
    }

    /// <summary>
    /// Split text into overlapping chunks for better context preservation
    /// </summary>
    private static List<string> ChunkTextWithOverlap(string text, int maxChars = 1500, int overlap = 200)
    {
        var chunks = new List<string>();
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
        
        var currentChunk = new StringBuilder();
        var overlapBuffer = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > maxChars && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                
                // Start new chunk with overlap from end of previous
                currentChunk.Clear();
                if (overlapBuffer.Length > 0)
                {
                    currentChunk.Append(overlapBuffer);
                    currentChunk.Append(" ");
                }
            }
            
            currentChunk.Append(sentence);
            currentChunk.Append(" ");
            
            // Update overlap buffer (keep last ~overlap chars)
            if (currentChunk.Length > overlap)
            {
                var text2 = currentChunk.ToString();
                var startIndex = Math.Max(0, text2.Length - overlap);
                overlapBuffer.Clear();
                overlapBuffer.Append(text2.Substring(startIndex));
            }
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    private record LearnedCheat
    {
        public string GameName { get; init; } = "";
        public string CheatName { get; init; } = "";
        public long Address { get; init; }
        public string ValueType { get; init; } = "";
        public DateTime LearnedAt { get; init; }
    }

    /// <summary>
    /// Extended search result for hybrid keyword+semantic search
    /// </summary>
    private class SearchResult
    {
        public Guid EntryId { get; init; }
        public string Content { get; init; } = "";
        public string Category { get; init; } = "";
        public double SimilarityScore { get; set; }
        public string? Metadata { get; init; }
    }
}

/// <summary>
/// Knowledge category constants
/// </summary>
public static class KnowledgeCategories
{
    public const string GameTips = "game_tips";
    public const string CheatGuides = "cheat_guides";
    public const string CheatAddresses = "cheat_addresses";
    public const string UserNotes = "user_notes";
    public const string SystemDocs = "system_docs";
    public const string PointerPaths = "pointer_paths";
}
