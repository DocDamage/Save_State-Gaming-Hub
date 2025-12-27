using System.Numerics;
using Microsoft.EntityFrameworkCore;
using SaveState.Core.Data;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;

namespace SaveState.Core.Services;

/// <summary>
/// Vector store using SQLite with SIMD-accelerated cosine similarity
/// </summary>
public class VectorStoreService : IVectorStoreService
{
    private readonly SaveStateDbContext _db;
    private readonly ILogger _logger = Log.ForContext<VectorStoreService>();

    public VectorStoreService(SaveStateDbContext db)
    {
        _db = db;
    }

    public async Task IndexDocumentAsync(string id, string content, string category, float[] embedding)
    {
        var guidId = Guid.TryParse(id, out var parsed) ? parsed : Guid.NewGuid();
        
        var existing = await _db.KnowledgeEntries.FindAsync(guidId);
        if (existing != null)
        {
            existing.Content = content;
            existing.Category = category;
            existing.Embedding = EmbeddingService.SerializeEmbedding(embedding);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.KnowledgeEntries.Add(new KnowledgeEntry
            {
                Id = guidId,
                Content = content,
                Category = category,
                Embedding = EmbeddingService.SerializeEmbedding(embedding),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        _logger.Debug("Indexed document {Id} in category {Category}", id, category);
    }

    public async Task<List<RetrievalResult>> SearchAsync(float[] queryEmbedding, int topK = 5, string? category = null)
    {
        // Load all documents (for small knowledge bases, this is efficient)
        // For large bases, consider chunking or approximate nearest neighbor
        var query = _db.KnowledgeEntries.AsQueryable();
        
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(k => k.Category == category);
        }

        var documents = await query.ToListAsync();
        
        if (documents.Count == 0)
        {
            return new List<RetrievalResult>();
        }

        // Calculate cosine similarity for each document
        var scored = new List<(KnowledgeEntry Doc, double Score)>();
        
        foreach (var doc in documents)
        {
            if (doc.Embedding.Length == 0) continue;
            
            var docEmbedding = EmbeddingService.DeserializeEmbedding(doc.Embedding);
            var similarity = CosineSimilarity(queryEmbedding, docEmbedding);
            scored.Add((doc, similarity));
        }

        // Sort by similarity and take top K
        return scored
            .OrderByDescending(s => s.Score)
            .Take(topK)
            .Select(s => new RetrievalResult
            {
                Id = s.Doc.Id.ToString(),
                Content = s.Doc.Content,
                Category = s.Doc.Category,
                SimilarityScore = s.Score
            })
            .ToList();
    }

    public async Task DeleteAsync(string id)
    {
        if (Guid.TryParse(id, out var guidId))
        {
            var entry = await _db.KnowledgeEntries.FindAsync(guidId);
            if (entry != null)
            {
                _db.KnowledgeEntries.Remove(entry);
                await _db.SaveChangesAsync();
                _logger.Information("Deleted document {Id}", id);
            }
        }
    }

    public async Task<int> GetDocumentCountAsync()
    {
        return await _db.KnowledgeEntries.CountAsync();
    }

    /// <summary>
    /// SIMD-accelerated cosine similarity calculation
    /// </summary>
    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0;

        // Use SIMD for vectorized computation
        int simdLength = Vector<float>.Count;
        int i = 0;
        
        var dotProduct = Vector<float>.Zero;
        var normA = Vector<float>.Zero;
        var normB = Vector<float>.Zero;

        // Process SIMD-sized chunks
        for (; i <= a.Length - simdLength; i += simdLength)
        {
            var vecA = new Vector<float>(a, i);
            var vecB = new Vector<float>(b, i);
            
            dotProduct += vecA * vecB;
            normA += vecA * vecA;
            normB += vecB * vecB;
        }

        // Sum SIMD results
        float dot = 0, magA = 0, magB = 0;
        for (int j = 0; j < simdLength; j++)
        {
            dot += dotProduct[j];
            magA += normA[j];
            magB += normB[j];
        }

        // Process remaining elements
        for (; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        var magnitude = Math.Sqrt(magA) * Math.Sqrt(magB);
        return magnitude > 0 ? dot / magnitude : 0;
    }
}
