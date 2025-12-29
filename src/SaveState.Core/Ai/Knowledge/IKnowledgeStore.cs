namespace SaveState.Core.Ai.Knowledge;

public interface IKnowledgeStore
{
    Task UpsertAsync(string id, float[] embedding, string content, object metadata, CancellationToken ct);
    Task<IReadOnlyList<KnowledgeHit>> SearchAsync(float[] queryEmbedding, int limit, float minRelevance, CancellationToken ct);

    // Feedback and learning methods
    Task BoostAsync(string id, float relevanceMultiplier, CancellationToken ct);
    Task FlagAsync(string id, CancellationToken ct);
    Task PruneLowQualityAsync(float relevanceThreshold, CancellationToken ct);
}

public record KnowledgeHit(string Id, string Content, object Metadata, float Relevance);
