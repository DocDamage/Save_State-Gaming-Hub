using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveState.Core.Data;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;

namespace SaveState.Core.Services;

/// <summary>
/// Knowledge base management service for RAG
/// </summary>
public class KnowledgeService : IKnowledgeService
{
    private readonly SaveStateDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStore;
    private readonly ILogger _logger = Log.ForContext<KnowledgeService>();

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
        // Generate embedding
        var embedding = await _embeddingService.GetEmbeddingAsync(content);

        var entry = new KnowledgeEntry
        {
            Id = Guid.NewGuid(),
            Content = content,
            Category = category,
            Embedding = EmbeddingService.SerializeEmbedding(embedding),
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.KnowledgeEntries.Add(entry);
        await _db.SaveChangesAsync();

        _logger.Information("Added knowledge entry {Id} in category {Category}", entry.Id, category);
        return entry;
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

    public async Task<string> GetRelevantContextAsync(string query, int maxTokens = 2000)
    {
        if (!_embeddingService.IsConfigured)
        {
            _logger.Warning("Embedding service not configured, skipping RAG retrieval");
            return "";
        }

        // Get query embedding
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);

        // Search for relevant documents
        var results = await _vectorStore.SearchAsync(queryEmbedding, topK: 5);

        if (results.Count == 0)
        {
            return "";
        }

        // Build context string, respecting token limit (approximate: 4 chars per token)
        var sb = new StringBuilder();
        int charLimit = maxTokens * 4;
        int currentChars = 0;

        foreach (var result in results.Where(r => r.SimilarityScore > 0.5)) // Only include relevant results
        {
            var section = $"[{result.Category}] (relevance: {result.SimilarityScore:P0})\n{result.Content}\n\n";
            
            if (currentChars + section.Length > charLimit)
            {
                // Truncate if needed
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

    public async Task<int> ImportFromFileAsync(string filePath, string category)
    {
        if (!File.Exists(filePath))
        {
            _logger.Warning("File not found: {Path}", filePath);
            return 0;
        }

        var content = await File.ReadAllTextAsync(filePath);
        
        // Split into chunks for large files (roughly 500 tokens per chunk)
        var chunks = ChunkText(content, maxChars: 2000);
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
    /// Split text into chunks for processing
    /// </summary>
    private static List<string> ChunkText(string text, int maxChars = 2000)
    {
        var chunks = new List<string>();
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new StringBuilder();
        
        foreach (var para in paragraphs)
        {
            if (currentChunk.Length + para.Length > maxChars && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }
            
            currentChunk.AppendLine(para);
            currentChunk.AppendLine();
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}

/// <summary>
/// Knowledge category constants
/// </summary>
public static class KnowledgeCategories
{
    public const string GameTips = "game_tips";
    public const string CheatGuides = "cheat_guides";
    public const string UserNotes = "user_notes";
    public const string SystemDocs = "system_docs";
}
