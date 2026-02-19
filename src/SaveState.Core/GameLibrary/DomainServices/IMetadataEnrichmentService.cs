using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.DomainServices;

public interface IMetadataEnrichmentService
{
    Task EnrichGameMetadataAsync(Game game, CancellationToken ct = default);
    Task<Result<string?>> GetCoverImageUrlAsync(Game game, CancellationToken ct = default);
    Task<Result<IEnumerable<string>>> GetTagsAsync(Game game, CancellationToken ct = default);
    Task<Result<string?>> GetDescriptionAsync(Game game, CancellationToken ct = default);
    Task<Result<Platform>> DetectPlatformAsync(string gamePath, CancellationToken ct = default);
}
