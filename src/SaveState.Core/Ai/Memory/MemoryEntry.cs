namespace SaveState.Core.Ai.Memory;

public record MemoryEntry(
    string Id,
    string Content,
    DateTime Timestamp,
    IReadOnlyList<string> Contexts,
    int AccessCount = 0,
    DateTime? LastAccessed = null);

public record MemoryConfig
{
    public int MaxEntries { get; set; } = 500;
    public int MaxTokens { get; set; } = 50000;
    public int PruneBatchSize { get; set; } = 50;
}
