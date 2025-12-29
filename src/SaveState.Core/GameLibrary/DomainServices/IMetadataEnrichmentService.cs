using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.DomainServices;

public interface IMetadataEnrichmentService
{
    Task EnrichGameMetadataAsync(Game game, CancellationToken ct = default);
    Task<string?> GetCoverImageUrlAsync(Game game, CancellationToken ct = default);
    Task<IEnumerable<string>> GetTagsAsync(Game game, CancellationToken ct = default);
    Task<string?> GetDescriptionAsync(Game game, CancellationToken ct = default);
    Task<Platform?> DetectPlatformAsync(string gamePath, CancellationToken ct = default);
}
