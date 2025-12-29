namespace SaveState.Core.Ai.Memory;

public interface IShortTermMemory
{
    Task StoreAsync(MemoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryEntry>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);
    Task<MemoryEntry?> GetByIdAsync(string id, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
    int CurrentEntryCount { get; }
    long CurrentTokenCount { get; }
}
