using SaveState.Core.Models;

namespace SaveState.Core.Interfaces;

public interface IMetadataProvider
{
    string Id { get; }
    string Name { get; }
    
    Task<GameMetadata?> GetMetadataAsync(string title, string? platformHint = null);
    Task<string?> GetCoverImageAsync(string title, string? platformHint = null);
}
