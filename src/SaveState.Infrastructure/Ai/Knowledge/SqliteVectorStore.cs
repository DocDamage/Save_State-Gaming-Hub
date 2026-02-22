using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Knowledge;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Ai.Knowledge;

public class SqliteVectorStore : IKnowledgeStore
{
    private readonly SaveStateDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITaskRunner _taskRunner;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<SqliteVectorStore> _logger;

    public SqliteVectorStore(
        SaveStateDbContext context,
        IServiceScopeFactory scopeFactory,
        ITaskRunner taskRunner,
        ITimeProvider timeProvider,
        ILogger<SqliteVectorStore> logger)
    {
        _context = context;
        _scopeFactory = scopeFactory;
        _taskRunner = taskRunner;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task UpsertAsync(string id, float[] embedding, string content, object metadata, CancellationToken ct)
    {
        var record = await _context.KnowledgeRecords.FindAsync(new object[] { id }, ct).ConfigureAwait(false);

        if (record is null)
        {
            record = new KnowledgeRecord(id, embedding, content, metadata.ToString());
            _context.KnowledgeRecords.Add(record);
        }
        else
        {
            record.UpdateEmbedding(embedding);
            record.UpdateContent(content);
            record.UpdateMetadata(metadata.ToString());
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

    }

    public async Task<IReadOnlyList<KnowledgeHit>> SearchAsync(float[] queryEmbedding, int limit, float minRelevance, CancellationToken ct)
    {
        // Load all records for similarity computation (in a real system, you'd use a vector database)
        var allRecords = await _context.KnowledgeRecords.ToListAsync(ct).ConfigureAwait(false);

        var hits = allRecords
            .Select(record =>
            {
                var similarity = CosineSimilarity(queryEmbedding, record.Embedding);
                record.RecordAccess(_timeProvider);
                return new KnowledgeHit(record.Id, record.Content, record.Metadata, similarity);
            })
            .Where(hit => hit.Relevance >= minRelevance)
            .OrderByDescending(hit => hit.Relevance)
            .Take(limit)
            .ToList();

        // Update access counts asynchronously using centralized TaskRunner
        var hitIds = hits.Select(h => h.Id).ToList();
        _taskRunner.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();

            var recordsToUpdate = await dbContext.KnowledgeRecords
                .Where(r => hitIds.Contains(r.Id))
                .ToListAsync(CancellationToken.None)
                .ConfigureAwait(false);

            foreach (var record in recordsToUpdate)
            {
                record.RecordAccess(_timeProvider);
            }

            await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }, "UpdateKnowledgeAccessCounts");

        return hits;
    }

    private static float CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            return 0f;
        }

        float dotProduct = 0f;
        float normA = 0f;
        float normB = 0f;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        normA = MathF.Sqrt(normA);
        normB = MathF.Sqrt(normB);

        if (normA == 0f || normB == 0f)
        {
            return 0f;
        }

        return dotProduct / (normA * normB);
    }

    public async Task BoostAsync(string id, float relevanceMultiplier, CancellationToken ct)
    {
        var record = await _context.KnowledgeRecords.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (record is not null)
        {
            record.UpdateRelevanceScore(record.RelevanceScore * relevanceMultiplier);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task FlagAsync(string id, CancellationToken ct)
    {
        var record = await _context.KnowledgeRecords.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (record is not null)
        {
            // Mark as flagged by setting relevance to a very low value
            record.UpdateRelevanceScore(0.1f);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Flagged knowledge record {Id} for review", id);
        }
    }

    public async Task PruneLowQualityAsync(float relevanceThreshold, CancellationToken ct)
    {
        var lowQualityRecords = await _context.KnowledgeRecords
            .Where(r => r.RelevanceScore < relevanceThreshold)
            .ToListAsync(ct).ConfigureAwait(false);

        if (lowQualityRecords.Any())
        {
            _context.KnowledgeRecords.RemoveRange(lowQualityRecords);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Pruned {Count} low-quality knowledge records", lowQualityRecords.Count);
        }
        else
        {
        }
    }

    public async Task ClearAllAsync(CancellationToken ct)
    {
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM KnowledgeRecords", ct).ConfigureAwait(false);
        _logger.LogInformation("Knowledge base cleared.");
    }
}
