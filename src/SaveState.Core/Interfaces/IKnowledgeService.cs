using SaveState.Core.Entities;

namespace SaveState.Core.Interfaces;

/// <summary>
/// Service for managing the knowledge base for RAG
/// </summary>
public interface IKnowledgeService
{
    /// <summary>
    /// Add new knowledge entry and index it
    /// </summary>
    Task<KnowledgeEntry> AddKnowledgeAsync(string content, string category, Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Get all knowledge entries, optionally filtered by category
    /// </summary>
    Task<List<KnowledgeEntry>> GetAllAsync(string? category = null);

    /// <summary>
    /// Delete a knowledge entry
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Get relevant context for a query using semantic search
    /// </summary>
    Task<string> GetRelevantContextAsync(string query, int maxTokens = 2000);

    /// <summary>
    /// Import knowledge from a text file, returns count of entries imported
    /// </summary>
    Task<int> ImportFromFileAsync(string filePath, string category);

    /// <summary>
    /// Rebuild all embeddings (useful after model changes)
    /// </summary>
    Task RebuildIndexAsync();
}
