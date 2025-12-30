using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Ai.Memory;
using System.Collections.Concurrent;

namespace SaveState.Infrastructure.Ai.Memory;

public class EnhancedShortTermMemory : IShortTermMemory
{
    private readonly ConcurrentDictionary<string, MemoryEntry> _memories = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _keywordIndex = new();
    private readonly MemoryConfig _config;
    private readonly ILogger<EnhancedShortTermMemory> _logger;
    private long _totalTokens;

    public int CurrentEntryCount => _memories.Count;
    public long CurrentTokenCount => _totalTokens;

    public EnhancedShortTermMemory(
        IOptions<MemoryConfig> config,
        ILogger<EnhancedShortTermMemory> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task StoreAsync(MemoryEntry entry, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var estimatedTokens = EstimateTokenCount(entry.Content);

        if (_memories.Count >= _config.MaxEntries || _totalTokens + estimatedTokens > _config.MaxTokens)
        {
            await PruneAsync(ct).ConfigureAwait(false);
        }

        ct.ThrowIfCancellationRequested();

        if (_memories.Count >= _config.MaxEntries)
        {
            throw new MemoryCapacityExceededException(
                $"Cannot store: would exceed {_config.MaxEntries} entries");
        }

        _memories[entry.Id] = entry;
        Interlocked.Add(ref _totalTokens, estimatedTokens);
        UpdateKeywordIndex(entry);

        _logger.LogDebug("Stored memory {Id} ({Tokens} tokens)", entry.Id, estimatedTokens);
    }

    public Task<IReadOnlyList<MemoryEntry>> SearchAsync(string query, int maxResults, CancellationToken ct)
    {
        var keywords = ExtractKeywords(query);
        var candidates = new HashSet<string>();

        foreach (var keyword in keywords)
        {
            if (_keywordIndex.TryGetValue(keyword, out var ids))
                candidates.UnionWith(ids);
        }

        var results = candidates
            .Select(id => _memories.GetValueOrDefault(id))
            .Where(e => e is not null)
            .OrderByDescending(e => CalculateRelevance(e!, query, keywords))
            .Take(maxResults)
            .ToList();

        return Task.FromResult<IReadOnlyList<MemoryEntry>>(results!);
    }

    public Task<MemoryEntry?> GetByIdAsync(string id, CancellationToken ct)
        => Task.FromResult(_memories.GetValueOrDefault(id));

    public Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        if (_memories.TryRemove(id, out var entry))
        {
            var estimatedTokens = EstimateTokenCount(entry.Content);
            Interlocked.Add(ref _totalTokens, -estimatedTokens);
            RemoveFromKeywordIndex(entry);
            _logger.LogDebug("Removed memory {Id}", id);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task ClearAsync(CancellationToken ct)
    {
        _memories.Clear();
        _keywordIndex.Clear();
        Interlocked.Exchange(ref _totalTokens, 0);
        return Task.CompletedTask;
    }

    private Task PruneAsync(CancellationToken ct)
    {
        var toRemove = _memories
            .OrderBy(kvp => kvp.Value.LastAccessed ?? kvp.Value.Timestamp)
            .Take(_config.PruneBatchSize)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in toRemove)
        {
            if (_memories.TryRemove(id, out var entry))
            {
                Interlocked.Add(ref _totalTokens, -EstimateTokenCount(entry.Content));
            }
        }

        _logger.LogInformation("Pruned {Count} memory entries", toRemove.Count);
        return Task.CompletedTask;
    }

    private void UpdateKeywordIndex(MemoryEntry entry)
    {
        // Index content keywords
        foreach (var keyword in ExtractKeywords(entry.Content))
        {
            var ids = _keywordIndex.GetOrAdd(keyword, _ => new HashSet<string>());
            ids.Add(entry.Id);
        }

        // Index context/tag keywords
        foreach (var context in entry.Contexts)
        {
            var ids = _keywordIndex.GetOrAdd(context.ToLowerInvariant(), _ => new HashSet<string>());
            ids.Add(entry.Id);
        }
    }

    private void RemoveFromKeywordIndex(MemoryEntry entry)
    {
        // Remove content keywords
        foreach (var keyword in ExtractKeywords(entry.Content))
        {
            if (_keywordIndex.TryGetValue(keyword, out var ids))
            {
                ids.Remove(entry.Id);
                if (ids.Count == 0)
                {
                    _keywordIndex.TryRemove(keyword, out _);
                }
            }
        }

        // Remove context/tag keywords
        foreach (var context in entry.Contexts)
        {
            var keyword = context.ToLowerInvariant();
            if (_keywordIndex.TryGetValue(keyword, out var ids))
            {
                ids.Remove(entry.Id);
                if (ids.Count == 0)
                {
                    _keywordIndex.TryRemove(keyword, out _);
                }
            }
        }
    }

    private static IEnumerable<string> ExtractKeywords(string text)
    {
        var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for" };
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant().Trim())
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .Distinct();
    }

    private static float CalculateRelevance(MemoryEntry entry, string query, IEnumerable<string> keywords)
    {
        var entryKeywords = ExtractKeywords(entry.Content).ToHashSet();
        var matches = keywords.Count(k => entryKeywords.Contains(k));
        return matches / (float)Math.Max(keywords.Count(), 1);
    }

    private static int EstimateTokenCount(string text) => Math.Max(1, text.Length / 4);
}

public class MemoryCapacityExceededException : Exception
{
    public MemoryCapacityExceededException(string message) : base(message) { }
}
