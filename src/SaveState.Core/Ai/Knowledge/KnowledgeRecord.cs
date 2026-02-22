using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Ai.Knowledge;

public class KnowledgeRecord : EntityBase
{
    public new string Id { get; private set; } = string.Empty;
    public float[] Embedding { get; private set; } = Array.Empty<float>();
    public string Content { get; private set; } = string.Empty;
    public string? Metadata { get; private set; }
    public DateTime IndexedAt { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }
    public int AccessCount { get; private set; }
    public float RelevanceScore { get; private set; } = 1.0f;

    protected KnowledgeRecord() { } // EF Core

    public KnowledgeRecord(string id, float[] embedding, string content, string? metadata = null)
    {
        Id = Guard.Against.NullOrWhiteSpace(id, nameof(id));
        Embedding = Guard.Against.Null(embedding, nameof(embedding));
        Content = Guard.Against.NullOrWhiteSpace(content, nameof(content));
        Metadata = metadata;
        RelevanceScore = 1.0f;
    }

    public static KnowledgeRecord Create(string id, float[] embedding, string content, ITimeProvider timeProvider, string? metadata = null)
    {
        return new KnowledgeRecord(id, embedding, content, metadata)
        {
            IndexedAt = timeProvider.UtcNow
        };
    }

    public void UpdateEmbedding(float[] newEmbedding)
    {
        Embedding = Guard.Against.Null(newEmbedding, nameof(newEmbedding));
    }

    public void UpdateContent(string newContent)
    {
        Content = Guard.Against.NullOrWhiteSpace(newContent, nameof(newContent));
    }

    public void UpdateMetadata(string? newMetadata)
    {
        Metadata = newMetadata;
    }

    public void RecordAccess(ITimeProvider timeProvider)
    {
        LastAccessedAt = timeProvider.UtcNow;
        AccessCount++;
    }

    public void UpdateRelevanceScore(float score)
    {
        RelevanceScore = Guard.Against.OutOfRange(score, nameof(score), 0f, 2f);
    }
}
